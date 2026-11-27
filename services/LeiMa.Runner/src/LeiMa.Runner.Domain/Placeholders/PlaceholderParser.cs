using System.Globalization;
using System.Text;
using LeiMa.Common;

namespace LeiMa.Runner.Domain.Placeholders;

/// <summary>
/// Splits a parameter string into text and placeholders (ADR 0004). A placeholder is <c>{{tasks.&lt;n&gt;.output.&lt;path&gt;}}</c>,
/// where the path is property names joined by dots, each optionally followed by list indexes: <c>items[0].id</c>.
/// A literal <c>{{</c> is written <c>\{{</c>. Creation uses it to validate; the run-time resolution uses it to substitute.
/// </summary>
public static class PlaceholderParser
{
    /// <summary>The error code for text that is not a well formed placeholder.</summary>
    public const string InvalidCode = "task.placeholder_invalid";

    /// <summary>What opens a placeholder.</summary>
    private const string Open = "{{";

    /// <summary>What closes a placeholder.</summary>
    private const string Close = "}}";

    /// <summary>The escape that stands for a literal <see cref="Open"/>.</summary>
    private const string EscapedOpen = "\\{{";

    /// <summary>What every placeholder starts with, before the task number.</summary>
    private const string TasksPrefix = "tasks.";

    /// <summary>What follows the task number, before the path.</summary>
    private const string OutputPrefix = "output.";

    /// <summary>The longest placeholder text quoted in an error message.</summary>
    private const int ExcerptLength = 60;

    /// <summary>Splits the text into literal parts and references, in order; adjacent text is merged into one part.</summary>
    /// <param name="text">The parameter string.</param>
    /// <returns>The parts (one literal for text without placeholders, none for an empty string), or a validation error.</returns>
    public static Result<IReadOnlyList<PlaceholderPart>> Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var parts = new List<PlaceholderPart>();
        var literal = new StringBuilder();
        var index = 0;
        while (index < text.Length)
        {
            var rest = text.AsSpan(index);
            if (rest.StartsWith(EscapedOpen, StringComparison.Ordinal))
            {
                literal.Append(Open);
                index += EscapedOpen.Length;
            }
            else if (rest.StartsWith(Open, StringComparison.Ordinal))
            {
                var end = text.IndexOf(Close, index + Open.Length, StringComparison.Ordinal);
                if (end < 0)
                {
                    return Invalid($"The placeholder starting at \"{Excerpt(text[index..])}\" is not closed with }}}}.");
                }

                var reference = ParseReference(text.Substring(index + Open.Length, end - index - Open.Length));
                if (reference.IsFailure)
                {
                    return reference.Error;
                }

                Flush(literal, parts);
                parts.Add(reference.Value);
                index = end + Close.Length;
            }
            else
            {
                literal.Append(text[index]);
                index++;
            }
        }

        Flush(literal, parts);
        return parts;
    }

    /// <summary>Builds the error for a placeholder that is not well formed.</summary>
    /// <param name="message">What is wrong, without the task it belongs to.</param>
    /// <returns>The validation error.</returns>
    private static Error Invalid(string message) => Error.Validation(InvalidCode, message);

    /// <summary>Cuts long text for an error message so it stays readable.</summary>
    /// <param name="text">The text to quote.</param>
    /// <returns>The text, or its start followed by an ellipsis.</returns>
    private static string Excerpt(string text) =>
        text.Length <= ExcerptLength ? text : string.Concat(text.AsSpan(0, ExcerptLength), "...");

    /// <summary>Moves the text collected so far into a literal part.</summary>
    /// <param name="literal">The collected text; emptied.</param>
    /// <param name="parts">The parts found so far.</param>
    private static void Flush(StringBuilder literal, List<PlaceholderPart> parts)
    {
        if (literal.Length > 0)
        {
            parts.Add(new LiteralPart(literal.ToString()));
            literal.Clear();
        }
    }

    /// <summary>Parses what is between the braces: <c>tasks.&lt;n&gt;.output.&lt;path&gt;</c>.</summary>
    /// <param name="body">The text between <c>{{</c> and <c>}}</c>.</param>
    /// <returns>The reference, or a validation error.</returns>
    private static Result<ReferencePart> ParseReference(string body)
    {
        var shape = $"\"{Excerpt(body)}\" must look like tasks.<n>.output.<path>, for example tasks.1.output.items[0].id.";
        if (!body.StartsWith(TasksPrefix, StringComparison.Ordinal))
        {
            return Invalid($"The placeholder {shape}");
        }

        var afterTasks = body[TasksPrefix.Length..];
        var dot = afterTasks.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0
            || !int.TryParse(afterTasks.AsSpan(0, dot), NumberStyles.None, CultureInfo.InvariantCulture, out var taskPosition)
            || taskPosition < 1)
        {
            return Invalid($"The placeholder {shape} The task number must be 1 or more.");
        }

        var afterNumber = afterTasks[(dot + 1)..];
        if (!afterNumber.StartsWith(OutputPrefix, StringComparison.Ordinal))
        {
            return Invalid($"The placeholder {shape}");
        }

        var path = ParsePath(afterNumber[OutputPrefix.Length..]);
        return path is null
            ? Invalid($"The placeholder {shape} The path must be names joined by dots, each optionally followed by [index].")
            : new ReferencePart(taskPosition, path);
    }

    /// <summary>Parses a path such as <c>items[0].id</c>.</summary>
    /// <param name="path">The text after <c>output.</c>.</param>
    /// <returns>The steps, or <see langword="null"/> if the path is not well formed.</returns>
    private static List<PathStep>? ParsePath(string path)
    {
        var steps = new List<PathStep>();
        var index = 0;
        while (true)
        {
            var nameStart = index;
            while (index < path.Length && IsNameCharacter(path[index]))
            {
                index++;
            }

            if (index == nameStart)
            {
                return null;
            }

            steps.Add(new PropertyStep(path[nameStart..index]));
            while (index < path.Length && path[index] == '[')
            {
                var close = path.IndexOf(']', index);
                if (close < 0
                    || !int.TryParse(path.AsSpan(index + 1, close - index - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var listIndex))
                {
                    return null;
                }

                steps.Add(new IndexStep(listIndex));
                index = close + 1;
            }

            if (index == path.Length)
            {
                return steps;
            }

            if (path[index] != '.')
            {
                return null;
            }

            index++;
        }
    }

    /// <summary>Tells whether the character may be part of a property name in a path.</summary>
    /// <param name="character">The character.</param>
    /// <returns><see langword="true"/> for letters, digits, underscore and hyphen.</returns>
    private static bool IsNameCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '_' or '-';
}
