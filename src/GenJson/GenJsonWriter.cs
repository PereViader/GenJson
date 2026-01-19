#nullable enable
using System;

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

        private static char GetHex(int n) => (char)(n < 10 ? n + '0' : n - 10 + 'a');
    }
}
