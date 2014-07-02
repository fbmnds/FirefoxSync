namespace FirefoxSync

open System

open FSharp.Data
open FSharp.Data.JsonExtensions

open Utilities
open ServerUrls


module Collections =

    /// Return the full collection as a Result<string> (encrypted).
    // https://github.com/mikerowehl/firefox-sync-client-php/blob/master/sync.php#L94
    let fetchFullCollection username password collection =
        let setClusterURL () = 
            match (clusterURL username) with
            | Success url -> Success (url + "1.1/" + username + "/storage/" + collection + "?full=1")
            | Failure f -> Failure ( ClusterUrlError((ErrorLabel) (sprintf "Failed to full collection %s" collection), (Stacktrace) "") :: f ) 
        setClusterURL ()
        |> Results.bind (fetchUrlResponse "GET" (Some (username, password)) None None None)


    /// Return the parsed encryted collection as Result<EncryptedCollection[]>.
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
            |> Success
        with | ex -> EncryptedCollectionParseError
                     |> Results.setError (sprintf "Parse error on collection %s" collection) ex 


    /// Return the first Firefox Sync Crypto Key as Result<byte[]>.
    let getFirstCryptoKey (cryptokeys : CryptoKeys) = 
        match cryptokeys.``default`` with
        | [||] -> Failure [ FirstCryptoKeyError ((ErrorLabel) "Failed to get first crypto key" , (Stacktrace) "") ]
        | _ as x -> Success x.[0]


    /// Return the decrypted seq<EncryptedCollection> as Result<string[]>.
    let decryptCollectionArray cryptokeys collection = 
        try
            let key = cryptokeys |> getFirstCryptoKey |> Results.setOrFail
            [|for b in collection do yield DecryptAES b.ciphertext key (b.iv |> Convert.FromBase64String) |]
            |> Array.map keepAsciiPrintableChars
            |> Success
        with 
        | ex -> DecryptCollectionError
                |> Results.setError (sprintf "Failed to decrypt an encrypted collection %s" (collection.ToString())) ex 


    /// Return the decrypted collection by name 'collection' as Result<string[]>.
    let getDecryptedCollection (secrets : Secret) cryptokeys collection = 
        fetchFullCollection secrets.username secrets.password 
        >> Results.bind parseEncryptedCollectionArray
        >> Results.bind (decryptCollectionArray cryptokeys)
        <| collection   

    let emptyBookmark description = 
        { id            = "" |> (WeaveGUID) 
          ``type``      = ""
          title         = ""
          parentName    = ""
          bmkUri        = "" |> (URI)
          tags          = [||]
          keyword       = ""
          description   = description
          loadInSidebar = false
          parentid      = "" |> (WeaveGUID)
          children      = [||] }

    let parseBookmark bm = 
        { id            = "id"            |> tryGetString bm |> (WeaveGUID)
          ``type``      = "type"          |> tryGetString bm
          title         = "title"         |> tryGetString bm
          parentName    = "parentName"    |> tryGetString bm
          bmkUri        = "bmkUri"        |> tryGetString bm |> (URI)
          tags          = "tags"          |> tryGetArray bm  |> Array.map (fun x -> x.AsString())
          keyword       = "keyword"       |> tryGetString bm
          description   = "description"   |> tryGetString bm
          loadInSidebar = "loadInSidebar" |> tryGetBoolean bm false
          parentid      = "parentid"      |> tryGetString bm |> (WeaveGUID)
          children      = "children"      |> tryGetArray bm  |> Array.map (fun x -> x.AsString() |> (WeaveGUID)) }

    let getBookmarksRelaxed secrets cryptokeys =
        let parseBookmark' bm = 
            try
                parseBookmark bm
            with
            | _ -> bm.ToString() |> emptyBookmark
        try 
            "bookmarks" 
            |> getDecryptedCollection secrets cryptokeys
            |> Results.setOrFail
            |> Array.map JsonValue.Parse
            |> Array.map parseBookmark'
        with | _ -> [||]

    let getBookmarks secrets cryptokeys =
        let parseBookmark' bm = 
            try
                parseBookmark bm
            with
            | _ -> failwith (sprintf "Failed to parse bookmark %s" (bm.ToString()))
        try 
            "bookmarks" 
            |> getDecryptedCollection secrets cryptokeys
            |> Results.setOrFail
            |> Array.map JsonValue.Parse
            |> Array.map parseBookmark'
            |> Success
        with 
        | ex -> Results.setError "" ex GetBookmarksError

    
    let bookmarkToJson (bm: Bookmarks) =
        JsonValue.Record(
            [|("id",            JsonValue.String((string) bm.id)); 
              ("type",          JsonValue.String(bm.``type``)) 
              ("title",         JsonValue.String(bm.title)) 
              ("parentName",    JsonValue.String(bm.parentName))
              ("bmkUri",        JsonValue.String((string) bm.bmkUri))
              ("tags",          JsonValue.Array(bm.tags |> Array.map JsonValue.String))
              ("keyword",       JsonValue.String(bm.keyword))
              ("description",   JsonValue.String(bm.description))
              ("loadInSidebar", JsonValue.Boolean(bm.loadInSidebar))
              ("parentid",      JsonValue.String((string) bm.parentid))
              ("children",      JsonValue.Array(bm.children 
                                                |> Array.map (string) 
                                                |> Array.map JsonValue.String))|])


    let bookmarkSeqToJson (bms: Bookmarks seq) =
        [| for bm in bms do yield bookmarkToJson bm |]   
        |> JsonValue.Array


    let bookmarkToJsonString (bm: Bookmarks) =
        let str x = x |> escapeString
        let str'  (WeaveGUID x) = x |> escapeString
        let str'' (URI x)       = x |> escapeString
        let arrayToString x = 
            x
            |> (String.concat "\",\"")
            |> fun s -> if s.Length > 0 then sprintf "[\"%s\"]" s else "[]"
        let arrayToString' x = 
            x
            |> Seq.map str'
            |> arrayToString
        let boolToString x = if x then "true" else "false"
        [ "{ \"id\"            : \"" ; (str' bm.id)            ; "\",\n" ;
          "  \"type\"          : \"" ; (str bm.``type``)       ; "\",\n" ;
          "  \"title\"         : \"" ; (str bm.title)          ; "\",\n" ;
          "  \"parentName\"    : \"" ; (str bm.parentName)     ; "\",\n" ;
          "  \"bmkUri\"        : \"" ; (str'' bm.bmkUri)       ; "\",\n" ;
          "  \"tags\"          : "   ; (arrayToString bm.tags) ; ",\n"   ;
          "  \"keyword\"       : \"" ; (str bm.keyword)        ; "\",\n" ;
          "  \"description\"   : \"" ; (str bm.description)    ; "\",\n" ;
          "  \"loadInSidebar\" : "   ; ((boolToString) bm.loadInSidebar)  ; ",\n" ;
          "  \"parentid\"      : \"" ; (str' bm.parentid)      ; "\",\n" ;
          "  \"children\"      : "   ; (arrayToString' bm.children) ; " }" ]
        |> (String.concat "")


    let bookmarkSeqToJsonString name (bms: Bookmarks seq) =
        [| for bm in bms do yield bookmarkToJsonString bm |]   
        |> String.concat ",\n"
        |> sprintf "{ \"%s\" : [%s] }" name
