namespace FirefoxSync

open System
open System.IO
open System.Security.Cryptography

open FSharp.Data
open FSharp.Data.JsonExtensions

open SecretStore
open Utilities
open ServerUrls


module CryptoKey =

    /// Return the Firefox Crypto Keys as a Result<string> (encrypted).
    let fetchCryptoKeys username password =
        let setClusterURL () = 
            match (clusterURL username) with
            | Success url -> Success (url + "1.1/" + username + "/storage/crypto/keys")
            | Failure f -> Failure ( ClusterUrlError((ErrorLabel) "Failed to fetch crypto keys", (Stacktrace) "") :: f ) 
        setClusterURL ()
        |> Results.bind (fetchUrlResponse "GET" (Some (username, password)) None None None)


    /// Fetch the remote Firefox Crypto Keys and write them to disk as Result<unit>.
    let writeCryptoKeysToDisk username password (file : string option) = 
        let file' = 
            match file with 
            | Some file -> file
            | _ -> defaultRemoteSecretFile
        let errLabel = (sprintf "Error while write secret keys to file '%s'" file')
        let secretKeys() = fetchCryptoKeys username password
        let write (secretKeys' : Result<string>) =
            match secretKeys' with
            | Success secretKeys -> 
                try
                    use stream = new StreamWriter(file', false)
                    stream.WriteLine(secretKeys)
                    stream.Close()
                    |> Success
                with | ex -> WriteFileError
                             |> Results.setError errLabel ex 
            | Failure f -> Failure ( WriteFileError((ErrorLabel) errLabel, (Stacktrace) "") :: f )
        secretKeys () 
        |> write        
    

    /// Read the prefetched remote Firefox Sync Crypto Keys as a Result<string> (encrypted) from file.
    let readRemoteSecretFile (file : string option) =  
        let file' = 
            match file with 
            | Some file -> file 
            | _ -> defaultRemoteSecretFile
        try
            file'
            |> File.ReadAllText
            |> Success
        with 
        | ex -> ReadFileError
                |> Results.setError (sprintf "Error while reading remote secrets from file '%s'" file') ex


    /// Return decrypted Firefox Sync Crypto Keys from encrypted string 'cryptoKeys' as a Result<string>.
    let decryptCryptoKeys (secrets : Secret) cryptoKeys = 
        try
            let ck = cryptoKeys |> JsonValue.Parse
            let ck_pl = (ck?payload).AsString() |> JsonValue.Parse

            // https://docs.services.mozilla.com/sync/storageformat5.html
            //
            //    ciphertext  = record.ciphertext
            //    iv          = record.iv
            //    record_hmac = record.hmac
            let record = { iv         = (ck_pl?IV).AsString()
                           ciphertext = (ck_pl?ciphertext).AsString()
                           hmac       = (ck_pl?hmac).AsString() }
            //
            //    encryption_key = bundle.encryption_key
            //    hmac_key       = bundle.hmac_key
            let bundle = secrets.encryptionpassphrase 
                         |> base32'8'9Decode 
                         |> Results.setOrFail
                         |> buildSyncKeyBundle secrets.username 
            //
            //    local_hmac = HMACSHA256(hmac_key, base64(ciphertext))
            let local_hmac = record.ciphertext 
                             |> Convert.FromBase64String 
                             |> (new HMACSHA256(bundle.hmac_key)).ComputeHash
            //
            //    if local_hmac != record_hmac:
            //      throw Error("HMAC verification failed.")
            //
            //    cleartext = AESDecrypt(ciphertext, encryption_key, iv)         
            record.iv 
            |> Convert.FromBase64String 
            |> DecryptAES record.ciphertext bundle.encryption_key 
            |> keepAsciiPrintableChars
            |> Success
        with
        | ex -> Results.setError "Error while decrypting crypto keys" ex DecryptCryptoKeysError
      
        
    /// Get Crypto Keys from string as Result<CryptoKeys>.
    let getCryptoKeysFromString secrets cryptokeys =
        try
            { ``default`` =
                cryptokeys
                |> decryptCryptoKeys secrets
                |> Results.setOrFail
                |> JsonValue.Parse 
                |> fun x -> (x.GetProperty "default").AsArray()
                |> Array.map (fun x -> x.AsString())
                |> Array.map Convert.FromBase64String }
            |> Success
        with 
        | ex -> GetCryptoKeysFromStringError
                |> Results.setError (sprintf "Error while 'string' to 'CryptoKeys' type conversion of '%s'" cryptokeys) ex

                
    /// Get remote Crypto Keys as Result<CryptoKeys>.
    let getCryptoKeys (secrets : Secret) =
        try
            secrets.password
            |> (fetchCryptoKeys secrets.username >> Results.bind (getCryptoKeysFromString secrets))
            |> Results.setOrFail  // enforce 'GetCryptoKeysError' on 'Failure' case
            |> Success
        with 
        | ex -> GetCryptoKeysError
                |> Results.setError "Failed to get remote crypto keys" ex


    /// Get prefetched remote Crypto Keys from file as Result<CryptoKeys>.    
    let getCryptokeysFromFile secrets file =
        try
            file
            |> (readRemoteSecretFile >> Results.bind (getCryptoKeysFromString secrets))
            |> Results.setOrFail  // enforce 'GetCryptoKeysFromFileError' on 'Failure' case
            |> Success
        with 
        | ex -> GetCryptoKeysFromFileError
                |> Results.setError "Failed to get remote crypto keys" ex


