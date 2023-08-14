namespace FilterProvider

open System
open System.Text
open System.Threading
open Berlino
open Filters
open Fumble
open NBitcoin
open FSharpPlus
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

open Suave.Operators
open Suave.Filters

module Program =

    let webSocketHandler (db : Sql.SqlProps) (taprootActivation : TaprootActivation) (webSocket : WebSocket) (context: HttpContext) =

        let worker =
            MailboxProcessor<byte[]>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    let! _ = webSocket.send Binary (ByteSegment msg) true
                    return! loop ()
                }
                loop () )
        let send = worker.Post

        let filterFetcher x = [[|0uy|]]
        let rec messageLoop () = socket {
            let! msg = webSocket.read()
            match msg with
            | Binary, data, true ->
                let parseBlockHash = Result.protect (fun (x : byte[]) -> uint256 x)
                let blockHashResult = parseBlockHash data
                match blockHashResult with
                | Error _ ->
                    send (Encoding.UTF8.GetBytes "Invalid request")
                | Ok blockId ->
                    filterFetcher blockId
                    |> Seq.iter send

                return! messageLoop()
            | Close, _, _ ->
                let emptyResponse = [||] |> ByteSegment
                do! webSocket.send Close emptyResponse true
            | _ ->
                return! messageLoop()
        }

        messageLoop ()

    let webapp (db : Sql.SqlProps) (taprootActivation : TaprootActivation) : WebPart =
        choose [
            path "/" >=> handShake (webSocketHandler db taprootActivation)
        ]

    open Suave.Logging

    let loggingOptions =
        { Literate.LiterateOptions.create() with
            getLogLevelText = function Verbose->"V" | Debug->"D" | Info->"I" | Warn->"W" | Error->"E" | Fatal->"F" }

    let logger =
        LiterateConsoleTarget(
            name = [|"Provider"|],
            minLevel = Verbose,
            options = loggingOptions,
            outputTemplate = "[{level}] {timestampUtc:o} {message} [{source}]{exceptions}"
        ) :> Logger

    [<EntryPoint>]
    let main args =
        let config = Configuration.load "config.json" args
        let network = Configuration.network config
        let db = Database.connection config.DatabaseConnectionString
        let taprootActivation = taprootActivation network

        let app = webapp db taprootActivation
        use cts = new CancellationTokenSource()
        let conf = { defaultConfig with cancellationToken = cts.Token; logger = logger }
        startWebServer conf app
        0 // return an integer exit code
