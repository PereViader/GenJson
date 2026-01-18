#nullable enable
namespace GenJson
{
    using System;
    using System.Globalization;
    using System.Buffers;

    public static class GenJsonParser
    {
        public static void SkipWhitespace(ReadOnlySpan<char> json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        public static void Expect(ReadOnlySpan<char> json, ref int index, char expected)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != expected)
            {
                throw new Exception($"Expected '{expected}' at {index}");
            }
            index++;
        }

        public static string ParseString(ReadOnlySpan<char> json, ref int index)
        {
            Expect(json, ref index, '"');
            int start = index;
            bool escaped = false;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    if (!escaped)
                        return new string(json.Slice(start, index - start - 1));

                    var content = json.Slice(start, index - start - 1);
                    return UnescapeString(content);
                }

                if (c == '\\')
                {
                    escaped = true;
                    if (index >= json.Length) throw new Exception("Unexpected end of json string at " + index);
                    c = json[index++];
                    if (c == 'u') index += 4;
                }
            }
            throw new Exception("Unterminated string at " + index);
        }

        private static string UnescapeString(ReadOnlySpan<char> input)
        {
            int maxLen = input.Length;
            if (maxLen <= 128)
            {
                Span<char> buffer = stackalloc char[maxLen];
                int written = UnescapeInto(input, buffer);
                return new string(buffer.Slice(0, written));
            }
            else
            {
                char[] rented = ArrayPool<char>.Shared.Rent(maxLen);
                try
                {
                    int written = UnescapeInto(input, rented);
                    return new string(rented, 0, written);
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(rented);
                }
            }
        }

        private static int UnescapeInto(ReadOnlySpan<char> input, Span<char> output)
        {
            int readIdx = 0;
            int writeIdx = 0;
            while (readIdx < input.Length)
            {
                var c = input[readIdx++];
                if (c == '\\')
                {
                    c = input[readIdx++];
                    switch (c)
                    {
                        case '"': output[writeIdx++] = '"'; break;
                        case '\\': output[writeIdx++] = '\\'; break;
                        case '/': output[writeIdx++] = '/'; break;
                        case 'b': output[writeIdx++] = '\b'; break;
                        case 'f': output[writeIdx++] = '\f'; break;
                        case 'n': output[writeIdx++] = '\n'; break;
                        case 'r': output[writeIdx++] = '\r'; break;
                        case 't': output[writeIdx++] = '\t'; break;
                        case 'u':
                            var hexSequence = input.Slice(readIdx, 4);
                            readIdx += 4;
                            output[writeIdx++] = (char)int.Parse(hexSequence, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            break;
                        default: output[writeIdx++] = c; break;
                    }
                }
                else
                {
                    output[writeIdx++] = c;
                }
            }
            return writeIdx;
        }

        public static void SkipString(ReadOnlySpan<char> json, ref int index)
        {
            Expect(json, ref index, '"');
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"') return;
                if (c == '\\')
                {
                    if (index >= json.Length) throw new Exception("Unexpected end of json string at " + index);
                    index++;
                }
            }
            throw new Exception("Unterminated string at " + index);
        }

        public static bool MatchesKey(ReadOnlySpan<char> json, ref int index, string expected)
        {
            int originalIndex = index;
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != '"')
            {
                index = originalIndex;
                return false;
            }
            index++; // '"'

            int expectedIndex = 0;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    if (expectedIndex == expected.Length) return true;
                    index = originalIndex;
                    return false;
                }

                if (c == '\\')
                {
                    if (index >= json.Length) throw new Exception("Unexpected end of json string at " + index);
                    c = json[index++];
                    char unescaped;
                    switch (c)
                    {
                        case '"': unescaped = '"'; break;
                        case '\\': unescaped = '\\'; break;
                        case '/': unescaped = '/'; break;
                        case 'b': unescaped = '\b'; break;
                        case 'f': unescaped = '\f'; break;
                        case 'n': unescaped = '\n'; break;
                        case 'r': unescaped = '\r'; break;
                        case 't': unescaped = '\t'; break;
                        case 'u':
                            var hexSequence = json.Slice(index, 4);
                            index += 4;
                            unescaped = (char)int.Parse(hexSequence, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            break;
                        default: unescaped = c; break;
                    }
                    if (expectedIndex >= expected.Length || expected[expectedIndex++] != unescaped)
                    {
                        index = originalIndex;
                        return false;
                    }
                }
                else
                {
                    if (expectedIndex >= expected.Length || expected[expectedIndex++] != c)
                    {
                        index = originalIndex;
                        return false;
                    }
                }
            }
            index = originalIndex;
            return false;
        }

        public static char ParseChar(ReadOnlySpan<char> json, ref int index)
        {
            var s = ParseString(json, ref index);
            if (s.Length != 1) throw new Exception("Expected string of length 1 for char at " + index);
            return s[0];
        }

        public static int ParseInt(ReadOnlySpan<char> json, ref int index) => (int)ParseLong(json, ref index);
        public static uint ParseUInt(ReadOnlySpan<char> json, ref int index) => (uint)ParseLong(json, ref index);
        public static short ParseShort(ReadOnlySpan<char> json, ref int index) => (short)ParseLong(json, ref index);
        public static ushort ParseUShort(ReadOnlySpan<char> json, ref int index) => (ushort)ParseLong(json, ref index);
        public static byte ParseByte(ReadOnlySpan<char> json, ref int index) => (byte)ParseLong(json, ref index);
        public static sbyte ParseSByte(ReadOnlySpan<char> json, ref int index) => (sbyte)ParseLong(json, ref index);

        public static long ParseLong(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return long.Parse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static ulong ParseULong(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return ulong.Parse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static double ParseDouble(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            // Handle negative numbers manually or let double.Parse handle it?
            // double.Parse handles leading sign if allowed.
            // But the manual skipping loop below seems to want to find the end of the number.
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return double.Parse(slice, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static float ParseFloat(ReadOnlySpan<char> json, ref int index) => (float)ParseDouble(json, ref index);

        public static decimal ParseDecimal(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return decimal.Parse(slice, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        public static bool ParseBoolean(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("true".AsSpan()))
            {
                int nextIndex = index + 4;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 4;
                    return true;
                }
            }
            if (json.Length - index >= 5 && json.Slice(index, 5).SequenceEqual("false".AsSpan()))
            {
                int nextIndex = index + 5;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 5;
                    return false;
                }
            }
            throw new Exception("Expected boolean at " + index);
        }

        private static bool IsDelimiter(char c)
        {
            return char.IsWhiteSpace(c) || c == ',' || c == '}' || c == ']';
        }

        public static bool IsNull(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            return json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("null".AsSpan());
        }

        public static void ParseNull(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("null".AsSpan()))
            {
                index += 4;
                return;
            }
            throw new Exception("Expected null at " + index);
        }

        public static void SkipValue(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return;
            char c = json[index];
            if (c == '"')
            {
                SkipString(json, ref index);
            }
            else if (c == '{')
            {
                index++;
                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == '}')
                    {
                        index++;
                        return;
                    }
                    SkipValue(json, ref index); // Key (string) is a value
                    SkipWhitespace(json, ref index);
                    if (json[index] == ':') index++;
                    SkipValue(json, ref index); // Value
                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
            }
            else if (c == '[')
            {
                index++;
                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == ']')
                    {
                        index++;
                        return;
                    }
                    SkipValue(json, ref index);
                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
            }
            else if (char.IsDigit(c) || c == '-')
            {
                if (c == '-') index++;
                while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            }
            else if (c == 't') // true
            {
                index += 4;
            }
            else if (c == 'f') // false
            {
                index += 5;
            }
            else if (c == 'n') // null
            {
                index += 4;
            }
            else
            {
                index++; // Unknown
            }
        }
    }
}
