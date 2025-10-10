using System;
using System.Diagnostics.CodeAnalysis;

namespace SigSharp;

public class SignalDisposedException : ObjectDisposedException
{
    public new static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
    {
        if (condition)
            throw new SignalDisposedException(instance?.GetType().FullName);
    }
    
    public SignalDisposedException(string? objectName)
        : base(objectName) { }

    public SignalDisposedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public SignalDisposedException(string? objectName, string? message)
        : base(objectName, message) { }
}