using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SigSharp.Utils;

internal static class WeakTableExtensions
{
    /// <summary>
    /// Safely invoke removal of item
    /// </summary>
    /// <remarks>
    ///  There is a runtime bug which leads to invalid state of weaktable<br/>
    /// see: https://github.com/dotnet/runtime/issues/85980
    /// </remarks>
    public static bool RemoveSafe<T1, T2>(this ConditionalWeakTable<T1, T2> weakTable, T1? key)
        where T1 : class
        where T2 : class
    {
        if (key is null)
            return false;

        while (true)
        {
            try
            {
                return weakTable.Remove(key);
            }
            catch (NullReferenceException)
            {
                // ignore
            }
            
            // Give chance to be resurrected
            Thread.Sleep(TimeSpan.FromMilliseconds(10));
        }
    }
}