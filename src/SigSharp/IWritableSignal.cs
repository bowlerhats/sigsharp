using System;

namespace SigSharp;

public interface IWritableSignal: IDisposable;

public interface IWritableSignal<in T> : IWritableSignal
{
    T Value { set; }
    void Set(T value);

    void SetDefault();
}