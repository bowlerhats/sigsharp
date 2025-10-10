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

    private bool _isSmall = true;
    private readonly SmallSet<T> _set;
    
    bool ICollection<T>.IsReadOnly => false;

    public int Count => _isSmall ? _set.Count : _count;

    public bool IsEmpty => _isSmall ? _set.IsEmpty : _count <= 0;

    public bool HasAny => !this.IsEmpty;

    private int _count;
    
    public ConcurrentHashSet()
    {
        _set = [];
        _dict = [];
    }

    public ConcurrentHashSet(IEnumerable<T> collection)
    {
        if (_isSmall)
        {
            _set = new SmallSet<T>(collection);
            _dict = [];
        }
        else
        {
            _set = [];
            _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)));
            _count = _dict.Count;
        }
    }
    
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        if (_isSmall)
        {
            _set = new SmallSet<T>(collection, comparer);
            _dict = [];
        }
        else
        {
            _set = [];
            _dict = new ConcurrentDictionary<T, bool>(collection.Select(d => new KeyValuePair<T, bool>(d, true)), comparer);
            _count = _dict.Count;
        }
    }
    
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
    {
        _set = new SmallSet<T>(comparer);
        _dict = new ConcurrentDictionary<T, bool>(comparer);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
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

        if (_isSmall)
        {
            if (_set.Add(item))
            {
                return true;
            }

            if (_set.HasCapacity)
            {
                return false;
            }
        }

        if (!_dict.TryAdd(item, true))
            return false;

        Interlocked.Increment(ref _count);

        var wasSmall = Interlocked.CompareExchange(ref _isSmall, false, true);
        if (wasSmall && !_isSmall)
        {
            foreach (var setItem in _set)
            {
                _dict.TryAdd(setItem, true);
            }
            
            _set.Clear();

            Interlocked.Exchange(ref _count, _dict.Count);
        }
        
        return true;
    }
    
    public void Clear()
    {
        if (_isSmall)
        {
            _set.Clear();
            return;
        }

        var wasSmall = Interlocked.CompareExchange(ref _isSmall, true, false);
        if (!wasSmall && _isSmall)
        {
            _set.Clear();
        }
        
        Interlocked.Exchange(ref _count, 0);
        foreach (var (key, _) in _dict)
        {
            _dict.Remove(key, out var _);
        }
    }
    
    public bool Contains(T item)
    {
        if (_isSmall)
            return _set.Contains(item);
            
        return this.HasAny && _dict.ContainsKey(item);
    }
    
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (_isSmall)
        {
            _set.CopyTo(array, arrayIndex);
            return;
        }
        
        if (this.IsEmpty)
            return;

        foreach (var (key, _) in _dict)
        {
            array[arrayIndex++] = key;
        }
    }
    
    public bool Remove(T item)
    {
        if (_isSmall)
        {
            return _set.Remove(item);
        }
        
        if (this.IsEmpty || !_dict.Remove(item, out _))
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
    
    public struct Enumerator : IEnumerator<T>
    {
        public T Current { get; private set; } = default!;
        
        object? IEnumerator.Current => this.Current;

        private readonly IEnumerator<KeyValuePair<T, bool>> _dictEnumerator = null!;
        private SmallSet<T>.Enumerator _setEnumerator;
        
        private readonly bool _isSmall;

        public Enumerator(ConcurrentHashSet<T> hashSet)
        {
            _isSmall = hashSet._isSmall;
            if (!_isSmall)
            {
                _dictEnumerator = hashSet._dict.GetEnumerator();
            }
            else
            {
                _setEnumerator = hashSet._set.GetEnumerator();
            }
        }

        public void Dispose()
        {
            if (_isSmall)
            {
                _setEnumerator.Dispose();
            }
            else
            {
                _dictEnumerator.Dispose();
            }
        }

        public bool MoveNext()
        {
            if (!_isSmall)
            {
                if (!_dictEnumerator.MoveNext())
                    return false;
                
                this.Current = _dictEnumerator.Current.Key;
                return true;
            }

            var setEnumerator = _setEnumerator;

            if (!setEnumerator.MoveNext())
                return false;

            this.Current = setEnumerator.Current;

            _setEnumerator = setEnumerator;

            return true;
        }

        public void Reset()
        {
            var setEnumerator = _setEnumerator;
            setEnumerator.Reset();
            _setEnumerator = setEnumerator;
            
            _dictEnumerator.Reset();
        }
    }
}