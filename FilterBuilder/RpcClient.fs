module FilterBuilder.RpcClient

    open NBitcoin
    open NBitcoin.RPC
    open Thoth.Json.Net

    type VerboseOutputInfo = {
        ScriptPubKey : byte[]
        PubKeyType : string
        }

    type VerboseInputInfo = {
        OutPoint : OutPoint
        PrevOutput : VerboseOutputInfo
        }

    type VerboseTransactionInfo = {
        Id : uint256
        Inputs : VerboseInputInfo list
        Outputs : VerboseOutputInfo list
        }

    type VerboseBlockInfo = {
        PrevBlockHash : uint256
        Hash : uint256
        Height: uint64
        Transactions: VerboseTransactionInfo list
        }


    module Decode =
        open Berlino.Serialization
        open Berlino.RpcClient

        let output : Decoder<VerboseOutputInfo> =
            Decode.object(fun get ->
                {
                    ScriptPubKey = get.Required.Field "scriptPubKey" (Decode.object (fun g -> g.Required.Field "hex" Decode.byteArray))
                    PubKeyType =  get.Required.Field "scriptPubKey" (Decode.object (fun g -> g.Required.Field "type" Decode.string))
                })

        let input : Decoder<VerboseInputInfo> =
            Decode.map2 (fun outpoint output -> {
                OutPoint = outpoint
                PrevOutput = output
            }) Decode.outpoint (Decode.object (fun get -> get.Required.Field "prevout" output))

        let inputsExceptCoinbase : Decoder<VerboseInputInfo list> =
            Decode.list (Decode.ignoreFail input)
            |> Decode.andThen (fun ls -> ls |> List.choose id |> Decode.succeed )

        let transaction : Decoder<VerboseTransactionInfo> =
            Decode.object (fun get ->
                { Id = get.Required.Field "txid" Decode.uint256
                  Inputs = get.Required.Field "vin" inputsExceptCoinbase
                  Outputs = get.Required.Field "vout" (Decode.list output)
                })

        let block: Decoder<VerboseBlockInfo> =
            Decode.object (fun get ->
                {
                  PrevBlockHash = get.Required.Field "previousblockhash" Decode.uint256
                  Hash = get.Required.Field "hash" Decode.uint256
                  Height = get.Required.Field "height" Decode.uint64
                  Transactions = get.Required.Field "tx" (Decode.list transaction)
                })

        let verboseBlockFromString blockStr = Decode.fromString block blockStr

    let getVerboseBlock (rpcClient : RPCClient) blockId = async {
        let VERBOSE = 3
        let ct = Async.DefaultCancellationToken
        let! block =
            rpcClient.SendCommandAsync (RPCOperations.getblock, ct, blockId :> obj, VERBOSE :> obj)
            |> Async.AwaitTask
        return Decode.verboseBlockFromString block.ResultString
        }

