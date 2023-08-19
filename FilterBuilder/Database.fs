module Database

    open Berlino.Filters
    open Fumble
    open Microsoft.Data.Sqlite
    open NBitcoin

    let connection connectionString =
        Sql.existingConnection (new SqliteConnection(connectionString))

    let createTables connection =

        connection
        |> Sql.command
            """
            CREATE TABLE IF NOT EXISTS filters (
                block_hash BLOB NOT NULL,
                prev_block_hash BLOB NOT NULL,
                filter BLOB NOT NULL
                );

            CREATE UNIQUE INDEX IF NOT EXISTS block_hash_index ON filters(block_hash);
            """
        |> Sql.executeCommand
        |> Result.map (fun _ -> ())

    let save connection (filter : FilterData) =
        connection
        |> Sql.query
            "INSERT INTO filters(block_hash, prev_block_hash, filter)
                VALUES (@block_hash, @prev_block_hash, @filter)"
        |> Sql.parameters [
            "@block_hash", Sql.bytes (filter.BlockHash.ToBytes())
            "@prev_block_hash", Sql.bytes (filter.PrevBlockHash.ToBytes())
            "@filter", Sql.bytes (filter.Filter.ToBytes()) ]
        |> Sql.executeNonQueryAsync

    let getTipFilter connection =
        connection
        |> Sql.query
            "SELECT block_hash FROM filters WHERE block_hash NOT IN (SELECT prev_block_hash FROM filters)"
        |> Sql.execute (fun reader -> uint256 (reader.bytes "block_hash") )
