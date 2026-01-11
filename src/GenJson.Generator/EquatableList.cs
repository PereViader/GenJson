using System;
using System.Collections.Generic;
using System.Linq;

namespace GenJson.Generator;

public class EquatableList<T>(List<T> list) : IEquatable<EquatableList<T>>
{
    public readonly List<T> Value = list;
    private readonly int _hashCodeCache = CalculateHashCode(list);

    public bool Equals(EquatableList<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.SequenceEqual(other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

    //This is a readonly structure and thus the list won't change
    //avoid calculating the hash code more than once and cache it on creation
    private static int CalculateHashCode(List<T> list)
    {
        int hash = 17;
        foreach (var item in list)
        {
            hash = hash * 23 + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }
    
    public override int GetHashCode() => _hashCodeCache;
}