module FirefoxSyncTest 

open System
open Xunit
open FsCheck
open FsCheck.Xunit

open FirefoxSync
open FirefoxSync.Utilities
open FirefoxSync.CryptoKey
open FirefoxSync.GeneralInfo
open FirefoxSync.Collections


// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/
// http://www.clear-lines.com/blog/post/FsCheck-and-XUnit-is-The-Bomb.aspx
//
[<Property>]
let ``square should be positive failing`` (x:float) =
    x * x >= 0.
//
[<Property>]
let ``square should be positive`` (x:float) =
    not (Double.IsNaN(x)) ==> (x * x >= 0.)


let s = SecretStore.secrets

// base32Decode

// https://docs.services.mozilla.com/sync/storageformat5.html
// 
//"Y4NKPS6YXAVI75XNUVODSR472I" 
// Python: \xc7\x1a\xa7\xcb\xd8\xb8\x2a\x8f\xf6\xed\xa5\x5c\x39\x47\x9f\xd2
//
[<Fact>]
let ``base32Decode (mozilla docs example)`` () =
    let x =
        "Y4NKPS6YXAVI75XNUVODSR472I" 
        |> base32'8'9Decode 
        |> bytesToHex 
    x = [|"c7"; "1a"; "a7"; "cb"; "d8"; "b8"; "2a"; "8f"; 
          "f6"; "ed"; "a5"; "5c"; "39"; "47"; "9f"; "d2"|]
    |> Assert.True

// syncKeyBundle

// https://docs.services.mozilla.com/sync/storageformat5.html
// 
//    sync_key = \xc7\x1a\xa7\xcb\xd8\xb8\x2a\x8f\xf6\xed\xa5\x5c\x39\x47\x9f\xd2
//    username = johndoe@example.com
//    HMAC_INPUT = Sync-AES_256_CBC-HMAC256
//
//    # Combine HMAC_INPUT and username to form HKDF info input.
//    info = HMAC_INPUT + username
//      -> "Sync-AES_256_CBC-HMAC256johndoe@example.com"
//
//    # Perform HKDF Expansion (1)
//    encryption_key = HKDF-Expand(sync_key, info + "\x01", 32)
//      -> 0x8d0765430ea0d9dbd53c536c6c5c4cb639c093075ef2bd77cd30cf485138b905
//
//    # Second round of HKDF
//    hmac = HKDF-Expand(sync_key, encryption_key + info + "\x02", 32)
//      -> 0xbf9e48ac50a2fcc400ae4d30a58dc6a83a7720c32f58c60fd9d02db16e406216
//
[<Fact>]
let ``syncKeyBundle (mozilla docs example)`` () = 
    let ff_skb' = syncKeyBundle "johndoe@example.com" ("Y4NKPS6YXAVI75XNUVODSR472I" |> base32'8'9Decode)
    let ff_skb'' = ( ff_skb'.encryption_key |> Array.map (sprintf "%x"), 
                     ff_skb'.hmac_key |> Array.map (sprintf "%x"))
    let ff_skb''' = ([|"8d"; "7"; "65"; "43"; "e"; "a0"; "d9"; "db"; "d5"; "3c"; "53"; "6c";
                       "6c"; "5c"; "4c"; "b6"; "39"; "c0"; "93"; "7"; "5e"; "f2"; "bd"; "77";
                       "cd"; "30"; "cf"; "48"; "51"; "38"; "b9"; "5"|],
                     [|"bf"; "9e"; "48"; "ac"; "50"; "a2"; "fc"; "c4"; "0"; "ae"; "4d"; "30";
                       "a5"; "8d"; "c6"; "a8"; "3a"; "77"; "20"; "c3"; "2f"; "58"; "c6"; "f";
                       "d9"; "d0"; "2d"; "b1"; "6e"; "40"; "62"; "16"|])
    ff_skb'' = ff_skb'''
    |> Assert.True

// writeCryptoKeysToDisk

[<Fact>]
let ``writeCryptoKeysToDisk`` () = 
    try
        writeCryptoKeysToDisk s.username s.password None
        true
    with | _ -> false
    |> Assert.True


// getRecordFields/getRecordField

[<Fact>]
let ``getRecordFields/getRecordField`` () = 
    let x = {value = "value"; name = "name"; ``type`` = "type"; }
    let x' = getRecordFields x
    let x'' = { value = getRecordField x x'.[0]; 
                name = getRecordField x x'.[1]; 
                ``type`` = getRecordField x x'.[2] }
    x = x''
    |> Assert.True

// GeneralInfo

[<Fact>]
let ``fetchInfoCollection/Quota`` () = 
    try
        let ic = fetchInfoCollections s.username s.password
        let iq = fetchInfoQuota s.username s.password
        let icu = fetchInfoCollectionUsage s.username s.password
        let icc = fetchInfoCollectionCounts s.username s.password
        true
    with 
    | _ -> false 
    |> Assert.True


// Collections

let bm = getBookmarks s (getCryptokeysFromDisk s None)
[<Fact>]
let ``Collection Bookmark (retrieve bookmarks)`` () =
    printfn "# bookmarks '= %d" bm.Length
    bm.Length > 800 
    |> Assert.True

[<Fact>]
let ``Collection Bookmark (select children)`` () =
    let bm' = bm |> Array.filter (fun x -> if x.children <> [||] then true else false)
    bm'.Length > 40 
    |> Assert.True

[<Fact>]
let ``Collection Bookmark (select by id)`` () =
    let bm'' = bm |> Array.filter (fun x -> if x.id = (WeaveGUID) "dkqtmNFIvhbg" then true else false)
    bm''.Length = 1
    |> Assert.True

[<Fact>]
let ``Collection Bookmark (select tags)`` () =
    let bm''' = bm |> Array.filter (fun x -> if x.tags <> [||] then true else false)
    bm'''.Length > 1
    |> Assert.True

[<Fact>]
let ``Collection MetaGlobal`` () =
    match (getMetaGlobal s) with
    | Some x -> true
    | _ -> false
    |> Assert.True