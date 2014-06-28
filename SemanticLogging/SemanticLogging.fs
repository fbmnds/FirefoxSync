module SemanticLogging

open Microsoft.Diagnostics.Tracing
open Microsoft.FSharp.Core.LanguagePrimitives
 
/// Simple enum wrapper for possible Task values
module Tasks =
    let [<Literal>] Request = 1
    let [<Literal>] Response = 2
    let [<Literal>] OpBegin = 3
    let [<Literal>] OpEnd = 4
    let [<Literal>] OpTrace = 5
    let [<Literal>] Warning = 6
    let [<Literal>] Error = 7
    let [<Literal>] Debug = 8
 
/// Simple enum wrapper for the possible keyword values
/// attached to the ETW events.
module Keywords' =
    /// Identifies an ETW trace as diagnostic information.
    [<Literal>]
    let Debug = EnumOfValue<int64,EventKeywords> 1L
    [<Literal>]
    let Critical = EnumOfValue<int64,EventKeywords> 2L
    [<Literal>]
    let Diagnostic = EnumOfValue<int64,EventKeywords> 3L

/// Implementation of <see cref="EventSource"/> used by <see cref="FirefoxSyncTraceWriter"/>
/// to emit ETW events.
[<Sealed>]
[<EventSource(Name = "Samples-FirefoxSync")>]
type FirefoxSyncEventSource() =
    inherit EventSource()

    static let mutable log = new FirefoxSyncEventSource()
    /// <summary>
    /// Returns a singleton instance of this class.
    /// </summary>
    /// <remarks>
    /// This is the convention used by other <see cref="EventSource"/> implementations.
    /// </remarks>
    static member Log
        with get() = log
        and set(v) = log <- v

    /// <summary>
    /// Emit an ETW event for a debug message.
    /// </summary>
    /// <param name="msg">The debug message.</param>
    [<Event(1,
            Keywords = SemanticLoggingKeywords.Keywords.Debug, 
            Level = EventLevel.Verbose, 
            Channel = EventChannel.Debug)>]
    member x.Debug(msg: string) =
        x.WriteEvent(1, msg)

//    Keywords = Keywords'.Debug

//    1>------ Neues Erstellen gestartet: Projekt: SemanticLoggingKeywords, Konfiguration: Release Any CPU ------
//    2>------ Neues Erstellen gestartet: Projekt: FirefoxSync, Konfiguration: Release Any CPU ------
//    2>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\FirefoxSync.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\FirefoxSync.XML --optimize+ -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\FSharp.Data.2.0.8\lib\net40\FSharp.Data.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.Linq.dll" --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" FirefoxSync.fs Results.fs Utilities.fs SecretStore.fs ServerUrls.fs CryptoKey.fs GeneralInfo.fs Collections.fs 
//    1>  SemanticLoggingKeywords -> E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll
//    1>  Info: No event source classes needing registration found in E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll
//    2>	FirefoxSync -> E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll
//    2>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.eventRegister.rsp" "E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll" 
//    2>	Info: No event source classes needing registration found in E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll
//    3>------ Neues Erstellen gestartet: Projekt: SemanticLogging, Konfiguration: Release Any CPU ------
//    3>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\SemanticLogging.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\SemanticLogging.XML --optimize+ -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventSource.Redist.1.0.24\lib\net40\Microsoft.Diagnostics.Tracing.EventSource.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" SemanticLogging.fs 
//    3>	SemanticLogging -> E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll
//    3>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.eventRegister.rsp" "E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll" 
//    3>EXEC: Fehler : SemanticLogging+FirefoxSyncEventSource: Generation of ETW manifest failed
//    3>EXEC: Fehler : SemanticLogging+FirefoxSyncEventSource: Use of undefined keyword value 0x1 for event Debug.
//    3>EXEC: Fehler : Failures encountered creating registration DLLs for EventSources in E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll
//    3>E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\Microsoft.Diagnostics.Tracing.EventRegister.targets(132,5): Fehler MSB3073: Der Befehl ""E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.eventRegister.rsp" "E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll" " wurde mit dem Code 1 beendet.
//    3>Die Erstellung des Projekts "SemanticLogging.fsproj" ist abgeschlossen -- FEHLER.
//    3>
//    3>Fehler beim Buildvorgang.
//    4>------ Neues Erstellen gestartet: Projekt: FirefoxSyncTest, Konfiguration: Release Any CPU ------
//    4>	Die Erstellung des Projekts "SemanticLogging.fsproj" ist abgeschlossen -- FEHLER.
//    4>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\FirefoxSyncTest.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\FSharpTest.XML --optimize+ -r:E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll -r:E:\projects\FirefoxSync\packages\FsCheck.0.9.4.0\lib\net40-Client\FsCheck.dll -r:E:\projects\FirefoxSync\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\FsCheck.Xunit.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventSource.Redist.1.0.24\lib\net40\Microsoft.Diagnostics.Tracing.EventSource.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.core.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.core.interfaces.dll -r:E:\projects\FirefoxSync\packages\NUnit.2.6.3\lib\nunit.framework.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.util.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\NUnit.VisualStudio.TestAdapter.dll -r:E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" -r:E:\projects\FirefoxSync\packages\xunit.1.9.2\lib\net20\xunit.dll --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" FirefoxSyncTest.fs 
//    4>	FirefoxSyncTest -> E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll
//    4>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.eventRegister.rsp" "E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll" 
//    4>	Info: No event source classes needing registration found in E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll
//    ========== Alles neu erstellen: 3 erfolgreich, 1 fehlerhaft, 0 übersprungen ==========



//    Keywords = SemanticLoggingKeywords.Keywords.Debug


//    1>------ Neues Erstellen gestartet: Projekt: SemanticLoggingKeywords, Konfiguration: Release Any CPU ------
//    2>------ Neues Erstellen gestartet: Projekt: FirefoxSync, Konfiguration: Release Any CPU ------
//    2>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\FirefoxSync.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\FirefoxSync.XML --optimize+ -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\FSharp.Data.2.0.8\lib\net40\FSharp.Data.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Xml.Linq.dll" --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" FirefoxSync.fs Results.fs Utilities.fs SecretStore.fs ServerUrls.fs CryptoKey.fs GeneralInfo.fs Collections.fs 
//    1>  SemanticLoggingKeywords -> E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll
//    1>  Info: No event source classes needing registration found in E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll
//    2>	FirefoxSync -> E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll
//    2>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.eventRegister.rsp" "E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll" 
//    2>	Info: No event source classes needing registration found in E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll
//    3>------ Neues Erstellen gestartet: Projekt: SemanticLogging, Konfiguration: Release Any CPU ------
//    3>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\SemanticLogging.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\SemanticLogging.XML --optimize+ -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventSource.Redist.1.0.24\lib\net40\Microsoft.Diagnostics.Tracing.EventSource.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:E:\projects\FirefoxSync\SemanticLoggingKeywords\bin\Release\SemanticLoggingKeywords.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" SemanticLogging.fs 
//    3>	SemanticLogging -> E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll
//    3>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.eventRegister.rsp" "E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll" 
//    3>EXEC: Fehler : SemanticLogging+FirefoxSyncEventSource: Generation of ETW manifest failed
//    3>EXEC: Fehler : SemanticLogging+FirefoxSyncEventSource: Use of undefined keyword value 0x1 for event Debug.
//    3>EXEC: Fehler : Failures encountered creating registration DLLs for EventSources in E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll
//    3>E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\Microsoft.Diagnostics.Tracing.EventRegister.targets(132,5): Fehler MSB3073: Der Befehl ""E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.eventRegister.rsp" "E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll" " wurde mit dem Code 1 beendet.
//    3>Die Erstellung des Projekts "SemanticLogging.fsproj" ist abgeschlossen -- FEHLER.
//    3>
//    3>Fehler beim Buildvorgang.
//    4>------ Neues Erstellen gestartet: Projekt: FirefoxSyncTest, Konfiguration: Release Any CPU ------
//    4>	Die Erstellung des Projekts "SemanticLogging.fsproj" ist abgeschlossen -- FEHLER.
//    4>	C:\Program Files (x86)\Microsoft SDKs\F#\3.1\Framework\v4.0\fsc.exe -o:obj\Release\FirefoxSyncTest.dll --debug:pdbonly --noframework --define:TRACE --doc:bin\Release\FSharpTest.XML --optimize+ -r:E:\projects\FirefoxSync\FirefoxSync\bin\Release\FirefoxSync.dll -r:E:\projects\FirefoxSync\packages\FsCheck.0.9.4.0\lib\net40-Client\FsCheck.dll -r:E:\projects\FirefoxSync\packages\FsCheck.Xunit.0.4.1.0\lib\net40-Client\FsCheck.Xunit.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\FSharp\.NETFramework\v4.0\4.3.1.0\FSharp.Core.dll" -r:E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventSource.Redist.1.0.24\lib\net40\Microsoft.Diagnostics.Tracing.EventSource.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll" -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.core.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.core.interfaces.dll -r:E:\projects\FirefoxSync\packages\NUnit.2.6.3\lib\nunit.framework.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\nunit.util.dll -r:E:\projects\FirefoxSync\packages\NUnitTestAdapter.1.0\lib\NUnit.VisualStudio.TestAdapter.dll -r:E:\projects\FirefoxSync\SemanticLogging\bin\Release\SemanticLogging.dll -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Core.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.dll" -r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\System.Numerics.dll" -r:E:\projects\FirefoxSync\packages\xunit.1.9.2\lib\net20\xunit.dll --target:library --warn:3 --warnaserror:76 --vserrors --validate-type-providers --LCID:1031 --utf8output --fullpaths --flaterrors --subsystemversion:6.00 --highentropyva+ --sqmsessionguid:51db4d62-d108-43cc-a56b-48bd502052c0 "C:\Users\Friedrich\AppData\Local\Temp\.NETFramework,Version=v4.5.AssemblyAttributes.fs" FirefoxSyncTest.fs 
//    4>	FirefoxSyncTest -> E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll
//    4>	"E:\projects\FirefoxSync\packages\Microsoft.Diagnostics.Tracing.EventRegister.1.0.24\build\eventRegister.exe" -DumpRegDlls @"E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.eventRegister.rsp" "E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll" 
//    4>	Info: No event source classes needing registration found in E:\projects\FirefoxSync\FirefoxSyncTest\bin\Release\FirefoxSyncTest.dll
//    ========== Alles neu erstellen: 3 erfolgreich, 1 fehlerhaft, 0 übersprungen ==========
