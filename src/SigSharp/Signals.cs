using System;
using System.Threading.Tasks;
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

    public static async Task<bool> WaitIdleAsync(TimeSpan? waitBetweenChecks = null)
    {
        waitBetweenChecks ??= TimeSpan.FromMilliseconds(50);

        var currentGroup = SignalGroup.Current;

        var wasWorking = false;
        bool isWorking;
        do
        {
            isWorking = false;
            
            foreach (var group in SignalGroup.GetAllGroups())
            {
                if (currentGroup?.HasBound(group) ?? false)
                    continue; // Can't wait for self or bound chain
                
                if (await group.WaitIdleAsync())
                {
                    isWorking = true;
                    wasWorking = true;
                }
            }

            if (isWorking)
            {
                await Task.Delay(waitBetweenChecks.Value);
            }

        } while (isWorking);

        return wasWorking;
    }
}