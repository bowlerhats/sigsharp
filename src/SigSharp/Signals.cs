using System;
using System.Linq;
using System.Threading.Tasks;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public static partial class Signals
{
    /// <summary>
    /// Global options for signal handling
    /// </summary>
    public static GlobalSignalOptions Options { get; } = new();
    
    public static SignalSuspender Suspend()
    {
        return SignalGroup.CreateSuspended();
    }

    public static SignalSuspender GlobalSuspend()
    {
        return SignalGroup.CreateGlobalSuspended();
    }

    public static async Task<bool> WaitIdleAsync(TimeSpan? waitBetweenChecks = null)
    {
        waitBetweenChecks ??= TimeSpan.FromMilliseconds(50);

        var currentGroup = SignalGroup.Current;

        var wasWorking = false;
        bool isWorking;
        do
        {
            isWorking = false;

            var signals = SignalGroup.GetAllGroups().ToArray();
            
            foreach (var group in signals)
            {
                if (group.IsDisposing || group == SignalGroup.DetachedGroup)
                    continue;
                
                if (currentGroup?.HasBound(group) ?? false)
                    continue; // Can't wait for self or bound chain
                
                if (SignalGroup.GlobalSuspender?.HasBound(group) ?? false)
                    continue; // Same for global suspender
                
                if (await group.WaitIdleAsync())
                {
                    isWorking = true;
                    wasWorking = true;

                    break;
                }
            }

            if (isWorking)
            {
                await Task.Delay(waitBetweenChecks.Value);
            }

        } while (isWorking);

        return wasWorking;
    }
    
    public static ComputedSignal<TValue>? PluckComputed<T, TValue>(T anchor, Func<T, TValue> func)
    {
        ComputedExtensions.CapturePluck.Value = true;
        try
        {
            func(anchor);

            return null;
        }
        catch (SignalPluckException<TValue> ex)
        {
            ComputedExtensions.CapturePluck.Value = false;
            
            return ex.Signal;
        }
    }

    public static void DropCaches()
    {
        SignalTracker.TrackerPool.Clear();
        
    }

}