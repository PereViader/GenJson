using System;
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace GenJson
{
    public static class GenJsonParser
    {
        private static readonly byte[] NullBytes = Encoding.UTF8.GetBytes("null");
        private static readonly byte[] TrueBytes = Encoding.UTF8.GetBytes("true");
        private static readonly byte[] FalseBytes = Encoding.UTF8.GetBytes("false");


        public static bool TryExpect(ReadOnlySpan<char> json, ref int index, char expected)
        {
            if (index >= json.Length || json[index] != expected) return false;
            index++;
            return true;
        }

        public static bool TryParseStringSpan(ReadOnlySpan<char> json, ref int index, out ReadOnlySpan<char> result, out bool escaped)
        {
            result = default;
            escaped = false;
            if (!TryExpect(json, ref index, '"')) return false;

            var start = index;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    result = json.Slice(start, index - start - 1);
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

        public static bool TryParseString(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out string? result)
        {
            result = null;
            if (!TryExpect(json, ref index, '"')) return false;

            var start = index;
            var escaped = false;
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



        public static string UnescapeString(ReadOnlySpan<char> input)
        {
            var maxLen = input.Length;
            if (maxLen <= 128)
            {
                Span<char> buffer = stackalloc char[maxLen];
                var written = UnescapeInto(input, buffer);
                return new string(buffer.Slice(0, written));
            }

            var rented = ArrayPool<char>.Shared.Rent(maxLen);
            try
            {
                var written = UnescapeInto(input, rented);
                return new string(rented, 0, written);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }

        private static int UnescapeInto(ReadOnlySpan<char> input, Span<char> output)
        {
            var readIdx = 0;
            var writeIdx = 0;
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
                            output[writeIdx++] = (char)int.Parse(hexSequence, NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture);
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


        public static string UnescapeStringUtf8(ReadOnlySpan<byte> input)
        {
            var rented = ArrayPool<char>.Shared.Rent(input.Length);
            try
            {
                int written = 0;
                int readIdx = 0;
                while (readIdx < input.Length)
                {
                    var c = (char)input[readIdx++];
                    if (c == '\\')
                    {
                        c = (char)input[readIdx++];
                        switch (c)
                        {
                            case '"': rented[written++] = '"'; break;
                            case '\\': rented[written++] = '\\'; break;
                            case '/': rented[written++] = '/'; break;
                            case 'b': rented[written++] = '\b'; break;
                            case 'f': rented[written++] = '\f'; break;
                            case 'n': rented[written++] = '\n'; break;
                            case 'r': rented[written++] = '\r'; break;
                            case 't': rented[written++] = '\t'; break;
                            case 'u':
                                Span<char> hexChars = stackalloc char[4];
                                for (int i = 0; i < 4; i++) hexChars[i] = (char)input[readIdx++];
                                rented[written++] = (char)int.Parse(hexChars, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                break;
                            default: rented[written++] = c; break;
                        }
                    }
                    else
                    {
                        rented[written++] = c;
                    }
                }
                return new string(rented, 0, written);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }


        public static bool TrySkipString(ReadOnlySpan<char> json, ref int index)
        {
            if (!TryExpect(json, ref index, '"')) return false;

            while (index < json.Length)
            {
                var slice = json.Slice(index);
                var offset = slice.IndexOfAny('"', '\\');
                if (offset < 0) return false;

                index += offset;
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
            var originalIndex = index;

            if (index >= json.Length || json[index] != '"')
            {
                index = originalIndex;
                return false;
            }

            // Fast path: Exact match, no escapes
            int requiredLength = expected.Length + 2; // +2 for quotes
            if (json.Length - index >= requiredLength)
            {
                var slice = json.Slice(index + 1, expected.Length);
                if (slice.SequenceEqual(expected.AsSpan()) && json[index + expected.Length + 1] == '"')
                {
                    index += requiredLength;
                    return true;
                }
            }

            index++; // '"'

            var expectedIndex = 0;
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
                            unescaped = (char)int.Parse(hexSequence, NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture);
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

        public static bool TryParseBoolean(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out bool? result)
        {
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("true".AsSpan()))
            {
                var nextIndex = index + 4;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 4;
                    result = true;
                    return true;
                }
            }

            if (json.Length - index >= 5 && json.Slice(index, 5).SequenceEqual("false".AsSpan()))
            {
                var nextIndex = index + 5;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 5;
                    result = false;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static bool TryParseBoolean(ReadOnlySpan<char> json, ref int index, out bool result)
        {
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("true".AsSpan()))
            {
                var nextIndex = index + 4;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 4;
                    result = true;
                    return true;
                }
            }

            if (json.Length - index >= 5 && json.Slice(index, 5).SequenceEqual("false".AsSpan()))
            {
                var nextIndex = index + 5;
                if (nextIndex >= json.Length || IsDelimiter(json[nextIndex]))
                {
                    index += 5;
                    result = false;
                    return true;
                }
            }

            result = default;
            return false;
        }

        public static bool TryParseChar(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out char? result)
        {
            if (!TryParseString(json, ref index, out var s) || s.Length != 1)
            {
                result = null;
                return false;
            }

            result = s[0];
            return true;
        }

        public static bool TryParseChar(ReadOnlySpan<char> json, ref int index, out char result)
        {
            if (!TryParseString(json, ref index, out var s) || s.Length != 1)
            {
                result = default;
                return false;
            }

            result = s[0];
            return true;
        }

        public static bool TryParseInt(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out int? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (int.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseInt(ReadOnlySpan<char> json, ref int index, out int result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return int.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseUInt(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out uint? result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (uint.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseUInt(ReadOnlySpan<char> json, ref int index, out uint result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return uint.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseShort(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out short? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (short.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseShort(ReadOnlySpan<char> json, ref int index, out short result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return short.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseUShort(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out ushort? result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (ushort.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseUShort(ReadOnlySpan<char> json, ref int index, out ushort result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return ushort.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseByte(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out byte? result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (byte.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseByte(ReadOnlySpan<char> json, ref int index, out byte result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return byte.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseSByte(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out sbyte? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (sbyte.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseSByte(ReadOnlySpan<char> json, ref int index, out sbyte result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return sbyte.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseLong(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out long? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseLong(ReadOnlySpan<char> json, ref int index, out long result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return long.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseULong(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out ulong? result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            if (ulong.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseULong(ReadOnlySpan<char> json, ref int index, out ulong result)
        {
            var start = index;
            while (index < json.Length && (char.IsDigit(json[index]))) index++;
            var slice = json.Slice(start, index - start);
            return ulong.TryParse(slice, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseDouble(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out double? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            if (double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseDouble(ReadOnlySpan<char> json, ref int index, out double result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseFloat(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out float? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            if (float.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseFloat(ReadOnlySpan<char> json, ref int index, out float result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return float.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        public static bool TryParseDecimal(ReadOnlySpan<char> json, ref int index, [NotNullWhen(true)] out decimal? result)
        {
            var start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            if (decimal.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out var varRes))
            {
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseDecimal(ReadOnlySpan<char> json, ref int index, out decimal result)
        {
            var start = index;
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
            var c = json[index];
            if (c == '"') // string
                return TrySkipString(json, ref index);

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
                var slice = json.Slice(index);
                var offset = slice.IndexOfAny(',', '}', ']');
                if (offset >= 0)
                {
                    index += offset;
                }
                else
                {
                    index = json.Length;
                }
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

            var count = 0;
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

            var count = 0;
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

        public static bool TryFindProperty(ReadOnlySpan<char> json, int startIndex, string propertyName,
            out int valueIndex)
        {
            valueIndex = -1;
            var index = startIndex;

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

        public static bool TryExpect(ReadOnlySpan<byte> json, ref int index, byte expected)
        {
            if (index >= json.Length || json[index] != expected) return false;
            index++;
            return true;
        }

        public static bool TryParseStringSpan(ReadOnlySpan<byte> json, ref int index, out ReadOnlySpan<byte> result, out bool escaped)
        {
            result = default;
            escaped = false;
            if (!TryExpect(json, ref index, (byte)'"')) return false;

            var start = index;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    result = json.Slice(start, index - start - 1);
                    return true;
                }

                if (c == '\\')
                {
                    escaped = true;
                    if (index >= json.Length) return false;
                    index++;
                }
            }

            return false;
        }

        public static bool TryParseString(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out string? result)
        {
            result = null;
            if (!TryExpect(json, ref index, (byte)'"')) return false;

            var start = index;
            var escaped = false;
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"')
                {
                    if (!escaped)
                    {
                        result = Encoding.UTF8.GetString(json.Slice(start, index - start - 1));
                        return true;
                    }

                    result = UnescapeStringUtf8(json.Slice(start, index - start - 1));
                    return true;
                }

                if (c == '\\')
                {
                    escaped = true;
                    if (index >= json.Length) return false;
                    index++;
                }
            }

            return false;
        }

        public static bool MatchesKey(ReadOnlySpan<byte> json, ref int index, string expected)
        {
            var originalIndex = index;

            if (index >= json.Length || json[index] != '"')
            {
                index = originalIndex;
                return false;
            }

            // Fast path: ASCII match, no escapes
            int requiredLength = expected.Length + 2; // +2 for quotes
            if (json.Length - index >= requiredLength)
            {
                bool fastMatch = true;
                for (int i = 0; i < expected.Length; i++)
                {
                    char c = expected[i];
                    if (c > 127 || json[index + 1 + i] != (byte)c)
                    {
                        fastMatch = false;
                        break;
                    }
                }

                if (fastMatch && json[index + expected.Length + 1] == (byte)'"')
                {
                    index += requiredLength;
                    return true;
                }
            }

            index++;

            var expectedIndex = 0;
            while (index < json.Length)
            {
                var b = json[index++];
                if (b == '"')
                {
                    if (expectedIndex == expected.Length) return true;
                    index = originalIndex;
                    return false;
                }

                if (b == '\\')
                {
                    if (index >= json.Length)
                    {
                        index = originalIndex;
                        return false;
                    }

                    b = json[index++];

                    // Decode properly for escape
                    // Since this is rare path in keys, fallback to decoding the key if needed?
                    // But we already advanced index.
                    // Just decode the escape char properly.
                    char unescaped;
                    switch ((char)b)
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
                            // Hex
                            if (index + 4 > json.Length)
                            {
                                index = originalIndex;
                                return false;
                            }

                            var val = 0;
                            for (var i = 0; i < 4; i++)
                            {
                                var h = json[index + i];
                                val <<= 4;
                                if (h >= '0' && h <= '9')
                                {
                                    val |= h - '0';
                                }
                                else if (h >= 'a' && h <= 'f')
                                {
                                    val |= h - 'a' + 10;
                                }
                                else if (h >= 'A' && h <= 'F')
                                {
                                    val |= h - 'A' + 10;
                                }
                                else
                                {
                                    index = originalIndex;
                                    return false;
                                }
                            }

                            index += 4;
                            unescaped = (char)val;
                            break;
                        default: unescaped = (char)b; break;
                    }

                    if (expectedIndex >= expected.Length || expected[expectedIndex++] != unescaped)
                    {
                        index = originalIndex;
                        return false;
                    }
                }
                else
                {
                    if (b < 128)
                    {
                        if (expectedIndex >= expected.Length || expected[expectedIndex++] != (char)b)
                        {
                            index = originalIndex;
                            return false;
                        }
                    }
                    else
                    {
                        // Non-ASCII. Fallback to full string decode for simplicity and correctness.
                        // But we are in the middle of parsing.
                        // We can try to decode next char from json.

                        // Simple check: 
                        // If we are at non-ascii, just assume mismatch for now in "fast" logic?
                        // No, user might use non-ascii keys.

                        // Try to match expected[expectedIndex] encoded as UTF8?
                        // Or decode json char.

                        // Let's decode one codepoint from JSON manually?
                        // Or use Utf8Parser? No.
                        // Bit manipulation for UTF8 is standard.

                        var codepoint = 0;
                        var extraBytes = 0;
                        if ((b & 0xE0) == 0xC0)
                        {
                            codepoint = b & 0x1F;
                            extraBytes = 1;
                        }
                        else if ((b & 0xF0) == 0xE0)
                        {
                            codepoint = b & 0x0F;
                            extraBytes = 2;
                        }
                        else if ((b & 0xF8) == 0xF0)
                        {
                            codepoint = b & 0x07;
                            extraBytes = 3;
                        }
                        else
                        {
                            index = originalIndex;
                            return false;
                        } // Invalid start byte

                        if (index + extraBytes > json.Length)
                        {
                            index = originalIndex;
                            return false;
                        }

                        for (var i = 0; i < extraBytes; i++)
                        {
                            var next = json[index++];
                            if ((next & 0xC0) != 0x80)
                            {
                                index = originalIndex;
                                return false;
                            }

                            codepoint = (codepoint << 6) | (next & 0x3F);
                        }

                        // Codepoint to char(s)
                        if (codepoint <= 0xFFFF)
                        {
                            if (expectedIndex >= expected.Length || expected[expectedIndex++] != (char)codepoint)
                            {
                                index = originalIndex;
                                return false;
                            }
                        }
                        else
                        {
                            // Surrogate pair in expected string?
                            // expected is UTF-16
                            // codepoint > 0xFFFF
                            // TODO: Handle surrogate logic or just fail?
                            // Standard JSON libraries handle this.
                            // For now, let's just fail if complex unicode. this is edge case.
                            // Or better: Use string comparison fallback.
                            index = originalIndex;
                            return false;
                        }
                    }
                }
            }

            index = originalIndex;
            return false;
        }

        public static bool MatchesKey(ReadOnlySpan<byte> json, ref int index, string expected, ReadOnlySpan<byte> expectedUtf8)
        {
            int requiredLength = expectedUtf8.Length + 2;
            if (json.Length - index >= requiredLength)
            {
                if (json[index] == (byte)'"' &&
                    json[index + expectedUtf8.Length + 1] == (byte)'"' &&
                    json.Slice(index + 1, expectedUtf8.Length).SequenceEqual(expectedUtf8))
                {
                    index += requiredLength;
                    return true;
                }
            }
            // Fall back to original slow implementation, which will handle escapes
            return MatchesKey(json, ref index, expected);
        }

        public static bool TryParseBoolean(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out bool? result)
        {
            if (json.Length - index >= 4)
            {
                var slice = json.Slice(index, 4);
                if (slice.SequenceEqual(TrueBytes))
                {
                    var next = index + 4;
                    if (next >= json.Length || IsDelimiter(json[next]))
                    {
                        index += 4;
                        result = true;
                        return true;
                    }
                }
            }

            if (json.Length - index >= 5)
            {
                var slice = json.Slice(index, 5);
                if (slice.SequenceEqual(FalseBytes))
                {
                    var next = index + 5;
                    if (next >= json.Length || IsDelimiter(json[next]))
                    {
                        index += 5;
                        result = false;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public static bool TryParseBoolean(ReadOnlySpan<byte> json, ref int index, out bool result)
        {
            if (json.Length - index >= 4)
            {
                var slice = json.Slice(index, 4);
                if (slice.SequenceEqual(TrueBytes))
                {
                    var next = index + 4;
                    if (next >= json.Length || IsDelimiter(json[next]))
                    {
                        index += 4;
                        result = true;
                        return true;
                    }
                }
            }

            if (json.Length - index >= 5)
            {
                var slice = json.Slice(index, 5);
                if (slice.SequenceEqual(FalseBytes))
                {
                    var next = index + 5;
                    if (next >= json.Length || IsDelimiter(json[next]))
                    {
                        index += 5;
                        result = false;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        public static bool TryParseChar(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out char? result)
        {
            if (!TryParseString(json, ref index, out var s) || s!.Length != 1)
            {
                result = null;
                return false;
            }
            result = s[0];
            return true;
        }

        public static bool TryParseChar(ReadOnlySpan<byte> json, ref int index, out char result)
        {
            if (!TryParseString(json, ref index, out var s) || s!.Length != 1)
            {
                result = default;
                return false;
            }
            result = s[0];
            return true;
        }

        public static bool TryParseInt(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out int? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out int varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseInt(ReadOnlySpan<byte> json, ref int index, out int result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseUInt(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out uint? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out uint varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseUInt(ReadOnlySpan<byte> json, ref int index, out uint result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseShort(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out short? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out short varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseShort(ReadOnlySpan<byte> json, ref int index, out short result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseUShort(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out ushort? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out ushort varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseUShort(ReadOnlySpan<byte> json, ref int index, out ushort result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseByte(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out byte? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out byte varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseByte(ReadOnlySpan<byte> json, ref int index, out byte result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseSByte(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out sbyte? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out sbyte varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseSByte(ReadOnlySpan<byte> json, ref int index, out sbyte result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseLong(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out long? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out long varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseLong(ReadOnlySpan<byte> json, ref int index, out long result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseULong(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out ulong? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out ulong varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseULong(ReadOnlySpan<byte> json, ref int index, out ulong result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseDouble(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out double? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out double varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseDouble(ReadOnlySpan<byte> json, ref int index, out double result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseFloat(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out float? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out float varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseFloat(ReadOnlySpan<byte> json, ref int index, out float result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseDecimal(ReadOnlySpan<byte> json, ref int index, [NotNullWhen(true)] out decimal? result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out decimal varRes, out var bytesConsumed))
            {
                index += bytesConsumed;
                result = varRes;
                return true;
            }

            result = null;
            return false;
        }

        public static bool TryParseDecimal(ReadOnlySpan<byte> json, ref int index, out decimal result)
        {
            if (Utf8Parser.TryParse(json.Slice(index), out result, out var bytesConsumed))
            {
                index += bytesConsumed;
                return true;
            }
            return false;
        }

        public static bool TryParseNull(ReadOnlySpan<byte> json, ref int index)
        {
            //TODO: maybe optimize the length
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual(NullBytes))
            {
                index += 4;
                return true;
            }

            return false;
        }

        public static bool TrySkipValue(ReadOnlySpan<byte> json, ref int index)
        {
            if (index >= json.Length) return false;
            var c = json[index];
            if (c == '"') // string
                return TrySkipString(json, ref index);

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
            else if ((c >= '0' && c <= '9') || c == '-')
            {
                var slice = json.Slice(index);
                var offset = slice.IndexOfAny((byte)',', (byte)'}', (byte)']');
                if (offset >= 0)
                {
                    index += offset;
                }
                else
                {
                    index = json.Length;
                }
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

        public static bool TrySkipString(ReadOnlySpan<byte> json, ref int index)
        {
            if (!TryExpect(json, ref index, (byte)'"')) return false;

            while (index < json.Length)
            {
                var slice = json.Slice(index);
                var offset = slice.IndexOfAny((byte)'"', (byte)'\\');
                if (offset < 0) return false;

                index += offset;
                var c = json[index++];
                if (c == (byte)'"') return true;
                if (c == (byte)'\\')
                {
                    if (index >= json.Length) return false;
                    index++;
                }
            }

            return false;
        }

        public static int CountListItems(ReadOnlySpan<byte> json, int index)
        {
            if (index >= json.Length || json[index] == ']') return 0;

            var count = 0;
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

        public static int CountDictionaryItems(ReadOnlySpan<byte> json, int index)
        {
            if (index >= json.Length || json[index] == '}') return 0;

            var count = 0;
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

        public static bool TryFindProperty(ReadOnlySpan<byte> json, int startIndex, string propertyName,
            out int valueIndex)
        {
            valueIndex = -1;
            var index = startIndex;

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


        private static bool IsDelimiter(char c)
        {
            return c is ',' or '}' or ']';
        }

        private static bool IsDelimiter(byte c)
        {
            return c == ',' || c == '}' || c == ']';
        }
    }
}