using System;
using System.IO;
using Microsoft.Diagnostics.Tracing;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Linq;


namespace SemanticLogging
{

    [EventSource(Name = "FirefoxSync-EventLog")]
    public sealed class FirefoxSyncEventSource : EventSource
    {
        #region Singleton instance
        static public FirefoxSyncEventSource Log = new FirefoxSyncEventSource();
        #endregion

        [Event(1, Keywords = Keywords.Requests, Message = "Start processing request\n\t*** {0} ***\nfor URL\n\t=== {1} ===",
            Channel = EventChannel.Admin, Task = Tasks.Request, Opcode = EventOpcode.Start)]
        public void RequestStart(int RequestID, string Url) { WriteEvent(1, RequestID, Url); }

        [Event(2, Keywords = Keywords.Requests, Message = "Entering Phase {1} for request {0}",
            Channel = EventChannel.Analytic, Task = Tasks.Request, Opcode = EventOpcode.Info, Level = EventLevel.Verbose)]
        public void RequestPhase(int RequestID, string PhaseName) { WriteEvent(2, RequestID, PhaseName); }

        [Event(3, Keywords = Keywords.Requests, Message = "Stop processing request\n\t*** {0} ***",
            Channel = EventChannel.Admin, Task = Tasks.Request, Opcode = EventOpcode.Stop)]
        public void RequestStop(int RequestID) { WriteEvent(3, RequestID); }

        [Event(4, Keywords = Keywords.Requests, Message = "DebugMessage: {0}", Channel = EventChannel.Admin)]
        public void DebugTrace(string Message) { WriteEvent(4, Message); }

        #region Keywords / Tasks / Opcodes

        public class Keywords   // This is a bitvector
        {
            public const EventKeywords Requests = (EventKeywords)0x0001;
            public const EventKeywords Debug = (EventKeywords)0x0002;
        }

        public class Tasks
        {
            public const EventTask Request = (EventTask)0x1;
        }

        #endregion
    }

   

    public class FirefoxSyncEventSourceManager
    {
        

        static string DeploymentFolder { get; set; }

        /// <summary>
        /// This is a demo of using ChannelEventSource.  
        /// </summary>
        public static void Start()
        {

            Console.WriteLine("******************** EventLogEventSource Demo ********************");

            // Deploy the app
            DeploymentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"EventSourceSamples");
            RegisterEventSourceWithOperatingSystem.SimulateInstall(DeploymentFolder, FirefoxSyncEventSource.Log.Name);

            // Let the user inspect that results
            Console.WriteLine("Launching 'start eventvwr' to view the newly generated events.");
            Console.WriteLine("Look in 'Application and Services Logs/Samples/EventSourceDemos/EventLog'");
            Console.WriteLine("Close the event viewer when complete.");
            Process.Start(new ProcessStartInfo("eventvwr") { UseShellExecute = true }).WaitForExit();
            //Console.WriteLine("Press <Enter> to continue.");
            //Console.ReadLine();
            
          
        }


        public static void Stop()
        {

            DeploymentFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"EventSourceSamples");
            // Uninstall.  
            RegisterEventSourceWithOperatingSystem.SimulateUninstall(DeploymentFolder);
            Console.WriteLine();
            Console.Out.Close();
            
        }

        public static void GenerateEvents()
        {
            var requests = new string[] {
                "/home/index.aspx",
                "/home/catalog/index.aspx",
                "/home/catalog/100",
                "/home/catalog/121",
                "/home/catalog/144",
            };

            Console.WriteLine("Writing some events to the EventSource.");
            int id = 0;
            foreach (var req in requests)
                DoRequest(req, ++id);
            Console.WriteLine("Done writing events.");
        }

        private static void DoRequest(string request, int requestId)
        {
            FirefoxSyncEventSource.Log.RequestStart(requestId, request);

            foreach (var phase in new string[] { "initialize", "query_db", "query_webservice", "process_results", "send_results" })
            {
                FirefoxSyncEventSource.Log.RequestPhase(requestId, phase);
                // simulate error on request for "/home/catalog/121"
                if (request == "/home/catalog/121" && phase == "query_db")
                {
                    FirefoxSyncEventSource.Log.DebugTrace("Error on page: " + request);
                    break;
                }
            }

            FirefoxSyncEventSource.Log.RequestStop(requestId);
        }

    }

    /// <summary>
    /// For the Windows EventLog to listen for EventSources, they must be
    /// registered with the operating system.  This is a deployment step 
    /// (typically done by a installer).   For demo purposes, however we 
    /// have written code run by the demo itself that accomplishes this 
    /// </summary>
    static class RegisterEventSourceWithOperatingSystem
    {
        

        /// <summary>
        /// Simulate an installation to 'destFolder' for the named eventSource.  If you don't
        /// specify eventSourceName all eventSources information next to the EXE is registered.
        /// </summary>
        public static void SimulateInstall(string destFolder, string eventSourceName = "", bool prompt = false)
        {
 
            Console.WriteLine("Simulating the steps needed to register the EventSource with the OS");
            Console.WriteLine("These steps are only needed for Windows Event Log support.");
            Console.WriteLine("Admin privileges are needed to do this, so you will see elevation prompts");
            Console.WriteLine("If you are not already elevated.  Consider running from an admin window.");
            Console.WriteLine();

            if (prompt)
            {
                Console.WriteLine("Press <Enter> to proceed with installation");
                Console.ReadLine();
            }

            Console.WriteLine("Deploying EventSource to {0}", destFolder);
            // create deployment folder if needed
            if (Directory.Exists(destFolder))
            {
                Console.WriteLine("Error: detected a previous deployment.   Cleaning it up.");
                SimulateUninstall(destFolder, false);
                Console.WriteLine("Done Cleaning up orphaned installation.");
            }

            Console.WriteLine("Copying the EventSource manifest and compiled Manifest DLL to target directory.");
            Directory.CreateDirectory(destFolder);
            var sourceFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var filename in Directory.GetFiles(sourceFolder, "*" + eventSourceName + "*.etwManifest.???"))
            {
                var destPath = Path.Combine(destFolder, Path.GetFileName(filename));
                Console.WriteLine("xcopy \"{0}\" \"{1}\"", filename, destPath);
                File.Copy(filename, destPath, true);
            }

            Console.WriteLine("Registering the manifest with the OS (Need to be elevated)");
            foreach (var filename in Directory.GetFiles(destFolder, "*.etwManifest.man"))
            {
                var commandArgs = string.Format("im {0} /rf:\"{1}\" /mf:\"{1}\"",
                    filename,
                    Path.Combine(destFolder, Path.GetFileNameWithoutExtension(filename) + ".dll"));

                // as a precaution uninstall the manifest.   It is easy for the demos to not be cleaned up 
                // and the install will fail if the EventSource is already registered.   
                Process.Start(new ProcessStartInfo("wevtutil.exe", "um" + commandArgs.Substring(2)) { Verb = "runAs" }).WaitForExit();
                Thread.Sleep(200);          // just in case elevation makes the wait not work.  

                Console.WriteLine("  wevtutil " + commandArgs);
                // The 'RunAs' indicates it needs to be elevated. 
                // Unfortunately this also makes it problematic to get the output or error code.  
                Process.Start(new ProcessStartInfo("wevtutil.exe", commandArgs) { Verb = "runAs" }).WaitForExit();
            }

            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Done deploying app.");
            Console.WriteLine();
            
        }

        /// <summary>
        /// Reverses the Install step 
        /// </summary>
        public static void SimulateUninstall(string destFolder, bool prompt = false)
        {
            
            Console.WriteLine("Uninstalling the EventSoure demos from {0}", destFolder);
            Console.WriteLine("This also requires elevation.");
            Console.WriteLine("Please close the event viewer if you have not already done so!");

            if (prompt)
            {
                Console.WriteLine("Press <Enter> to proceed with uninstall.");
                Console.ReadLine();
            }

            // run wevtutil elevated to unregister the ETW manifests
            Console.WriteLine("Unregistering manifests");
            foreach (var filename in Directory.GetFiles(destFolder, "*.etwManifest.man"))
            {
                var commandArgs = string.Format("um {0}", filename);
                Console.WriteLine("    wevtutil " + commandArgs);
                // The 'RunAs' indicates it needs to be elevated.  
                var process = Process.Start(new ProcessStartInfo("wevtutil.exe", commandArgs) { Verb = "runAs" });
                process.WaitForExit();
            }

            Console.WriteLine("Removing {0}", destFolder);
            // If this fails, it means that something is using the directory.  Typically this is an eventViewer or 
            // a command prompt in that directory or visual studio.    If all else fails, rebooting should fix this.  
            if (Directory.Exists(destFolder))
                Directory.Delete(destFolder, true);
            Console.WriteLine("Done uninstalling app.");
        }
        
    }

    public class ConsoleEventListener : EventListener
    {
        static TextWriter Out = Console.Out;

        /// <summary>
        /// Override this method to get a list of all the eventSources that exist.  
        /// </summary>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            // Because we want to turn on every EventSource, we subscribe to a callback that triggers
            // when new EventSources are created.  It is also fired when the EventListner is created
            // for all pre-existing EventSources.  Thus this callback get called once for every 
            // EventSource regardless of the order of EventSource and EventListener creation.  

            // For any EventSource we learn about, turn it on.   
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
        }

        /// <summary>
        /// We override this method to get a callback on every event we subscribed to with EnableEvents
        /// </summary>
        /// <param name="eventData"></param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // report all event information
            Out.Write("  Event {0} ", eventData.EventName);

            // Don't display activity information, as that's not used in the demos
            // Out.Write(" (activity {0}{1}) ", ShortGuid(eventData.ActivityId), 
            //                                  eventData.RelatedActivityId != Guid.Empty ? "->" + ShortGuid(eventData.RelatedActivityId) : "");

            // Events can have formatting strings 'the Message property on the 'Event' attribute.  
            // If the event has a formatted message, print that, otherwise print out argument values.  
            if (eventData.Message != null)
                Console.WriteLine(eventData.Message, eventData.Payload.ToArray());
            else
            {
                string[] sargs = eventData.Payload != null ? eventData.Payload.Select(o => o.ToString()).ToArray() : null;
                Console.WriteLine("({0}).", sargs != null ? string.Join(", ", sargs) : "");
            }
        }

        #region Private members

        private static string ShortGuid(Guid guid)
        { return guid.ToString().Substring(0, 8); }

        #endregion
    }


}
