namespace LeiMa.Common.Tests;

public class ResultTests
{
    private static readonly Error NotFound = Error.NotFound("provider.not_found", "Provider not found.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void Failure_ExposesError()
    {
        var result = Result.Failure(NotFound);

        Assert.True(result.IsFailure);
        Assert.Equal(NotFound, result.Error);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void ValueResult_Success_ExposesValue()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Throws<InvalidOperationException>(() => result.Error);
    }

    [Fact]
    public void ValueResult_Failure_ThrowsOnValue()
    {
        Result<int> result = NotFound;

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_TransformsSuccess_AndPassesFailureThrough()
    {
        Result<int> ok = 2;
        Result<int> bad = NotFound;

        Assert.Equal("2!", ok.Map(v => $"{v}!").Value);
        Assert.Equal(NotFound, bad.Map(v => $"{v}!").Error);
    }

    [Fact]
    public void Match_PicksTheMatchingBranch()
    {
        Result<int> ok = 2;
        Result<int> bad = NotFound;

        Assert.Equal("ok 2", ok.Match(v => $"ok {v}", e => e.Code));
        Assert.Equal("provider.not_found", bad.Match(v => $"ok {v}", e => e.Code));
    }

    [Theory]
    [InlineData(ErrorType.Failure)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    public void ErrorFactories_SetTheType(ErrorType type)
    {
        var error = type switch
        {
            ErrorType.Failure => Error.Failure("c", "m"),
            ErrorType.Validation => Error.Validation("c", "m"),
            ErrorType.NotFound => Error.NotFound("c", "m"),
            ErrorType.Conflict => Error.Conflict("c", "m"),
            ErrorType.Unauthorized => Error.Unauthorized("c", "m"),
            _ => Error.Forbidden("c", "m"),
        };

        Assert.Equal(type, error.Type);
    }
}
