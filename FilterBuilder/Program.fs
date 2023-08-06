namespace FilterBuilder


open System.Threading
open NBitcoin
open Berlino
open Filters
open NBitcoin.RPC

module Program =

    [<EntryPoint>]
    let main args =
        let network = Network.Main
        let rpcClient = RPCClient(RPCCredentialString.Parse "****:****", network)
        let initialData = taprootActivation network
        use cts = new CancellationTokenSource()
        let filterSaver = createFilterSaver ()
        use _ = filterSaver.Error.Subscribe (fun _ -> cts.Cancel())

        let buildingEnv = {
            rpc = {
                getVerboseBlock = fun blkHash -> RpcClient.getVerboseBlock blkHash rpcClient
                getBestBlockHash = fun  () ->  RpcClient.getBestBlockHash rpcClient
            }
            buildFilter = filterSaver.Post
        }
        let filterCreationProcess = startBuilding buildingEnv initialData.PrevBlockHash
        Async.RunSynchronously (filterCreationProcess, cancellationToken=cts.Token)
        0
