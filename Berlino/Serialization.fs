module Berlino.Serialization

    open NBitcoin
    open Thoth.Json.Net

    [<RequireQualifiedAccess>]
    module Decode =
        let uint256 : Decoder<uint256> =
            Decode.string
            |> Decode.andThen (fun value ->
                match uint256.TryParse value with
                | true, parsed -> Decode.succeed parsed
                | _ -> Decode.fail "Invalid uint256 value")

        let money : Decoder<Money> =
            Decode.float32
            |> Decode.andThen (fun v -> Decode.succeed (Money.Coins (decimal v)))

        let byteArray : Decoder<byte[]> =
            Decode.string
            |> Decode.andThen (fun v -> Decode.succeed (DataEncoders.Encoders.Hex.DecodeData v))

        let outpoint : Decoder<OutPoint> =
            Decode.object (fun get ->
                 let txid = get.Required.Field "txid" uint256
                 let n = get.Required.Field "vout" Decode.uint32
                 OutPoint(txid, n))

        let extPubKey (network : Network) : Decoder<ExtPubKey> =
            Decode.string
            |> Decode.andThen (fun epk ->
                match ExtPubKey.Parse (epk, network) with
                | null -> Decode.fail $"Invalid extpubkey '{epk}' for network '{network}'"
                | v -> Decode.succeed v)

        let fingerprint : Decoder<HDFingerprint> =
            Decode.string
            |> Decode.andThen (fun fp ->
                match HDFingerprint.TryParse fp with
                | true, fp -> Decode.succeed fp
                | false, _ -> Decode.fail $"Invalid fingerprint '{fp}'")

        let keyPath : Decoder<KeyPath> =
            Decode.string |> Decode.andThen (fun v ->
                match KeyPath.TryParse v with
                | true, keypath -> Decode.succeed keypath
                | _ -> Decode.fail $"Invalid key path '{v}'")

        let network : Decoder<Network> =
            Decode.string |> Decode.andThen (fun v ->
                match Network.GetNetwork(v) with
                | null -> Decode.fail $"Invalid network '{v}"
                | network -> Decode.succeed network)

    [<RequireQualifiedAccess>]
    module Encode =
        let extPubKey (extPubKey : ExtPubKey) (network : Network) =
            Encode.string (extPubKey.GetWif(network).ToString())

        let fingerprint (fp : HDFingerprint) =
            Encode.string (fp.ToString())

        let keyPath (kp : KeyPath) =
            Encode.string (kp.ToString())

        let network (network : Network) =
            Encode.string network.Name