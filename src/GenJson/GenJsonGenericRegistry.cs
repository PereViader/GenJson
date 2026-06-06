#nullable enable
using System;

namespace GenJson
{
    public delegate string StructToJson<T>(T value, bool useCountOptimization) where T : struct;
    public delegate byte[] StructToJsonUtf8<T>(T value, bool useCountOptimization) where T : struct;
    public delegate T? StructSpanParser<T>(ReadOnlySpan<char> span, bool useCountOptimization) where T : struct;
    public delegate T? StructUtf8SpanParser<T>(ReadOnlySpan<byte> span, bool useCountOptimization) where T : struct;

    public delegate string? ClassToJson<T>(T? value, bool useCountOptimization) where T : class;
    public delegate byte[]? ClassToJsonUtf8<T>(T? value, bool useCountOptimization) where T : class;
    public delegate T? ClassSpanParser<T>(ReadOnlySpan<char> span, bool useCountOptimization) where T : class;
    public delegate T? ClassUtf8SpanParser<T>(ReadOnlySpan<byte> span, bool useCountOptimization) where T : class;

    public static class GenJsonGenericRegistry
    {
        public readonly struct StructHelper {}
        public sealed class ClassHelper {}

        private static class StructInfo<T> where T : struct
        {
            public static StructToJson<T>? ToJson;
            public static StructToJsonUtf8<T>? ToJsonUtf8;
            public static StructSpanParser<T>? FromJson;
            public static StructUtf8SpanParser<T>? FromJsonUtf8;
        }

        private static class ClassInfo<T> where T : class
        {
            public static ClassToJson<T>? ToJson;
            public static ClassToJsonUtf8<T>? ToJsonUtf8;
            public static ClassSpanParser<T>? FromJson;
            public static ClassUtf8SpanParser<T>? FromJsonUtf8;
        }

        public static void RegisterStruct<T>(
            StructToJson<T> toJson,
            StructToJsonUtf8<T> toJsonUtf8,
            StructSpanParser<T> fromJson,
            StructUtf8SpanParser<T> fromJsonUtf8) where T : struct
        {
            StructInfo<T>.ToJson = toJson;
            StructInfo<T>.ToJsonUtf8 = toJsonUtf8;
            StructInfo<T>.FromJson = fromJson;
            StructInfo<T>.FromJsonUtf8 = fromJsonUtf8;
        }

        public static void RegisterClass<T>(
            ClassToJson<T> toJson,
            ClassToJsonUtf8<T> toJsonUtf8,
            ClassSpanParser<T> fromJson,
            ClassUtf8SpanParser<T> fromJsonUtf8) where T : class
        {
            ClassInfo<T>.ToJson = toJson;
            ClassInfo<T>.ToJsonUtf8 = toJsonUtf8;
            ClassInfo<T>.FromJson = fromJson;
            ClassInfo<T>.FromJsonUtf8 = fromJsonUtf8;
        }

        // Value type variants
        public static string ToJson<T>(T value, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            var delegateInstance = StructInfo<T>.ToJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(value, useCountOptimization);
        }

        public static byte[] ToJsonUtf8<T>(T value, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            var delegateInstance = StructInfo<T>.ToJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(value, useCountOptimization);
        }

        public static T? FromJson<T>(string? json, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            if (json == null) return null;
            var delegateInstance = StructInfo<T>.FromJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json.AsSpan(), useCountOptimization);
        }

        public static T? FromJson<T>(ReadOnlySpan<char> json, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            var delegateInstance = StructInfo<T>.FromJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }

        public static T? FromJsonUtf8<T>(byte[]? json, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            if (json == null) return null;
            var delegateInstance = StructInfo<T>.FromJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }

        public static T? FromJsonUtf8<T>(ReadOnlySpan<byte> json, bool useCountOptimization = false, StructHelper _ = default) where T : struct
        {
            var delegateInstance = StructInfo<T>.FromJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }

        // Reference type variants
        public static string? ToJson<T>(T? value, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            if (value == null) return null;
            var delegateInstance = ClassInfo<T>.ToJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(value, useCountOptimization);
        }

        public static byte[]? ToJsonUtf8<T>(T? value, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            if (value == null) return null;
            var delegateInstance = ClassInfo<T>.ToJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(value, useCountOptimization);
        }

        public static T? FromJson<T>(string? json, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            if (json == null) return null;
            var delegateInstance = ClassInfo<T>.FromJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json.AsSpan(), useCountOptimization);
        }

        public static T? FromJson<T>(ReadOnlySpan<char> json, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            var delegateInstance = ClassInfo<T>.FromJson;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }

        public static T? FromJsonUtf8<T>(byte[]? json, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            if (json == null) return null;
            var delegateInstance = ClassInfo<T>.FromJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }

        public static T? FromJsonUtf8<T>(ReadOnlySpan<byte> json, bool useCountOptimization = false, ClassHelper? _ = default) where T : class
        {
            var delegateInstance = ClassInfo<T>.FromJsonUtf8;
            if (delegateInstance == null) throw new InvalidOperationException($"Type '{typeof(T).FullName}' is not registered in GenJsonGenericRegistry.");
            return delegateInstance(json, useCountOptimization);
        }
    }
}
