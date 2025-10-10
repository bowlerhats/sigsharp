namespace SigSharp;

internal sealed class SignalPluckException<TValue> : SignalException
{
    public ComputedSignal<TValue>? Signal { get; }

    public SignalPluckException(ComputedSignal<TValue>? signal)
    {
        this.Signal = signal;
    }
}