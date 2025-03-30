using System.Collections.Generic;
using SigSharp.Utils;

namespace SigSharp;

public class HashSetSignal<T> : CollectionSignal<T, ICollection<T>>
{
    public HashSetSignal(CollectionSignalOptions opts = null)
        :base([], opts: opts)
    {
    }
    
    public HashSetSignal(IEnumerable<T> initialValues, CollectionSignalOptions opts = null)
        :base(initialValues, opts)
    {
    }

    protected override ICollection<T> CreateBackingCollection(IEnumerable<T> initialValues)
    {
        return new ConcurrentHashSet<T>(initialValues);
    }
}
