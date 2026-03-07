[![Test and publish](https://github.com/PereViader/GenJson/actions/workflows/TestAndPublish.yml/badge.svg)](https://github.com/PereViader/GenJson/actions/workflows/TestAndPublish.yml) ![Unity version 2022.3.29](https://img.shields.io/badge/Unity-2022.3.29-57b9d3.svg?style=flat&logo=unity) ![GitHub Release](https://img.shields.io/github/v/release/PereViader/GenJson?include_prereleases) [![NuGet](https://img.shields.io/nuget/v/GenJson?label=nuget)](https://www.nuget.org/packages/GenJson/)


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
  - Standard Structs: `Guid`, `Version`, `DateTime`, `TimeSpan`, `DateTimeOffset`
  - Dictionaries: `IReadOnlyDictionary<K, V>` 
  - Collections: `IEnumerable<T>`
  - Enums: Serialized as backing type (default) or string
  - Nested Objects: Recursive serialization of complex object graphs

> Zero-allocation* means that no unnecessary memory allocations are performed. Only the resulting strings are allocated.

## [Benchmark](https://github.com/PereViader/GenJson/blob/main/src/GenJson.Benchmark/Program.cs)

| Method                     | Mean [ns]  | Error [ns] | StdDev [ns] | Gen0   | Allocated [KB] |
|--------------------------- |-----------:|-----------:|------------:|-------:|---------------:|
| GenJson_ToJson             |   996.0 ns |   19.91 ns |    23.70 ns | 0.0343 |        1.72 KB |
| MicrosoftJson_ToJson       | 1,212.3 ns |   23.66 ns |    25.31 ns | 0.0381 |        1.92 KB |
| NewtonsoftJson_ToJson      | 2,277.3 ns |   44.80 ns |    56.66 ns | 0.1183 |        5.95 KB |
| Utf8Json_ToJson            |   985.9 ns |   17.89 ns |    16.73 ns | 0.0324 |        1.63 KB |
| GenJson_FromJson           | 1,077.7 ns |   20.89 ns |    26.42 ns | 0.0439 |        2.16 KB |
| MicrosoftJson_FromJson     | 2,387.2 ns |   47.67 ns |    65.24 ns | 0.0610 |           3 KB |
| NewtonsoftJson_FromJson    | 4,075.0 ns |   79.57 ns |   119.10 ns | 0.1678 |        8.23 KB |
| Utf8Json_FromJson          | 1,717.2 ns |   34.28 ns |    32.06 ns | 0.0629 |        3.13 KB |
| GenJson_FromJsonUtf8       | 1,020.9 ns |   20.20 ns |    31.45 ns | 0.0439 |        2.16 KB |
| Utf8Json_FromJsonUtf8      | 1,635.8 ns |   26.82 ns |    23.78 ns | 0.0458 |         2.3 KB |
| MicrosoftJson_FromJsonUtf8 | 2,316.8 ns |   44.93 ns |    58.43 ns | 0.0610 |           3 KB |

<img width="1408" height="768" alt="image" src="https://github.com/user-attachments/assets/a0d1ccf9-1914-4483-b7ac-037f8fc6732b" />


- [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/api/system.text.json)
- [Newtonsoft.Json](https://www.newtonsoft.com/json)
- [Utf8Json](https://github.com/Cryptisk/Utf8Json)

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
- Non-record types must have a parameterless constructor (implicit or explicit).
- Record types support primary constructors.

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

1.  Define a class with static methods `GetSize`, `WriteJson`, and `FromJson` (and their UTF8 variants if needed).
2.  Apply `[GenJsonConverter(typeof(YourConverter))]` to the property, class, or struct.

```csharp
public static class MyCustomConverter
{
    public static int GetSize(int value) => ... // Calculate size
    public static void WriteJson(Span<char> span, ref int index, int value) => ... // Write to span
    public static int FromJson(ReadOnlySpan<char> span, ref int index) => ... // Read from span
}

[GenJson]
public partial class MyClass
{
    [GenJsonConverter(typeof(MyCustomConverter))]
    public int MyProperty { get; set; }
}
```

You can also apply it directly to a type, and override it on specific properties if needed:

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
```

### 8. Deserialization

The generator creates static `FromJson` and `FromJsonUtf8` methods on your class.

```csharp
Product product = Product.FromJson(json);

// You can also deserialize directly from a UTF-8 byte span or array
byte[] utf8Json = ... // e.g. from a network stream
Product productUtf8 = Product.FromJsonUtf8(utf8Json);
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
- **Serialization**: The generator automatically emits a hidden property named after the collection with a `$` prefix (e.g., `"$MyList": 5`) immediately before the collection property.
- **Deserialization**: The parser reads this count property first and initializes the collection with the correct capacity (e.g., `new List<int>(5)`).

> [!NOTE]
> GenJson can still parse standard JSON without the count property. If the property is missing, it will automatically fall back to counting the elements of the collection before doing the allocation.

**Disabling Optimization:**
To maintain strictly standard JSON or avoid extra metadata properties, apply the [GenJsonSkipCountOptimization] attribute to your class or struct.

> [!TIP] 
> If the receiving end doesn't use count metadata, disabling this optimization speeds up ToJson execution and reduces memory allocations.

```csharp
[GenJson]
[GenJsonSkipCountOptimization] // Disables usage of $MyList property
public partial class MyClass
{
    public List<int> MyList { get; set; }
}
```

## How It Works

GenJson analyzes your code during compilation and generates specialized serialization code.

- **Serialization**: It pre-calculates the exact size needed for the JSON string and uses `string.Create` to fill the content directly via a `Span<char>`. This avoids the "double allocation" problem of `StringBuilder` (buffer resizing + final string) and eliminates allocations for formatting numbers and other primitives.
- **Deserialization**: It generates a recursive descent parser that operates on `ReadOnlySpan<char>`, avoiding substring allocations during parsing.
