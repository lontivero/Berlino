namespace FilterBuilder

open System
open System.Threading
open NBitcoin
open NBitcoin.RPC
open Berlino
open Filters
open Suave
open WebSocket
open Suave.Filters
open Suave.RequestErrors

module Program =

    let handleWebSocket filterFetcher knownHashString =
          match uint256.TryParse knownHashString with
          | true, knownHash -> handShake (webSocketHandler filterFetcher knownHash)
          | false, _ -> NOT_FOUND "Invalid known hash"

    let webapp handleWebSocket : WebPart =
        choose [
            pathScan "/%s" handleWebSocket
        ]

    let startWebSocketServer db =
        let getFiltersFromDatabase = Database.get db 500
        let filterFetcher = fetchFilters getFiltersFromDatabase
        let webSocketHandler = handleWebSocket filterFetcher
        let app = webapp webSocketHandler
        use cts = new CancellationTokenSource()
        let conf = { defaultConfig with cancellationToken = cts.Token; logger = logger }
        startWebServerAsync conf app

    let startFiltersBuilder db config =
        let network = Configuration.network config
        let rpcClient = RPCClient(RPCCredentialString.Parse config.RpcConnectionString, network)
        let taprootActivation = taprootActivation network
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
        startBuilding bestBlockHashProvider blockFetcher logError tipFilter

    [<EntryPoint>]
    let main args =
        let config = Configuration.load "config.json" args
        let db = Database.connection config.DatabaseConnectionString
        Database.createTables db |> Result.requiresOk

        use cts = new CancellationTokenSource()

        let _, webServer = startWebSocketServer db
        let filterBuilder = startFiltersBuilder db config
        [ webServer; filterBuilder ]
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously
        0