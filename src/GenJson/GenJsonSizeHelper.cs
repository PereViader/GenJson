#nullable enable
using System;
using System.Globalization;

namespace GenJson
{
    public static class GenJsonSizeHelper
    {
        public static int GetSize(byte value)
        {
            Span<char> buffer = stackalloc char[3];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(sbyte value)
        {
            Span<char> buffer = stackalloc char[4];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(short value)
        {
            Span<char> buffer = stackalloc char[6];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(ushort value)
        {
            Span<char> buffer = stackalloc char[5];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(int value)
        {
            Span<char> buffer = stackalloc char[11];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(uint value)
        {
            Span<char> buffer = stackalloc char[10];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(long value)
        {
            Span<char> buffer = stackalloc char[20];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }
        
        public static int GetSize(ulong value)
        {
            Span<char> buffer = stackalloc char[20];
            value.TryFormat(buffer, out int charsWritten);
            return charsWritten;
        }

        public static int GetSize(bool value) => value ? 4 : 5; // "true" or "false"
        
        public static int GetSize(char c) => c switch
        {
            '\n' => 4, // \n
            '\r' => 4, // \r
            '\t' => 4, // \t
            '\\' => 4, // \\
            '\"' => 4, // \"
            '\0' => 4, // \0
            _ when char.IsControl(c) => 8, // \uXXXX format for other control chars
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
                    '\\' => 2, // \\
                    '\"' => 2, // \"
                    '\0' => 2, // \0
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
            if (value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture))
            {
                return written;
            }

            return value.ToString("R", CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(float value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "R", CultureInfo.InvariantCulture))
            {
                return written;
            }

            return value.ToString("R", CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(decimal value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "G", CultureInfo.InvariantCulture))
            {
                return written;
            }

            return value.ToString("G", CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(DateTime value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture))
            {
                return written + 2;
            }

            return value.ToString("O", CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(DateTimeOffset value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture))
            {
                return written + 2;
            }

            return value.ToString("O", CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(TimeSpan value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "c", CultureInfo.InvariantCulture))
            {
                return written + 2;
            }

            return value.ToString("c", CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(Version value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written))
            {
                return written + 2;
            }

            return value.ToString().Length + 2;
        }
    }
}
