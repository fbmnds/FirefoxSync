using System;
using System.Diagnostics.Tracing;

namespace SemanticLoggingEventSource
{
    [EventSource(Name = "[DEBUG]")]
    public sealed class FirefoxSyncTestEventSource : EventSource
    {
        [Event(1, 
               Level = EventLevel.LogAlways, 
               Keywords = Keywords.Debug)]
        public void Message1(string Name) { if (IsEnabled()) WriteEvent(1, Name); }

        [Event(2, 
               Level = EventLevel.Verbose, 
               Keywords = Keywords.Critical)]
        public void Message2(string Name1, string Name2) { if (IsEnabled()) WriteEvent(2, Name1, Name2); }

        public class Keywords
        {
            public const EventKeywords Debug    = (EventKeywords) 0x0001;
            public const EventKeywords Critical = (EventKeywords) 0x0002;
        }
    }
}



