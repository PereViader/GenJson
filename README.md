# GenJson

GenJson is a **zero-allocation**, high-performance C# Source Generator library that automatically creates `ToJson()` and `FromJson()` methods for your classes and structs.

## Features

- **Compile-Time Generation**: No reflection overhead at runtime.
- **Zero* Allocation Serialization**: Uses `Span` based string creation to write directly into the result string's memory, avoiding `StringBuilder` and intermediate string allocations for primitives.
- **Zero* Allocation Deserialization**: Uses `ReadOnlySpan<char>` based parsing logic to avoid intermediate string allocations.
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

| Method                  | Mean [ns]  | Error [ns] | StdDev [ns] | Gen0   | Allocated [KB] |
|------------------------ |-----------:|-----------:|------------:|-------:|---------------:|
| GenJson_ToJson          |   955.3 ns |   18.49 ns |    19.78 ns | 0.0324 |         1.6 KB |
| MicrosoftJson_ToJson    | 1,219.7 ns |   23.88 ns |    29.32 ns | 0.0381 |        1.92 KB |
| NewtonsoftJson_ToJson   | 2,391.1 ns |   43.50 ns |    40.69 ns | 0.1183 |        5.95 KB |
| GenJson_FromJson        | 1,700.2 ns |   32.93 ns |    40.44 ns | 0.0534 |        2.69 KB |
| MicrosoftJson_FromJson  | 2,450.4 ns |   48.04 ns |    60.75 ns | 0.0610 |           3 KB |
| NewtonsoftJson_FromJson | 4,000.7 ns |   78.60 ns |    96.53 ns | 0.1678 |        8.23 KB |

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

### 2 Mark your enum

Enum properties of classes may be marked with
- `[GenJsonEnumAsNumber]` to serialize as a number (default, not required)
- `[GenJsonEnumAsText]` to serialize as a string.

```csharp
    public ProductSizeType ProductSize { get; set; } // <-- Json will be de/serialized using 0, 1
```

```csharp
    [GenJsonEnumAsNumber] // <-- Json will be de/serialized using 0 or 1
    public ProductSize ProductSize { get; set; }
```

```csharp
    [GenJsonEnumAsText] // <-- Json will be de/serialized using "Small" or "Large"
    public ProductSize ProductSize { get; set; }
```

### 3. Enum Fallback

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

### 4. Custom Conversion

You can define custom logic for serializing and deserializing specific properties using the `[GenJsonConverter]` attribute.

1.  Define a class with static methods `GetSize`, `WriteJson`, and `FromJson`.
2.  Apply `[GenJsonConverter(typeof(YourConverter))]` to the property.

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

### 5. Serialization

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
```

### 6. Deserialization

The generator creates a static `FromJson` method on your class.

```csharp
Product product = Product.FromJson(json);
```

GenJson will will generate slightly different code depending on the status of the nullable C# feature.

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

## How It Works

GenJson analyzes your code during compilation and generates specialized serialization code.

- **Serialization**: It pre-calculates the exact size needed for the JSON string and uses `string.Create` to fill the content directly via a `Span<char>`. This avoids the "double allocation" problem of `StringBuilder` (buffer resizing + final string) and eliminates allocations for formatting numbers and other primitives.
- **Deserialization**: It generates a recursive descent parser that operates on `ReadOnlySpan<char>`, avoiding substring allocations during parsing.
