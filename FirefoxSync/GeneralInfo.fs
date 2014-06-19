namespace FirefoxSync

open FSharp.Data
open FSharp.Data.JsonExtensions

open Utilities
open ServerUrls


module GeneralInfo =  

    let setClusterURL username collection = 
        match (clusterURL username) with
        | Success url -> Success (url + "1.1/" + username + collection)
        | Failure f -> Failure ( ClusterUrlError((ErrorLabel) (sprintf "Failed to cluster url for collection '%s'" collection), (Stacktrace) "") :: f ) 

    let fetchCollections username password collection =
        setClusterURL username collection
        |> Results.bind (fetchUrlResponse "GET" (Some (username, password)) None None None)

    let fetchInfoCollections username password = fetchCollections username password "/info/collections"
        
    let fetchInfoQuota username password = fetchCollections username password "/info/quota"

    let fetchInfoCollectionUsage username password = fetchCollections username password "/info/collection_usage"

    let fetchInfoCollectionCounts username password = fetchCollections username password "/info/collection_counts"

//    let deleteStorage username password =
//        let url = (clusterURL username) + "1.1/" + username
//        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let fetchMetaGlobal username password = fetchCollections username password "/storage/meta/global"

    let getMetaGlobal (secrets : Secret) =
        let parseMetaGlobalPayload p =
            try 
                let p' = JsonValue.Parse p
                { syncID         = "syncID" |> tryGetString p' |> (WeaveGUID)
                  storageVersion = "storageVersion" |> tryGetIntegerWithDefault p' -99 
                  engines        = "engines" 
                                   |> p'.GetProperty 
                                   |> fun x -> x.Properties 
                                   |> Seq.map (fun (x,y) -> 
                                                   let v = "version" |> tryGetIntegerWithDefault y -99 
                                                   let s = "syncID"  |> tryGetString y |> (WeaveGUID)
                                                   ((Engine) x, { version = v; syncID = s } ))
                                   |> Map.ofSeq
                  declined       = "declined" |> tryGetArray p' |> Array.map (fun x -> x.AsString() |> (Engine)) }    
                |> Success
            with
            | ex -> ParseMetaGlobalPayloadError
                    |> Results.setError (sprintf "Parse error on '/storage/meta/global' payload '%s'" p) ex    
        let parseMetaGlobal mg = 
            try 
                { username = "username" |> tryGetString mg
                  payload  = "payload"  |> tryGetString mg |> parseMetaGlobalPayload |> Results.setOrFail
                  id       = "id"       |> tryGetString mg
                  modified = "modified" |> tryGetString mg |> (float) }
                |> Success
            with 
            | ex -> ParseMetaGlobalError
                    |> Results.setError (sprintf "Parse error on '/storage/meta/global' in '%s'" (mg.ToString())) ex 
        fetchMetaGlobal secrets.username secrets.password
        |> Results.setOrFail
        |> JsonValue.Parse
        |> parseMetaGlobal


