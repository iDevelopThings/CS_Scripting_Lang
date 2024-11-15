﻿using System;

namespace CSScriptingLang.Common.CodeWriter;

public sealed class UsingHandle : IDisposable
{
    private Action _disposed;

    public UsingHandle(Action disposed)
    {
        _disposed = disposed;
    }

    public void Dispose()
    {
        if (_disposed != null)
        {
            _disposed();
            _disposed = null;
        }
    }
}