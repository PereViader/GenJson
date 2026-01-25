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
    
    /// <summary>
    /// This attribute can be used to implement special logic to map from/back json to instance
    /// Instead of using whatever default logic the generator would have used, it is going to use
    /// the static methods on the static class provided as a parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class GenJsonConverterAttribute : Attribute
    {
        public GenJsonConverterAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; set; }
    }


    /// <summary>
    /// This attribute can be used to specify the name of the property in the generated json.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class GenJsonPropertyNameAttribute : Attribute
    {
        public GenJsonPropertyNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
    
    /// <summary>
    /// This attribute can be used when mapping from/back json to instance to specify
    /// that the enum should be serialized as a string. So an enum with value `Potato = 1`
    /// will be serialized as "Potato". 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Enum)]
    public sealed class GenJsonEnumAsTextAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute can be used when mapping from/back json to instance to specify
    /// that the enum should be serialized as a number. So an enum with value `Potato = 1`
    /// will be serialized as 1. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Enum)]
    public sealed class GenJsonEnumAsNumberAttribute : Attribute
    {
    }

    /// <summary>
    /// This attribute can be used when mapping json back to an instance to specify a fallback value 
    /// to be used when the deserialized json key contains a value that cannot be deserialized back to the enum.
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class GenJsonEnumFallbackAttribute : Attribute
    {
        public GenJsonEnumFallbackAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}