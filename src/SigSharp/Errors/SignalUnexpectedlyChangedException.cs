namespace SigSharp;

public class SignalUnexpectedlyChangedException: SignalException
{
    public uint OldVersion { get; }
    
    public uint NewVersion { get; }
    
    public SignalUnexpectedlyChangedException(uint oldVersion, uint newVersion)
        : base("Signal changed unexpectedly")
    {
        this.OldVersion = oldVersion;
        this.NewVersion = newVersion;
    }
}