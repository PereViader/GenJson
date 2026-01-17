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

## Benchmark

```
| Method                  | Mean [ns]  | Error [ns] | StdDev [ns] | Median [ns] | Gen0   | Allocated [KB] |
|------------------------ |-----------:|-----------:|------------:|------------:|-------:|---------------:|
| GenJson_ToJson          |   903.7 ns |   18.07 ns |    29.68 ns |    904.7 ns | 0.0324 |         1.6 KB |
| MicrosoftJson_ToJson    | 1,211.2 ns |   24.14 ns |    36.87 ns |  1,188.1 ns | 0.0381 |        1.92 KB |
| NewtonsoftJson_ToJson   | 2,320.1 ns |   45.44 ns |    80.77 ns |  2,305.5 ns | 0.1183 |        5.95 KB |
| GenJson_FromJson        | 1,647.0 ns |   32.49 ns |    51.54 ns |  1,643.2 ns | 0.0534 |        2.69 KB |
| MicrosoftJson_FromJson  | 2,410.6 ns |   48.02 ns |    62.44 ns |  2,372.8 ns | 0.0610 |           3 KB |
| NewtonsoftJson_FromJson | 4,094.4 ns |   79.59 ns |   106.25 ns |  4,049.3 ns | 0.1678 |        8.23 KB |
```
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
