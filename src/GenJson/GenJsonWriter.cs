#nullable enable
using System;
using System.Text;

namespace GenJson
{
    public static class GenJsonWriter
    {
        public static void WriteString(Span<char> span, ref int index, string value)
        {
            int len = value.Length;
            int escapeIndex = -1;
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\' || c < ' ' || c == char.MaxValue)
                {
                    escapeIndex = i;
                    break;
                }
            }

            if (escapeIndex == -1)
            {
                span[index++] = '"';
                if (len > 0)
                {
                    value.AsSpan().CopyTo(span.Slice(index));
                    index += len;
                }
                span[index++] = '"';
                return;
            }

            span[index++] = '"';
            int start = 0;
            for (int i = escapeIndex; i < len; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\' || c < ' ' || c == char.MaxValue)
                {
                    // Write previous chunk
                    if (i > start)
                    {
                        value.AsSpan(start, i - start).CopyTo(span.Slice(index));
                        index += (i - start);
                    }

                    // Write escape
                    span[index++] = '\\';
                    switch (c)
                    {
                        case '"': span[index++] = '"'; break;
                        case '\\': span[index++] = '\\'; break;
                        case '\b': span[index++] = 'b'; break;
                        case '\f': span[index++] = 'f'; break;
                        case '\n': span[index++] = 'n'; break;
                        case '\r': span[index++] = 'r'; break;
                        case '\t': span[index++] = 't'; break;
                        case '\uffff':
                            span[index++] = 'u';
                            span[index++] = 'f';
                            span[index++] = 'f';
                            span[index++] = 'f';
                            span[index++] = 'f';
                            break;
                        default: // Control < 32
                            span[index++] = 'u';
                            span[index++] = '0';
                            span[index++] = '0';
                            int val = c;
                            span[index++] = GetHex(val >> 4);
                            span[index++] = GetHex(val & 0xF);
                            break;
                    }
                    start = i + 1;
                }
            }

            // Write remaining
            if (start < len)
            {
                value.AsSpan(start).CopyTo(span.Slice(index));
                index += (len - start);
            }

            span[index++] = '"';
        }

        public static void WriteString(Span<byte> span, ref int index, string value)
        {
            int len = value.Length;
            int escapeIndex = -1;
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\' || c < ' ' || c == char.MaxValue)
                {
                    escapeIndex = i;
                    break;
                }
            }

            if (escapeIndex == -1)
            {
                span[index++] = (byte)'"';
                if (len > 0)
                {
                    int written = Encoding.UTF8.GetBytes(value.AsSpan(), span.Slice(index));
                    index += written;
                }
                span[index++] = (byte)'"';
                return;
            }

            span[index++] = (byte)'"';
            int start = 0;
            for (int i = escapeIndex; i < len; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\' || c < ' ' || c == char.MaxValue)
                {
                    // Write previous chunk
                    if (i > start)
                    {
                        int written = Encoding.UTF8.GetBytes(value.AsSpan(start, i - start), span.Slice(index));
                        index += written;
                    }

                    // Write escape
                    span[index++] = (byte)'\\';
                    switch (c)
                    {
                        case '"': span[index++] = (byte)'"'; break;
                        case '\\': span[index++] = (byte)'\\'; break;
                        case '\b': span[index++] = (byte)'b'; break;
                        case '\f': span[index++] = (byte)'f'; break;
                        case '\n': span[index++] = (byte)'n'; break;
                        case '\r': span[index++] = (byte)'r'; break;
                        case '\t': span[index++] = (byte)'t'; break;
                        case '\uffff':
                            span[index++] = (byte)'u';
                            span[index++] = (byte)'f';
                            span[index++] = (byte)'f';
                            span[index++] = (byte)'f';
                            span[index++] = (byte)'f';
                            break;
                        default: // Control < 32
                            span[index++] = (byte)'u';
                            span[index++] = (byte)'0';
                            span[index++] = (byte)'0';
                            int val = c;
                            span[index++] = (byte)GetHex(val >> 4);
                            span[index++] = (byte)GetHex(val & 0xF);
                            break;
                    }
                    start = i + 1;
                }
            }

            // Write remaining
            if (start < len)
            {
                int written = Encoding.UTF8.GetBytes(value.AsSpan(start), span.Slice(index));
                index += written;
            }

            span[index++] = (byte)'"';
        }

        private static char GetHex(int n) => (char)(n < 10 ? n + '0' : n - 10 + 'a');
    }
}
