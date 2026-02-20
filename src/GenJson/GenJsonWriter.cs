#nullable enable
using System;
using System.Text;

namespace GenJson
{
    public static class GenJsonWriter
    {
        public static void WriteString(Span<char> span, ref int index, string value)
        {
            span[index++] = '"';
            foreach (var c in value)
            {
                if (c == '"') { span[index++] = '\\'; span[index++] = '"'; }
                else if (c == '\\') { span[index++] = '\\'; span[index++] = '\\'; }
                else if (c == '\b') { span[index++] = '\\'; span[index++] = 'b'; }
                else if (c == '\f') { span[index++] = '\\'; span[index++] = 'f'; }
                else if (c == '\n') { span[index++] = '\\'; span[index++] = 'n'; }
                else if (c == '\r') { span[index++] = '\\'; span[index++] = 'r'; }
                else if (c == '\t') { span[index++] = '\\'; span[index++] = 't'; }
                else if (c < ' ')
                {
                    span[index++] = '\\';
                    span[index++] = 'u';
                    span[index++] = '0';
                    span[index++] = '0';
                    int val = c;
                    span[index++] = GetHex(val >> 4);
                    span[index++] = GetHex(val & 0xF);
                }
                else
                {
                    span[index++] = c;
                }
            }
            span[index++] = '"';
        }

        public static void WriteString(Span<byte> span, ref int index, string value)
        {
            span[index++] = (byte)'"';

            int start = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '"' || c == '\\' || c < ' ')
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
            if (start < value.Length)
            {
                int written = Encoding.UTF8.GetBytes(value.AsSpan(start), span.Slice(index));
                index += written;
            }

            span[index++] = (byte)'"';
        }

        private static char GetHex(int n) => (char)(n < 10 ? n + '0' : n - 10 + 'a');
    }
}
