using System;
using System.Buffers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenJson.SystemTextJson
{
    public class GenJsonStjBridgeConverter<T> : JsonConverter<T>
    {
        private delegate void WriteUtf8Delegate(Span<byte> span, ref int index, T value);
        private delegate T ReadUtf8Delegate(ReadOnlySpan<byte> span, ref int index);
        private delegate int GetSizeUtf8Delegate(T value);

        private delegate void WriteCharDelegate(Span<char> span, ref int index, T value);
        private delegate T ReadCharDelegate(ReadOnlySpan<char> span, ref int index);
        private delegate int GetSizeCharDelegate(T value);

        private readonly WriteUtf8Delegate? _writeUtf8;
        private readonly ReadUtf8Delegate? _readUtf8;
        private readonly GetSizeUtf8Delegate? _getSizeUtf8;

        private readonly WriteCharDelegate? _writeChar;
        private readonly ReadCharDelegate? _readChar;
        private readonly GetSizeCharDelegate? _getSizeChar;

        private readonly IValueTypeReader<T>? _valueTypeReader;
        private readonly MethodInfo? _readUtf8Method;
        private readonly MethodInfo? _readCharMethod;

        public GenJsonStjBridgeConverter(Type staticConverterType)
        {
            // 1. Try to bind UTF-8 methods
            var writeUtf8Method = staticConverterType.GetMethod("WriteJsonUtf8", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Span<byte>), typeof(int).MakeByRefType(), typeof(T) }, null);
            if (writeUtf8Method != null)
            {
                _writeUtf8 = (WriteUtf8Delegate)writeUtf8Method.CreateDelegate(typeof(WriteUtf8Delegate));
            }

            var readUtf8Method = staticConverterType.GetMethod("FromJsonUtf8", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ReadOnlySpan<byte>), typeof(int).MakeByRefType() }, null);
            if (readUtf8Method != null)
            {
                if (readUtf8Method.ReturnType == typeof(T))
                {
                    _readUtf8 = (ReadUtf8Delegate)readUtf8Method.CreateDelegate(typeof(ReadUtf8Delegate));
                }
                else
                {
                    _readUtf8Method = readUtf8Method;
                    var helperType = typeof(ValueTypeReaderHelper<,>).MakeGenericType(typeof(T), typeof(T));
                    _valueTypeReader = (IValueTypeReader<T>)Activator.CreateInstance(helperType);
                }
            }

            var getSizeUtf8Method = staticConverterType.GetMethod("GetSizeUtf8", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(T) }, null);
            if (getSizeUtf8Method != null)
            {
                _getSizeUtf8 = (GetSizeUtf8Delegate)getSizeUtf8Method.CreateDelegate(typeof(GetSizeUtf8Delegate));
            }

            // 2. Try to bind Char methods as fallback
            var writeCharMethod = staticConverterType.GetMethod("WriteJson", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Span<char>), typeof(int).MakeByRefType(), typeof(T) }, null);
            if (writeCharMethod != null)
            {
                _writeChar = (WriteCharDelegate)writeCharMethod.CreateDelegate(typeof(WriteCharDelegate));
            }

            var readCharMethod = staticConverterType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ReadOnlySpan<char>), typeof(int).MakeByRefType() }, null);
            if (readCharMethod != null)
            {
                if (readCharMethod.ReturnType == typeof(T))
                {
                    _readChar = (ReadCharDelegate)readCharMethod.CreateDelegate(typeof(ReadCharDelegate));
                }
                else
                {
                    _readCharMethod = readCharMethod;
                    if (_valueTypeReader == null)
                    {
                        var helperType = typeof(ValueTypeReaderHelper<,>).MakeGenericType(typeof(T), typeof(T));
                        _valueTypeReader = (IValueTypeReader<T>)Activator.CreateInstance(helperType);
                    }
                }
            }

            var getSizeCharMethod = staticConverterType.GetMethod("GetSize", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(T) }, null);
            if (getSizeCharMethod != null)
            {
                _getSizeChar = (GetSizeCharDelegate)getSizeCharMethod.CreateDelegate(typeof(GetSizeCharDelegate));
            }

            if (_writeUtf8 == null && _writeChar == null)
            {
                ThrowHelper.ThrowInvalidConverterWrite(staticConverterType);
            }
            if (_readUtf8 == null && _readChar == null && _valueTypeReader == null)
            {
                ThrowHelper.ThrowInvalidConverterRead(staticConverterType);
            }
        }

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            using (var doc = JsonDocument.ParseValue(ref reader))
            {
                if (_readUtf8 != null)
                {
                    byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(doc.RootElement);
                    int index = 0;
                    return _readUtf8(bytes, ref index);
                }
                else if (_valueTypeReader != null && _readUtf8Method != null)
                {
                    byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(doc.RootElement);
                    int index = 0;
                    return _valueTypeReader.ReadUtf8(bytes, ref index, _readUtf8Method);
                }
                else if (_readChar != null)
                {
                    string jsonStr = doc.RootElement.GetRawText();
                    int index = 0;
                    return _readChar(jsonStr.AsSpan(), ref index);
                }
                else if (_valueTypeReader != null && _readCharMethod != null)
                {
                    string jsonStr = doc.RootElement.GetRawText();
                    int index = 0;
                    return _valueTypeReader.ReadChar(jsonStr.AsSpan(), ref index, _readCharMethod);
                }
            }

            ThrowHelper.ThrowDeserializationNotSupported();
            return default;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (_writeUtf8 != null && _getSizeUtf8 != null)
            {
                int size = _getSizeUtf8(value);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
                try
                {
                    int index = 0;
                    _writeUtf8(buffer, ref index, value);
                    writer.WriteRawValue(buffer.AsSpan(0, index));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            else if (_writeChar != null && _getSizeChar != null)
            {
                int size = _getSizeChar(value);
                char[] buffer = ArrayPool<char>.Shared.Rent(size);
                try
                {
                    int index = 0;
                    _writeChar(buffer, ref index, value);
                    
                    ReadOnlySpan<char> jsonChars = buffer.AsSpan(0, index);
                    int byteCount = System.Text.Encoding.UTF8.GetByteCount(jsonChars);
                    byte[] byteBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
                    try
                    {
                        int written = System.Text.Encoding.UTF8.GetBytes(jsonChars, byteBuffer);
                        writer.WriteRawValue(byteBuffer.AsSpan(0, written));
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(byteBuffer);
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }
            else
            {
                ThrowHelper.ThrowSerializationNotSupported();
            }
        }
    }

    internal interface IValueTypeReader<T>
    {
        T ReadUtf8(ReadOnlySpan<byte> span, ref int index, MethodInfo method);
        T ReadChar(ReadOnlySpan<char> span, ref int index, MethodInfo method);
    }

    internal class ValueTypeReaderHelper<TVal, T> : IValueTypeReader<T> where TVal : struct
    {
        private delegate TVal? ReadUtf8NullableDelegate(ReadOnlySpan<byte> span, ref int index);
        private delegate TVal? ReadCharNullableDelegate(ReadOnlySpan<char> span, ref int index);

        private ReadUtf8NullableDelegate? _utf8Delegate;
        private ReadCharNullableDelegate? _charDelegate;

        public T ReadUtf8(ReadOnlySpan<byte> span, ref int index, MethodInfo method)
        {
            _utf8Delegate ??= (ReadUtf8NullableDelegate)method.CreateDelegate(typeof(ReadUtf8NullableDelegate));
            var res = _utf8Delegate(span, ref index);
            if (res == null) return default!;
            return (T)(object)res.Value;
        }

        public T ReadChar(ReadOnlySpan<char> span, ref int index, MethodInfo method)
        {
            _charDelegate ??= (ReadCharNullableDelegate)method.CreateDelegate(typeof(ReadCharNullableDelegate));
            var res = _charDelegate(span, ref index);
            if (res == null) return default!;
            return (T)(object)res.Value;
        }
    }
}
