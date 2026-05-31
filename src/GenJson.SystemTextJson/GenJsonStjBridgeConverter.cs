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
                _readUtf8 = (ReadUtf8Delegate)readUtf8Method.CreateDelegate(typeof(ReadUtf8Delegate));
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
                _readChar = (ReadCharDelegate)readCharMethod.CreateDelegate(typeof(ReadCharDelegate));
            }

            var getSizeCharMethod = staticConverterType.GetMethod("GetSize", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(T) }, null);
            if (getSizeCharMethod != null)
            {
                _getSizeChar = (GetSizeCharDelegate)getSizeCharMethod.CreateDelegate(typeof(GetSizeCharDelegate));
            }

            if (_writeUtf8 == null && _writeChar == null)
            {
                throw new InvalidOperationException($"The converter {staticConverterType.FullName} must define either static WriteJsonUtf8 or WriteJson.");
            }
            if (_readUtf8 == null && _readChar == null)
            {
                throw new InvalidOperationException($"The converter {staticConverterType.FullName} must define either static FromJsonUtf8 or FromJson.");
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
                else if (_readChar != null)
                {
                    string jsonStr = doc.RootElement.GetRawText();
                    int index = 0;
                    return _readChar(jsonStr.AsSpan(), ref index);
                }
            }

            throw new NotSupportedException("No valid deserialization method found on custom converter.");
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
                throw new NotSupportedException("No valid serialization method found on custom converter.");
            }
        }
    }
}
