
open System
open System.Reflection

#I @"..\packages\xunit.1.9.2\lib\net20\"
#r "xunit.dll"
open Xunit

#I @"..\packages\FsCheck.0.9.4.0\lib\net40-Client\"
#r "FsCheck.dll"
open FsCheck

#I @"..\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\"
#r "FsCheck.Xunit.dll"
open FsCheck.Xunit

// Load FirefoxSyncTest after xUnit libraries !
#r @".\bin\Debug\FirefoxSyncTest.dll"
open FirefoxSyncTest


//https://github.com/fsharp/FsCheck/blob/master/docs/Documentation.md#implementing-irunner-to-integrate-fscheck-with-mbxncsunit

let xUnitRunner =
    { new IRunner with
        member x.OnStartFixture t = ()
        member x.OnArguments (ntest,args, every) = ()
        member x.OnShrink(args, everyShrink) = ()
        member x.OnFinished(name,testResult) = 
            match testResult with 
            | TestResult.True _ -> Assert.True(true)
            | _ -> Assert.True(false, Runner.onFinishedToString name testResult) 
    }

let withxUnitConfig = { Config.Default with Runner = xUnitRunner }

// Run ad hoc defined test 
let Lazy a = a <> 0 ==> (lazy (1/a = 1/a))
Check.Quick Lazy

// Run tests of FirefoxSyncTest
Check.Quick ``square should be positive``
Check.Quick ``square should be positive failing``
