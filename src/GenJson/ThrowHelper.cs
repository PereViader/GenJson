#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GenJson
{
    public static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowUnknownDerivedType(Type type)
        {
            throw new NotSupportedException("Unknown derived type: " + type.Name);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowTypeNotRegistered(Type type)
        {
            throw new InvalidOperationException($"Type '{type.FullName}' is not registered in GenJsonGenericRegistry.");
        }
    }
}
