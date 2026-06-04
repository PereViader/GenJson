using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GenJson.SystemTextJson
{
    public static class GenJsonStjOptions
    {
        /// <summary>
        /// Creates JsonSerializerOptions for System.Text.Json configured to align with GenJson serialization/deserialization.
        /// </summary>
        public static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                // GenJson assumes minified JSON with no whitespace/newlines
                WriteIndented = false,

                // Omit null values by default to prevent client-side parsing failures
                // for non-nullable properties in #nullable enable contexts
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

                // GenJson encodes NaN/Infinity as strings ("NaN", "Infinity")
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            // Register all type-level custom converters dynamically
            RegisterTypeConverters(options);

            // Map GenJson metadata attributes to System.Text.Json dynamically using resolvers
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ModifyTypeInfo }
            };

            return options;
        }

        private static void RegisterTypeConverters(JsonSerializerOptions options)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var registeredTypes = new System.Collections.Generic.HashSet<Type>();

                foreach (var assembly in assemblies)
                {
                    string? name = assembly.FullName;
                    if (name != null && (name.StartsWith("System") || name.StartsWith("Microsoft") || name.StartsWith("mscorlib") || name.StartsWith("netstandard")))
                    {
                        continue;
                    }

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        types = ex.Types.Where(t => t != null).ToArray()!;
                    }

                    foreach (var type in types)
                    {
                        var converterAttr = type.GetCustomAttribute<GenJsonConverterAttribute>();
                        if (converterAttr != null)
                        {
                            var bridgeType = typeof(GenJsonStjBridgeConverter<>).MakeGenericType(type);
                            var converterInstance = (JsonConverter)Activator.CreateInstance(bridgeType, converterAttr.Type)!;
                            options.Converters.Add(converterInstance);
                            registeredTypes.Add(type);
                        }
                    }
                }

                foreach (var assembly in assemblies)
                {
                    string? name = assembly.FullName;
                    if (name != null && (name.StartsWith("System") || name.StartsWith("Microsoft") || name.StartsWith("mscorlib") || name.StartsWith("netstandard")))
                    {
                        continue;
                    }

                    var assemblyConverterAttrs = assembly.GetCustomAttributes(typeof(GenJsonConverterForAttribute), false);
                    foreach (var attrObj in assemblyConverterAttrs)
                    {
                        if (attrObj is GenJsonConverterForAttribute attr)
                        {
                            if (!registeredTypes.Contains(attr.TargetType))
                            {
                                var bridgeType = typeof(GenJsonStjBridgeConverter<>).MakeGenericType(attr.TargetType);
                                var converterInstance = (JsonConverter)Activator.CreateInstance(bridgeType, attr.ConverterType)!;
                                options.Converters.Add(converterInstance);
                                registeredTypes.Add(attr.TargetType);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback gracefully in restricted environments
            }
        }

        private static void ModifyTypeInfo(JsonTypeInfo typeInfo)
        {
            // 1. Handle Polymorphism [GenJsonPolymorphic] and [GenJsonDerivedType]
            var polyAttr = typeInfo.Type.GetCustomAttribute<GenJsonPolymorphicAttribute>(false);
            var derivedAttrs = typeInfo.Type.GetCustomAttributes<GenJsonDerivedTypeAttribute>(false).ToList();
            if (polyAttr != null || derivedAttrs.Count > 0)
            {
                var polyOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = polyAttr?.TypeDiscriminatorPropertyName ?? "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true
                };

                foreach (var derived in derivedAttrs)
                {
                    object? discriminatorValue = derived.TypeDiscriminatorValue;
                    if (discriminatorValue is int intVal)
                    {
                        polyOptions.DerivedTypes.Add(new JsonDerivedType(derived.Type, intVal));
                    }
                    else
                    {
                        string strVal = discriminatorValue?.ToString() ?? derived.Type.Name;
                        polyOptions.DerivedTypes.Add(new JsonDerivedType(derived.Type, strVal));
                    }
                }

                typeInfo.PolymorphismOptions = polyOptions;
            }

            // 2. Property-level configurations
            if (typeInfo.Kind == JsonTypeInfoKind.Object)
            {
                // Remove ignored properties first to prevent serialization and deserialization
                for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
                {
                    var property = typeInfo.Properties[i];
                    if (property.AttributeProvider != null && 
                        property.AttributeProvider.GetCustomAttributes(typeof(GenJsonIgnoreAttribute), true).Any())
                    {
                        typeInfo.Properties.RemoveAt(i);
                    }
                }

                foreach (var property in typeInfo.Properties)
                {
                    if (property.AttributeProvider == null) continue;

                    // Property Name overrides: [GenJsonPropertyName("custom_name")]
                    if (property.AttributeProvider.GetCustomAttributes(typeof(GenJsonPropertyNameAttribute), true)
                            .FirstOrDefault() is GenJsonPropertyNameAttribute nameAttr)
                    {
                        property.Name = nameAttr.Name;
                    }

                    // Property Custom Converters: [GenJsonConverter]
                    if (property.AttributeProvider.GetCustomAttributes(typeof(GenJsonConverterAttribute), true)
                            .FirstOrDefault() is GenJsonConverterAttribute propConverterAttr)
                    {
                        var bridgeType = typeof(GenJsonStjBridgeConverter<>).MakeGenericType(property.PropertyType);
                        property.CustomConverter = (JsonConverter)Activator.CreateInstance(bridgeType, propConverterAttr.Type);
                        continue;
                    }

                    // Enum Casing Behavior (String vs Backing Number)
                    Type propType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    if (propType.IsEnum)
                    {
                        bool serializeAsString = false;

                        if (property.AttributeProvider.GetCustomAttributes(typeof(GenJsonEnumAsTextAttribute), true).Any())
                        {
                            serializeAsString = true;
                        }
                        else if (property.AttributeProvider.GetCustomAttributes(typeof(GenJsonEnumAsNumberAttribute), true).Any())
                        {
                            serializeAsString = false;
                        }
                        else if (propType.GetCustomAttribute<GenJsonEnumAsTextAttribute>() != null)
                        {
                            serializeAsString = true;
                        }

                        if (serializeAsString)
                        {
                            property.CustomConverter = new JsonStringEnumConverter();
                        }
                    }
                }
            }
        }
    }
}
