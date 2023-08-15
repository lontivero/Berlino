module Berlino.Filters

open NBitcoin

type FilterData = {
    BlockHash : uint256
    PrevBlockHash : uint256
    Filter : GolombRiceFilter
}

let wireSerialize filter =
    Array.concat [
        filter.BlockHash.ToBytes()
        filter.PrevBlockHash.ToBytes()
        filter.Filter.ToBytes()]

type TaprootActivation = {
    Hash : uint256
    PrevBlockHash : uint256
    Height : int
}

let taprootActivation (network : Network) =
    match network.Name with
    | "Main" -> {
        Hash = uint256.Parse "0000000000000000000687bca986194dc2c1f949318629b44bb54ec0a94d8244"
        PrevBlockHash = uint256.Parse "000000000000000000013712fc242ee6dd28476d0e9c931c75f83e6974c6bccc"
        Height = 709632
        }
    | "TestNet" -> {
        Hash = uint256.Parse "00000000000000216dc4eb2bd27764891ec0c961b0da7562fe63678e164d62a0"
        PrevBlockHash = uint256.Parse "0000000000000001e17cc7358ee658affcb0a23146176581a7606a15f73993e3"
        Height = 2007000
        }
    | "RegTest" -> {
        Hash = Network.RegTest.GenesisHash
        PrevBlockHash = Network.RegTest.GetGenesis().Header.HashPrevBlock
        Height = 0
        }
    | _ -> failwith $"Unknown network '{network.Name}'"

