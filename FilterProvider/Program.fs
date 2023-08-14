namespace FilterProvider

open System.Text
open System.Text.Unicode
open Berlino
open Filters
open NBitcoin
open FSharpPlus
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket

module Program =

    let handle (webSocket : WebSocket) log (context: HttpContext) =

        let worker =
            MailboxProcessor<byte[]>.Start(fun inbox ->
                let rec loop () = async {
                    let! msg = inbox.Receive()
                    let! result = webSocket.send Binary (ByteSegment msg) true
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
