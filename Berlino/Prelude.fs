namespace Berlino

[<AutoOpen>]
module Utils =
    let curry f a b = f (a,b)
    let uncurry f (a,b) = f a b
