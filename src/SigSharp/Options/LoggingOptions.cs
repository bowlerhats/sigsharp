using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SigSharp.Nodes;

namespace SigSharp;

public sealed partial class GlobalSignalOptions
{
    public record LoggingOptions
    {
        /// <summary>
        /// When enabled the provided logger factory will be used,
        /// otherwise the NullLogger
        /// </summary>
        public bool Enabled { get; init; }
        
        /// <summary>
        /// The LoggerFactory to be used.
        /// </summary>
        /// <remarks>
        /// If it's null (by default it is null) it represents the NullLogger.
        /// </remarks>
        public ILoggerFactory LoggerFactory { get; init; }

        /// <summary>
        /// Capture the originating stack trace when synchronously invoking effect's RunImmediate
        /// </summary>
        /// <returns>An arbitrary object representing stack or other info which will be passed to the augment function</returns>
        /// <remarks>
        /// Internally the effect run is performed by a Task, but blockingly awaited.<br/>
        /// Because of either the task completion or the blocking behaviour the invoker thread's
        /// stack frame gets trunked/destroyed.<br/>
        /// This function is called before the task run, when the current thread still has the
        /// invoker's synchronous frame chain.
        /// </remarks>
        public Func<SignalNode, object> CaptureStackInfo { get; init; }
        
        /// <summary>
        /// Custom action to perform when synchronously invoking effect's RunImmedate results in exception 
        /// </summary>
        /// <remarks>
        /// The object parameter contains the object returned by CaptureStackInfo.<br/>
        /// </remarks>
        public Action<SignalNode, Exception, object> AugmentWithStackInfo { get; init; }
        
        public ILogger CreateLogger(Type type)
        {
            if (this.Enabled && this.LoggerFactory is not null)
            {
                return this.LoggerFactory.CreateLogger(type);
            }
            
            return NullLogger.Instance;
        }
        
        public ILogger<T> CreateLogger<T>()
        {
            if (this.Enabled && this.LoggerFactory is not null)
            {
                return this.LoggerFactory.CreateLogger<T>();
            }
            
            return NullLogger<T>.Instance;
        }
    }
}