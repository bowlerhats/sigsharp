using System;
using System.Threading;
using System.Threading.Tasks;

namespace SigSharp;

public interface ISignalEffectScheduler
{
    bool Schedule(SignalEffect effect, Func<Task> effectRunFunction, CancellationToken stopToken = default);
}

public class SignalEffectScheduler : ISignalEffectScheduler
{
    public static SignalEffectScheduler Default { get; } = new();
    
    public bool Schedule(SignalEffect effect, Func<Task> effectRunFunction, CancellationToken stopToken = default)
    {
        Task.Factory.StartNew(effectRunFunction, stopToken);

        return true;
    }
}