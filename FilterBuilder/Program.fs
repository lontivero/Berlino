namespace FilterBuilder

open System
open System.Threading
open NBitcoin

module Program =
    Console.WriteLine "generate filters"
    open Filters
    open NBitcoin.RPC
    open Building

    let network = Network.Main
    let rpcClient = RPCClient(RPCCredentialString.Parse "****:****", network)
    let initialData = taprootActivation network
    let cts = new CancellationTokenSource()
    let filterSaver = createFilterSaver ()
    let s = filterSaver.Error.Subscribe (fun _ -> cts.Cancel())
    let filterBuilder = createFilterBuilder filterSaver.Post

    let filterCreationProcess = startBuilding rpcClient initialData.PrevBlockHash filterBuilder.Post
    Async.RunSynchronously (filterCreationProcess, cancellationToken=cts.Token)
    s.Dispose()
