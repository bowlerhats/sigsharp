using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace SigSharp.Utils;

internal sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    where T: notnull
{
    private readonly ConcurrentDictionary<T, bool> _dict;
    
    bool ICollection<T>.IsReadOnly => false;
    
    public int Count => _count;

    public bool IsEmpy => _count <= 0;

    private int _count;
    
    public ConcurrentHashSet()
    {
        _dict = new ConcurrentDictionary<T, bool>();
    }

    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)));
        _count = _dict.Count;
    }
    
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)), comparer);
        _count = _dict.Count;
    }
    
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        _dict = new ConcurrentDictionary<T, bool>(comparer);
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        if (this.IsEmpy)
            yield break;
        
        using var enumerator = _dict.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current.Key;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
    
    void ICollection<T>.Add(T item)
    {
        this.Add(item);
    }

    public bool Add(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        if (!_dict.TryAdd(item, true))
            return false;
        
        Interlocked.Increment(ref _count);
        return true;
    }
    
    public void Clear()
    {
        if (this.IsEmpy)
            return;
        
        Interlocked.Exchange(ref _count, 0);
        foreach (var (key, _) in _dict)
        {
            _dict.Remove(key, out _);
        }
    }
    
    public bool Contains(T item)
    {
        return !this.IsEmpy && _dict.ContainsKey(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (this.IsEmpy)
            return;

        foreach (var (key, _) in _dict)
        {
            array[arrayIndex++] = key;
        }
    }
    
    public bool Remove(T item)
    {
        if (this.IsEmpy || !_dict.Remove(item, out _))
            return false;

        Interlocked.Decrement(ref _count);
        return true;
    }

    public AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>()
        where TAlternate : notnull, allows ref struct
    {
        var alt = _dict.GetAlternateLookup<TAlternate>();

        return new AlternateLookup<TAlternate>(alt);
    }

    public bool TryGetAlternateLookup<TAlternate>(out AlternateLookup<TAlternate> lookup)
        where TAlternate : notnull, allows ref struct
    {
        if (_dict.TryGetAlternateLookup(out ConcurrentDictionary<T, bool>.AlternateLookup<TAlternate> alt))
        {
            lookup = new AlternateLookup<TAlternate>(alt);

            return true;
        }

        lookup = default;

        return false;
    }

    public readonly struct AlternateLookup<TAlternate>
        where TAlternate : notnull, allows ref struct
    {
        private readonly ConcurrentDictionary<T, bool>.AlternateLookup<TAlternate> _alt;
        
        internal AlternateLookup(ConcurrentDictionary<T, bool>.AlternateLookup<TAlternate> alt)
        {
            _alt = alt;
        }

        public bool Add(TAlternate item)
        {
            return _alt.TryAdd(item, true);
        }

        public bool Remove(TAlternate item)
        {
            return _alt.TryRemove(item, out _);
        }

        public bool Contains(TAlternate item)
        {
            return _alt.ContainsKey(item);
        }

        public bool TryGetValue(TAlternate equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            return _alt.TryGetValue(equalValue, out actualValue, out _);
        }
    }
}