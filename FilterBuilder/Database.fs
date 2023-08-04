module Database

    open Berlino.Filters
    open Fumble
    open Microsoft.Data.Sqlite

    let connection connectionString =
        Sql.existingConnection (new SqliteConnection(connectionString))

    let createTables connection =

        connection
        |> Sql.command
            """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS filters (
                id INTEGER PRIMARY KEY,
                block_hash BLOB NOT NULL,
                prev_block_hash BLOB NOT NULL,
                filter BLOB NOT NULL
                );

            CREATE UNIQUE INDEX IF NOT EXISTS block_hash_index ON filters(block_hash);
            CREATE INDEX IF NOT EXISTS prev_block_hash_index ON filters(prev_block_hash);
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
