module Berlino.Filters

open NBitcoin

type FilterData = {
    BlockHash : uint256
    PrevBlockHash : uint256
    Height : uint64
    Filter : GolombRiceFilter
}
