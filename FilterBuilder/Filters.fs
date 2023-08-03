module Filters

    open System
    open FilterBuilder.RpcClient
    open NBitcoin

    module Building =
        open Berlino
        open RpcClient

        let build (block : VerboseBlockInfo) =
            let isSupportedKeyType t =
                t = "witness_v0_keyhash" || t = "witness_v1_taproot"

            let relevantScripts outputs =
                outputs
                |> Seq.filter (fun o -> isSupportedKeyType o.PubKeyType)
                |> Seq.map (fun o -> o.ScriptPubKey)

            let spent =
                block.Transactions
                |> Seq.collect (fun t -> t.Inputs)
                |> Seq.map (fun i -> i.PrevOutput)

            let received =
                block.Transactions
                |> Seq.collect (fun t -> t.Outputs)

            let scripts = relevantScripts ( spent |> Seq.append received )
            GolombRiceFilterBuilder()
                .SetKey(block.Hash)
                .SetP(24)
                .SetM(1u <<< 24)
                .AddEntries(scripts).Build()

        let logError x = Console.WriteLine $"{x}"

        let taprootActivation (network : Network) =
            match network.Name with
            | "Main" -> {|
                Hash = uint256.Parse "0000000000000000000687bca986194dc2c1f949318629b44bb54ec0a94d8244"
                PrevBlockHash = uint256.Parse "000000000000000000013712fc242ee6dd28476d0e9c931c75f83e6974c6bccc"
                Height = 709632
                |}
            | "TestNet" -> {|
                Hash = uint256.Parse "00000000000000216dc4eb2bd27764891ec0c961b0da7562fe63678e164d62a0"
                PrevBlockHash = uint256.Parse "0000000000000001e17cc7358ee658affcb0a23146176581a7606a15f73993e3"
                Height = 2007000
                |}
            | "RegTest" -> {|
                Hash = Network.RegTest.GenesisHash
                PrevBlockHash = Network.RegTest.GetGenesis().Header.HashPrevBlock
                Height = 0
                |}
            | _ -> failwith $"Unknown network '{network.Name}'"


        let startBuilding rpcClient stopAt buildFilter = async {

            let fetchBlock blockHash =
                async {
                    let! block = getVerboseBlock blockHash rpcClient
                    let blockResult = Decode.verboseBlockFromString block.ResultString
                    return
                        blockResult
                        |> Result.map (fun verboseBlockInfo ->
                            buildFilter verboseBlockInfo
                            verboseBlockInfo.PrevBlockHash)
                }
                |> Async.CatchResult
                |> AsyncResult.mapError exnAsString
                |> AsyncResult.join

            let! bci = getBlockchainInfo rpcClient
            let tipBlock = bci.BestBlockHash

            do! Runner.forever (tipBlock, stopAt)
                    (fun (fromBlock, toBlock) -> async {
                        do! Runner.loopWhile fromBlock
                                (fun curBlock -> curBlock <> toBlock)
                                (fun curBlock -> async {
                                    match! fetchBlock curBlock with
                                    | Ok prevBlockHash ->
                                        return prevBlockHash
                                    | Error error ->
                                        logError error
                                        do! Async.Sleep (TimeSpan.FromSeconds 2)
                                        return curBlock
                                })
                        let nothingToDo = fromBlock = toBlock
                        if nothingToDo then do! Async.Sleep (TimeSpan.FromSeconds 10)
                        let! bci = getBlockchainInfo rpcClient
                        let newTipBlock = bci.BestBlockHash
                        return (newTipBlock, fromBlock)
                    })
        }