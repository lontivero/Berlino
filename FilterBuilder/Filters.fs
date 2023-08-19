module Filters

    open System
    open Berlino
    open Berlino.Filters
    open FilterBuilder.RpcClient
    open Fumble
    open Microsoft.FSharp.Core
    open NBitcoin

    let build M P (block : VerboseBlockInfo) =
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
            .SetP(P)
            .SetM(M)
            .AddEntries(scripts).Build()

    type Logger = {
        logError : string -> unit
    }

    type FilterSaver = FilterData -> Async<Result<int, exn>>
    type FilterBuilder = VerboseBlockInfo -> GolombRiceFilter
    type VerboseBlockProvider = uint256 -> Async<Result<VerboseBlockInfo,string>>
    type BestBlockHashProvider = unit -> Async<uint256>

    let createFilterSaver (build : FilterBuilder) (saveFilter : FilterSaver) logError =
        MailboxProcessor<VerboseBlockInfo>.Start(fun inbox ->
            forever () <| fun () -> async {
                let! verboseBlockInfo = inbox.Receive()
                let filter = build verboseBlockInfo
                let! result =
                    saveFilter {
                         BlockHash = verboseBlockInfo.Hash
                         PrevBlockHash = verboseBlockInfo.PrevBlockHash
                         Filter = filter }
                match result with
                | Ok _ -> ()
                | Error e ->
                    logError (e.ToString())
                    failwith e.Message
            })

    let fetchBlock (getVerboseBlock : VerboseBlockProvider) buildFilter blockHash =
        getVerboseBlock blockHash
        |> AsyncResult.map (fun verboseBlockInfo ->
            buildFilter verboseBlockInfo
            verboseBlockInfo.PrevBlockHash)
        |> Async.CatchResult
        |> AsyncResult.mapError exnAsString
        |> AsyncResult.join

    let startBuilding (getBestBlockHash : BestBlockHashProvider) fetchBlock logError stopAt = async {
        let! tipBlock = getBestBlockHash ()

        let rec loop0 fromBlock toBlock = async {
            let rec loop1 curBlock = async {
                if curBlock <> toBlock then
                    match! fetchBlock curBlock with
                    | Ok prevBlockHash ->
                        do! loop1 prevBlockHash
                    | Error error ->
                        logError error
                        do! Async.Sleep (TimeSpan.FromSeconds 2)
                        do! loop1 curBlock
            }
            do! loop1 fromBlock

            let nothingToDo = fromBlock = toBlock
            if nothingToDo then do! Async.Sleep (TimeSpan.FromSeconds 10)
            let! newTipBlockResult = getBestBlockHash () |> Async.CatchResult
            let newTipBlock =
                match newTipBlockResult with
                | Ok newTipBlock -> newTipBlock
                | Error error ->
                    logError (exnAsString error)
                    tipBlock
            do! loop0 newTipBlock fromBlock
        }
        try
            do! loop0 tipBlock stopAt
        finally
            printfn "finisehd"
    }