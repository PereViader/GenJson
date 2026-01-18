#nullable enable
namespace GenJson
{
    using System;
    using System.Globalization;

    public static class GenJsonSizeHelper
    {
        public static int GetSize(int value)
        {
            if (value == 0) return 1;
            if (value == int.MinValue) return 11;
            if (value < 0) return 1 + GetSize(-value);
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            if (value < 100000) return 5;
            if (value < 1000000) return 6;
            if (value < 10000000) return 7;
            if (value < 100000000) return 8;
            if (value < 1000000000) return 9;
            return 10;
        }

        public static int GetSize(long value)
        {
            if (value == 0) return 1;
            if (value == long.MinValue) return 20;
            if (value < 0) return 1 + GetSize(-value);
            if (value < 10L) return 1;
            if (value < 100L) return 2;
            if (value < 1000L) return 3;
            if (value < 10000L) return 4;
            if (value < 100000L) return 5;
            if (value < 1000000L) return 6;
            if (value < 10000000L) return 7;
            if (value < 100000000L) return 8;
            if (value < 1000000000L) return 9;
            if (value < 10000000000L) return 10;
            if (value < 100000000000L) return 11;
            if (value < 1000000000000L) return 12;
            if (value < 10000000000000L) return 13;
            if (value < 100000000000000L) return 14;
            if (value < 1000000000000000L) return 15;
            if (value < 10000000000000000L) return 16;
            if (value < 100000000000000000L) return 17;
            if (value < 1000000000000000000L) return 18;
            return 19;
        }

        public static int GetSize(bool value) => value ? 4 : 5;
        public static int GetSize(char _) => 3; // "c"

        public static int GetSize(string? value)
        {
            if (value is null) return 0;
            int size = 2;
            foreach (var c in value)
            {
                if (c == '"' || c == '\\' || c == '\b' || c == '\f' || c == '\n' || c == '\r' || c == '\t') size += 2;
                else if (c < ' ') size += 6;
                else size++;
            }
            return size;
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

        public static int GetSize(DateOnly value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture))
            {
                return written + 2;
            }

            return value.ToString("O", CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(TimeOnly value)
        {
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", CultureInfo.InvariantCulture))
            {
                return written + 2;
            }

            return value.ToString("O", CultureInfo.InvariantCulture).Length + 2;
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
