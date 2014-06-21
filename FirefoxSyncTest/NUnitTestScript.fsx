
open System
open System.Reflection

#I @"..\packages\NUnit.2.6.3\lib"
#r "nunit.framework.dll"
open NUnit
open NUnit.Framework
#I @"..\packages\NUnitTestAdapter.1.0\lib"
#r "nunit.core.dll"
#r "nunit.core.interfaces.dll"
open NUnit.Core

#I @"..\packages\FsCheck.0.9.4.0\lib\net40-Client\"
#r "FsCheck.dll"
open FsCheck


#r @".\bin\Debug\FirefoxSyncTest.dll"
open FirefoxSyncTest


// NUnit Runner 
 
// http://stackoverflow.com/questions/2798561/how-to-run-nunit-from-my-code

let binDebugDir = Environment.GetEnvironmentVariable("PROJECTS") + @"\FirefoxSync\FirefoxSyncTest\bin\Debug\"

type TestResultOrString = | TestResult of NUnit.Core.TestResult | String of string

// http://stackoverflow.com/questions/14340934/nunit-accessing-the-failure-message-in-teardown
// Handle the NUnit test events 
type EventListener() = 
    interface NUnit.Core.EventListener with
        member this.RunStarted (x,y) = printfn "%s %d" x y
        member this.RunFinished (x : NUnit.Core.TestResult) = printfn "%A" x.Results
        member this.RunFinished (x : Exception) = printfn "%s" x.Message
        member this.TestStarted x = printfn "%s" x.Name
        member this.TestFinished x = printfn "%A" x.ResultState
        member this.SuiteStarted x = printfn "%s" x.Name
        member this.SuiteFinished x = printfn "%A" x.ResultState
        member this.UnhandledException x = printfn "%s" x.Message
        member this.TestOutput x = printfn "%s" x.Text        

/// Run tests;
/// print the program's System.Console output
let simpleTestRunner () = 
    try 
        CoreExtensions.Host.InitializeService()
        let runner = new SimpleTestRunner()
        let package = new TestPackage ( binDebugDir + "FirefoxSyncTest.dll" )
        let loc = binDebugDir + "FirefoxSyncTest.dll" // Assembly.GetExecutingAssembly().Location
        runner.Load(package) |> ignore
        runner.Run( EventListener(), NUnit.Core.TestFilter.Empty, false, LoggingThreshold() )
        |> (TestResultOrString.TestResult)
    with | ex -> String ex.Message

let testResult = simpleTestRunner ()


/// Run tests;
/// (swallows the program's System.Console output; 
/// does not work with EventListener).
let remoteTestRunner () = 
    try 
        //CoreExtensions.Host.InitializeService() 
        let package = new TestPackage( binDebugDir + "FirefoxSyncTest.dll" )
        let remoteTestRunner = new RemoteTestRunner()
        remoteTestRunner.Load(package) |> ignore
        remoteTestRunner.Run( NullListener(), NUnit.Core.TestFilter.Empty, false, LoggingThreshold() )
        |> (TestResultOrString.TestResult)
    with | ex -> String(ex.Message )

let testResult' = remoteTestRunner ()

