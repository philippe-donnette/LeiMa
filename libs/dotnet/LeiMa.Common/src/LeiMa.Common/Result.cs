namespace LeiMa.Common;

/// <summary>Outcome of an operation without a value: success, or failure with an <see cref="Common.Error"/>.</summary>
public readonly record struct Result
{
    private readonly Error? _error;

    private Result(Error? error) => _error = error;

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>The failure. Throws when the result is a success.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>A successful result.</summary>
    public static Result Success() => new(null);

    /// <summary>A failed result.</summary>
    public static Result Failure(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>A failed result of type <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    /// <summary>Implicitly converts an error into a failed result.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation with a value: success with a <typeparamref name="T"/>, or failure with an <see cref="Common.Error"/>.</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly record struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T? value, Error? error)
    {
        _value = value;
        _error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess => _error is null;

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => _error is not null;

    /// <summary>The value. Throws when the result is a failure.</summary>
    /// <exception cref="InvalidOperationException">The result is a failure.</exception>
    public T Value => _error is null
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    /// <summary>The failure. Throws when the result is a success.</summary>
    /// <exception cref="InvalidOperationException">The result is a success.</exception>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value, null);

    /// <summary>A failed result.</summary>
    public static Result<T> Failure(Error error) => new(default, error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>Implicitly converts a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>Implicitly converts an error into a failed result.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>Returns <paramref name="onSuccess"/> applied to the value, or <paramref name="onFailure"/> applied to the error.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        _error is null ? onSuccess(_value!) : onFailure(_error);

    /// <summary>Transforms the value of a successful result; a failure is passed through.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        _error is null ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(_error);
}
