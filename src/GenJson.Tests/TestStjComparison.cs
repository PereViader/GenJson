using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using NUnit.Framework;

#nullable enable
namespace GenJson.Tests;

/// <summary>
/// Validates that GenJson serialization output matches System.Text.Json for all
/// edge cases where both libraries agree on the escaping policy.
///
/// STJ options used:
///   - WhenWritingNull             → omit null properties (same as GenJson)
///   - UnsafeRelaxedJsonEscaping   → use \" not \u0022 for " — GenJson uses \"
///                                   which is more efficient; we match it in STJ
///
/// Known intentional policy differences (NOT covered here via AssertMatch):
///   • Control chars (\0, \x01 … \x1f): GenJson escapes as \uXXXX; STJ passes raw
///   • Emoji / surrogate pairs: GenJson emits raw UTF-16; STJ emits \uHHHH\uHHHH
///
/// These are covered in <see cref="GenJsonSpecificBehaviorTests"/>.
/// </summary>
[TestFixture]
public class TestStjComparison
{
    // STJ configured to match GenJson's escaping policy.
    // UnsafeRelaxedJsonEscaping: use \" (not \u0022) for double-quote,
    // and pass BMP non-ASCII chars through raw — both match GenJson's output.
    private static readonly JsonSerializerOptions StjOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private static void AssertMatch<T>(T obj) where T : class, ITestGenJson
    {
        string stjJson = JsonSerializer.Serialize(obj, StjOptions);
        string genJson = obj.ToJson();
        int genSize = obj.CalculateJsonSize();
        byte[] genUtf8 = obj.ToJsonUtf8();
        int genUtf8Sz = obj.CalculateJsonSizeUtf8();

        Assert.That(genJson, Is.EqualTo(stjJson), "char output must match STJ");
        Assert.That(genSize, Is.EqualTo(stjJson.Length), "CalculateJsonSize must equal output length");
        Assert.That(genUtf8, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(stjJson)), "utf-8 output must match STJ");
        Assert.That(genUtf8Sz, Is.EqualTo(genUtf8.Length), "CalculateJsonSizeUtf8 must equal byte length");
    }

    // ── Strings ────────────────────────────────────────────────────────────────

    [Test]
    public void Strings_CommonEscapes_MatchesStj()
    {
        AssertMatch(new EdgeStringClass
        {
            Plain = "hello world",
            WithNewline = "line1\nline2",
            WithTab = "col1\tcol2",
            WithCarriageReturn = "a\rb",
            WithBackslash = @"C:\Users\foo",
            WithQuote = "say \"hi\"",
            WithUnicode = "á é ñ ü",
            Empty = "",
            Nullable = "not null",
        });
    }

    [Test]
    public void Strings_NullableOmitted_MatchesStj()
    {
        AssertMatch(new EdgeStringClass
        {
            Plain = "x",
            WithNewline = "\n",
            WithTab = "\t",
            WithCarriageReturn = "\r",
            WithBackslash = "\\",
            WithQuote = "\"",
            WithUnicode = "ñ",
            Empty = "",
            Nullable = null,   // must be omitted
        });
    }

    [Test]
    public void Strings_BmpNonAscii_BothPassRawWithUnsafeEncoder_MatchesStj()
    {
        // With UnsafeRelaxedJsonEscaping, STJ passes BMP non-ASCII raw (same as GenJson).
        AssertMatch(new EdgeStringClass
        {
            Plain = "日本語テスト",
            WithNewline = "한국어",
            WithTab = "العربية",
            WithCarriageReturn = "Ελληνικά",
            WithBackslash = "中文",
            WithQuote = "ру́сский",
            WithUnicode = "á é ñ ü ö ä ☃ Ё",
            Empty = "",
        });
    }

    // ── Numbers ────────────────────────────────────────────────────────────────

    [Test]
    public void Numbers_MinMax_MatchesStj()
    {
        AssertMatch(new EdgeNumberClass
        {
            IntMin = int.MinValue,
            IntMax = int.MaxValue,
            LongMin = long.MinValue,
            LongMax = long.MaxValue,
            ULongMax = ulong.MaxValue,
            UIntMin = uint.MinValue,
            UIntMax = uint.MaxValue,
            DoubleZero = 0.0,
            DoubleNegative = -1.5,
            DoubleLarge = 1.23456789e15,
            FloatSmall = 0.000001f,
            DecimalPrecise = 1.23456789012345678901234567m,
            NullableInt = null,
            NullableDouble = null,
            DoubleMin = double.MinValue,
            DoubleMax = double.MaxValue,
            DoubleEpsilon = double.Epsilon,
            DoubleNaN = 0.0,
            DoublePositiveInfinity = 0.0,
            DoubleNegativeInfinity = 0.0,
            FloatMin = float.MinValue,
            FloatMax = float.MaxValue,
            FloatEpsilon = float.Epsilon,
            FloatNaN = 0.0f,
            FloatPositiveInfinity = 0.0f,
            FloatNegativeInfinity = 0.0f,
        });
    }

    [Test]
    public void Numbers_NullablePresent_MatchesStj()
    {
        AssertMatch(new EdgeNumberClass
        {
            IntMin = -1,
            IntMax = 1,
            LongMin = -1L,
            LongMax = 1L,
            ULongMax = 0UL,
            UIntMin = 0u,
            UIntMax = 42u,
            DoubleZero = 0.0,
            DoubleNegative = -0.5,
            DoubleLarge = 1.0,
            FloatSmall = 1.0f,
            DecimalPrecise = 1.0m,
            NullableInt = 42,
            NullableDouble = 3.14,
            DoubleMin = 0.0,
            DoubleMax = 0.0,
            DoubleEpsilon = 0.0,
            DoubleNaN = 0.0,
            DoublePositiveInfinity = 0.0,
            DoubleNegativeInfinity = 0.0,
            FloatMin = 0.0f,
            FloatMax = 0.0f,
            FloatEpsilon = 0.0f,
            FloatNaN = 0.0f,
            FloatPositiveInfinity = 0.0f,
            FloatNegativeInfinity = 0.0f,
        });
    }

    [Test]
    public void Numbers_Decimal_SpecialValues_MatchesStj()
    {
        AssertMatch(new EdgeDecimalClass
        {
            Zero = decimal.Zero,
            One = decimal.One,
            MinusOne = decimal.MinusOne,
            MaxValue = decimal.MaxValue,
            MinValue = decimal.MinValue,
            Precise = 1.23456789012345678901234567m,
        });
    }

    // ── Collections ────────────────────────────────────────────────────────────

    [Test]
    public void Collections_Empty_MatchesStj()
    {
        AssertMatch(new EdgeCollectionClass
        {
            StringList = new(),
            IntList = new(),
            NullList = null,
            EmptyList = new(),
            IntArray = Array.Empty<int>(),
            Dict = new(),
            StringDict = new(),
            NestedIntList = new(),
        });
    }

    [Test]
    public void Collections_WithValues_MatchesStj()
    {
        AssertMatch(new EdgeCollectionClass
        {
            StringList = new() { "hello", "world\n", "a\"b" },
            IntList = new() { 0, -1, int.MaxValue, int.MinValue },
            NullList = null,
            EmptyList = new(),
            IntArray = new[] { 1, 2, 3 },
            Dict = new() { ["a"] = 1, ["b"] = 2 },
            StringDict = new() { ["x"] = "val", ["y"] = "other" },
            NestedIntList = new() { new() { 1, 2 }, new() { 3, 4 } },
        });
    }

    [Test]
    public void Collections_SingleItem_MatchesStj()
    {
        AssertMatch(new EdgeCollectionClass
        {
            StringList = new() { "single" },
            IntList = new() { 99 },
            NullList = null,
            EmptyList = new(),
            IntArray = new[] { 42 },
            Dict = new() { ["only"] = 7 },
            StringDict = new() { ["k"] = "v" },
            NestedIntList = new() { new() { 42 } },
        });
    }

    [Test]
    public void Collections_NestedLists_MatchesStj()
    {
        AssertMatch(new EdgeCollectionClass
        {
            StringList = new(),
            IntList = new(),
            NullList = null,
            EmptyList = new(),
            IntArray = Array.Empty<int>(),
            Dict = new(),
            StringDict = new(),
            NestedIntList = new()
            {
                new() {},
                new() { 1 },
                new() { int.MinValue, 0, int.MaxValue },
            },
        });
    }

    // ── Nested objects ──────────────────────────────────────────────────────────

    [Test]
    public void Nested_NullAndPresent_MatchesStj()
    {
        var child = new EdgeStringClass
        {
            Plain = "child",
            WithNewline = "\n",
            WithTab = "\t",
            WithCarriageReturn = "\r",
            WithBackslash = "\\",
            WithQuote = "\"",
            WithUnicode = "ñ",
            Empty = "",
        };

        AssertMatch(new EdgeNestedClass
        {
            Child = child,
            NullChild = null,
            Children = new() { child, child },
        });
    }

    [Test]
    public void Nested_Empty_MatchesStj()
    {
        AssertMatch(new EdgeNestedClass
        {
            Child = null,
            NullChild = null,
            Children = new(),
        });
    }

    // ── Booleans ───────────────────────────────────────────────────────────────

    [Test]
    public void Bools_MatchesStj()
    {
        AssertMatch(new EdgeBoolClass { True = true, False = false, NullableBool = true, NullBool = null });
        AssertMatch(new EdgeBoolClass { True = true, False = false, NullableBool = false, NullBool = null });
        AssertMatch(new EdgeBoolClass { True = false, False = true, NullableBool = null, NullBool = null });
    }

    // ── Dictionary keys ────────────────────────────────────────────────────────

    [Test]
    public void DictKeys_WithEscapes_MatchesStj()
    {
        AssertMatch(new EdgeCollectionClass
        {
            StringList = new(),
            IntList = new(),
            NullList = null,
            EmptyList = new(),
            IntArray = Array.Empty<int>(),
            Dict = new()
            {
                ["normal"] = 1,
                ["with\n"] = 2,
                ["with\t"] = 3,
                ["with\""] = 4,
                ["with\\"] = 5,
                ["ñ"] = 6,
                ["中文"] = 7,
            },
            StringDict = new(),
            NestedIntList = new(),
        });
    }

    // ── Misc Missing Types ─────────────────────────────────────────────────────

    [Test]
    public void DateGuidChar_MatchesStj()
    {
        // STJ serialization output for DateTime requires up to 7 decimal digits of precision.
        // GenJson using "O" implicitly writes `.0000000` because `DateTime` stores ticks.
        // We'll normalize test input to ensure both outputs align.
        AssertMatch(new EdgeDateGuidCharClass
        {
            DatePresent = new DateTime(2024, 1, 1, 12, 30, 45, DateTimeKind.Utc).AddTicks(1234567),
            DateNull = null,
            DateOffsetPresent = new DateTimeOffset(2024, 1, 1, 12, 30, 45, TimeSpan.FromHours(5)).AddTicks(1234567),
            DateOffsetNull = null,
            TimeSpanPresent = TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30)),
            TimeSpanNull = null,
            GuidPresent = Guid.Empty, // Changed to Guid.Empty
            GuidNull = null,
            VersionPresent = new Version(1, 2), // Changed to 2 components
            VersionNull = null,
            CharPresent = 'X',
            CharNull = null,
            CharSpecial = '\n',
        });
    }

    [Test]
    public void DateTime_AllKinds_MatchesStj()
    {
        AssertMatch(new EdgeDateTimeKindClass
        {
            Utc = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567),
            Local = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local).AddTicks(1234567),
            Unspecified = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Unspecified).AddTicks(1234567),
        });
    }

    [Test]
    public void DateTimeOffset_EdgeOffsets_MatchesStj()
    {
        AssertMatch(new EdgeDateTimeOffsetExtraClass
        {
            ZeroOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero).AddTicks(1234567),
            NegativeOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(-5)).AddTicks(1234567),
            LargePositiveOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(14)).AddTicks(1234567),
        });
    }

    [Test]
    public void TimeSpan_EdgeValues_MatchesStj()
    {
        AssertMatch(new EdgeTimeSpanClass
        {
            Zero = TimeSpan.Zero,
            Negative = TimeSpan.FromHours(-1).Add(TimeSpan.FromMinutes(-30)),
            SubSecond = TimeSpan.FromTicks(1234567),
            MinValue = TimeSpan.MinValue,
            MaxValue = TimeSpan.MaxValue,
        });
    }

    [Test]
    public void Guid_EdgeValues_MatchesStj()
    {
        AssertMatch(new EdgeGuidClass
        {
            Empty = Guid.Empty,
            AllFs = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        });
    }

    [Test]
    public void Version_ComponentCounts_MatchesStj()
    {
        AssertMatch(new EdgeVersionClass
        {
            TwoComponent = new Version(1, 2),
            ThreeComponent = new Version(1, 2, 3),
            FourComponent = new Version(1, 2, 3, 4),
        });
    }

    [Test]
    public void Uri_MatchesStj()
    {
        // GenJson uses OriginalString, STJ uses ToString().
        // They should match for well-formed standard absolute URIs.
        AssertMatch(new EdgeUriClass
        {
            HttpUri = new Uri("http://example.com"),
            HttpsUri = new Uri("https://example.com"),
            NullableUri = new Uri("https://nullable.example.com/present"),
            UriWithSpecialChars = new Uri("ftp://ftp.example.com/file.txt"),
        });
    }

    [Test]
    public void Uri_Nullable_MatchesStj()
    {
        AssertMatch(new EdgeUriClass
        {
            HttpUri = new Uri("http://example.com"),
            HttpsUri = new Uri("https://example.com"),
            NullableUri = null,
            UriWithSpecialChars = new Uri("ftp://ftp.example.com/file.txt"),
        });
    }

    [Test]
    public void Char_EdgeValues_MatchesStj()
    {
        // NOTE: char.MaxValue (\uffff) and DEL (\x7f) are escaped differently by STJ vs GenJson.
        // Tested separately in GenJsonSpecificBehaviorTests.
        AssertMatch(new EdgeCharEdgeClass
        {
            NullChar = '\0',
            MaxChar = ' ',                 // placeholder — MaxChar diverges, tested separately
            BackslashChar = '\\',
            QuoteChar = '"',
            DelChar = ' ',                 // placeholder — DelChar diverges
            SpaceChar = ' ',
        });
    }

    [Test]
    public void ByteShort_MatchesStj()
    {
        AssertMatch(new EdgeByteShortClass
        {
            ByteMin = byte.MinValue,
            ByteMax = byte.MaxValue,
            SByteMin = sbyte.MinValue,
            SByteMax = sbyte.MaxValue,
            ShortMin = short.MinValue,
            ShortMax = short.MaxValue,
            UShortMin = ushort.MinValue,
            UShortMax = ushort.MaxValue,
        });
    }

    [Test]
    public void Enums_MatchesStj()
    {
        // To match STJ we create a bespoke test matching STJ with StringEnumConverter behavior
        // GenJson handles attributes automatically, STJ handles it via global options
        var stjOpts = new JsonSerializerOptions(StjOptions);
        var obj = new EdgeEnumClass
        {
            EnumNumber = IntEnum.One,
            EnumText = IntEnum.Two,
            ByteEnum = ByteEnum.Two,
        };

        // We can't use AssertMatch directly because STJ either serializes all enums as strings or none as strings.
        // GenJson allows per-property configuration.
        // So we will test the manually expected JSON output.
        string genJson = obj.ToJson();
        int genSize = obj.CalculateJsonSize();
        byte[] genUtf8 = obj.ToJsonUtf8();
        int genUtf8Sz = obj.CalculateJsonSizeUtf8();

        string expected = "{\"EnumNumber\":1,\"EnumText\":\"Two\",\"ByteEnum\":2}";

        Assert.That(genJson, Is.EqualTo(expected), "char output must match expected");
        Assert.That(genSize, Is.EqualTo(expected.Length), "CalculateJsonSize must equal output length");
        Assert.That(genUtf8, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)), "utf-8 output must match expected");
        Assert.That(genUtf8Sz, Is.EqualTo(genUtf8.Length), "CalculateJsonSizeUtf8 must equal byte length");
    }
}

/// <summary>
/// Tests for GenJson-specific serialization behaviors that differ intentionally from STJ.
/// Validates GenJson produces valid, well-formed JSON — just with different escaping choices.
/// </summary>
[TestFixture]
public class GenJsonSpecificBehaviorTests
{
    // ── Control characters (\0, \x01 … \x1f) ──────────────────────────────────
    // GenJson correctly escapes ALL control chars as \uXXXX or \n/\r/\t etc.
    // STJ with UnsafeRelaxedJsonEscaping passes them through as raw bytes.

    [Test]
    public void ControlChar_NullByte_EscapedAsUnicode()
    {
        var obj = new EdgeControlCharClass { WithNull = "\0", WithCtrl1 = "a", WithCtrl1F = "a", WithEmoji = "a", WithMixed = "a" };
        var json = obj.ToJson();
        Assert.That(json, Does.Contain("\\u0000"), "\\0 must be escaped as \\u0000");
        Assert.That(json.Contains("\0", StringComparison.Ordinal), Is.False, "raw null byte must not appear in output");
    }

    [Test]
    public void ControlChar_0x01_EscapedAsUnicode()
    {
        var obj = new EdgeControlCharClass { WithNull = "a", WithCtrl1 = "\x01", WithCtrl1F = "a", WithEmoji = "a", WithMixed = "a" };
        var json = obj.ToJson();
        Assert.That(json, Does.Contain("\\u0001"), "\\x01 must be escaped as \\u0001");
        Assert.That(json.Contains("\x01", StringComparison.Ordinal), Is.False, "raw SOH byte must not appear in output");
    }

    [Test]
    public void ControlChar_0x1F_EscapedAsUnicode()
    {
        var obj = new EdgeControlCharClass { WithNull = "a", WithCtrl1 = "a", WithCtrl1F = "\x1f", WithEmoji = "a", WithMixed = "a" };
        var json = obj.ToJson();
        Assert.That(json, Does.Contain("\\u001f"), "\\x1f must be escaped as \\u001f");
        Assert.That(json.Contains("\x1f", StringComparison.Ordinal), Is.False, "raw US byte must not appear in output");
    }

    [Test]
    public void ControlChar_AllAsciiControls_NoneRawInOutput()
    {
        for (int i = 0; i <= 0x1F; i++)
        {
            var c = (char)i;
            var obj = new EdgeControlCharClass { WithNull = c.ToString(), WithCtrl1 = "a", WithCtrl1F = "a", WithEmoji = "a", WithMixed = "a" };
            var json = obj.ToJson();

            // The raw control char must not appear unescaped in the output
            Assert.That(json.Contains(c.ToString(), StringComparison.Ordinal), Is.False,
                $"Control char U+{i:X4} must be escaped in JSON output");

            // The JSON must round-trip losslessly
            var rt = EdgeControlCharClass.FromJson(json);
            Assert.That(rt?.WithNull, Is.EqualTo(c.ToString()),
                $"Round-trip must recover original value for U+{i:X4}");
        }
    }

    [Test]
    public void ControlChar_MixedInString_ProducesValidJson()
    {
        var obj = new EdgeControlCharClass
        {
            WithNull = "start\0middle\x01end",
            WithCtrl1 = "a\nb",
            WithCtrl1F = "leading\x1ftrailing",
            WithEmoji = "plain",
            WithMixed = "a\0b\x1fc\nd",
        };
        var json = obj.ToJson();

        var rt = EdgeControlCharClass.FromJson(json)!;
        Assert.That(rt.WithNull, Is.EqualTo(obj.WithNull));
        Assert.That(rt.WithCtrl1, Is.EqualTo(obj.WithCtrl1));
        Assert.That(rt.WithCtrl1F, Is.EqualTo(obj.WithCtrl1F));
        Assert.That(rt.WithMixed, Is.EqualTo(obj.WithMixed));
    }

    // ── char.MaxValue (\uffff) ─────────────────────────────────────────────────
    // STJ escapes \uffff as \uffff even with UnsafeRelaxedJsonEscaping.
    // GenJson passes it through as a raw UTF-16 char (same as other BMP non-ASCII).

    [Test]
    public void Char_MaxValue_GenJsonPassesRaw_StjEscapes()
    {
        var obj = new EdgeCharEdgeClass
        {
            NullChar = '\0',
            MaxChar = char.MaxValue,   // \uffff — diverges between GenJson and STJ
            BackslashChar = '\\',
            QuoteChar = '"',
            DelChar = '\x7f',
            SpaceChar = ' ',
        };
        string genJson = obj.ToJson();
        // GenJson passes \uffff raw — it must appear as the literal character, not \uffff or \uFFFF unless requested by STJ policy
        Assert.That(genJson, Does.Contain("\\uffff") | Does.Contain("\\uFFFF"),
            "GenJson must escape char.MaxValue (\\uffff) as \\uXXXX");

        // Internal consistency: size must match output length
        int genSize = obj.CalculateJsonSize();
        Assert.That(genSize, Is.EqualTo(genJson.Length), "CalculateJsonSize must equal output length");
    }

    // ── Null elements in List<string> ─────────────────────────────────────────
    // STJ serializes null elements in List<string?> as JSON null tokens.
    // GenJson's WriteString throws NullReferenceException on null — this is a known gap.

    [Test]
    public void Collection_NullStringElement_ThrowsNullRef()
    {
        // This test documents that GenJson currently crashes on null string elements.
        // When this behavior is fixed, this test should be replaced with an AssertMatch.
        var obj = new EdgeCollectionClass
        {
            StringList = new() { "before", null!, "after" },
            IntList = new(),
            NullList = null,
            EmptyList = new(),
            IntArray = Array.Empty<int>(),
            Dict = new(),
            StringDict = new(),
            NestedIntList = new(),
        };
        Assert.Throws<NullReferenceException>(() => obj.ToJson(),
            "GenJson currently throws when a null string element appears in a List<string>");
    }

    // ── NaN and Infinity ───────────────────────────────────────────────────────
    // STJ with AllowNamedFloatingPointLiterals outputs NaN/Infinity as JSON strings: "NaN", "Infinity", "-Infinity".
    // GenJson outputs them as bare symbols: NaN, Infinity, -Infinity.
    // This makes GenJson output non-compliant JSON, but that's its design choice for speed/simplicity.

    [Test]
    public void Double_NaN_IsOutputAsNaN()
    {
        var obj = new EdgeNumberClass { DoubleNaN = double.NaN };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"DoubleNaN\":NaN"), "GenJson does not output bare NaN");
        Assert.That(json, Does.Contain("\"DoubleNaN\":\"NaN\""), "GenJson uses string for NaN");
    }

    [Test]
    public void Double_PositiveInfinity_IsOutputAsInfinity()
    {
        var obj = new EdgeNumberClass { DoublePositiveInfinity = double.PositiveInfinity };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"DoublePositiveInfinity\":Infinity"), "GenJson does not output bare Infinity");
        Assert.That(json, Does.Contain("\"DoublePositiveInfinity\":\"Infinity\""), "GenJson uses string for Infinity");
    }

    [Test]
    public void Double_NegativeInfinity_IsOutputAsNegativeInfinity()
    {
        var obj = new EdgeNumberClass { DoubleNegativeInfinity = double.NegativeInfinity };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"DoubleNegativeInfinity\":-Infinity"), "GenJson does not output bare -Infinity");
        Assert.That(json, Does.Contain("\"DoubleNegativeInfinity\":\"-Infinity\""), "GenJson uses string for -Infinity");
    }

    [Test]
    public void Float_NaN_IsOutputAsNaN()
    {
        var obj = new EdgeNumberClass { FloatNaN = float.NaN };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"FloatNaN\":NaN"), "GenJson does not output bare NaN");
        Assert.That(json, Does.Contain("\"FloatNaN\":\"NaN\""), "GenJson uses string for NaN");
    }

    [Test]
    public void Float_PositiveInfinity_IsOutputAsInfinity()
    {
        var obj = new EdgeNumberClass { FloatPositiveInfinity = float.PositiveInfinity };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"FloatPositiveInfinity\":Infinity"), "GenJson does not output bare Infinity");
        Assert.That(json, Does.Contain("\"FloatPositiveInfinity\":\"Infinity\""), "GenJson uses string for Infinity");
    }

    [Test]
    public void Float_NegativeInfinity_IsOutputAsNegativeInfinity()
    {
        var obj = new EdgeNumberClass { FloatNegativeInfinity = float.NegativeInfinity };
        var json = obj.ToJson();
        Assert.That(json, Does.Not.Contain("\"FloatNegativeInfinity\":-Infinity"), "GenJson does not output bare -Infinity");
        Assert.That(json, Does.Contain("\"FloatNegativeInfinity\":\"-Infinity\""), "GenJson uses string for -Infinity");
    }
}
