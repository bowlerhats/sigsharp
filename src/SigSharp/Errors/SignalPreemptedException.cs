using SigSharp.Nodes;

namespace SigSharp;

public class SignalPreemptedException: SignalException
{
    public SignalNode PreemptedBy { get; }
    
    public bool IsRescheduled { get; init; }

    public SignalPreemptedException(SignalNode node)
        : base($"Signal access was preempted '{node}'")
    {
        this.PreemptedBy = node;
    }
}