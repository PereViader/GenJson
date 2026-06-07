#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GenJson.SystemTextJson
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidConverterWrite(Type type)
        {
            throw new InvalidOperationException($"The converter {type.FullName} must define either static WriteJsonUtf8 or WriteJson.");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidConverterRead(Type type)
        {
            throw new InvalidOperationException($"The converter {type.FullName} must define either static FromJsonUtf8 or FromJson.");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowDeserializationNotSupported()
        {
            throw new NotSupportedException("No valid deserialization method found on custom converter.");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowSerializationNotSupported()
        {
            throw new NotSupportedException("No valid serialization method found on custom converter.");
        }
    }
}
