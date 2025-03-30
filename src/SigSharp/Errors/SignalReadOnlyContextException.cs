namespace SigSharp;

public class SignalReadOnlyContextException : SignalException
{
    public SignalReadOnlyContextException()
        : base("Signal context is readonly") { }
}