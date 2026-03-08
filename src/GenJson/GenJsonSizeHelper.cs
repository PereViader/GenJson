#nullable enable
using System;
using System.Globalization;
using System.Text;

namespace GenJson
{
    public static class GenJsonSizeHelper
    {
        public static int GetSize(byte value)
        {
            if (value < 10) return 1;
            if (value < 100) return 2;
            return 3;
        }

        public static int GetSize(sbyte value)
        {
            uint v = (uint)(value >= 0 ? value : -value);
            int size = value >= 0 ? 0 : 1;
            if (v < 10) return size + 1;
            if (v < 100) return size + 2;
            return size + 3;
        }

        public static int GetSize(short value)
        {
            uint v = (uint)(value >= 0 ? value : -value);
            int size = value >= 0 ? 0 : 1;
            if (v < 100)
            {
                if (v < 10) return size + 1;
                return size + 2;
            }
            if (v < 1000) return size + 3;
            if (v < 10000) return size + 4;
            return size + 5;
        }

        public static int GetSize(ushort value)
        {
            if (value < 100)
            {
                if (value < 10) return 1;
                return 2;
            }
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            return 5;
        }

        public static int GetSize(int value)
        {
            uint v = (uint)(value >= 0 ? value : -value);
            int size = value >= 0 ? 0 : 1;
            if (v < 10000)
            {
                if (v < 100)
                {
                    if (v < 10) return size + 1;
                    return size + 2;
                }
                if (v < 1000) return size + 3;
                return size + 4;
            }
            if (v < 10000000)
            {
                if (v < 100000) return size + 5;
                if (v < 1000000) return size + 6;
                return size + 7;
            }
            if (v < 100000000) return size + 8;
            if (v < 1000000000) return size + 9;
            return size + 10;
        }

        public static int GetSize(uint value)
        {
            if (value < 10000)
            {
                if (value < 100)
                {
                    if (value < 10) return 1;
                    return 2;
                }
                if (value < 1000) return 3;
                return 4;
            }
            if (value < 10000000)
            {
                if (value < 100000) return 5;
                if (value < 1000000) return 6;
                return 7;
            }
            if (value < 100000000) return 8;
            if (value < 1000000000) return 9;
            return 10;
        }

        public static int GetSize(long value)
        {
            ulong v = (ulong)(value >= 0 ? value : -value);
            int size = value >= 0 ? 0 : 1;
            if (v < 1000000000ul)
            {
                if (v < 10000)
                {
                    if (v < 100)
                    {
                        if (v < 10) return size + 1;
                        return size + 2;
                    }
                    if (v < 1000) return size + 3;
                    return size + 4;
                }
                if (v < 10000000)
                {
                    if (v < 100000) return size + 5;
                    if (v < 1000000) return size + 6;
                    return size + 7;
                }
                if (v < 100000000) return size + 8;
                return size + 9;
            }
            if (v < 100000000000000ul)
            {
                if (v < 100000000000ul)
                {
                    if (v < 10000000000ul) return size + 10;
                    return size + 11;
                }
                if (v < 1000000000000ul) return size + 12;
                if (v < 10000000000000ul) return size + 13;
                return size + 14;
            }
            if (v < 100000000000000000ul)
            {
                if (v < 1000000000000000ul) return size + 15;
                if (v < 10000000000000000ul) return size + 16;
                return size + 17;
            }
            if (v < 1000000000000000000ul) return size + 18;
            if (v < 10000000000000000000ul) return size + 19;
            return size + 20;
        }

        public static int GetSize(ulong value)
        {
            if (value < 1000000000ul)
            {
                if (value < 10000)
                {
                    if (value < 100)
                    {
                        if (value < 10) return 1;
                        return 2;
                    }
                    if (value < 1000) return 3;
                    return 4;
                }
                if (value < 10000000)
                {
                    if (value < 100000) return 5;
                    if (value < 1000000) return 6;
                    return 7;
                }
                if (value < 100000000) return 8;
                return 9;
            }
            if (value < 100000000000000ul)
            {
                if (value < 100000000000ul)
                {
                    if (value < 10000000000ul) return 10;
                    return 11;
                }
                if (value < 1000000000000ul) return 12;
                if (value < 10000000000000ul) return 13;
                return 14;
            }
            if (value < 100000000000000000ul)
            {
                if (value < 1000000000000000ul) return 15;
                if (value < 10000000000000000ul) return 16;
                return 17;
            }
            if (value < 1000000000000000000ul) return 18;
            if (value < 10000000000000000000ul) return 19;
            return 20;
        }

        public static int GetSize(bool value) => value ? 4 : 5; // "true" or "false"

        public static int GetSize(char c) => c switch
        {
            '\n' => 4, // "\n"  (2 quotes + 2 content chars)
            '\r' => 4, // "\r"
            '\t' => 4, // "\t"
            '\b' => 4, // "\b"
            '\f' => 4, // "\f"
            '\\' => 4, // "\\"
            '\"' => 4, // "\""  (2 quotes + 2 content chars)
            '\0' => 8, // "\u0000" — \0 is not a JSON escape; must use \u0000 (2 quotes + 6 content chars)
            _ when char.IsControl(c) => 8, // "\uXXXX" (2 quotes + 6 content chars)
            _ => 3
        };

        public static int GetSize(ReadOnlySpan<char> input)
        {
            int length = 2;
            foreach (char c in input)
            {
                length += c switch
                {
                    '\n' => 2, // \n
                    '\r' => 2, // \r
                    '\t' => 2, // \t
                    '\b' => 2, // \b
                    '\f' => 2, // \f
                    '\\' => 2, // \\
                    '\"' => 2, // \"
                    '\0' => 6, // \u0000 — null is not \0 in JSON, must be \u0000
                    _ when char.IsControl(c) => 6, // \uXXXX format for other control chars
                    _ => 1
                };
            }
            return length;
        }

        public static int GetSize(Guid _) => 38;

        public static int GetSize(double value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture);
            return written;
        }

        public static int GetSize(float value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture);
            return written;
        }

        public static int GetSize(decimal value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "G", CultureInfo.InvariantCulture);
            return written;
        }

        public static int GetSize(DateTime value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture);
            return written + 2;
        }

        public static int GetSize(DateTimeOffset value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture);
            return written + 2;
        }

        public static int GetSize(TimeSpan value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written, "c", CultureInfo.InvariantCulture);
            return written + 2;
        }

        public static int GetSize(Version value)
        {
            Span<char> buffer = stackalloc char[128];
            value.TryFormat(buffer, out int written);
            return written + 2;
        }

        public static int GetSizeUtf8(byte value) => GetSize(value); // ASCII
        public static int GetSizeUtf8(sbyte value) => GetSize(value);
        public static int GetSizeUtf8(short value) => GetSize(value);
        public static int GetSizeUtf8(ushort value) => GetSize(value);
        public static int GetSizeUtf8(int value) => GetSize(value);
        public static int GetSizeUtf8(uint value) => GetSize(value);
        public static int GetSizeUtf8(long value) => GetSize(value);
        public static int GetSizeUtf8(ulong value) => GetSize(value);
        public static int GetSizeUtf8(bool value) => GetSize(value);
        public static int GetSizeUtf8(Guid value) => GetSize(value);
        public static int GetSizeUtf8(double value) => GetSize(value);
        public static int GetSizeUtf8(float value) => GetSize(value);
        public static int GetSizeUtf8(decimal value) => GetSize(value);
        public static int GetSizeUtf8(DateTime value) => GetSize(value);
        public static int GetSizeUtf8(DateTimeOffset value) => GetSize(value);
        public static int GetSizeUtf8(TimeSpan value) => GetSize(value);
        public static int GetSizeUtf8(Version value) => GetSize(value);

        public static int GetSizeUtf8(char c)
        {
            // Quoted char
            // \b, \f, \n, \r, \t, \\, \" -> 4 bytes ("\n")
            // Control -> 8 bytes ("\u0000")
            // ASCII -> 3 bytes ("A")
            // Non-ASCII -> 2 + bytes count

            if (c == '"' || c == '\\') return 4;
            if (c == '\b' || c == '\f' || c == '\n' || c == '\r' || c == '\t') return 4;
            if (char.IsControl(c)) return 8; // "\uXXXX"

            if (c < 128) return 3;
            return 2 + Encoding.UTF8.GetByteCount(new ReadOnlySpan<char>(new[] { c }));
        }

        public static int GetSizeUtf8(ReadOnlySpan<char> input)
        {
            int length = 2; // Quotes
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '"' || c == '\\') length += 2;
                else if (c == '\b' || c == '\f' || c == '\n' || c == '\r' || c == '\t') length += 2;
                else if (char.IsControl(c)) length += 6; // \uXXXX
                else if (c < 128) length += 1;
                else
                {
                    if (char.IsHighSurrogate(c) && i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                    {
                        length += Encoding.UTF8.GetByteCount(input.Slice(i, 2));
                        i++;
                    }
                    else
                    {
                        length += Encoding.UTF8.GetByteCount(input.Slice(i, 1));
                    }
                }
            }
            return length;
        }
    }
}
