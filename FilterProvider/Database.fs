module Database

    open Berlino.Filters
    open Fumble
    open Microsoft.Data.Sqlite
    open NBitcoin

    let connection connectionString =
        Sql.existingConnection (new SqliteConnection(connectionString))


    let get connection (from : uint256) =
        connection
        |> Sql.query
            "WITH RECURSIVE parent_of(bh,pbh) AS (
                SELECT block_hash, prev_block_hash FROM filters WHERE block_hash = @block_hash
                UNION
                SELECT block_hash, prev_block_hash FROM filters, parent_of WHERE block_hash = pbh)
                SELECT filters.block_hash, filters.prev_block_hash, filters.filter
                    FROM filters, parent_of
                    WHERE block_hash = bh"
        |> Sql.parameters [
            "@block_hash", Sql.bytes (from.ToBytes())]
        |> Sql.executeAsync (fun reader ->
            {
              BlockHash = uint256 (reader.bytes "block_hash")
              PrevBlockHash = uint256 (reader.bytes "prev_block_hash")
              Filter = GolombRiceFilter (reader.bytes "filter")
            })