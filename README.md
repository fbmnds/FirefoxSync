FirefoxSync
===========

## Notes about Semantic Logging

Failed to create a custom EventSource in F# for Event Tracing for Windows while using the same approach as documented on [FPish][1]. The related discussion on [Stackoverflow][2] indicates that the underlying problem, i.e. generating an `EnumOfValue<int64, EventKeywords> 1L`, is resolved with F# 3.1. While the previously encountered compiler error does no longer occur, the resulting assembly is not recognized as a properly defined `EventSource` by the `eventRegister` tool. 

Obviously, the `EventSource` definition can be pushed into a C# subproject, which is done here. The registering/unregistering of the event source in the OS needs a specific design according to the desired operating model. Hence, Event Tracing should be handled in separate dedicated project with proper interfaces.

[1]: http://cs.hubfs.net/topic/None/76115
[2]: http://stackoverflow.com/questions/14531175/how-to-define-an-enum-constant-using-the-underlying-value

© @fbmnds, Apache 2.0. where applicable