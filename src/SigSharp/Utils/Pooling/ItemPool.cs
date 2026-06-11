namespace SigSharp.Utils.Pooling;

public interface ISignalItemPool
{
    void Clear();
}

public interface ISignalItemPool<T> : ISignalItemPool
    where T: new()
{
    T Rent();
    void Return(T item);
}