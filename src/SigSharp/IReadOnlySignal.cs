using System;

namespace SigSharp;

public interface IReadOnlySignal : IDisposable
{
    bool IsDefault { get; }
    bool IsNull { get; }
    
    bool IsDefaultUntracked { get; }
    bool IsNullUntracked { get; }
}

public interface IReadOnlySignal<out T> : IReadOnlySignal
{
    T? Value { get; }
    T? Get();
    
    T? Untracked { get; }
    T? GetUntracked();
    
    IObservable<T?> AsObservable();
}