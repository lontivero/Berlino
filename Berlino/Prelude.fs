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
