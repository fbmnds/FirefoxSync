namespace FirefoxSync

// Firefox Sync Secrets
module SecretStore = 

    open System
    open System.IO

    open FSharp.Data
    open FSharp.Data.JsonExtensions  


    let mutable defaultLocalSecretFile = 
        Environment.GetEnvironmentVariable("HOME") + @"\.ssh\ff-local-secret.json"
    let mutable defaultRemoteSecretFile = 
        Environment.GetEnvironmentVariable("HOME") + @"\.ssh\ff-remote-secret.json"
    
    /// Read the local Firefox Sync Secrets from disk,
    /// throw an exception on error cases.
    let readSecretFile file =
        let mutable file' = ""
        match file with
        | Some file -> file' <- file
        | _ -> file' <- defaultLocalSecretFile
        file'
        |> File.ReadAllText
        |> JsonValue.Parse
        
    let mutable secrets' = readSecretFile None

    let mutable secrets = 
        { email = (secrets'?email).AsString()
          username = (secrets'?username).AsString()
          password = (secrets'?password).AsString()
          encryptionpassphrase = (secrets'?encryptionpassphrase).AsString() }


