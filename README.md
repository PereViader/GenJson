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

| Method                  | Mean [ns]  | Error [ns] | StdDev [ns] | Median [ns] | Gen0   | Allocated [KB] |
|------------------------ |-----------:|-----------:|------------:|------------:|-------:|---------------:|
| GenJson_ToJson          |   889.2 ns |   17.74 ns |    22.44 ns |    880.9 ns | 0.0324 |         1.6 KB |
| MicrosoftJson_ToJson    | 1,166.0 ns |   23.29 ns |    34.86 ns |  1,158.0 ns | 0.0381 |        1.92 KB |
| NewtonsoftJson_ToJson   | 2,294.7 ns |   45.66 ns |    66.93 ns |  2,296.7 ns | 0.1183 |        5.95 KB |
| GenJson_FromJson        | 1,687.8 ns |   32.79 ns |    44.88 ns |  1,671.8 ns | 0.0477 |        2.39 KB |
| MicrosoftJson_FromJson  | 2,327.2 ns |   45.12 ns |    58.67 ns |  2,293.2 ns | 0.0610 |           3 KB |
| NewtonsoftJson_FromJson | 4,187.9 ns |   83.47 ns |   117.02 ns |  4,206.9 ns | 0.1678 |        8.23 KB |

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

### 4. Enum Fallback

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

### 5. Custom Conversion

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

### 6. Serialization

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

### 7. Deserialization

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


### 8. Inheritance

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

### 9. Polymorphism

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

## How It Works

GenJson analyzes your code during compilation and generates specialized serialization code.

- **Serialization**: It pre-calculates the exact size needed for the JSON string and uses `string.Create` to fill the content directly via a `Span<char>`. This avoids the "double allocation" problem of `StringBuilder` (buffer resizing + final string) and eliminates allocations for formatting numbers and other primitives.
- **Deserialization**: It generates a recursive descent parser that operates on `ReadOnlySpan<char>`, avoiding substring allocations during parsing.
