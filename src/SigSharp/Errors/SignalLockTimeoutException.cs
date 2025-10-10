using SigSharp.Nodes;

namespace SigSharp;

public class SignalLockTimeoutException : SignalException
{
    public SignalLockTimeoutException(SignalNode node)
        : base($"Signal access or update lock timeout exceeded for '{node.Name}'") { }
}