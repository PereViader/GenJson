using System;

namespace GenJson
{
    /// <summary>
    /// Explicitly mark this class to be processed by the source generator.
    /// This attribute is required for the source generator to generate the Default/Inject/Initialize/Dispose binding methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class GenJsonAttribute : Attribute
    {
    }
}