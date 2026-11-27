namespace LeiMa.Runner.Domain.Placeholders;

/// <summary>One step of the path into a task output: a property name or a list index.</summary>
public abstract record PathStep
{
    /// <summary>Initializes a new instance of the <see cref="PathStep"/> class; only the two steps below exist.</summary>
    private protected PathStep()
    {
    }
}

/// <summary>A step to a property of a JSON object, such as <c>items</c> in <c>items[0].id</c>.</summary>
/// <param name="Name">The property name.</param>
public sealed record PropertyStep(string Name) : PathStep;

/// <summary>A step to an element of a JSON array, such as <c>[0]</c> in <c>items[0].id</c>.</summary>
/// <param name="Index">The 0-based index.</param>
public sealed record IndexStep(int Index) : PathStep;
