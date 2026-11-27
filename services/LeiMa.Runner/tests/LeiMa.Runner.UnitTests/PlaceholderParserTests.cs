using LeiMa.Common;
using LeiMa.Runner.Domain.Placeholders;

namespace LeiMa.Runner.UnitTests;

/// <summary>Tests how parameter strings are split into text and placeholders (ADR 0004).</summary>
public class PlaceholderParserTests
{
    private static IReadOnlyList<PlaceholderPart> ParseOk(string text)
    {
        var result = PlaceholderParser.Parse(text);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    private static ReferencePart OnlyReference(string text) => Assert.IsType<ReferencePart>(Assert.Single(ParseOk(text)));

    [Fact]
    public void Parse_TextWithoutPlaceholders_IsOneLiteral()
    {
        var part = Assert.Single(ParseOk("hello customers"));

        Assert.Equal(new LiteralPart("hello customers"), part);
    }

    [Fact]
    public void Parse_EmptyString_HasNoParts()
    {
        Assert.Empty(ParseOk(string.Empty));
    }

    [Fact]
    public void Parse_WholeStringPlaceholder_IsOneReference()
    {
        var reference = OnlyReference("{{tasks.2.output.total}}");

        Assert.Equal(2, reference.TaskPosition);
        Assert.Equal([new PropertyStep("total")], reference.Path);
    }

    [Fact]
    public void Parse_PathWithIndexesAndNestedNames_IsSplitIntoSteps()
    {
        var reference = OnlyReference("{{tasks.1.output.items[0].tags[12][3].first-name_x}}");

        Assert.Equal(1, reference.TaskPosition);
        Assert.Equal(
            [
                new PropertyStep("items"),
                new IndexStep(0),
                new PropertyStep("tags"),
                new IndexStep(12),
                new IndexStep(3),
                new PropertyStep("first-name_x"),
            ],
            reference.Path);
    }

    [Fact]
    public void Parse_PlaceholderInsideText_KeepsTheTextAroundIt()
    {
        var parts = ParseOk("first of {{tasks.2.output.total}} items, then {{tasks.3.output.id}}!");

        Assert.Equal(5, parts.Count);
        Assert.Equal(new LiteralPart("first of "), parts[0]);
        Assert.Equal(2, Assert.IsType<ReferencePart>(parts[1]).TaskPosition);
        Assert.Equal(new LiteralPart(" items, then "), parts[2]);
        Assert.Equal(3, Assert.IsType<ReferencePart>(parts[3]).TaskPosition);
        Assert.Equal(new LiteralPart("!"), parts[4]);
    }

    [Fact]
    public void Parse_EscapedBraces_AreLiteralText()
    {
        var parts = ParseOk(@"write \{{tasks.1.output.id}} as text");

        Assert.Equal(new LiteralPart("write {{tasks.1.output.id}} as text"), Assert.Single(parts));
    }

    [Fact]
    public void Parse_EscapedAndRealPlaceholder_AreSeparated()
    {
        var parts = ParseOk(@"\{{ and {{tasks.1.output.id}}");

        Assert.Equal(new LiteralPart("{{ and "), parts[0]);
        Assert.IsType<ReferencePart>(parts[1]);
    }

    [Fact]
    public void Parse_ClosingBracesWithoutOpening_AreText()
    {
        Assert.Equal(new LiteralPart("a }} b"), Assert.Single(ParseOk("a }} b")));
    }

    [Theory]
    [InlineData("{{tasks.1.output.id")]
    [InlineData("{{")]
    [InlineData("{{}}")]
    [InlineData("{{ tasks.1.output.id }}")]
    [InlineData("{{task.1.output.id}}")]
    [InlineData("{{tasks.output.id}}")]
    [InlineData("{{tasks.x.output.id}}")]
    [InlineData("{{tasks.0.output.id}}")]
    [InlineData("{{tasks.-1.output.id}}")]
    [InlineData("{{tasks.+1.output.id}}")]
    [InlineData("{{tasks.99999999999.output.id}}")]
    [InlineData("{{tasks.1.input.id}}")]
    [InlineData("{{tasks.1.output}}")]
    [InlineData("{{tasks.1.output.}}")]
    [InlineData("{{tasks.1.output..id}}")]
    [InlineData("{{tasks.1.output.id.}}")]
    [InlineData("{{tasks.1.output.items[}}")]
    [InlineData("{{tasks.1.output.items[]}}")]
    [InlineData("{{tasks.1.output.items[-1]}}")]
    [InlineData("{{tasks.1.output.items[a]}}")]
    [InlineData("{{tasks.1.output.items[0]id}}")]
    [InlineData("{{tasks.1.output.[0]}}")]
    [InlineData("{{tasks.1.output.id name}}")]
    public void Parse_MalformedPlaceholder_IsRefusedWithTheInvalidCode(string text)
    {
        var result = PlaceholderParser.Parse(text);

        Assert.True(result.IsFailure, "The placeholder should have been refused.");
        Assert.Equal(PlaceholderParser.InvalidCode, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Parse_LongMalformedPlaceholder_QuotesOnlyTheStartInTheMessage()
    {
        var result = PlaceholderParser.Parse("{{" + new string('x', 500) + "}}");

        Assert.True(result.IsFailure);
        Assert.True(result.Error.Message.Length < 300, "The message should not repeat the whole text.");
    }

    [Fact]
    public void Parse_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => PlaceholderParser.Parse(null!));
    }
}
