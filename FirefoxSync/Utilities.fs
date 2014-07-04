namespace FirefoxSync

open System
open System.IO
open System.Net
//open System.Text
open System.Security.Cryptography
open System.Text.RegularExpressions

open Microsoft.FSharp.Collections

open FSharp.Data
open FSharp.Data.JsonExtensions


module Utilities = 
    
    // JSON

    let tryGetString (jsonvalue : JsonValue) property = 
        try 
            property 
            |> jsonvalue.TryGetProperty 
            |> fun x -> match x with | Some x -> x.AsString() | _ -> ""
            |> fun x -> if x = null then "" else x
        with | _ -> ""

    let tryGetBoolean (jsonvalue : JsonValue) defaultboolean property  = 
        try
            property 
            |> jsonvalue.TryGetProperty 
            |> fun x -> match x with | Some x -> x.AsBoolean() | _ -> defaultboolean
        with | _ -> defaultboolean

    let tryGetArray (jsonvalue : JsonValue) property = 
        try
            property 
            |> jsonvalue.TryGetProperty 
            |> fun x -> match x with | Some x -> x.AsArray() | _ -> [||]
            |> fun x -> if x = null then [||] else x 
        with | _ -> [||]
    
    let tryGetInteger (jsonvalue : JsonValue) property = 
        try
            property 
            |> jsonvalue.TryGetProperty 
            |> fun x -> match x with | Some x -> Some (x.AsInteger()) | _ -> None 
        with | _ -> None

    // TODO: get rid of this
    let tryGetIntegerWithDefault (jsonvalue : JsonValue) defaultinteger property = 
        try
            property 
            |> jsonvalue.TryGetProperty 
            |> fun x -> match x with | Some x -> x.AsInteger() | _ -> defaultinteger 
        with | _ -> defaultinteger

    let private pat1 = '\\'.ToString() + '\\'.ToString()
    let private pat2 = '\\'.ToString() + '"'.ToString()

    /// Escape the equotation of '\' and '"'.
    /// Do not handle single quotes (ref. JSON.org string definition). 
    let escapeString (s : string) =
        s.ToCharArray()
        |> Array.Parallel.map (fun c -> match c with 
                                        | '\\' -> pat1
                                        | '"'  -> pat2
                                        | _    -> c.ToString())
        |> String.concat ""
    
    /// Revert the escape quotation of '\' and '"';
    /// return the mismatching result, if the input string was not correctly escaped.
    /// Do not handle single quotes (ref. JSON.org string definition).
    let relaxedUnescapeString (x : string) =

        let rec revertEscape (s : string) acc =
            let concat s s' = sprintf "%s%s" s s'
            let update s acc = (acc |> revertEscape s) |> concat acc
            if s.Length < 2 then s |> concat acc
            else          
                let (s',s'') = (s.[0..1],s.[2..s.Length-1])
                if   s' = pat1 then ('\\'.ToString())  |> concat acc |> revertEscape s''
                elif s' = pat2 then ('"'.ToString())   |> concat acc |> revertEscape s''
                else                (s.[0].ToString()) |> concat acc |> revertEscape s.[1..s.Length-1]
        revertEscape x ""

    /// Revert the escape quotation of '\' and '"' as Result<string> 
    /// for correctly escaped input strings.
    /// Do not handle single quotes (ref. JSON.org string definition).
    let unescapeString (x : string) =
        let res = relaxedUnescapeString x 
        if x = escapeString res then Success res
        else [ UnescapeJsonStringError((ErrorLabel (sprintf "Failed to unescape JSON-string '%s'" x)), 
                                       (Stacktrace) (sprintf "Invalid unescaped JSON-string result '%s'" res)) ]
             |> Failure
         

    // Windows Registry

    let getIExplorerFavoritesFolder () =
        let x = Microsoft.Win32.Registry.CurrentUser.OpenSubKey "Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
        try
            (x.GetValue ("Favorites")).ToString() |> Success
        with 
        | exn -> Results.setError "" exn InternetExplorerFavoritesRegistryError


    // Misc.

    let inline padArray len (c : 'T) (b : 'T[])  =
        [| for i in [0 .. len-1] do if i < b.Length then yield b.[i] else yield c |]

    let stringToBytes (s : string) = s.ToCharArray() |> Array.Parallel.map (fun x -> (byte) x)
    let bytesToString (b : byte[]) = b |> Array.Parallel.map (char) |> fun cs -> new string(cs)
    let bytesToHex (b : byte[]) = b |> Array.Parallel.map (sprintf "%x")

    let inline isSubset set subset = 
        let set' = set |> Set.ofSeq
        let mutable res = 1
        for c in subset do
            if set'.Contains c then res <- 1 * res else res <- 0 * res  
        if res = 1 then true else false

    let removeChars (chars : string) (x : string) = 
        let set = chars.ToCharArray() |> Set.ofArray
        x.ToCharArray() 
        |> Array.filter (fun x -> if set.Contains x then false else true) 
        |> fun x -> new string (x)

    let keepAsciiPrintableChars (x : string) =
        x.ToCharArray() 
        |> Array.filter (fun x -> if int x < 32 || int x > 126 then false else true) 
        |> fun x -> new string (x)

    let private writeStringToFile' append LF file (text : string) =
        let errLabel = (sprintf "Error while writing text to file '%s'" file)
        try
            use stream = new StreamWriter(file, append)
            stream.WriteLine(text)
            if LF then stream.WriteLine()
            stream.Close()
            |> Success
        with | ex -> WriteFileError
                     |> Results.setError errLabel ex

    let writeStringToFile     append file text = writeStringToFile' append false file text
    let writeLineStringToFile append file text = writeStringToFile' append true  file text

    let tryCreateDirectory path =
        try 
            if Directory.Exists path then Success path
            else
                path
                |> Directory.CreateDirectory 
                |> fun x -> Success path
        with
        | exn -> CreateDirectoryError 
                    |> Results.setError (sprintf "Failed at path '%s'" path) exn 

    // http://www.fssnip.net/3y
    let getRecordFields (r: 'record) =
        typeof<'record> |> Microsoft.FSharp.Reflection.FSharpType.GetRecordFields 
        
    let getRecordField (r: 'record) (field : Reflection.PropertyInfo) =
        Microsoft.FSharp.Reflection.FSharpValue.GetRecordField(r,field) |> unbox
       
    // Encoding schemes

    let random = new Random()
    
    let base32Chars     = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
    let base32'8'9Chars = "ABCDEFGHIJK8MN9PQRSTUVWXYZ234567"
    let base64Chars     = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890+/"
    let base64urlChars  = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_"

    let doBase32'8'9 (s : string) = 
        if s = "" then ""
        else
            s.ToUpper().ToCharArray() 
            |> Array.Parallel.map (fun x -> match x with | 'L' -> '8' | 'O' -> '9' | _ -> x )
            |> fun cs -> new string(cs)

    let undoBase32'8'9 (s : string) = 
        if s = "" then ""
        else
            s.ToUpper().ToCharArray() 
            |> Array.Parallel.map (fun x -> match x with | '8' -> 'L' | '9' -> 'O' | _ -> x )
            |> fun cs -> new string(cs)
    
    let generateWeaveGUID() = 
        [| for i in [0 .. 11] do yield base64urlChars.Substring(random.Next(63), 1) |]
        |> Array.fold (fun r s -> r + s) ""
        |> (WeaveGUID)


    // https://bitbucket.org/devinmartin/base32/src/90d7d530beea52a2a82b187728a06404794600b9/Base32/Base32Encoder.cs?at=default
    let base32Decode (s' : string) = 
        if s' = "" then [||] |> Success
        else
            let s = 
                s'.ToUpper().ToCharArray() 
                |> Array.filter (fun x -> if x = '=' then false else true) 
                |> fun cs -> new string(cs)        
            let encodedBitCount = 5
            let byteBitCount = 8
            if isSubset base32Chars s then       
                try
                    let outputBuffer = Array.create (s.Length * encodedBitCount / byteBitCount) 0uy
                    let mutable workingByte = 0uy
                    let mutable bitsRemaining = byteBitCount
                    let mutable mask = 0
                    let mutable arrayIndex = 0
                    for c in s.ToCharArray() do 
                        let value = base32Chars.IndexOf c
                        if bitsRemaining > encodedBitCount then
                            mask <- value <<< (bitsRemaining - encodedBitCount)
                            workingByte <- (workingByte ||| (byte) mask)
                            bitsRemaining <- bitsRemaining - encodedBitCount
                        else
                            mask <- value >>> (encodedBitCount - bitsRemaining)
                            workingByte <- (workingByte ||| (byte) mask)
                            outputBuffer.[arrayIndex] <- workingByte
                            arrayIndex <- arrayIndex + 1
                            workingByte <- (byte)(value <<< (byteBitCount - encodedBitCount + bitsRemaining))
                            bitsRemaining <- bitsRemaining + byteBitCount - encodedBitCount
                    outputBuffer 
                    |> Success
                with
                | ex -> Base32DecodeError
                        |> Results.setError (sprintf "Unspecified Base32 decode error in '%s'" s') ex
            else
                Failure [ Base32DecodeError((ErrorLabel) (sprintf "Invalid Base32 character in '%s'" s'), (Stacktrace) "") ]

    let base32'8'9Decode (s' : string) = s' |> undoBase32'8'9 |> base32Decode
     

    // Cryptography, Hashes

    // E:\projects\fs-random-snippets>"%HOME%\Documents\Visual Studio 2012\Projects\Tutorial3\.nuget\nuget" PBKDF2.NET
    // Installing 'PBKDF2.NET 2.0.0'.
    // Successfully installed 'PBKDF2.NET 2.0.0'.
    // #r @"PBKDF2.NET.2.0.0\lib\net45\PBKDF2.NET.dll"

    // https://github.com/crowleym/HKDF
    // #r @"HKDF\RFC5869.dll"
    // open RFC5869


    let buildSyncKeyBundle username key =
        let info = "Sync-AES_256_CBC-HMAC256" + username
        let hmac256 = new HMACSHA256(key)
        let T1 = hmac256.ComputeHash (Array.append (info |> stringToBytes ) [| 1uy |] )
        let T2 = hmac256.ComputeHash (Array.append T1 <| Array.append (info |> stringToBytes) [| 2uy |])   
        { encryption_key = T1 ; hmac_key = T2 }


    // http://msdn.microsoft.com/en-us/library/system.security.cryptography.aescryptoserviceprovider%28v=vs.110%29.aspx
    let DecryptAES (s : string) (key : byte[]) (iv : byte[]) =
        // Check arguments.
        if s.Length * key.Length * iv.Length = 0 then ""
        else
            // Create an AesCryptoServiceProvider object 
            // with the specified key and IV. 
            use aesAlg = new AesManaged()

            aesAlg.Key <- key
            aesAlg.IV <- iv
            aesAlg.Padding <- PaddingMode.Zeros

            // Create a decrytor to perform the stream transform.
            use decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV)

            // Create the streams used for decryption. 
            use msDecrypt = new MemoryStream(Convert.FromBase64String(s)) 
            use csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)
            use srDecrypt = new StreamReader(csDecrypt)
        
            // Read the decrypted bytes from the decrypting stream 
            // and place them in a string.
            let plaintext = srDecrypt.ReadToEnd()
            plaintext

    
    // Net Utilities 

    /// Return the server response as a Result<string>
    let fetchUrlResponse requestMethod 
                         (credentials: (string*string) option)
                         (data : byte[] option) (contentType : string option) 
                         timeout 
                         (url : string) =    
        let getRequest () =
            try
                let req = WebRequest.Create(url)
                Success req
            with 
            | ex -> Failure [ InvalidUrl ]
        let setCredentials (reqResult : Result<WebRequest>) =     
            match reqResult, credentials with
            | (Failure x, _) -> Failure x
            | (Success req, Some(username, password)) -> req.Credentials <- new NetworkCredential(username,password); Success (req)
            | (Success req, _) -> Success req
        let setMethod (reqResult : Result<WebRequest>) =     
            match reqResult with
            | Failure req -> Failure req
            | Success req -> req.Method <- requestMethod; Success req
        let sendData (reqResult : Result<WebRequest>) =     
            match reqResult with
            | Failure req -> Failure req
            | Success req -> 
                match data, contentType with
                | Some data, Some contentType -> req.ContentType <- contentType; req.ContentLength <- (int64) data.Length
                | Some data, _ -> req.ContentLength <- (int64) data.Length
                | _ -> ignore data
                match data with 
                | Some data -> 
                    try
                        use wstream = req.GetRequestStream() 
                        wstream.Write(data , 0, (data.Length))
                        wstream.Flush()
                        wstream.Close()
                        Success req
                    with
                    | ex -> Results.setError "Send data error" ex SendDataError
                | _ -> Success req
        let getResponse (reqResult : Result<WebRequest>) =     
            match reqResult with
            | Failure req -> Failure req
            | Success req ->
                try
                    match timeout with
                    | Some timeout -> req.Timeout <- timeout
                    | _ -> req.Timeout <- 9 * 60 * 1000
                    use resp = req.GetResponse()
                    use strm = resp.GetResponseStream()
                    let text = (new StreamReader(strm)).ReadToEnd()
                    Success text
                with
                | :? WebException as ex -> 
                    try
                        use stream = ex.Response.GetResponseStream()
                        use  reader = new StreamReader(stream)
                        Failure [ GetResponseError((ErrorLabel) "Received error response stream", reader.ReadToEnd() |> (Stacktrace))  ]
                    with 
                    | _ -> Failure [ GetResponseError((ErrorLabel) "Received empty response stream", (Stacktrace) "") ]
                | _ as ex -> Results.setError "Get response error" ex GetResponseError
        getRequest
        >> setCredentials 
        >> setMethod
        >> sendData
        >> getResponse
        <| ()


    // Logging

    /// ILogger log-to-console implementation
    type ConsoleLogger() =

        let printBaseType (x: LogMessageBaseType) =
            match x with
            | Integer x -> sprintf "%d" x
            | Integer64 x  -> sprintf "%d" x
            | Float x -> sprintf "%f" x
            | String x -> sprintf "%s" x
        
        let printSeq format parms =
            let x = parms |> Seq.map printBaseType |> List.ofSeq
            // standard behavior, i.e. do not consider escaped string format ("\%s")
            let words = Regex.Split(format, "%s")
            if words.Length <> x.Length + 1 then
                printfn "Log message format error:\nformat : %s\nparameter : %A" format parms
            else
                x @ [""] |> Seq.zip words |> Seq.map (fun (x,y) -> x+y) |> (String.concat "") |> (printfn "%s")

        interface ILogger with
            member x.Log format parms = printSeq format parms
            member x.Dispose() = ()


    /// ILogger do-not-log implementation
    type PseudoLogger() =
        interface ILogger with
            member x.Log format parms = ()
            member x.Dispose() = ()
