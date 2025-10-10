using System.Threading.Channels;

namespace SigSharp.Utils;

internal sealed class ItemPool<T>
    where T: new()
{
    private readonly Channel<T> _channel;
    
    public ItemPool(int capacity = 50)
    {
        _channel = Channel.CreateBounded<T>(capacity);
    }

    public void Clear()
    {
        while (_channel.Reader.TryRead(out _)) { }
    }
    
    public T Rent()
    {
        if (!_channel.Reader.TryRead(out var tracker))
        {
            tracker = new T();
        }

        return tracker;
    }

    public void Return(T tracker)
    {
        _channel.Writer.TryWrite(tracker);
    }
}