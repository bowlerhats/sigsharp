using System;
using System.Threading.Tasks;

namespace SigSharp;

public static partial class Signals
{
    public static async ValueTask RunDetachedToCompletion(Func<ValueTask> action, TimeSpan? rerunDelay = null)
    {
        rerunDelay ??= TimeSpan.FromMilliseconds(300);
        
        while (true)
        {
            try
            {
                await Detached(action);
                
                break;
            }
            catch (SignalDeadlockedException)
            {
                // ignore
            }
            catch (SignalPreemptedException)
            {
                // ignore
            }
            
            await Task.Delay(rerunDelay.Value);
        }
    }
}