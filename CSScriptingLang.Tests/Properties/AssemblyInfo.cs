using System.Diagnostics.CodeAnalysis;

[module: SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
[module: SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks")]
[module: SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods")]
[module: SuppressMessage("Usage", "VSTHRD110:Observe result of async calls")]
[module: SuppressMessage("Usage", "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current")]
