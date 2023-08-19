module Database

    open Berlino.Filters
    open Fumble
    open Microsoft.Data.Sqlite
    open NBitcoin

    let connection connectionString =
        Sql.existingConnection (new SqliteConnection(connectionString))


    let get connection howMany (from : uint256) =
        connection
        |> Sql.query
            "WITH RECURSIVE parent_of(bh,pbh) AS (
                SELECT block_hash, prev_block_hash FROM filters WHERE block_hash = @block_hash
                UNION ALL
                SELECT block_hash, prev_block_hash FROM filters, parent_of WHERE prev_block_hash = bh)
             SELECT block_hash, prev_block_hash, filter FROM filters, parent_of WHERE prev_block_hash = bh
             LIMIT @count"
        |> Sql.parameters [
            "@block_hash", Sql.bytes (from.ToBytes())
            "@count", Sql.int howMany]
        |> Sql.executeAsync (fun reader ->
            {
              BlockHash = uint256 (reader.bytes "block_hash")
              PrevBlockHash = uint256 (reader.bytes "prev_block_hash")
              Filter = GolombRiceFilter (reader.bytes "filter")
            })

// WITH RECURSIVE parent_of(bh,pbh,depth) AS (
//      SELECT block_hash, prev_block_hash, 0 FROM filters WHERE block_hash = X'86630EC9B4C0C85A7EB868F6A4DA32459FAA6ED2C813A5DB2B00000000000000'
//      UNION ALL
//      SELECT block_hash, prev_block_hash, parent_of.depth+1 FROM filters, parent_of WHERE  prev_block_hash = bh AND parent_of.depth < 9 ) SELECT hex(block_hash) FROM filters, parent_of WHERE block_hash = bh;

// tail: select hex(block_hash) from filters where prev_block_hash not in (select block_hash from filters);

// tip: select hex(block_hash) from filters where block_hash not in (select prev_block_hash from filters);

