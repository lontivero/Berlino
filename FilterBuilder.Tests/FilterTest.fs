namespace FilterBuilder.Tests

open FilterBuilder.RpcClient
open NBitcoin
open Thoth.Json.Net
open Xunit

module FilterTest =

    [<Fact>]
    let ``Can decode verbose blocks`` () =
        let output = """
			{
				"hash": "27cac34bec2bfc3422c352d558b4db29e6d7e8114db2dbc955df06a63cda82fe",
				"confirmations": 1,
				"strippedsize": 442,
				"size": 478,
				"weight": 1804,
				"height": 102,
				"version": 536870912,
				"versionHex": "20000000",
				"merkleroot": "ddd7eab214fe2dc1f875ba0087b3dee60c5e55876d1494eacea88f259204004a",
				"tx": [
					{
						"txid": "5d95a076a2231feae22dfcf10285bd4069e6ca4e7e2a896a266e17e6807d8d8c",
						"hash": "b9cde924c05e72afaba40d03ea91b01fdccb976a119d9f3de57d5e2eb3f46006",
						"version": 2,
						"size": 172,
						"vsize": 145,
						"weight": 580,
						"locktime": 0,
						"vin": [
							{
								"coinbase": "01660101",
								"sequence": 4294967295
							}
						],
						"vout": [
							{
								"value": 50.000045,
								"n": 0,
								"scriptPubKey": {
									"asm": "OP_DUP OP_HASH160 381907cb00a047109bc340afe06504d67472d3de OP_EQUALVERIFY OP_CHECKSIG",
									"desc": "addr(mkda7wbQ9nVQa8ayYbTVifVzNie1kf8gKy)#yzcmfs2s",
									"hex": "76a914381907cb00a047109bc340afe06504d67472d3de88ac",
									"reqSigs": 1,
									"type": "pubkeyhash",
									"addresses": [
										"mkda7wbQ9nVQa8ayYbTVifVzNie1kf8gKy"
									]
								}
							},
							{
								"value": 0,
								"n": 1,
								"scriptPubKey": {
									"asm": "OP_RETURN aa21a9ed8198d32b4242fa8a0bd0ae04903f602d33a6f92e768da643ad3b72ad9ce72a06",
									"desc": "raw(6a24aa21a9ed8198d32b4242fa8a0bd0ae04903f602d33a6f92e768da643ad3b72ad9ce72a06)#87566l0s",
									"hex": "6a24aa21a9ed8198d32b4242fa8a0bd0ae04903f602d33a6f92e768da643ad3b72ad9ce72a06",
									"type": "nulldata"
								}
							}
						],
						"hex": "020000000001010000000000000000000000000000000000000000000000000000000000000000ffffffff0401660101ffffffff029403062a010000001976a914381907cb00a047109bc340afe06504d67472d3de88ac0000000000000000266a24aa21a9ed8198d32b4242fa8a0bd0ae04903f602d33a6f92e768da643ad3b72ad9ce72a060120000000000000000000000000000000000000000000000000000000000000000000000000"
					},
					{
						"txid": "f5a2f2747dc8c2ba9d362ef3c47400b01586a811fd0d0003549bce54b5c51ed4",
						"hash": "f5a2f2747dc8c2ba9d362ef3c47400b01586a811fd0d0003549bce54b5c51ed4",
						"version": 2,
						"size": 225,
						"vsize": 225,
						"weight": 900,
						"locktime": 101,
						"vin": [
							{
								"txid": "4815e72e2d967b666097c476473d0175b94d2a22f384e6389ab44dc9260dd8e0",
								"vout": 0,
								"scriptSig": {
									"asm": "30440220242cb6ccdfa7a4f83b3226b6694af52a9eafc94c7640a89786ffc93a07d79cd3022051375bc352b1f96223523e262ab93d9081135edafffd8e03a4fd38f49150e9b9[ALL] 02302fc55898d0b2adaf49be6c17c5804651ddb8ee114a05eb9da0a9517b8bccef",
									"hex": "4730440220242cb6ccdfa7a4f83b3226b6694af52a9eafc94c7640a89786ffc93a07d79cd3022051375bc352b1f96223523e262ab93d9081135edafffd8e03a4fd38f49150e9b9012102302fc55898d0b2adaf49be6c17c5804651ddb8ee114a05eb9da0a9517b8bccef"
								},
								"prevout": {
									"height": 1,
									"value": 50,
									"generated": true,
									"scriptPubKey": {
										"asm": "OP_DUP OP_HASH160 381907cb00a047109bc340afe06504d67472d3de OP_EQUALVERIFY OP_CHECKSIG",
										"desc": "addr(mkda7wbQ9nVQa8ayYbTVifVzNie1kf8gKy)#yzcmfs2s",
										"hex": "76a914381907cb00a047109bc340afe06504d67472d3de88ac",
										"reqSigs": 1,
										"type": "pubkeyhash",
										"addresses": [
											"mkda7wbQ9nVQa8ayYbTVifVzNie1kf8gKy"
										]
									}
								},
								"sequence": 4294967294
							}
						],
						"vout": [
							{
								"value": 48.999955,
								"n": 0,
								"scriptPubKey": {
									"asm": "OP_DUP OP_HASH160 6028ad75c715247d9179946458f946de0b83d3db OP_EQUALVERIFY OP_CHECKSIG",
									"desc": "addr(mpHPtoCqC8XJkCbRAoDfJFk8Uiidov8JCd)#f0l9dcff",
									"hex": "76a9146028ad75c715247d9179946458f946de0b83d3db88ac",
									"reqSigs": 1,
									"type": "pubkeyhash",
									"addresses": [
										"mpHPtoCqC8XJkCbRAoDfJFk8Uiidov8JCd"
									]
								}
							},
							{
								"value": 1,
								"n": 1,
								"scriptPubKey": {
									"asm": "OP_DUP OP_HASH160 29f5bf0598ecef7ae4f9f1163cdeecf1182c51f9 OP_EQUALVERIFY OP_CHECKSIG",
									"desc": "addr(mjLpPfQNYKCJGc1qXyU71wr6vt9yuVPLR6)#4ezwynfz",
									"hex": "76a91429f5bf0598ecef7ae4f9f1163cdeecf1182c51f988ac",
									"reqSigs": 1,
									"type": "pubkeyhash",
									"addresses": [
										"mjLpPfQNYKCJGc1qXyU71wr6vt9yuVPLR6"
									]
								}
							}
						],
						"fee": 0.000045,
						"hex": "0200000001e0d80d26c94db49a38e684f3222a4db975013d4776c49760667b962d2ee71548000000006a4730440220242cb6ccdfa7a4f83b3226b6694af52a9eafc94c7640a89786ffc93a07d79cd3022051375bc352b1f96223523e262ab93d9081135edafffd8e03a4fd38f49150e9b9012102302fc55898d0b2adaf49be6c17c5804651ddb8ee114a05eb9da0a9517b8bcceffeffffff026cff0f24010000001976a9146028ad75c715247d9179946458f946de0b83d3db88ac00e1f505000000001976a91429f5bf0598ecef7ae4f9f1163cdeecf1182c51f988ac65000000"
					}
				],
				"time": 1583444802,
				"mediantime": 1583444739,
				"nonce": 1,
				"bits": "207fffff",
				"difficulty": 4.656542373906925e-10,
				"chainwork": "00000000000000000000000000000000000000000000000000000000000000ce",
				"nTx": 2,
				"previousblockhash": "1d434df0cdd3fe26535ebe9734ef013b036441be38921606a9336ce74ab1cf04"
			}
			"""

        let blockInfoResult = Decode.fromString Decode.block output
        let expected = {
            Hash =  uint256.Parse "27cac34bec2bfc3422c352d558b4db29e6d7e8114db2dbc955df06a63cda82fe"
            PrevBlockHash = uint256.Parse "1d434df0cdd3fe26535ebe9734ef013b036441be38921606a9336ce74ab1cf04"
            Height = 102UL
            Transactions = [
                {   Id = uint256.Parse "5d95a076a2231feae22dfcf10285bd4069e6ca4e7e2a896a266e17e6807d8d8c"
                    Inputs = []
                    Outputs = [
                        {   PubKeyType = "pubkeyhash";
                            ScriptPubKey = [|
                                118uy; 169uy; 20uy; 56uy; 25uy; 7uy; 203uy; 0uy; 160uy; 71uy; 16uy; 155uy;
                                195uy; 64uy; 175uy; 224uy; 101uy; 4uy; 214uy; 116uy; 114uy; 211uy; 222uy;
                                136uy; 172uy |] }
                        {   PubKeyType = "nulldata"
                            ScriptPubKey = [|
                                106uy; 36uy; 170uy; 33uy; 169uy; 237uy; 129uy; 152uy; 211uy; 43uy; 66uy;
                                66uy; 250uy; 138uy; 11uy; 208uy; 174uy; 4uy; 144uy; 63uy; 96uy; 45uy; 51uy;
                                166uy; 249uy; 46uy; 118uy; 141uy; 166uy; 67uy; 173uy; 59uy; 114uy; 173uy;
                                156uy; 231uy; 42uy; 6uy|] }
                ]}
                {   Id = uint256.Parse "f5a2f2747dc8c2ba9d362ef3c47400b01586a811fd0d0003549bce54b5c51ed4"
                    Inputs = [
                        {   OutPoint = OutPoint.Parse "4815e72e2d967b666097c476473d0175b94d2a22f384e6389ab44dc9260dd8e0-0"
                            PrevOutput =
                               { ScriptPubKey = [|
                                    118uy; 169uy; 20uy; 56uy; 25uy; 7uy; 203uy; 0uy; 160uy; 71uy; 16uy;
                                    155uy; 195uy; 64uy; 175uy; 224uy; 101uy; 4uy; 214uy; 116uy; 114uy;
                                    211uy; 222uy; 136uy; 172uy |]
                                 PubKeyType = "pubkeyhash" } }]
                    Outputs = [
                        {   ScriptPubKey = [|
                                118uy; 169uy; 20uy; 96uy; 40uy; 173uy; 117uy; 199uy; 21uy; 36uy; 125uy;
                                145uy; 121uy; 148uy; 100uy; 88uy; 249uy; 70uy; 222uy; 11uy; 131uy;
                                211uy; 219uy; 136uy; 172uy |]
                            PubKeyType = "pubkeyhash" };
                        {   ScriptPubKey = [|
                                118uy; 169uy; 20uy; 41uy; 245uy; 191uy; 5uy; 152uy; 236uy; 239uy; 122uy;
                                228uy; 249uy; 241uy; 22uy; 60uy; 222uy; 236uy; 241uy; 24uy; 44uy; 81uy;
                                249uy; 136uy; 172uy|]
                            PubKeyType = "pubkeyhash" }] }
            ]
        }
        match blockInfoResult with
        | Ok blockInfo -> Assert.Equal (expected, blockInfo)
        | Error e -> failwith e
