
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

//#I @"..\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\"
//#r "FsCheck.Xunit.dll"
//open FsCheck.Xunit


#r @".\bin\Debug\FirefoxSyncTest.dll"
open FirefoxSyncTest


// NUnit Runner 
 
// http://stackoverflow.com/questions/2798561/how-to-run-nunit-from-my-code

let binDebugDir = Environment.GetEnvironmentVariable("PROJECTS") + @"\FirefoxSync\FirefoxSyncTest\bin\Debug\"

type TestResultOrString = | TestResult of NUnit.Core.TestResult | String of string

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
let remoteTestRunner () = 
    try 
        // CoreExtensions.Host.InitializeService() 
        let package = new TestPackage( binDebugDir + "FirefoxSyncTest.dll" )
        let remoteTestRunner = new RemoteTestRunner()
        remoteTestRunner.Load(package) |> ignore
        TestResult ( remoteTestRunner.Run( NullListener(), NUnit.Core.TestFilter.Empty, false, LoggingThreshold() ) )
    with | ex -> String(ex.Message )

//let testResult' = remoteTestRunner ()

