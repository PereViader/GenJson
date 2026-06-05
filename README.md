[![Test and publish](https://github.com/PereViader/GenJson/actions/workflows/TestAndPublish.yml/badge.svg)](https://github.com/PereViader/GenJson/actions/workflows/TestAndPublish.yml) ![Unity version 6000.0](https://img.shields.io/badge/Unity-6000.0-57b9d3.svg?style=flat&logo=unity) ![GitHub Release](https://img.shields.io/github/v/release/PereViader/GenJson?include_prereleases) [![NuGet](https://img.shields.io/nuget/v/GenJson?label=nuget)](https://www.nuget.org/packages/GenJson/) [![openupm](https://img.shields.io/npm/v/com.pereviader.genjson.unity3d?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.pereviader.genjson.unity3d/)


# GenJson

GenJson is a **zero-allocation**, high-performance C# Source Generator library that automatically creates `ToJson()` and `FromJson()` methods for your classes and structs.

This project is compatible with both pure C# projects and Unity3D.

## Features

- **Compile-Time Generation**: No reflection overhead at runtime.
- **Zero* Allocation Serialization**: Uses `Span` based string creation to write directly into the result string's memory, avoiding `StringBuilder` and intermediate string allocations for primitives.
- **Zero* Allocation Deserialization**: Uses `ReadOnlySpan<char>` and `ReadOnlySpan<byte>` (UTF-8) based parsing logic to avoid intermediate string allocations.
- **Easy Integration**: Simply mark your classes with the `[GenJson]` attribute.
- **Rich Type Support**:
  - Primitives: `int`, `string`, `bool`, `double`, `float`, `decimal` etc
  - Standard Types: `Guid`, `Uri`, `Version`, `DateTime`, `TimeSpan`, `DateTimeOffset`
  - Dictionaries: `IDictionary<K, V>` 
  - Collections: `ICollection<T>`
  - Enums: Serialized as backing type (default) or string
  - Nested Objects: Recursive serialization of complex object graphs
  - Properties / Fields: Selectively or globally serialize public and non-public properties and fields

> `DateOnly` and `TimeOnly` are **not supported** (requires .NET 6+; the library targets netstandard2.1)

> Zero-allocation* means that no unnecessary memory allocations are performed. Only the resulting objects are allocated.

## [Benchmark](https://github.com/PereViader/GenJson/blob/main/src/GenJson.Benchmark/Program.cs)

| Method                     | Mean [ns]  | Error [ns] | StdDev [ns] | Median [ns] | Allocated [KB] |
|--------------------------- |-----------:|-----------:|------------:|------------:|---------------:|
| GenJson_ToJson             |   653.2 ns |   10.99 ns |    10.28 ns |    649.9 ns |         1.6 KB |
| Utf8Json_ToJson            |   982.8 ns |   19.38 ns |    21.54 ns |    978.1 ns |        1.63 KB |
| MicrosoftJson_ToJson       | 1,183.4 ns |   23.34 ns |    24.98 ns |  1,178.5 ns |        1.92 KB |
| NewtonsoftJson_ToJson      | 2,255.5 ns |   44.86 ns |    58.33 ns |  2,261.5 ns |        5.95 KB |
| GenJson_ToJsonUtf8         |   724.6 ns |   14.04 ns |    15.02 ns |    721.2 ns |        0.81 KB |
| Utf8Json_ToJsonUtf8        |   965.1 ns |   18.66 ns |    18.33 ns |    968.4 ns |        0.83 KB |
| MicrosoftJson_ToJsonUtf8   | 1,151.4 ns |   22.30 ns |    26.55 ns |  1,148.7 ns |        1.13 KB |
| MessagePack_Serialize      |   386.1 ns |    3.36 ns |     3.14 ns |    385.6 ns |        0.59 KB |
| GenJson_FromJson           | 1,584.9 ns |   11.53 ns |    10.79 ns |  1,584.2 ns |        1.95 KB |
| Utf8Json_FromJson          | 1,632.1 ns |   16.79 ns |    15.70 ns |  1,626.5 ns |        3.13 KB |
| MicrosoftJson_FromJson     | 2,292.2 ns |   20.31 ns |    16.96 ns |  2,289.1 ns |           3 KB |
| NewtonsoftJson_FromJson    | 4,068.7 ns |   31.04 ns |    29.03 ns |  4,070.6 ns |        8.23 KB |
| GenJson_FromJsonUtf8       | 1,282.9 ns |    5.68 ns |     4.75 ns |  1,283.2 ns |        1.95 KB |
| Utf8Json_FromJsonUtf8      | 1,621.9 ns |   17.21 ns |    15.26 ns |  1,617.5 ns |         2.3 KB |
| MicrosoftJson_FromJsonUtf8 | 2,234.2 ns |   18.91 ns |    16.76 ns |  2,228.5 ns |           3 KB |
| MessagePack_Deserialize    |   835.5 ns |    7.55 ns |     7.07 ns |    832.4 ns |        1.95 KB |

- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [Utf8Json](https://github.com/Cryptisk/Utf8Json)

Note: When count optimization is enabled (by passing useCountOptimization: true), GenJson_ToJson consumes slightly more memory than Utf8Json_ToJson due to GenJson adding an extra `$Count` property so that the receiving end can optimize the deserialization of the collections.

## Installation

### NuGet

Install from [Nuget](https://www.nuget.org/packages/GenJson/)
```bash
dotnet add package GenJson
```

### Unity Package Manager

### From OpenUPM

Install from [OpenUPM](https://openupm.com/packages/com.pereviader.genjson.unity3d/#modal-manualinstallation)

### From Tarball

- Download the latest release from [releases](https://github.com/PereViader/GenJson/releases)
- Place the downloaded package file inside the `Packages` folder in your unity project
- Reference the package using the `Add Package from tar` button in the Unity Package Manager [(docs)](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-ui-tarball.html)

## Usage

### 1. Mark your class, record, or struct

- Add the `[GenJson]` attribute to any class, record, or struct you wish to serialize. 
- The type must be `partial`.
- All types (classes, records, and structs) support parameterized constructors and primary constructors.
- If no parameterized constructor is used, a parameterless constructor (implicit or explicit) must be available.

```csharp
using GenJson;

[GenJson]
public partial class Product
{
    public string Name { get; set; }
    public ProductSku[] ProductSkus { get; set; }
}

[GenJson]
public partial record ProductSku(
    Guid Id, 
    int Price, 
    ProductSize ProductSize
    );

public enum ProductSize : byte
{
    Small = 0,
    Large = 1
}
```

### 2. Mark your enum

You can control how enums are serialized by marking the enum type itself with:
- `[GenJsonEnumAsNumber]` to serialize as a number (default).
- `[GenJsonEnumAsText]` to serialize as a string.

```csharp
[GenJsonEnumAsText]
public enum ProductSize : byte
{
    Small = 0,
    Large = 1
}

[GenJson]
public partial class Product
{
    public ProductSize ProductSize { get; set; } // Serialized as "Small" or "Large"
}
```

You can also override this behavior for specific properties by applying the attribute directly to the property:

```csharp
[GenJson]
public partial class Product
{
    [GenJsonEnumAsNumber] // Overrides the enum's default behavior
    public ProductSize ProductSize { get; set; } // Serialized as 0 or 1
}
```

### 3. Rename Properties

You can customize the name of the property in the generated JSON using the `[GenJsonPropertyName]` attribute.

```csharp
[GenJson]
public partial class User
{
    [GenJsonPropertyName("user_id")]
    public int Id { get; set; }

    public string Name { get; set; }
}
```

This works for records as well:

```csharp
[GenJson]
public partial record User(
    [GenJsonPropertyName("user_id")] int Id, 
    string Name
);
```

### 4. Ignore Properties

You can prevent a property from being serialized or deserialized using the `[GenJsonIgnore]` attribute.

```csharp
[GenJson]
public partial class User
{
    public string Username { get; set; }

    [GenJsonIgnore]
    public string Password { get; set; } // Will not be included in JSON
}
```

### 5. Enum Fallback

When deserializing enums, you can specify a fallback value to use if the JSON contains a value that doesn't match any enum member. This is useful for handling unknown values from external APIs (e.g., future enum values).

Use the `[GenJsonEnumFallback]` attribute on the enum type definition.

```csharp
[GenJsonEnumFallback(Unknown)]
public enum Status
{
    Unknown = 0,
    Active = 1,
    Inactive = 2
}

// If JSON contains "Pending", it will deserialize to Status.Unknown
```

When an enum is used as a `Dictionary` key (e.g., `Dictionary<Status, int>`), and the JSON contains a key that doesn't match any enum member:
- If `[GenJsonEnumFallback]` is present, the invalid key-value pair will be **skipped** (ignored).
- If `[GenJsonEnumFallback]` is NOT present, deserialization will return `null` (fail).

### 6. Custom Conversion

You can define custom logic for serializing and deserializing specific properties or entire types using the `[GenJsonConverter]` attribute.

Since GenJson generates both string-based and UTF-8 byte-based serialization/deserialization code, a custom converter must define **both** sets of static methods.

> [!IMPORTANT]
> Custom converter deserialization methods must return a **nullable** version of the target type (either `T?` for value types, or a reference type). If parsing is not properly satisfied, the custom converter should return `null`. The source generator will then detect this and gracefully fail the entire `FromJson`/`FromJsonUtf8` operation by returning `null` to the caller.

#### Required Converter Contract

For any type `T` being converted, the converter class must implement the following six static methods:

##### 1. String-Based Methods (`char`)
*   `public static int GetSize(T value)`: Calculates the exact number of characters that `WriteJson` will write.
*   `public static void WriteJson(Span<char> span, ref int index, T value)`: Writes the value to the span starting at `index`, and advances `index` by the number of characters written.
*   `public static T? FromJson(ReadOnlySpan<char> span, ref int index)`: Parses the value from the span starting at `index`, advances `index` past the parsed token, and returns the value. Returns `null` if parsing fails.

##### 2. UTF-8 Byte-Based Methods (`byte`)
*   `public static int GetSizeUtf8(T value)`: Calculates the exact number of bytes that `WriteJson` will write.
*   `public static void WriteJsonUtf8(Span<byte> span, ref int index, T value)`: Writes the UTF-8 bytes to the span starting at `index`, and advances `index` by the number of bytes written.
*   `public static T? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)`: Parses the value from the byte span starting at `index`, advances `index` past the parsed token, and returns the value. Returns `null` if parsing fails.

> [!IMPORTANT]
> - `GetSize`/`GetSizeUtf8` must return the exact length of the written output. If they return a value smaller than what is actually written, memory corruption or exceptions will occur. If they return a larger value, the resulting JSON string or byte array will have extra unused allocated space.
> - The methods must always advance `index` by the exact number of elements (characters or bytes) written or parsed.

#### Example: Boolean as `1` or `0`

Here is a simple converter that serializes `bool` properties as `1` (true) or `0` (false) instead of `true`/`false`.

```csharp
using System;

public static class BoolToIntConverter
{
    // --- String-based conversion (char) ---

    public static int GetSize(bool value) => 1;

    public static void WriteJson(Span<char> span, ref int index, bool value)
    {
        span[index++] = value ? '1' : '0';
    }

    public static bool? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        char c = span[index++];
        if (c != '1' && c != '0') return null;
        return c == '1';
    }

    // --- UTF-8 Byte-based conversion (byte) ---

    public static int GetSizeUtf8(bool value) => 1;

    public static void WriteJsonUtf8(Span<byte> span, ref int index, bool value)
    {
        span[index++] = value ? (byte)'1' : (byte)'0';
    }

    public static bool? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        byte b = span[index++];
        if (b != (byte)'1' && b != (byte)'0') return null;
        return b == (byte)'1';
    }
}
```

#### Example: DateTime as Unix Epoch Milliseconds

Here is a more advanced example that serializes `DateTime` as Unix epoch milliseconds (a JSON number).

```csharp
using System;
using System.Buffers.Text;

public static class DateTimeEpochConverter
{
    // --- String-based conversion (char) ---

    public static int GetSize(DateTime value)
    {
        long ms = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        // Calculate number of digits without allocation
        int len = 0;
        if (ms <= 0) { len++; ms = -ms; }
        while (ms > 0) { len++; ms /= 10; }
        return len == 0 ? 1 : len;
    }

    public static void WriteJson(Span<char> span, ref int index, DateTime value)
    {
        long ms = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        ms.TryFormat(span.Slice(index), out var written);
        index += written;
    }

    public static DateTime? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        int start = index;
        while (index < span.Length && (char.IsDigit(span[index]) || span[index] == '-'))
        {
            index++;
        }
        if (start == index) return null;
        if (!long.TryParse(span.Slice(start, index - start), out var ms)) return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    }

    // --- UTF-8 Byte-based conversion (byte) ---

    public static int GetSizeUtf8(DateTime value) => GetSize(value);

    public static void WriteJsonUtf8(Span<byte> span, ref int index, DateTime value)
    {
        long ms = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        Utf8Formatter.TryFormat(ms, span.Slice(index), out var written);
        index += written;
    }

    public static DateTime? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        int start = index;
        while (index < span.Length && ((span[index] >= (byte)'0' && span[index] <= (byte)'9') || span[index] == (byte)'-'))
        {
            index++;
        }
        if (start == index) return null;
        if (!Utf8Parser.TryParse(span.Slice(start, index - start), out long ms, out _)) return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    }
}
```

Applying the custom converter using `[GenJsonConverter]`:

```csharp
[GenJson]
public partial class MyClass
{
    [GenJsonConverter(typeof(BoolToIntConverter))]
    public bool IsActive { get; set; }

    [GenJsonConverter(typeof(DateTimeEpochConverter))]
    public DateTime CreatedAt { get; set; }
}
```

You can also apply the converter directly to a custom class or struct definition:

```csharp
[GenJsonConverter(typeof(MyStructConverter))]
public struct MyStruct
{
    public int Value { get; set; }
}

[GenJson]
public partial class MyClass
{
    public MyStruct TypedProp { get; set; } // Uses MyStructConverter automatically
    
    [GenJsonConverter(typeof(AnotherConverter))]
    public MyStruct OverriddenProp { get; set; } // Overrides with AnotherConverter
}
```

#### Dictionaries and Collections with Custom Converters

If a custom class or struct has a custom converter registered (either via the type-level `[GenJsonConverter]` attribute or an assembly-level `[assembly: GenJsonConverter]` registration), that converter will automatically be resolved and used when the type is utilized:
- As keys in a dictionary (e.g. `K` in `IDictionary<K, V>`)
- As values in a dictionary (e.g. `V` in `IDictionary<K, V>`)
- As elements in a collection (e.g. `T` in `ICollection<T>`)

```csharp
[GenJsonConverter(typeof(ResIdConverter))]
public struct ResId
{
    public int Value { get; }
    public ResId(int value) => Value = value;
}

[GenJson]
public partial class UserInventory
{
    // The keys (ResId) will automatically be serialized/deserialized using ResIdConverter
    public IDictionary<ResId, int> ResourceAmounts { get; set; } = new Dictionary<ResId, int>();
}
```

#### Member-Level Dictionary and Collection Overrides

If you want to apply a custom converter to the **keys** or **values** of a dictionary property/field/parameter, or to the **elements** of a collection property/field/parameter, you can use the unified `[GenJsonConverter]` attribute with the `Key` or `Value` properties set to `true`:

- **`Key = true`**: Specifies a custom converter for the keys of a dictionary.
- **`Value = true`**: Specifies a custom converter for the values of a dictionary or elements of a collection.

```csharp
[GenJson]
public partial class CustomData
{
    // Override the converter used for the dictionary keys and values
    [GenJsonConverter(typeof(MyCustomKeyConverter), Key = true)]
    [GenJsonConverter(typeof(MyCustomValueConverter), Value = true)]
    public IDictionary<int, bool> ConfigMap { get; set; } = new Dictionary<int, bool>();

    // Override the converter used for collection elements
    [GenJsonConverter(typeof(MyElementConverter), Value = true)]
    public ICollection<int> Items { get; set; } = new List<int>();
}
```

#### External Types (Assembly-Level Registration)

If you need to define a custom converter for an external or third-party type that you do not have source control over (meaning you cannot annotate the type directly), you can register a converter at the assembly level by setting the `TargetType` named parameter of the `[assembly: GenJsonConverter(Type converterType, TargetType = typeof(TargetType))]` attribute:

```csharp
using GenJson;

[assembly: GenJsonConverter(typeof(MyStructConverter), TargetType = typeof(ExternalStruct))]
```

#### Converter Resolution Priority

When resolving which custom converter to use for a member (property, field, or constructor parameter), GenJson evaluates the available converters in the following order of priority (from highest to lowest):

1. **Member-level**: `[GenJsonConverter(typeof(MyConverter))]` (with neither `Key` nor `Value` set to `true`) applied directly to a property, field, or parameter to convert the entire member.
2. **Member-level overrides (Keys/Values/Elements)**: `[GenJsonConverter(typeof(MyConverter), Key = true)]` or `[GenJsonConverter(typeof(MyConverter), Value = true)]` applied to a property, field, or parameter of a dictionary or collection.
3. **Type-level**: `[GenJsonConverter(typeof(MyConverter))]` applied to the class or struct definition.
4. **Assembly-level**: `[assembly: GenJsonConverter(typeof(MyConverter), TargetType = typeof(TargetType))]` registered at the assembly level.

If no custom converter matches, GenJson will fall back to its default serialization/deserialization strategy.

### 7. Serialization

The generator creates a `ToJson()` method.

```csharp
var product = new Product
{ 
    Name = "Shoes", 
    ProductSkus = [
        new ProductSku(Guid.NewGuid(), 20, ProductSize.Small),
        new ProductSku(Guid.NewGuid(), 30, ProductSize.Large)
    ]
};

// Zero-allocation serialization (allocates only the result string)
string json = product.ToJson();

// You can also serialize directly to a UTF-8 byte array
byte[] utf8Json = product.ToJsonUtf8();

// You can enable collection count optimization by passing useCountOptimization: true
string optimizedJson = product.ToJson(useCountOptimization: true);
```

### 8. Deserialization

The generator creates static `FromJson` and `FromJsonUtf8` methods on your class.

```csharp
Product product = Product.FromJson(json);

// You can also deserialize directly from a UTF-8 byte span or array
byte[] utf8Json = ... // e.g. from a network stream
Product productUtf8 = Product.FromJsonUtf8(utf8Json);

// If the JSON was serialized with count optimization, pass useCountOptimization: true to utilize it
Product productOptimized = Product.FromJson(optimizedJson, useCountOptimization: true);
```

> [!IMPORTANT]
> GenJson assumes that the input JSON is properly formatted and does not use any whitespace or linebreaks. To achieve maximum performance, it does not fully validate the JSON structure.

GenJson will generate slightly different code depending on the status of the nullable C# feature.

Given the class below with the nullable feature disabled, both Name and Description may be deserialized as null when they are not available in the JSON.

```csharp
#nullable disable

public partial class Product
{
    public string Name { get; set; } // <-- Nullable
    public string Description { get; set; } // <-- Nullable
}
```

Given the class below with the nullable feature enabled, Description may still be null like before, but the object will fail to be deserialized if Name is missing.

```csharp
#nullable enable

public partial class Product
{
    public string Name { get; set; } // <-- Required
    public string? Description { get; set; } // <-- Nullable
}
```


### 9. Inheritance

GenJson supports serialization of inherited properties. Simply mark both the base class and the derived class with `[GenJson]`.

```csharp
[GenJson]
public partial class Animal
{
    public string Name { get; set; }
}

[GenJson]
public partial class Dog : Animal
{
    public string Breed { get; set; }
}
```

### 10. Polymorphism

GenJson supports polymorphic serialization and deserialization.

1.  Mark the base class (can be abstract) with `[GenJsonPolymorphic]`.
    - This is optional and is only needed if you want to change it.
2.  Register known derived types using `[GenJsonDerivedType(typeof(Derived), identifier)]`.
    - The identifier can be an `int` or a `string`.

```csharp
[GenJson]
[GenJsonPolymorphic("$animal-type")] // Optional attribute, when unspecified defaults to "$type"
[GenJsonDerivedType(typeof(Dog), "dog")]
[GenJsonDerivedType(typeof(Cat), "cat")]
public abstract partial class Animal
{
    public string Name { get; set; }
}

[GenJson]
public partial class Dog : Animal
{
    public string Breed { get; set; }
}

[GenJson]
public partial class Cat : Animal
{
    public bool IsLazy { get; set; }
}
```

**Serialization**: The generator will automatically include the discriminator property (`$type`: "dog") in the JSON output.

**Deserialization**: `Animal.FromJson(...)` will inspect the `$type` property and deserialize into the correct derived type (`Dog` or `Cat`). If the type is unknown or missing (for abstract bases), it returns `null`.

### 11. Collection Count Optimization

GenJson optimizes collection deserialization (Lists, Arrays, Dictionaries) by pre-allocating the collection with the exact size. This avoids resizing overhead during population.

**How it works:**
- **Serialization**: When `useCountOptimization: true` is passed, the generator automatically emits a hidden property named after the collection with a `$` prefix (e.g., `"$MyList": 5`) immediately before the collection property.
- **Deserialization**: When `useCountOptimization: true` is passed, the parser reads this count property first and initializes the collection with the correct capacity (e.g., `new List<int>(5)`).

> [!NOTE]
> GenJson can still parse standard JSON without the count property. If the property is missing, or if `useCountOptimization` is `false`, it will automatically fall back to counting the elements of the collection before doing the allocation.

**Enabling Optimization:**
By default, count optimization is disabled to maintain strictly standard JSON and avoid extra metadata properties. To enable it, pass `useCountOptimization: true` to the serialization and deserialization methods.

> [!TIP] 
> If the receiving end doesn't use count metadata, leaving count optimization disabled (the default) speeds up ToJson execution and reduces memory allocations.

```csharp
var myObj = new MyClass { MyList = new List<int> { 1, 2, 3 } };

// Serialize with count optimization (emits "$MyList": 3 in JSON)
string json = myObj.ToJson(useCountOptimization: true);

// Deserialize using count optimization (utilizes "$MyList": 3 to pre-allocate)
var deserialized = MyClass.FromJson(json, useCountOptimization: true);
```

### 12. Custom Collections and Dictionaries

GenJson supports custom dictionary and collection classes (concrete types)

**Requirements**:
- The custom class must be a concrete type (neither abstract nor an interface).
- It must have a constructor that is either parameterless or accepts a single `int` parameter (for pre-allocating capacity). The generator will automatically detect and use the capacity constructor if available.
- It must implement `IDictionary<TKey, TValue>` or `ICollection<T>`.

### 13. Include Private Members

By default, GenJson serializes all public properties and public member fields. If you need to serialize private or non-public properties or fields, you can use the `[GenJsonIncludePrivateMember]` attribute.

You can apply this attribute:
- To individual private properties or fields.
- To a class, struct, or record as a whole to include all of its private/non-public properties and fields.

```csharp
[GenJson]
public partial class User
{
    [GenJsonIncludePrivateMember]
    private string _secretToken = "token"; // Serialized selectively

    private string _ignoredPrivate = "not_serialized"; // Skipped
}

[GenJson]
[GenJsonIncludePrivateMember]
public partial class SecureConfig
{
    private int _port = 8080; // Serialized
    private string _host = "localhost"; // Serialized
}
```

> [!NOTE]
> - Compiler-generated backing fields (such as those of auto-properties) are automatically skipped to avoid duplicate serialization.
> - Private members of base classes are ignored because a derived class's partial definition cannot access the private members of its base class. However, protected and internal members are fully supported and will be serialized if included.
> - Read-only private fields (marked `readonly`) will only be serialized and deserialized if they are constructor parameters. If not, they are ignored because they are not writable.

### 14. Readonly Members and Constructors

GenJson supports serializing and deserializing readonly fields (marked `readonly` in C#) and readonly properties (properties with no setter or `init`-only setters). 

However, because these members cannot be written to after the object is constructed, GenJson applies specific rules:

1. **Mapping to Constructor Parameters**: A readonly field or property is only included in serialization and deserialization if it maps case-insensitively to a parameter in one of the type's constructors (including primary constructors). For private fields, the parameter name can optionally omit the leading underscore (e.g., parameter `role` maps to private field `_role`).
2. **Excluded if not in Constructor**: If a readonly field or property does not map to any constructor parameter, it is completely ignored by GenJson (it will not be serialized or deserialized).
3. **Primary and Parameterized Constructors**: When deserializing, GenJson parses the values from the JSON and passes them directly to the constructor to initialize the readonly fields/properties. Any non-readonly writable properties are set afterward via an object initializer.

```csharp
[GenJson]
public partial class User
{
    // Included: maps case-insensitively to constructor parameter `name`
    public readonly string Name;

    // Included: maps to constructor parameter `role` (ignoring leading underscore)
    [GenJsonIncludePrivateMember]
    private readonly string _role;

    // Excluded: cannot be set after construction, and is not a constructor parameter
    public readonly int IgnoredReadonly = 42; 

    public User(string name, string role)
    {
        Name = name;
        _role = role;
    }
}
```

### 15. Root Collections, Arrays, Dictionaries, and Primitives

By default, serialization/deserialization requires starting from a class, record, or struct decorated with `[GenJson]`. If you need to serialize starting from root collections, arrays, dictionaries, or primitives, you can define a custom static partial class decorated with exactly one `[GenJsonSerializable(Type)]` attribute.

#### Defining a Serializer Class

To define your serializer, create a `static partial` class and decorate it with a single `[GenJsonSerializable(Type)]` for the target root type you want to support:

```csharp
using System.Collections.Generic;
using GenJson;

[GenJsonSerializable(typeof(List<Product>))]
public static partial class ProductListSerializer
{
}
```

GenJson will automatically generate the strongly-typed serialization, deserialization, and extension methods directly inside this partial static class.

#### Serialization and Deserialization

You can use the serializer class directly, or use the generated extension methods (since they are generated inside `ProductListSerializer`, make sure the namespace containing your serializer is imported):

```csharp
var products = new List<Product>
{
    new() { Name = "Shoes" }
};

// 1. Serialization via extension methods
string json = products.ToJson();
byte[] utf8Json = products.ToJsonUtf8();

// 2. Deserialization via strongly-typed, non-generic entry points on ProductListSerializer
List<Product> deserialized = ProductListSerializer.FromJson(json);
List<Product> deserializedUtf8 = ProductListSerializer.FromJsonUtf8(utf8Json);
```

> [!NOTE]
> - The serializer class must be declared as `static` and `partial`.
> - A serializer class can only have exactly one `[GenJsonSerializable]` attribute applied (the C# compiler will enforce this automatically because `AllowMultiple = false`).
> - If an annotated serializer class is not both `static` and `partial`, a compile-time error (`GENJSON005`) will be reported.
> - The target type in `[GenJsonSerializable]` must be a supported type (primitives, enums, classes/structs decorated with `[GenJson]`, or types with a registered custom converter). If not, a compile-time error (`GENJSON004`) will be reported.

## How It Works

GenJson analyzes your code during compilation and generates specialized serialization code.

- **Serialization**: It pre-calculates the exact size needed for the JSON string and uses `string.Create` to fill the content directly via a `Span<char>`. This avoids the "double allocation" problem of `StringBuilder` (buffer resizing + final string) and eliminates allocations for formatting numbers and other primitives.
- **Deserialization**: It generates a recursive descent parser that operates on `ReadOnlySpan<char>`, avoiding substring allocations during parsing.

## System.Text.Json Integration

If you want to serialize your JSON on the server using Microsoft's `System.Text.Json` (STJ) and deserialize it on the client (e.g., Unity) using `GenJson`, you can use the **`GenJson.SystemTextJson`** bridging library.

This bridge automatically translates GenJson custom attributes and formatting rules into System.Text.Json options, ensuring that the serialized JSON is perfectly compatible with the client-side parser.

### Key Compatibility Rules

GenJson's high-performance parser has specific requirements for incoming JSON:
1. **Minified JSON**: It does not tolerate whitespace or line breaks. The server must serialize with `WriteIndented = false` (default).
2. **Ignored Null Values**: In `#nullable enable` contexts, non-nullable reference properties will cause parsing to fail if explicitly sent as `null` (e.g. `"Name": null`). The server must omit null values.
3. **Special Float Literals**: Floating-point `NaN` and `Infinity` must be written as JSON strings (e.g. `"NaN"`), which STJ supports via `JsonNumberHandling.AllowNamedFloatingPointLiterals`.

### Usage

1. Reference the `GenJson.SystemTextJson` package or project.
2. Initialize `JsonSerializerOptions` on the server using `GenJsonStjOptions.CreateOptions()`:

```csharp
using System.Text.Json;
using GenJson.SystemTextJson;

// Create options that align with GenJson formatting and map custom attributes
var options = GenJsonStjOptions.CreateOptions();

// Serialize using System.Text.Json on the server
string json = JsonSerializer.Serialize(myObject, options);
```

### Dynamic Attribute Bridging

The bridge handles the translation of the following custom GenJson attributes automatically on the server:
- **`[GenJsonPropertyName("custom_name")]`**: Maps the property name in System.Text.Json.
- **`[GenJsonIgnore]`**: Excludes the property completely from both serialization and deserialization contracts.
- **`[GenJsonEnumAsText]` and `[GenJsonEnumAsNumber]`**: Controls enum formatting (string vs backing number) at both the property and type levels.
- **`[GenJsonPolymorphic]` and `[GenJsonDerivedType]`**: Maps polymorphism rules directly to System.Text.Json's native polymorphic contract options.
- **`[GenJsonIncludePrivateMember]`**: Dynamically registers public fields and private/non-public properties and fields in System.Text.Json options.
- **`[GenJsonConverter(typeof(MyConverter))]`**: Automatically routes property-level or type-level custom converters to a dynamic bridge that executes your custom GenJson static methods (`GetSizeUtf8`, `WriteJsonUtf8`, `FromJsonUtf8`) inside System.Text.Json.
- **`[assembly: GenJsonConverter(typeof(TargetType), typeof(ConverterType))]`**: Automatically routes assembly-level custom converters registered for external/third-party types to the dynamic System.Text.Json bridge.
