#nullable enable
using System;
using System.Globalization;
using System.Buffers;

namespace GenJson
{
    public static class GenJsonParser
    {


        public static bool TryExpect(ReadOnlySpan<char> json, ref int index, char expected)
        {

            if (index >= json.Length || json[index] != expected)
            {
                return false;
            }
            index++;
            return true;
        }

        public static bool TryParseString(ReadOnlySpan<char> json, ref int index, out string? result)
        {
            result = null;
            if (!TryExpect(json, ref index, '"')) return false;

            int start = index;
            bool escaped = false;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    if (!escaped)
                    {
                        result = new string(json.Slice(start, index - start - 1));
                        return true;
                    }

                    var content = json.Slice(start, index - start - 1);
                    result = UnescapeString(content);
                    return true;
                }

                if (c == '\\')
                {
                    escaped = true;
                    if (index >= json.Length) return false;
                    c = json[index++];
                    if (c == 'u') index += 4;
                }
            }
            return false;
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

        public static bool TrySkipString(ReadOnlySpan<char> json, ref int index)
        {
            if (!TryExpect(json, ref index, '"')) return false;

            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"') return true;
                if (c == '\\')
                {
                    if (index >= json.Length) return false;
                    index++;
                }
            }
            return false;
        }

        public static bool MatchesKey(ReadOnlySpan<char> json, ref int index, string expected)
        {
            int originalIndex = index;

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
                    if (index >= json.Length) return false;
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

        public static bool TryParseBoolean(ReadOnlySpan<char> json, ref int index, out bool result)
        {
            result = default;

            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("true".AsSpan()))
            {
                int nextIndex = index + 4;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 4;
                    result = true;
                    return true;
                }
            }
            if (json.Length - index >= 5 && json.Slice(index, 5).SequenceEqual("false".AsSpan()))
            {
                int nextIndex = index + 5;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 5;
                    result = false;
                    return true;
                }
            }
            return false;
        }

        private static bool IsDelimiter(char c)
        {
            return c is ',' or '}' or ']';
        }

        public static bool TryParseChar(ReadOnlySpan<char> json, ref int index, out char result)
        {
            result = default;
            if (!TryParseString(json, ref index, out var s)) return false;
            if (s!.Length != 1) return false;
            result = s[0];
            return true;
        }

        public static bool TryParseInt(ReadOnlySpan<char> json, ref int index, out int result) { if (TryParseLong(json, ref index, out var l)) { result = (int)l; return true; } result = 0; return false; }
        public static bool TryParseUInt(ReadOnlySpan<char> json, ref int index, out uint result) { if (TryParseLong(json, ref index, out var l)) { result = (uint)l; return true; } result = 0; return false; }
        public static bool TryParseShort(ReadOnlySpan<char> json, ref int index, out short result) { if (TryParseLong(json, ref index, out var l)) { result = (short)l; return true; } result = 0; return false; }
        public static bool TryParseUShort(ReadOnlySpan<char> json, ref int index, out ushort result) { if (TryParseLong(json, ref index, out var l)) { result = (ushort)l; return true; } result = 0; return false; }
        public static bool TryParseByte(ReadOnlySpan<char> json, ref int index, out byte result) { if (TryParseLong(json, ref index, out var l)) { result = (byte)l; return true; } result = 0; return false; }
        public static bool TryParseSByte(ReadOnlySpan<char> json, ref int index, out sbyte result) { if (TryParseLong(json, ref index, out var l)) { result = (sbyte)l; return true; } result = 0; return false; }

        public static bool TryParseLong(ReadOnlySpan<char> json, ref int index, out long result)
        {
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseULong(ReadOnlySpan<char> json, ref int index, out ulong result)
        {
            int start = index;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return ulong.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseDouble(ReadOnlySpan<char> json, ref int index, out double result)
        {
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseFloat(ReadOnlySpan<char> json, ref int index, out float result)
        {
            if (TryParseDouble(json, ref index, out var d))
            {
                result = (float)d; 
                return true;
            }
            result = 0; 
            return false;
        }

        public static bool TryParseDecimal(ReadOnlySpan<char> json, ref int index, out decimal result)
        {
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return decimal.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseNull(ReadOnlySpan<char> json, ref int index)
        {
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("null".AsSpan()))
            {
                index += 4;
                return true;
            }
            return false;
        }

        public static bool TrySkipValue(ReadOnlySpan<char> json, ref int index)
        {
            if (index >= json.Length) return false;
            char c = json[index];
            if (c == '"') // string
            {
                return TrySkipString(json, ref index);
            }

            if (c == '{') //object
            {
                index++;
                while (index < json.Length)
                {

                    if (index >= json.Length) return false;
                    if (json[index] == '}')
                    {
                        index++;
                        return true;
                    }
                    if (!TrySkipValue(json, ref index)) return false;

                    if (index >= json.Length) return false;
                    if (json[index] == ':') index++;
                    if (!TrySkipValue(json, ref index)) return false;

                    if (index >= json.Length) return false;
                    if (json[index] == ',') index++;
                }
            }
            else if (c == '[') //collection
            {
                index++;
                while (index < json.Length)
                {

                    if (index >= json.Length) return false;
                    if (json[index] == ']')
                    {
                        index++;
                        return true;
                    }
                    if (!TrySkipValue(json, ref index)) return false;

                    if (index >= json.Length) return false;
                    if (json[index] == ',') index++;
                }
            }
            else if (char.IsDigit(c) || c == '-')
            {
                if (c == '-') index++;
                while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
                return true;
            }
            else if (c == 't') //true
            {
                index += 4;
                return true;
            }
            else if (c == 'f') //false
            {
                index += 5;
                return true;
            }
            else if (c == 'n') //null
            {
                index += 4;
                return true;
            }
            else
            {
                if (IsDelimiter(c) && c != '-') return false;
                index++;
                return true;
            }
            return false;
        }

        public static int CountListItems(ReadOnlySpan<char> json, int index)
        {
            if (index >= json.Length || json[index] == ']') return 0;

            int count = 0;
            while (index < json.Length)
            {
                count++;
                TrySkipValue(json, ref index);

                if (index >= json.Length) return count;
                if (json[index] == ']') return count;
                if (json[index] == ',') index++;
                else return count;
            }
            return count;
        }

        public static int CountDictionaryItems(ReadOnlySpan<char> json, int index)
        {
            if (index >= json.Length || json[index] == '}') return 0;

            int count = 0;
            while (index < json.Length)
            {
                count++;
                if (!TrySkipString(json, ref index)) return count; // Skip Key

                if (index >= json.Length || json[index] != ':') return count;
                index++; // Skip colon
                if (!TrySkipValue(json, ref index)) return count; // Skip Value


                if (index >= json.Length) return count;
                if (json[index] == '}') return count;
                if (json[index] == ',') index++;
                else return count;
            }
            return count;
        }
        public static bool IsNull(ReadOnlySpan<char> json, ref int index)
        {
            return index + 4 <= json.Length && 
                   json.Slice(index, 4).SequenceEqual("null".AsSpan());
        }

        public static bool TryFindProperty(ReadOnlySpan<char> json, int startIndex, string propertyName, out int valueIndex)
        {
            valueIndex = -1;
            int index = startIndex;

            if (index >= json.Length || json[index] != '{') return false;
            index++;

            while (index < json.Length)
            {
                if (index >= json.Length || json[index] == '}') return false;

                if (MatchesKey(json, ref index, propertyName))
                {
                    if (index >= json.Length || json[index] != ':') return false;
                    index++;

                    valueIndex = index;
                    return true;
                }

                // Skip key
                if (!TrySkipString(json, ref index)) return false;


                if (index >= json.Length || json[index] != ':') return false;
                index++;

                if (!TrySkipValue(json, ref index)) return false;


                if (index >= json.Length) return false;
                if (json[index] == '}') return false;
                if (json[index] == ',') index++;
            }
            return false;
        }
    }
}
