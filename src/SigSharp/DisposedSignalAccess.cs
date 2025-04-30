using System;
using System.Diagnostics;
using SigSharp.Nodes;
using SigSharp.Utils;

namespace SigSharp;

public static class DisposedSignalAccess
{
    /// <summary>
    /// Strategy for how to handle the value of a signal node after dispose.
    /// </summary>
    public enum Strategy
    {
        /// <summary>
        /// Throws ObjectDisposed exception when attempting to Get() the value.
        /// </summary>
        Throw,
    
        /// <summary>
        /// Returns the default(T) value.
        /// </summary>
        DefaultValue,
    
        /// <summary>
        /// Returns the default(T) value for scalar types (value types), or throws if T is nullable.
        /// </summary>
        /// <remarks>
        /// Uses 'default(T) is null' check instead of reflection to determine nullability.<br/>
        /// This means that it will throw for boxed values like Nullable&lt;T&gt; 
        /// </remarks>
        DefaultScalar,
    
        /// <summary>
        /// Returns the last effective value<br/>
        /// </summary>
        /// <remarks>
        /// The last observed value will not be cleared, so watch out for memory leaks!
        /// </remarks>
        LastValue,
    
        /// <summary>
        /// Returns the last observed value for scalar types (value types), or throws if T is nullable
        /// </summary>
        /// <remarks>
        /// Uses 'default(T) is null' check instead of reflection to determine nullability.<br/>
        /// This means that it will throw for boxed values like Nullable&lt;T&gt; 
        /// </remarks>
        LastScalar
    }
    
    public record struct DisposedCapture<T>(T Value, Strategy Strategy = Strategy.Throw, bool? IsNullable = null);

    public static DisposedCapture<T> Capture<T>(
        T currentValue,
        SignalNode node,
        Strategy strategy,
        bool? isNullable = null)
    {
        return strategy switch
        {
            Strategy.LastValue => new DisposedCapture<T>(currentValue, strategy),
            Strategy.LastScalar
                => isNullable ?? TypeUtils.IsNullableByDefault<T>()
                    ? new DisposedCapture<T>(default!, strategy, true)
                    : new DisposedCapture<T>(currentValue, strategy, false),
            Strategy.DefaultScalar
                => new DisposedCapture<T>(default!, strategy, isNullable ?? TypeUtils.IsNullableByDefault<T>()),
            _ => new DisposedCapture<T>(default!, strategy)
        };
    }

    public static T Access<T>(DisposedCapture<T> capture, SignalNode node)
    {
        switch (capture.Strategy)
        {
            case Strategy.Throw:
                ObjectDisposedException.ThrowIf(node.IsDisposed, node);
                break;
            
            case Strategy.DefaultValue:
                return default!;
            
            case Strategy.DefaultScalar:
                if (capture.IsNullable is null or true)
                {
                    ObjectDisposedException.ThrowIf(node.IsDisposed, node);
                }

                return default!;
            
            case Strategy.LastValue:
                return capture.Value;
                
            case Strategy.LastScalar:
                if (capture.IsNullable is null or true)
                {
                    ObjectDisposedException.ThrowIf(node.IsDisposed, node);
                }

                return capture.Value;
            default:
                throw new ArgumentOutOfRangeException(nameof(capture), "Unknown signal dispose access strategy");
        }

        throw new UnreachableException();
    }
}