using System;

namespace SigSharp;

public class SignalException : Exception
{
    public SignalException()
    {
    }

    public SignalException(string message) : base(message)
    {
    }

    public SignalException(string message, Exception inner) : base(message, inner)
    {
    }
}