using System;

namespace GenJson
{
    public class JsonParseException : Exception
    {
        public int Index { get; }

        public JsonParseException(string message, int index) : base(message)
        {
            Index = index;
        }

        public JsonParseException(string message, int index, Exception innerException) : base(message, innerException)
        {
            Index = index;
        }
    }
}
