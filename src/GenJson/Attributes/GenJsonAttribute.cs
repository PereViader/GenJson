using System;

namespace GenJson
{
    /// <summary>
    /// Flag types with this attribute to source generate an efficient ToJson method for them
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class GenJsonAttribute : Attribute
    {
    }

    public static class GenJson
    {
        public static class Enum
        {
            [AttributeUsage(AttributeTargets.Property)]
            public sealed class AsText : Attribute
            {
            }

            [AttributeUsage(AttributeTargets.Property)]
            public sealed class AsNumber : Attribute
            {
            }
        }
    }
}