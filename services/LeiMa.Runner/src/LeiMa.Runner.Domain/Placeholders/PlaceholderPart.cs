namespace LeiMa.Runner.Domain.Placeholders;

/// <summary>A piece of a parameter string: plain text or a reference to the output of an earlier task.</summary>
public abstract record PlaceholderPart
{
    /// <summary>Initializes a new instance of the <see cref="PlaceholderPart"/> class; only the two parts below exist.</summary>
    private protected PlaceholderPart()
    {
    }
}

/// <summary>Plain text, with the escape <c>\{{</c> already turned into <c>{{</c>.</summary>
/// <param name="Text">The text.</param>
public sealed record LiteralPart(string Text) : PlaceholderPart;

/// <summary>A reference <c>{{tasks.&lt;n&gt;.output.&lt;path&gt;}}</c> to a value in the output of an earlier task.</summary>
/// <param name="TaskPosition">The 1-based position of the task whose output is used.</param>
/// <param name="Path">The steps from the output to the value, at least one.</param>
public sealed record ReferencePart(int TaskPosition, IReadOnlyList<PathStep> Path) : PlaceholderPart;
