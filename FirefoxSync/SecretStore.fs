namespace FirefoxSync

// Firefox Sync Secrets
module SecretStore = 

    open System
    open System.IO

    open FSharp.Data
    open FSharp.Data.JsonExtensions  

    /// Default settings
    let defaultLocalSecretFile = 
        Environment.GetEnvironmentVariable("SECRETS") + @"\FirefoxSyncLocalSecret.json"
    let defaultRemoteSecretFile = 
        Environment.GetEnvironmentVariable("SECRETS") + @"\FirefoxSyncRemoteSecret.json"
    
    /// Read the local Firefox Sync Secrets from disk as Result<Secret>
    let setSecretByFile file =
        let mutable file' = ""
        try 
            match file with
            | Some file -> file' <- file
            | _ -> file' <- defaultLocalSecretFile
            file'
            |> File.ReadAllText
            |> JsonValue.Parse
            |> fun secrets' ->
                { email = (secrets'?email).AsString()
                  username = (secrets'?username).AsString()
                  password = (secrets'?password).AsString()
                  encryptionpassphrase = (secrets'?encryptionpassphrase).AsString() }
            |> Success
        with | ex -> Results.setError (sprintf "Read secret file '%s' failed" file') ex ReadSecretFileError 
     
    let setSecretByDefaultFile() = setSecretByFile None

