
open System
open System.Reflection

#r @".\bin\Debug\FirefoxSyncTest.dll"
open FirefoxSyncTest

#I @"..\packages\NUnit.2.6.3\lib"
#r "nunit.framework.dll"
open NUnit
open NUnit.Framework
#I @"..\packages\NUnitTestAdapter.1.0\lib"
#r "nunit.core.dll"
#r "nunit.core.interfaces.dll"
open NUnit.Core

//#I @"..\packages\xunit.1.9.2\lib\net20\"
//#r "xunit.dll"
//open Xunit

#I @"..\packages\FsCheck.0.9.4.0\lib\net40-Client\"
#r "FsCheck.dll"
open FsCheck

#I @"..\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\"
#r "FsCheck.Xunit.dll"
open FsCheck.Xunit


//// ... this is for xUnit
//
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


// NUnit Runner 
 
// http://stackoverflow.com/questions/2798561/how-to-run-nunit-from-my-code

let binDebugDir = Environment.GetEnvironmentVariable("PROJECTS") + @"\FirefoxSync\FirefoxSyncTest\bin\Debug\"

type ScriptTestResult = | TestResult of NUnit.Core.TestResult | String of string

/// Run tests;
/// print the program's System.Console output
let simpleTestRunner () = 
    try 
        CoreExtensions.Host.InitializeService()
        let runner = new SimpleTestRunner()
        let package = new TestPackage ( binDebugDir + "FirefoxSyncTest.dll" )
        let loc = binDebugDir + "FirefoxSyncTest.dll" // Assembly.GetExecutingAssembly().Location
        runner.Load(package) |> ignore
        runner.Run( NullListener(), NUnit.Core.TestFilter.Empty, false, LoggingThreshold() )
        |> (TestResult)
    with | ex -> String ex.Message

let testResult = simpleTestRunner ()

/// Run tests;
/// swallow the program's System.Console output; 
/// otherwise apparently equivalent NUnit.Core.TestResult as 'simpleTestRunner'
let remoteTestRunner () : ScriptTestResult = 
    try 
        // CoreExtensions.Host.InitializeService() 
        let package = new TestPackage( binDebugDir + "FirefoxSyncTest.dll" )
        let remoteTestRunner = new RemoteTestRunner()
        remoteTestRunner.Load(package) |> ignore
        TestResult ( remoteTestRunner.Run( NullListener(), NUnit.Core.TestFilter.Empty, false, LoggingThreshold() ) )
    with | ex -> String(ex.Message )

//let testResult' = remoteTestRunner ()

