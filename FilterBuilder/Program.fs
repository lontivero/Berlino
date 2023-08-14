namespace FilterBuilder


open System
open System.Threading
open Berlino
open Filters
open NBitcoin.RPC

module Program =

    [<EntryPoint>]
    let main args =
        let config = Configuration.load "config.json" args
        let network = Configuration.network config
        let rpcClient = RPCClient(RPCCredentialString.Parse config.RpcConnectionString, network)
        let db = Database.connection config.DatabaseConnectionString
        let initialData = taprootActivation network
        use cts = new CancellationTokenSource()

        Database.createTables db |> Result.requiresOk
        let logError = fun (s : string) -> Console.WriteLine(s)

        let filterBuilder = build (uint32 1 <<< config.FalsePositiveRate) config.FalsePositiveRate
        let filterSaver = createFilterSaver filterBuilder (Database.save db) logError
        use _ = filterSaver.Error.Subscribe (fun _ -> cts.Cancel())

        let blockFetcher = fetchBlock (RpcClient.getVerboseBlock rpcClient) (filterSaver.Post)
        let bestBlockHashProvider = fun () -> RpcClient.getBestBlockHash rpcClient
        let filterCreationProcess = startBuilding bestBlockHashProvider blockFetcher logError initialData.PrevBlockHash
        Async.RunSynchronously (filterCreationProcess, cancellationToken=cts.Token)
        0
