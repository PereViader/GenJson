/*
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

namespace System.Text.Json
{
    internal abstract class JsonNamingPolicy
    {
        public abstract string ConvertName(string name);

        public static JsonNamingPolicy CamelCase { get; } = new JsonCamelCaseNamingPolicy();
        public static JsonNamingPolicy KebabCaseLower { get; } = new JsonKebabCaseLowerNamingPolicy();
        public static JsonNamingPolicy KebabCaseUpper { get; } = new JsonKebabCaseUpperNamingPolicy();
        public static JsonNamingPolicy SnakeCaseLower { get; } = new JsonSnakeCaseLowerNamingPolicy();
        public static JsonNamingPolicy SnakeCaseUpper { get; } = new JsonSnakeCaseUpperNamingPolicy();
    }

    internal sealed class JsonCamelCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
            {
                return name;
            }

            char[] chars = name.ToCharArray();
            FixCasing(chars);
            return new string(chars);
        }

        private static void FixCasing(char[] chars)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 1 && !char.IsUpper(chars[i]))
                {
                    break;
                }

                bool hasNext = (i + 1 < chars.Length);
                if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
                {
                    if (chars[i + 1] == ' ')
                    {
                        chars[i] = char.ToLowerInvariant(chars[i]);
                    }
                    break;
                }

                chars[i] = char.ToLowerInvariant(chars[i]);
            }
        }
    }

    internal abstract class JsonSeparatorNamingPolicy : JsonNamingPolicy
    {
        private readonly char _separator;
        private readonly bool _lowercase;

        protected JsonSeparatorNamingPolicy(char separator, bool lowercase)
        {
            _separator = separator;
            _lowercase = lowercase;
        }

        public override string ConvertName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                return name;
            }

            var builder = new StringBuilder();

            for (int i = 0; i < name.Length; i++)
            {
                char current = name[i];

                if (i > 0)
                {
                    char prev = name[i - 1];

                    // Boundary case 1: Lowercase/Digit to Uppercase
                    bool isTransition1 = (char.IsLower(prev) || char.IsDigit(prev)) && char.IsUpper(current);

                    // Boundary case 2: Uppercase to Uppercase followed by Lowercase (e.g. 'L' in HTMLReader followed by 'R'/'e')
                    bool isTransition2 = false;
                    if (char.IsUpper(prev) && char.IsUpper(current) && i + 1 < name.Length && char.IsLower(name[i + 1]))
                    {
                        isTransition2 = true;
                    }

                    if (isTransition1 || isTransition2)
                    {
                        if (prev != _separator && prev != '-' && prev != '_')
                        {
                            builder.Append(_separator);
                        }
                    }
                }

                builder.Append(_lowercase ? char.ToLowerInvariant(current) : char.ToUpperInvariant(current));
            }

            return builder.ToString();
        }
    }

    internal sealed class JsonSnakeCaseLowerNamingPolicy : JsonSeparatorNamingPolicy
    {
        public JsonSnakeCaseLowerNamingPolicy() : base('_', true) { }
    }

    internal sealed class JsonSnakeCaseUpperNamingPolicy : JsonSeparatorNamingPolicy
    {
        public JsonSnakeCaseUpperNamingPolicy() : base('_', false) { }
    }

    internal sealed class JsonKebabCaseLowerNamingPolicy : JsonSeparatorNamingPolicy
    {
        public JsonKebabCaseLowerNamingPolicy() : base('-', true) { }
    }

    internal sealed class JsonKebabCaseUpperNamingPolicy : JsonSeparatorNamingPolicy
    {
        public JsonKebabCaseUpperNamingPolicy() : base('-', false) { }
    }
}
