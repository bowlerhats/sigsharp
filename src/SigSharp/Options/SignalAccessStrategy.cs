namespace SigSharp;

public enum SignalAccessStrategy
{
    Unrestricted,
    ExclusiveLock,
    PreemptiveLock,
    Optimistic
}