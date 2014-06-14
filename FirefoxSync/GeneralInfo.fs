namespace FirefoxSync

open FSharp.Data
open FSharp.Data.JsonExtensions

open Utilities
open ServerUrls


module GeneralInfo =  

    let fetchInfoCollections username password =
        let url = (clusterURL username) + "1.1/" + username + "/info/collections"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let fetchInfoQuota username password =
        let url = (clusterURL username) + "1.1/" + username + "/info/quota"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let fetchInfoCollectionUsage username password =
        let url = (clusterURL username) + "1.1/" + username + "/info/collection_usage"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let fetchInfoCollectionCounts username password =
        let url = (clusterURL username) + "1.1/" + username + "/info/collection_counts"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let deleteStorage username password =
        let url = (clusterURL username) + "1.1/" + username
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let fetchMetaGlobal username password =
        let url = (clusterURL username) + "1.1/" + username + "/storage/meta/global"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    let getMetaGlobal (secrets : Secret) =
        let parseMetaGlobalPayload p =
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
        let parseMetaGlobal mg = 
            { username = "username" |> tryGetString mg
              payload  = "payload" |> tryGetString mg |> parseMetaGlobalPayload
              id       = "id" |> tryGetString mg
              modified = "modified" |> tryGetString mg |> (float) }
        try 
            fetchMetaGlobal secrets.username secrets.password
            |> JsonValue.Parse
            |> parseMetaGlobal
            |> Some
        with | _ -> None

