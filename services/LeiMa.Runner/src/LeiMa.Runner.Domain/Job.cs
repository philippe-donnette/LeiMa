using System.Text.Json;
using LeiMa.Common;
using LeiMa.Runner.Domain.Placeholders;

namespace LeiMa.Runner.Domain;

/// <summary>A job: an ordered list of tasks that run one after another against provider APIs.</summary>
public sealed class Job
{
    /// <summary>The longest allowed job name.</summary>
    public const int NameMaxLength = 200;

    /// <summary>The longest allowed job description.</summary>
    public const int DescriptionMaxLength = 2000;

    /// <summary>The longest allowed task type.</summary>
    public const int TaskTypeMaxLength = 100;

    /// <summary>The largest allowed number of tasks in one job.</summary>
    public const int MaxTasks = 100;

    /// <summary>The error code for a placeholder that refers to a task that does not run before the task that uses it.</summary>
    public const string PlaceholderReferenceInvalidCode = "task.placeholder_reference_invalid";

    /// <summary>The tasks of the job, in position order; the ORM maps this field.</summary>
    private readonly List<JobTask> _tasks = [];

    /// <summary>Initializes a new instance of the <see cref="Job"/> class; used by the ORM and <see cref="Create"/> only.</summary>
    private Job()
    {
    }

    /// <summary>Gets the id of the job.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the name of the job.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description of the job.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the status of the job.</summary>
    public JobStatus Status { get; private set; }

    /// <summary>Gets the time the job was soft deleted, or <c>null</c> if it was not.</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Gets the tasks of the job, in the order they run.</summary>
    public IReadOnlyList<JobTask> Tasks => _tasks;

    /// <summary>Creates a queued job whose tasks are numbered 1 to n in the order given.</summary>
    /// <param name="name">The job name; required, trimmed, at most <see cref="NameMaxLength"/> characters.</param>
    /// <param name="description">An optional description of at most <see cref="DescriptionMaxLength"/> characters; blank becomes <c>null</c>.</param>
    /// <param name="tasks">The tasks, in the order they must run; at least one.</param>
    /// <returns>The job, or a validation error naming the first rule that failed.</returns>
    public static Result<Job> Create(string? name, string? description, IReadOnlyList<TaskDefinition>? tasks)
    {
        var trimmedName = name?.Trim();
        if (string.IsNullOrEmpty(trimmedName))
        {
            return Error.Validation("job.name_required", "The job name is required.");
        }

        if (trimmedName.Length > NameMaxLength)
        {
            return Error.Validation("job.name_too_long", $"The job name can have at most {NameMaxLength} characters.");
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (trimmedDescription is { Length: > DescriptionMaxLength })
        {
            return Error.Validation("job.description_too_long", $"The job description can have at most {DescriptionMaxLength} characters.");
        }

        if (tasks is null || tasks.Count == 0)
        {
            return Error.Validation("job.tasks_required", "A job needs at least one task.");
        }

        if (tasks.Count > MaxTasks)
        {
            return Error.Validation("job.too_many_tasks", $"A job can have at most {MaxTasks} tasks.");
        }

        var job = new Job
        {
            Id = Ids.NewId(),
            Name = trimmedName,
            Description = trimmedDescription,
            Status = JobStatus.Queued,
        };

        for (var index = 0; index < tasks.Count; index++)
        {
            var position = index + 1;
            var task = BuildTask(position, tasks[index]);
            if (task.IsFailure)
            {
                return task.Error;
            }

            job._tasks.Add(task.Value);
        }

        return job;
    }

    /// <summary>Validates a task definition and builds the pending task at <paramref name="position"/>.</summary>
    /// <param name="position">The 1-based position of the task.</param>
    /// <param name="definition">What the caller asked for.</param>
    /// <returns>The task, or a validation error.</returns>
    private static Result<JobTask> BuildTask(int position, TaskDefinition definition)
    {
        var taskType = definition.TaskType?.Trim();
        if (string.IsNullOrEmpty(taskType))
        {
            return Error.Validation("task.type_required", $"Task {position}: the task type is required.");
        }

        if (taskType.Length > TaskTypeMaxLength)
        {
            return Error.Validation("task.type_too_long", $"Task {position}: the task type can have at most {TaskTypeMaxLength} characters.");
        }

        if (definition.Parameters.ValueKind != JsonValueKind.Object)
        {
            return Error.Validation("task.parameters_invalid", $"Task {position}: the parameters must be a JSON object.");
        }

        if (definition.Timeout is { } timeout && timeout <= TimeSpan.Zero)
        {
            return Error.Validation("task.timeout_invalid", $"Task {position}: the timeout must be greater than zero.");
        }

        var placeholderError = FindPlaceholderError(position, definition.Parameters);
        if (placeholderError is not null)
        {
            return placeholderError;
        }

        return new JobTask(position, taskType, definition.Parameters.Clone(), definition.Timeout);
    }

    /// <summary>Looks through every string value of the parameters for a placeholder that is wrong (ADR 0004).</summary>
    /// <param name="position">The 1-based position of the task the parameters belong to.</param>
    /// <param name="value">The JSON value to check, including everything nested in it.</param>
    /// <returns>The first error found, or <see langword="null"/> if all placeholders are valid.</returns>
    private static Error? FindPlaceholderError(int position, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return CheckPlaceholders(position, value.GetString() ?? string.Empty);
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    if (FindPlaceholderError(position, property.Value) is { } error)
                    {
                        return error;
                    }
                }

                return null;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (FindPlaceholderError(position, item) is { } error)
                    {
                        return error;
                    }
                }

                return null;
            default:
                return null;
        }
    }

    /// <summary>Checks the placeholders of one string: well formed, and only referring to earlier tasks.</summary>
    /// <param name="position">The 1-based position of the task the string belongs to.</param>
    /// <param name="text">The string value.</param>
    /// <returns>The error, or <see langword="null"/> if the string is valid.</returns>
    private static Error? CheckPlaceholders(int position, string text)
    {
        var parsed = PlaceholderParser.Parse(text);
        if (parsed.IsFailure)
        {
            return Error.Validation(parsed.Error.Code, $"Task {position}: {parsed.Error.Message}");
        }

        foreach (var part in parsed.Value)
        {
            if (part is ReferencePart reference && reference.TaskPosition >= position)
            {
                return Error.Validation(
                    PlaceholderReferenceInvalidCode,
                    $"Task {position}: a placeholder refers to task {reference.TaskPosition}, but a task can only use the output of an earlier task.");
            }
        }

        return null;
    }
}
