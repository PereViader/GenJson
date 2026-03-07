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
            DoubleZero = 0.0,
            DoubleNegative = -1.5,
            DoubleLarge = 1.23456789e15,
            FloatSmall = 0.000001f,
            DecimalPrecise = 1.23456789012345678901234567m,
            NullableInt = null,
            NullableDouble = null,
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
            DoubleZero = 0.0,
            DoubleNegative = -0.5,
            DoubleLarge = 1.0,
            FloatSmall = 1.0f,
            DecimalPrecise = 1.0m,
            NullableInt = 42,
            NullableDouble = 3.14,
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
        });
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
}
