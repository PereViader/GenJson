// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using GenJson;
using Perfolizer.Horology;
using Perfolizer.Metrology;

BenchmarkRunner.Run(typeof(BenchmarkToJson).Assembly, ManualConfig
    .Create(DefaultConfig.Instance)
    .WithSummaryStyle(new SummaryStyle(
        cultureInfo: System.Globalization.CultureInfo.InvariantCulture,
        printUnitsInHeader: true,
        sizeUnit: SizeUnit.KB,
        timeUnit: TimeUnit.Nanosecond,
        printZeroValuesInContent: true
    ))
    .WithOptions(ConfigOptions.JoinSummary)
    .WithOptions(ConfigOptions.DisableLogFile)
);

[GenJson]
public partial class RootObject
{
    public int Value1 { get; init; }
    public bool Value2 { get; init; }
    public required List<int> Value3 { get; init; }
    public required Dictionary<string, string> Value4 { get; init; }
    public required List<SomeEnum> Value5 { get; init; }
    public required Dictionary<string, NestedObject> Value6 { get; init; }
    public required NestedObject[] Value7 { get; init; }
}

[GenJson]
public partial class NestedObject
{
    public int Value1 { get; init; }
    public bool Value2 { get; init; }
}

public enum SomeEnum
{
    One = 1,
    Two = 25,
    Three = 33
}

[MemoryDiagnoser]
public class BenchmarkToJson
{
    [Benchmark]
    public string GenJson_ToJson()
    {
        return RootObject.ToJson();
    }

    [Benchmark]
    public string NewtonsoftJson_ToJson()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
    }
    
    [Benchmark]
    public string MicrosoftJson_ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(RootObject);
    }
    
    [Benchmark]
    public RootObject GenJson_FromJson()
    {
        return RootObject.FromJson(GenJson);
    }

    [Benchmark]
    public RootObject NewtonsoftJson_FromJson()
    {
        return Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(NewtonsoftJson)!;
    }
    
    [Benchmark]
    public RootObject MicrosoftJson_FromJson()
    {
        return System.Text.Json.JsonSerializer.Deserialize<RootObject>(MicrosoftJson)!;
    }

    private static readonly RootObject RootObject = new()
    {
        Value1 = int.MaxValue,
        Value2 = false,
        Value3 = [-1, 0, 1, 123456, 789012, int.MinValue],
        Value4 = new()
        {
            { "Description", "Standard dummy data for performance testing" },
            { "Platform", "Unity 2022.3 LTS" },
            { "Compiler", "Roslyn Source Generator" },
            { "Status", "Benchmarking" },
            { "Empty", "" },
        },
        Value5 = [
            SomeEnum.One, SomeEnum.Two, SomeEnum.Three,
            SomeEnum.Three, SomeEnum.Two, SomeEnum.One
        ],
        Value6 = new()
        {
            { "Node_Alpha", new NestedObject { Value1 = 10, Value2 = true } },
            { "Node_Beta", new NestedObject { Value1 = 20, Value2 = false } },
            { "Node_Gamma", new NestedObject { Value1 = 30, Value2 = true } },
            { "Node_Delta", new NestedObject { Value1 = 40, Value2 = false } }
        },
        Value7 = [
            new NestedObject { Value1 = 1000, Value2 = true },
            new NestedObject { Value1 = 2000, Value2 = false },
            new NestedObject { Value1 = 3000, Value2 = true },
            new NestedObject { Value1 = 4000, Value2 = false },
            new NestedObject { Value1 = 5000, Value2 = true }
        ]
    };
    
    private static readonly string GenJson = RootObject.ToJson();
    private static readonly string NewtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(RootObject);
    private static readonly string MicrosoftJson = System.Text.Json.JsonSerializer.Serialize(RootObject);
}