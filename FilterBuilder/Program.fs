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
        let taprootActivation = taprootActivation network
        use cts = new CancellationTokenSource()

        Database.createTables db |> Result.requiresOk
        let tipFilter =
            Database.getTipFilter db
            |> Result.requiresOk
            |> List.tryExactlyOne
            |> Option.defaultValue taprootActivation.PrevBlockHash

        let logError = fun (s : string) -> Console.WriteLine(s)

        let filterBuilder = build (uint32 1 <<< config.FalsePositiveRate) config.FalsePositiveRate
        let _, filterSaver = createFilterSaver filterBuilder (Database.save db) logError

        let blockFetcher = fetchBlock (RpcClient.getVerboseBlock rpcClient) filterSaver
        let bestBlockHashProvider = fun () -> RpcClient.getBestBlockHash rpcClient
        let filterCreationProcess = startBuilding bestBlockHashProvider blockFetcher logError tipFilter
        Async.RunSynchronously (filterCreationProcess, cancellationToken=cts.Token)
        0
