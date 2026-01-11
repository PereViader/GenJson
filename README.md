# GenJson

GenJson is a high-performance C# Source Generator library that automatically creates `ToJson()` methods for your classes and structs. By generating serialization logic at compile time, it eliminates the overhead associated with runtime reflection, ensuring fast execution and low memory allocation.

## Features

- **High Performance**: zero-overhead serialization logic generated at compile-time.
- **Easy Integration**: simply mark your classes with the `[GenJson]` attribute.
- **Rich Type Support**:
  - Primitives: `int`, `string`, `bool`, `double`, etc.
  - Collections: `IEnumerable<T>`, `List<T>`, arrays `T[]`.
  - Dictionaries: `IDictionary<K, V>`, `IReadOnlyDictionary<K, V>`.
  - Nested Objects: recursive serialization of complex object graphs.
  - Standard Structs: `DateTime`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Version`.
- **Flexible Output**: Generates both string-returning methods and `StringBuilder` extensions for efficient appending.

## Usage

### 1. Mark your class

Add the `[GenJson]` attribute to any class or struct you wish to serialize.

```csharp
using GenJson;

[GenJson]
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Hobbies { get; set; }
}
```

### 2. Serialize

The generator creates extension methods for your types. You can convert directly to a JSON string or append to a `StringBuilder`.

```csharp
var person = new Person 
{ 
    Name = "Alice", 
    Age = 30, 
    Hobbies = new List<string> { "Coding", "Hiking" } 
};

// Get JSON string
string json = person.ToJson();
// Output: {"Name":"Alice","Age":30,"Hobbies":["Coding","Hiking"]}

// Append to existing StringBuilder (efficient for large payloads)
var sb = new StringBuilder();
person.ToJson(sb);
```

## How It Works

GenJson analyzes your code during compilation and generates specialized `ToJson` extension methods. These methods write JSON directly without inspecting types at runtime, resulting in code that is as fast as hand-written serialization routines.
