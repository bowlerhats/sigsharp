using SigSharp.Nodes;

namespace SigSharp;

public class SignalDeadlockedException: SignalException
{
    public SignalDeadlockedException(SignalNode node)
        : base($"Signal access or update deadlocked for '{node}'") { }
}