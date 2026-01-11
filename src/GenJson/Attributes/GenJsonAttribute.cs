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
}