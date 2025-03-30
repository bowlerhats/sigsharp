using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SigSharp;

public sealed partial class GlobalSignalOptions
{
    public record LoggingOptions
    {
        public bool Enabled { get; init; }
        
        public ILoggerFactory LoggerFactory { get; init; }

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