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

                    var assemblyConverterAttrs = assembly.GetCustomAttributes(typeof(GenJsonConverterAttribute), false);
                    foreach (var attrObj in assemblyConverterAttrs)
                    {
                        if (attrObj is GenJsonConverterAttribute attr)
                        {
                            if (attr.TargetType != null && !registeredTypes.Contains(attr.TargetType))
                            {
                                var bridgeType = typeof(GenJsonStjBridgeConverter<>).MakeGenericType(attr.TargetType);
                                var converterInstance = (JsonConverter)Activator.CreateInstance(bridgeType, attr.Type)!;
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
                var type = typeInfo.Type;
                var genJsonAttr = type.GetCustomAttribute<GenJsonAttribute>();
                var namingPolicy = genJsonAttr?.NamingPolicy ?? GenJsonNamingPolicy.Unspecified;
                JsonNamingPolicy? stjNamingPolicy = null;
                switch (namingPolicy)
                {
                    case GenJsonNamingPolicy.CamelCase:
                        stjNamingPolicy = JsonNamingPolicy.CamelCase;
                        break;
                    case GenJsonNamingPolicy.KebabCaseLower:
                        stjNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                        break;
                    case GenJsonNamingPolicy.KebabCaseUpper:
                        stjNamingPolicy = JsonNamingPolicy.KebabCaseUpper;
                        break;
                    case GenJsonNamingPolicy.SnakeCaseLower:
                        stjNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                        break;
                    case GenJsonNamingPolicy.SnakeCaseUpper:
                        stjNamingPolicy = JsonNamingPolicy.SnakeCaseUpper;
                        break;
                }

                var existingPropertyNames = new System.Collections.Generic.HashSet<string>(typeInfo.Properties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                var currentType = type;
                while (currentType != null && currentType != typeof(object))
                {
                    bool includeAllPrivate = currentType.GetCustomAttribute<GenJsonIncludePrivateMemberAttribute>() != null;

                    // Properties
                    var properties = currentType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (var prop in properties)
                    {
                        bool isPublic = prop.GetMethod?.IsPublic == true || prop.SetMethod?.IsPublic == true;
                        if (isPublic) continue;

                        bool isAccessible = currentType == type || (prop.GetMethod != null && !prop.GetMethod.IsPrivate) || (prop.SetMethod != null && !prop.SetMethod.IsPrivate);
                        if (!isAccessible) continue;

                        bool hasAttr = prop.GetCustomAttribute<GenJsonIncludePrivateMemberAttribute>() != null;
                        if (hasAttr || includeAllPrivate)
                        {
                            string name = prop.Name;
                            if (existingPropertyNames.Contains(name)) continue;

                            bool isReadOnly = prop.SetMethod == null;
                            if (isReadOnly && !IsCtorArg(prop, type))
                            {
                                continue;
                            }

                            var propInfo = typeInfo.CreateJsonPropertyInfo(prop.PropertyType, prop.Name);
                            if (prop.CanRead)
                            {
                                propInfo.Get = obj => prop.GetValue(obj);
                            }
                            if (prop.CanWrite)
                            {
                                propInfo.Set = (obj, val) => prop.SetValue(obj, val);
                            }
                            propInfo.AttributeProvider = prop;
                            
                            typeInfo.Properties.Add(propInfo);
                            existingPropertyNames.Add(name);
                        }
                    }

                    // Fields
                    var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                    foreach (var field in fields)
                    {
                        if (field.Name.StartsWith("<")) continue;

                        bool isPublic = field.IsPublic;
                        bool isAccessible = isPublic || currentType == type || !field.IsPrivate;
                        if (!isAccessible) continue;

                        bool hasAttr = field.GetCustomAttribute<GenJsonIncludePrivateMemberAttribute>() != null;
                        bool shouldInclude = isPublic || hasAttr || includeAllPrivate;

                        if (shouldInclude)
                        {
                            string name = field.Name;
                            if (existingPropertyNames.Contains(name)) continue;

                            bool isReadOnly = field.IsInitOnly || field.IsLiteral;
                            if (isReadOnly && !IsCtorArg(field, type))
                            {
                                continue;
                            }

                            var propInfo = typeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
                            propInfo.Get = obj => field.GetValue(obj);
                            if (!field.IsInitOnly && !field.IsLiteral)
                            {
                                propInfo.Set = (obj, val) => field.SetValue(obj, val);
                            }
                            propInfo.AttributeProvider = field;

                            typeInfo.Properties.Add(propInfo);
                            existingPropertyNames.Add(name);
                        }
                    }

                    currentType = currentType.BaseType;
                }

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
                    else if (stjNamingPolicy != null && property.AttributeProvider is MemberInfo memberInfo)
                    {
                        property.Name = stjNamingPolicy.ConvertName(memberInfo.Name);
                    }

                    // Property Custom Converters: [GenJsonConverter]
                    var propConverterAttrs = property.AttributeProvider.GetCustomAttributes(typeof(GenJsonConverterAttribute), true)
                            .Cast<GenJsonConverterAttribute>();
                    var selfConverterAttr = propConverterAttrs.FirstOrDefault(a => !a.Key && !a.Value);
                    if (selfConverterAttr != null)
                    {
                        var bridgeType = typeof(GenJsonStjBridgeConverter<>).MakeGenericType(property.PropertyType);
                        property.CustomConverter = (JsonConverter)Activator.CreateInstance(bridgeType, selfConverterAttr.Type);
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

        private static bool IsCtorArg(MemberInfo member, Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (constructors.Length == 0) return false;
            
            string name = member.Name;
            string nameWithoutUnderscore = name.StartsWith("_") ? name.Substring(1) : name;
            
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    if (string.Equals(param.Name, name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(param.Name, nameWithoutUnderscore, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}
