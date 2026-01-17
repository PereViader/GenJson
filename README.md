# GenJson

GenJson is a **zero-allocation**, high-performance C# Source Generator library that automatically creates `ToJson()` and `FromJson()` methods for your classes and structs.

## Features

- **Compile-Time Generation**: No reflection overhead at runtime.
- **Zero* Allocation Serialization**: Uses `Span` based string creation to write directly into the result string's memory, avoiding `StringBuilder` and intermediate string allocations for primitives.
- **Zero* Allocation Deserialization**: Uses `ReadOnlySpan<char>` based parsing logic to avoid intermediate string allocations.
- **Easy Integration**: Simply mark your classes with the `[GenJson]` attribute.
- **Rich Type Support**:
  - Primitives: `int`, `string`, `bool`, `double`, `float`, `decimal` etc.
  - Standard Structs: `DateTime`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Version`, `DateTimeOffset`.
  - Dictionaries: `IReadOnlyDictionary<K, V>` 
  - Collections: `IEnumerable<T>`
  - Enums: Serialized as backing type (default) or string
  - Nested Objects: Recursive serialization of complex object graphs.

> Zero-allocation* means that no unnecessary memory allocations are performed. Only the resulting strings are allocated.

## Usage

### 1. Mark your class

Add the `[GenJson]` attribute to any class or struct you wish to serialize. The class must be `partial`.

```csharp
using GenJson;

[GenJson]
public partial class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public List<string> Hobbies { get; set; }
}

public enum Gender
{
    Male,
    Female
}
```

### 2 Mark your enum

Enum properties of classes may be marked with
- `[GenJson.Enum.AsNumber]` to serialize as a number (default, not required)
- `[GenJson.Enum.AsText]` to serialize as a string.

```csharp
    public Gender Gender { get; set; } // <-- Json will be de/serialized using 0 or 1
```

```csharp
    [GenJson.Enum.AsNumber] // <-- Json will be de/serialized using 0 or 1
    public Gender Gender { get; set; }
```

```csharp
    [GenJson.Enum.AsText] // <-- Json will be de/serialized using "Male" or "Female"
    public Gender Gender { get; set; }
```

### 3. Serialization

The generator creates a `ToJson()` method.

```csharp
var person = new Person 
{ 
    Name = "Alice", 
    Age = 30, 
    Gender = Gender.Female,
    Hobbies = new List<string> { "Coding", "Hiking" } 
};

// Zero-allocation serialization (allocates only the result string)
string json = person.ToJson();
// Output: {"Name":"Alice","Age":30,"Gender":"Female","Hobbies":["Coding","Hiking"]}

```

### 4. Deserialization

The generator creates a static `FromJson` method on your class.

```csharp
string json = """{"Name":"Alice","Age":30,"Gender":"Female"}""";

var person = Person.FromJson(json);
```

## How It Works

GenJson analyzes your code during compilation and generates specialized serialization code.

- **Serialization**: It pre-calculates the exact size needed for the JSON string and uses `string.Create` to fill the content directly via a `Span<char>`. This avoids the "double allocation" problem of `StringBuilder` (buffer resizing + final string) and eliminates allocations for formatting numbers and other primitives.
- **Deserialization**: It generates a recursive descent parser that operates on `ReadOnlySpan<char>`, avoiding substring allocations during parsing.
