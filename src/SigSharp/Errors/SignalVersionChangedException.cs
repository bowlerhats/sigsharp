using SigSharp.Nodes;

namespace SigSharp;

public class SignalVersionChangedException: SignalException
{
    public SignalNode Node { get; }
    public uint OldVersion { get; }
    
    public uint NewVersion { get; }
    
    public SignalVersionChangedException(SignalNode node, uint oldVersion, uint newVersion)
        : base("Signal version changed")
    {
        this.Node = node;
        this.OldVersion = oldVersion;
        this.NewVersion = newVersion;
    }
}