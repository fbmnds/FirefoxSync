open System
open System.IO
open System.Reflection
open System.Diagnostics
open System.Threading

open SemanticLogging


let uninstall() =
    0


let install configuration =
    let destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"FirefoxSync");
    let eventSourceName  = FirefoxSyncEventSource.Log.Name
    

//    Console.WriteLine("Simulating the steps needed to register the EventSource with the OS");
//    Console.WriteLine("These steps are only needed for Windows Event Log support.");
//    Console.WriteLine("Admin privileges are needed to do this, so you will see elevation prompts");
//    Console.WriteLine("If you are not already elevated.  Consider running from an admin window.");
//    Console.WriteLine();
//
//    if (prompt)
//    {
//        Console.WriteLine("Press <Enter> to proceed with installation");
//        Console.ReadLine();
//    }
//
//    Console.WriteLine("Deploying EventSource to {0}", destFolder);
//    // create deployment folder if needed
//    if (Directory.Exists(destFolder))
//    {
//        Console.WriteLine("Error: detected a previous deployment.   Cleaning it up.");
//        SimulateUninstall(destFolder, false);
//        Console.WriteLine("Done Cleaning up orphaned installation.");
//    }

    if (Directory.Exists(destFolder)) then uninstall() |> ignore
    
//
//    Console.WriteLine("Copying the EventSource manifest and compiled Manifest DLL to target directory.");

    Directory.CreateDirectory(destFolder) |> ignore

    // let sourceFolder' = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    let sourceFolder = 
        Environment.GetEnvironmentVariable("PROJECTS") + @"\FirefoxSync\SemanticLogging\bin\" + configuration

//    foreach (var filename in Directory.GetFiles(sourceFolder, "*" + eventSourceName + "*.etwManifest.???"))
//    {
//        var destPath = Path.Combine(destFolder, Path.GetFileName(filename));
//        Console.WriteLine("xcopy \"{0}\" \"{1}\"", filename, destPath);
//        File.Copy(filename, destPath, true);
//    }   
    
    for file in Directory.GetFiles( sourceFolder, "*" + eventSourceName + "*.etwManifest.???") do
        let destPath = Path.Combine(destFolder, Path.GetFileName(file))
        File.Copy(file, destPath, true);

//    Console.WriteLine("Registering the manifest with the OS (Need to be elevated)");
//    foreach (var filename in Directory.GetFiles(destFolder, "*.etwManifest.man"))
//    {
//        var commandArgs = string.Format("im {0} /rf:\"{1}\" /mf:\"{1}\"",
//            filename,
//            Path.Combine(destFolder, Path.GetFileNameWithoutExtension(filename) + ".dll"));
//
//        // as a precaution uninstall the manifest.   It is easy for the demos to not be cleaned up 
//        // and the install will fail if the EventSource is already registered.   
//        Process.Start(new ProcessStartInfo("wevtutil.exe", "um" + commandArgs.Substring(2)) { Verb = "runAs" }).WaitForExit();
//        Thread.Sleep(200);          // just in case elevation makes the wait not work. 
//
//        Console.WriteLine("  wevtutil " + commandArgs);
//        // The 'RunAs' indicates it needs to be elevated. 
//        // Unfortunately this also makes it problematic to get the output or error code.  
//        Process.Start(new ProcessStartInfo("wevtutil.exe", commandArgs) { Verb = "runAs" }).WaitForExit();

    for file in Directory.GetFiles (destFolder,"*.etwManifest.man") do
        let commandArgs = sprintf "im {0} /rf:\"%s\" /mf:\"%s\"" file (Path.GetFileNameWithoutExtension(file) + ".dll")
        
        let startInfoUninstall = new ProcessStartInfo("wevtutil.exe", "um" + commandArgs.Substring(2))
        startInfoUninstall.Verb <- "runAs"
        (Process.Start(startInfoUninstall)).WaitForExit()
        Thread.Sleep(200)
        
        let startInfoInstall = new ProcessStartInfo("wevtutil.exe", commandArgs)
        startInfoInstall.Verb <- "runAs"
        (Process.Start(startInfoInstall)).WaitForExit()
        
//

//    }
//
//    System.Threading.Thread.Sleep(1000);
//    Console.WriteLine("Done deploying app.");
//    Console.WriteLine();

    Thread.Sleep(1000)

    0

let error() =
    -1

[<EntryPoint>]
let main argv = 
    
    if argv.Length > 0 then
        match argv.[0].ToLower() with 
        | "--install-release" -> install "Release"
        | "--install-debug" -> install "Debug"
        | "--uninstall" -> uninstall()
        | _ -> error()
    else 
        error() 
     