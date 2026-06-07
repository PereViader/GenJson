#nullable enable
using System;

namespace GenJson
{
    public delegate string ToJsonDelegate<in T>(T value, bool useCountOptimization);
    public delegate byte[] ToJsonUtf8Delegate<in T>(T value, bool useCountOptimization);
    public delegate bool TryFromJsonDelegate<T>(ReadOnlySpan<char> span, bool useCountOptimization, out T? result);
    public delegate bool TryFromJsonUtf8Delegate<T>(ReadOnlySpan<byte> span, bool useCountOptimization, out T? result);

    public static class GenJsonGenericRegistry
    {
        private static class Info<T>
        {
            public static ToJsonDelegate<T>? ToJson;
            public static ToJsonUtf8Delegate<T>? ToJsonUtf8;
            public static TryFromJsonDelegate<T>? TryFromJson;
            public static TryFromJsonUtf8Delegate<T>? TryFromJsonUtf8;
        }

        public static void Register<T>(
            ToJsonDelegate<T> toJson,
            ToJsonUtf8Delegate<T> toJsonUtf8,
            TryFromJsonDelegate<T> tryFromJson,
            TryFromJsonUtf8Delegate<T> tryFromJsonUtf8)
        {
            Info<T>.ToJson = toJson;
            Info<T>.ToJsonUtf8 = toJsonUtf8;
            Info<T>.TryFromJson = tryFromJson;
            Info<T>.TryFromJsonUtf8 = tryFromJsonUtf8;
        }

        public static void Deregister<T>()
        {
            Info<T>.ToJson = null;
            Info<T>.ToJsonUtf8 = null;
            Info<T>.TryFromJson = null;
            Info<T>.TryFromJsonUtf8 = null;
        }

        public static string ToJson<T>(T value, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.ToJson;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(value, useCountOptimization);
        }

        public static byte[] ToJsonUtf8<T>(T value, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.ToJsonUtf8;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(value, useCountOptimization);
        }

        public static bool TryFromJson<T>(string json, out T? result, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.TryFromJson;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(json.AsSpan(), useCountOptimization, out result);
        }

        public static bool TryFromJson<T>(ReadOnlySpan<char> json, out T? result, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.TryFromJson;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(json, useCountOptimization, out result);
        }

        public static bool TryFromJsonUtf8<T>(byte[] json, out T? result, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.TryFromJsonUtf8;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(json, useCountOptimization, out result);
        }

        public static bool TryFromJsonUtf8<T>(ReadOnlySpan<byte> json, out T? result, bool useCountOptimization = false)
        {
            var delegateInstance = Info<T>.TryFromJsonUtf8;
            if (delegateInstance == null) ThrowHelper.ThrowTypeNotRegistered(typeof(T));
            return delegateInstance(json, useCountOptimization, out result);
        }
    }
}
