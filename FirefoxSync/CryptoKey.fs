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

    /// Return the Firefox Crypto Keys as a string (encrypted),
    /// return an empty string in case of error, log error messages to the console.
    let fetchCryptoKeys username password =
        let url = (clusterURL username) + "1.1/" + username + "/storage/crypto/keys"
        fetchUrlResponse url "GET" (Some (username, password)) None None None

    /// Fetch the remote Firefox Crypto Keys and write them to disk,
    /// throw an exception and/or log to console depending on error case.
    let writeCryptoKeysToDisk username password (file : string option) = 
        let secretKeys = fetchCryptoKeys username password
        let mutable file' = ""
        match file with 
        | Some file -> file' <- file
        | _ -> file' <- defaultRemoteSecretFile
        use stream = new StreamWriter(file', false)
        stream.WriteLine(secretKeys)
        stream.Close()
    
    /// Read the prefetched remote Firefox Crypto Keys as a string (encrypted) from disk,
    /// return an empty string on error.
    let readCryptoKeysFromDisk (file : string option) = 
        try 
            match file with 
            | Some file -> file |> File.ReadAllText
            | _ -> defaultRemoteSecretFile |> File.ReadAllText
        with | _ -> ""


    let decryptCryptoKeys (secrets : Secret) cryptoKeys = 
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
        let bundle = syncKeyBundle secrets.username (secrets.encryptionpassphrase |> base32'8'9Decode)
        //
        //    local_hmac = HMACSHA256(hmac_key, base64(ciphertext))
        let local_hmac = record.ciphertext |> Convert.FromBase64String |> (new HMACSHA256(bundle.hmac_key)).ComputeHash
        //
        //    if local_hmac != record_hmac:
        //      throw Error("HMAC verification failed.")
        //
        //    cleartext = AESDecrypt(ciphertext, encryption_key, iv)         
        record.iv 
        |> Convert.FromBase64String 
        |> DecryptAES record.ciphertext bundle.encryption_key 
        |> keepAsciiPrintableChars
        
    let getCryptoKeysFromString secrets cryptokeys =
        try
            { ``default`` =
                cryptokeys
                |> decryptCryptoKeys secrets
                |> JsonValue.Parse 
                |> fun x -> (x.GetProperty "default").AsArray()
                |> Array.map (fun x -> x.AsString())
                |> Array.map Convert.FromBase64String }
        with 
        | _ -> { ``default`` = [||] }
                

    let getCryptoKeys (secrets : Secret) =
        try
            fetchCryptoKeys secrets.username secrets.password
            |> getCryptoKeysFromString secrets
        with 
        | _ -> { ``default`` = [||] }

    
    let getCryptokeysFromDisk secrets file =
        try
            readCryptoKeysFromDisk file
            |> getCryptoKeysFromString secrets
        with 
        | _ -> { ``default`` = [||] }
