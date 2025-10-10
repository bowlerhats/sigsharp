using System.Collections.Generic;
using SigSharp.Utils;

namespace SigSharp;

public class HashSetSignal<T> : CollectionSignal<T, ICollection<T>>
    where T: notnull
{
    public HashSetSignal(IEnumerable<T> initialValues, CollectionSignalOptions? opts = null, string? name= null)
        :base(initialValues, opts, name ?? $"HashSetSignal<{typeof(T).Name}>")
    {
    }
    
    public HashSetSignal(CollectionSignalOptions? opts = null, string? name = null)
        :this([], opts: opts, name)
    {
    }

    protected override ICollection<T> CreateBackingCollection(IEnumerable<T> initialValues)
    {
        return new ConcurrentHashSet<T>(initialValues);
    }
}
