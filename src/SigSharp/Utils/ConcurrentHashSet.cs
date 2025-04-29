using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SigSharp.Utils;

internal sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    where T: notnull
{
    private readonly ConcurrentDictionary<T, bool> _dict;
    
    bool ICollection<T>.IsReadOnly => false;
    
    public int Count => _dict.Count;
    
    public ConcurrentHashSet()
    {
        _dict = new ConcurrentDictionary<T, bool>();
    }

    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)));
    }
    
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)), comparer);
    }
    
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        _dict = new ConcurrentDictionary<T, bool>(comparer);
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        return _dict.Keys.GetEnumerator();
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
        return _dict.TryAdd(item, true);
    }
    
    public void Clear()
    {
        _dict.Clear();
    }
    
    public bool Contains(T item)
    {
        return _dict.ContainsKey(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        _dict.Keys.CopyTo(array, arrayIndex);
    }
    
    public bool Remove(T item)
    {
        return _dict.Remove(item, out _);
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