namespace FirefoxSync

open System

open FSharp.Data
open FSharp.Data.JsonExtensions

open Utilities
open ServerUrls


module Collections =

    // https://github.com/mikerowehl/firefox-sync-client-php/blob/master/sync.php#L94
    let fetchFullCollection username password collection =
        let url = (clusterURL username) + "1.1/" + username + "/storage/" + collection + "?full=1"
        fetchUrlResponse url "GET" (Some (username, password)) None None None


    let parseEncryptedCollectionArray collection = 
        try
            collection
            |> JsonValue.Parse 
            |> fun x -> x.AsArray() 
            |> Array.map string 
            |> Array.map (fun x -> JsonValue.Parse x) 
            |> Array.map (fun x -> (x?payload).AsString())
            |> Array.map (fun x -> JsonValue.Parse x)
            |> Array.map (fun x -> { iv         = x?IV.AsString()
                                     ciphertext = x?ciphertext.AsString()
                                     hmac       = x?hmac.AsString() } )
        with | _ -> [||]


    let getFirstCryptoKey (cryptokeys : CryptoKeys) = 
        match cryptokeys.``default`` with
        | [||] -> [||]
        | _ as x -> x.[0]


    let decryptCollectionArray cryptokeys collection = 
        try
            let key = cryptokeys |> getFirstCryptoKey
            [|for b in collection do yield DecryptAES b.ciphertext key (b.iv |> Convert.FromBase64String) |]
            |> Array.map keepAsciiPrintableChars
        with | _ -> [||]


    let getDecryptedCollection (secrets : Secret) cryptokeys collection = 
        try
            collection
            |> fetchFullCollection secrets.username secrets.password 
            |> parseEncryptedCollectionArray
            |> decryptCollectionArray cryptokeys
        with | _ -> [||]       


    let getBookmarks secrets cryptokeys =
        let parseBookmark bm = 
            { id            = "id" |> tryGetString bm |> (WeaveGUID)
              ``type``      = "type" |> tryGetString bm
              title         = "title" |> tryGetString bm
              parentName    = "parentName" |> tryGetString bm
              bmkUri        = "bmkUri" |> tryGetString bm |> (URI)
              tags          = "tags" |> tryGetArray bm |> Array.map (fun x -> x.AsString())
              keyword       = "keyword" |> tryGetString bm
              description   = "description" |> tryGetString bm
              loadInSidebar = "loadInSidebar" |> tryGetBoolean bm false
              parentid      = "parentid" |> tryGetString bm |> (WeaveGUID)
              children      = "children" |> tryGetArray bm |> Array.map (fun x -> x.AsString() |> (WeaveGUID)) }
        try 
            "bookmarks" 
            |> getDecryptedCollection secrets cryptokeys
            |> Array.map JsonValue.Parse
            |> Array.map parseBookmark
        with | _ -> [||]