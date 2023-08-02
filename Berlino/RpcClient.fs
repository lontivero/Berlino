module Berlino.RpcClient

    open NBitcoin.RPC
    open Thoth.Json.Net

    module Decode =
        open Berlino.Serialization

        let ignoreFail (decoder : Decoder<'T>) : Decoder<'T option> =
            fun path token ->
                match decoder path token with
                | Ok x -> Ok(Some x)
                | Error _ -> Ok None

    let getBlockHash (height : uint) (rpcClient : RPCClient) = async {
        let ct = Async.DefaultCancellationToken
        let! blockId = rpcClient.GetBlockHashAsync (height, ct) |> Async.AwaitTask
        return blockId
        }

    let getBlockchainInfo (rpcClient : RPCClient) = async {
        let ct = Async.DefaultCancellationToken
        let! info = rpcClient.GetBlockchainInfoAsync ct |> Async.AwaitTask
        return info
        }
