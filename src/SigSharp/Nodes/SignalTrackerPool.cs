using System.Threading.Channels;

namespace SigSharp.Nodes;

public interface ISignalTrackerPool
{
    SignalTracker Rent();
    void Return(SignalTracker tracker);
}

internal class DefaultSignalTrackerPool : ISignalTrackerPool
{
    public static DefaultSignalTrackerPool Instance { get; } = new();

    private readonly Channel<SignalTracker> _channel;
    
    public DefaultSignalTrackerPool(int capacity = 50)
    {
        _channel = Channel.CreateBounded<SignalTracker>(capacity);
    }
    
    public SignalTracker Rent()
    {
        if (!_channel.Reader.TryRead(out var tracker))
        {
            tracker = new SignalTracker();
        }

        return tracker;
    }

    public void Return(SignalTracker tracker)
    {
        _channel.Writer.TryWrite(tracker);
    }
}