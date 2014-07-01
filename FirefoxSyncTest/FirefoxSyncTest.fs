module FirefoxSyncTest

open System
open Xunit
open NUnit.Framework
open NUnit.Framework.Constraints
open FsCheck
open FsCheck.Xunit

open FirefoxSync
open FirefoxSync.Utilities
open FirefoxSync.CryptoKey
open FirefoxSync.GeneralInfo
open FirefoxSync.Collections
open FirefoxSync.SecretStore
open FirefoxSync.InternetExplorer

let setLog (logger : ILogger) = 
    let log (x: string) = logger.Log "%s" [(LogMessageBaseType.String) x]
    log

#if DEBUG
let log = setLog (new ConsoleLogger())
#else
let log = setLog (new PseudoLogger())
#endif


(*----------------------------------------------------------------------------*)
(*   xUnit Tests                                                              *)
(*----------------------------------------------------------------------------*)

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

(*----------------------------------------------------------------------------*)
(*   NUnit Tests                                                              *)
(*----------------------------------------------------------------------------*)

// JSON string quotation

[<Test>]
let ``Confirm JSON string quotation`` () : unit =
    let x = "\\n \"  \\\"\"\\"
    let x' = escapeString x
    let x'' = unescapeString x' |> Results.setOrFail
    x = x''
    |> Assert.True


// base32Decode

// https://docs.services.mozilla.com/sync/storageformat5.html
// 
//"Y4NKPS6YXAVI75XNUVODSR472I" 
// Python: \xc7\x1a\xa7\xcb\xd8\xb8\x2a\x8f\xf6\xed\xa5\x5c\x39\x47\x9f\xd2
//
[<Test>]
let ``base32Decode (mozilla docs example)`` () : unit =
    let x =
        "Y4NKPS6YXAVI75XNUVODSR472I" 
        |> base32'8'9Decode 
        |> Results.setOrFail
        |> bytesToHex 
    x = [|"c7"; "1a"; "a7"; "cb"; "d8"; "b8"; "2a"; "8f"; 
          "f6"; "ed"; "a5"; "5c"; "39"; "47"; "9f"; "d2"|]
    |> Assert.True


// buildSyncKeyBundle

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
[<Test>]
let ``buildSyncKeyBundle (mozilla docs example)`` () : unit = 
    let ff_skb' = "Y4NKPS6YXAVI75XNUVODSR472I" 
                  |> base32'8'9Decode
                  |> Results.setOrFail
                  |> buildSyncKeyBundle "johndoe@example.com" 
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


// setSecretByDefaultFile

let s' = SecretStore.setSecretByDefaultFile()

[<Test>]
let ``setSecretByDefaultFile`` () : unit =
    Assert.DoesNotThrow( fun x -> s' |> Results.setOrFail |> ignore )

let s = s' |> Results.setOrFail


// writeCryptoKeysToDisk

[<Test>]
let ``writeCryptoKeysToDisk`` () : unit = 
    writeCryptoKeysToDisk s.username s.password None
    |> Results.setOrFail
    |> fun _ -> true
    |> Assert.True


// getRecordFields/getRecordField

[<Test>]
let ``getRecordFields/getRecordField`` () : unit = 
    let x = {value = "value"; name = "name"; ``type`` = "type"; }
    let x' = getRecordFields x
    let x'' = { value = getRecordField x x'.[0]; 
                name = getRecordField x x'.[1]; 
                ``type`` = getRecordField x x'.[2] }
    x = x''
    |> Assert.True


// GeneralInfo

[<Test>]
let ``fetchInfoCollection/InfoQuota`` () : unit = 
    [ fetchInfoCollections s.username s.password
      fetchInfoQuota s.username s.password
      fetchInfoCollectionUsage s.username s.password
      fetchInfoCollectionCounts s.username s.password ]
    |> List.map (fun x -> match x with 
                          | Success x' -> log x'; true 
                          | _ -> false)
    |> List.reduce (fun x y -> x && y)  
    |> Assert.True


// Collections

let bm = 
    try 
        getCryptokeysFromFile s None 
        |> Results.setOrFail
        |> getBookmarks s
    with 
    | ex -> GetBookmarksError
            |> Results.setError "Failed to retrieve bookmarks" ex

[<Test>]
let ``Collection Bookmark (retrieve bookmarks)`` () : unit =
    log  (bm.ToString())
    bm
    |> Results.setOrFail
    |> fun x -> x.Length > 600 
    |> Assert.True

[<Test>]
let ``Collection Bookmark (select children)`` () : unit =
    let bm' = bm 
              |> Results.setOrFail
              |> Array.filter (fun x -> if x.children <> [||] then true else false)
    log  (bm'.ToString())
    bm'.Length > 40 
    |> Assert.True

[<Test>]
let ``Collection Bookmark (select by id)`` () : unit =
    let bm'' = bm 
               |> Results.setOrFail
               |> Array.filter (fun x -> if x.id = (WeaveGUID) "dkqtmNFIvhbg" then true else false)
    log  (bm''.ToString())
    bm''.Length = 1
    |> Assert.True

[<Test>]
let ``Collection Bookmark (select tags)`` () : unit =
    let bm''' = bm
                |> Results.setOrFail 
                |> Array.filter (fun x -> if x.tags <> [||] then true else false)
    log  (bm'''.ToString())
    bm'''.Length > 1
    |> Assert.True

[<Test>]
let ``Collection MetaGlobal`` () : unit =
    match (getMetaGlobal s) with
    | Success x -> log (x.ToString()); true
    | _ -> false
    |> Assert.True

[<Test>]
let ``Get folders and links`` () : unit =
    let bm' = bm |> Results.setOrFail
    bm'
    |> getFoldersAndLinks
    |> fun (x,y) -> bm'.Length = x.Length + y.Length && x.Length > 0 && y.Length > 0
    |> Assert.True


[<Test>]
let ``Write bookmarks to disk`` () : unit =
    let file = Environment.GetEnvironmentVariable("HOME") + @"\Desktop\bookmarks.txt"
    bm 
    |> Results.setOrFail
    |> fun bm' -> (bm',(bm' |> getFoldersAndLinks))
    |> fun (x,(y,z)) -> (x |> bookmarkSeqToJsonString "bm", 
                         y |> bookmarkSeqToJsonString "folders",
                         z |> bookmarkSeqToJsonString "links")
    |> fun (x,y,z) -> [ writeStringToFile x    false file 
                        writeStringToFile "\n" true  file
                        writeStringToFile y    true  file
                        writeStringToFile "\n" true  file
                        writeStringToFile z    true  file ]
    |> List.map Results.setOrFail
    |> fun x -> true
    |> Assert.IsTrue