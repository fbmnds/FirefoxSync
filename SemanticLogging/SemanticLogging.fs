module SemanticLogging

open System.Diagnostics.Tracing
open System.IO
open System.IO.IsolatedStorage

open SemanticLoggingEventSource 

let getEventSource() = new FirefoxSyncTestEventSource()
let getLogMessage1(eventSource : FirefoxSyncTestEventSource) = eventSource.Message1 
let getLogMessage2(eventSource : FirefoxSyncTestEventSource) = eventSource.Message2

type FileStorageEventListener (location) =    
    inherit EventListener ()
    
    let stream = new StreamWriter(location, false)
    do stream.WriteLine ("-- Begin logging " + System.DateTime.Now.ToString() + " --")

    override x.OnEventWritten(eventData : EventWrittenEventArgs) = 
        stream.WriteLine(sprintf "Event %d " eventData.EventId)
        for o in eventData.Payload do
            stream.WriteLine(sprintf "payload:\t%A" o)        
            
    override x.Dispose() =
        stream.WriteLine ("-- End logging " + System.DateTime.Now.ToString() + " --")
        stream.Close()
        base.Dispose()

let getFileDebugListener (file : string) = new FileStorageEventListener ( file )


type IsolatedStorageEventListener (location) = 
    inherit EventListener ()
    
    let store = IsolatedStorageFile.GetUserStoreForApplication()
    let stream = new StreamWriter(store.OpenFile(location, FileMode.OpenOrCreate))

    override x.OnEventWritten(eventData : EventWrittenEventArgs) = 
            stream.WriteLine("Event {0} ", eventData.EventId)
            for o in eventData.Payload do
                stream.WriteLine("\t{0}", o)        
            
    override x.Dispose() =
            base.Dispose()
            store.Dispose()
            stream.Dispose()

