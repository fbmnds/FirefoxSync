using System;
using Microsoft.Diagnostics.Tracing;

namespace SemanticLoggingKeywords
{
    public class Keywords
    {
        public const EventKeywords Debug = (EventKeywords)0x0001;
        public const EventKeywords Critical = (EventKeywords)0x0002;
        public const EventKeywords Diagnostic = (EventKeywords)0x0004;

        //public static EventKeywords GetAll()
        //{
        //    return Debug | Critical | Diagnostic;
        //}
    }
}
