namespace Berlino

open System
open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module Utils =
    let curry f a b = f (a,b)
    let uncurry f (a,b) = f a b

    let memoize f =
        let dict = Dictionary<_, _>()
        fun c ->
            let exists, value = dict.TryGetValue c
            match exists with
            | true -> value
            | _ ->
                let value = f c
                dict.Add(c, value)
                value

[<AutoOpen; RequireQualifiedAccess>]
module Seq =
    let exceptBy excluded predicate ls =
        ls |> Seq.filter (fun x -> excluded |> Seq.exists (predicate x) |> not )

    let join (innerSequence : 'b seq) outerKeySelector innerKeySelector (outerSequence : 'a seq) =
        outerSequence.Join(innerSequence,
            Func<_, _>(outerKeySelector), Func<_, _>(innerKeySelector),
            fun outer inner -> (outer, inner))

type State<'s, 'a> = ('s -> 'a * 's)

[<AutoOpen>]
module State =

    let inline run state x = let (f) = x in f state
    let get = (fun s -> s, s)
    let put newState = (fun _ -> (), newState)
    let map f s = (fun (state: 's) ->
        let x, state = run state s
        f x, state)

    type StateBuilder() =
        member this.Zero () = (fun s -> (), s)
        member this.Return x = (fun s -> x, s)
        member inline this.ReturnFrom (x: State<'s, 'a>) = x
        member this.Bind (x, f) : State<'s, 'b> =
            (fun state ->
                let (result: 'a), state = run state x
                run state (f result))
        member this.Combine (x1: State<'s, 'a>, x2: State<'s, 'b>) =
            (fun state ->
                let result, state = run state x1
                run state x2)
        member this.Delay f : State<'s, 'a> = f ()
        member this.For (seq, (f: 'a -> State<'s, 'b>)) =
            seq
            |> Seq.map f
            |> Seq.reduceBack (fun x1 x2 -> this.Combine (x1, x2))
        member this.While (f, x) =
            if f () then this.Combine (x, this.While (f, x))
            else this.Zero ()

    let state = StateBuilder()

