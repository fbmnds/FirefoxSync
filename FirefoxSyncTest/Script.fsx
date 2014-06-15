#r @".\bin\Debug\FirefoxSyncTest.dll"

open FirefoxSyncTest

//#I @"..\packages\FSharp.Data.2.0.8\lib\net40"
#I @"..\packages\xunit.1.9.2\lib\net20\"
#r "xunit.dll"
open Xunit

#I @"..\packages\FsCheck.0.9.4.0\lib\net40-Client\"
#r "FsCheck.dll"
open FsCheck



#I @"..\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\"
#r "FsCheck.Xunit.dll"
open FsCheck.Xunit




////https://github.com/fsharp/FsCheck/blob/master/docs/Documentation.md#implementing-irunner-to-integrate-fscheck-with-mbxncsunit
//
//let xUnitRunner =
//    { new IRunner with
//        member x.OnStartFixture t = ()
//        member x.OnArguments (ntest,args, every) = ()
//        member x.OnShrink(args, everyShrink) = ()
//        member x.OnFinished(name,testResult) = 
//            match testResult with 
//            | TestResult.True _ -> Assert.True(true)
//            | _ -> Assert.True(false, Runner.onFinishedToString name testResult) 
//    }
//
//let withxUnitConfig = { Config.Default with Runner = xUnitRunner }


let runSingleTest (f : unit -> unit) =
    try f(); None with | ex -> sprintf "failed : %s" (ex.StackTrace.Split '\n').[0] |> Some

let runTestArray testArray = testArray |> Array.map runSingleTest

let printTestResult tests = 
    tests
    |> runTestArray
    |> Array.map (fun x -> match x with | Some x -> printfn "%s" x; false | _ -> true)
    |> Array.fold (fun (x,y,z) b -> if b then (x,y,z+1) else (b,y+1,z+1)) (true,0,0)
    |> fun (x,y,z)-> if x then printfn "tests ok" else printfn "%d test(s) of %d failed" y z
    

[| ``base32Decode (mozilla docs example)``  
   ``syncKeyBundle (mozilla docs example)`` 
   ``writeCryptoKeysToDisk`` 
   ``getRecordFields/getRecordField``
   ``fetchInfoCollection/Quota``
   ``Collection Bookmark (retrieve bookmarks)``
   ``Collection Bookmark (select children)``
   ``Collection Bookmark (select by id)``
   ``Collection Bookmark (select tags)``
   ``Collection MetaGlobal`` |]
|> printTestResult

