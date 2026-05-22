namespace Squash.Exceptions;

[Serializable]
public sealed class UnableToReachTargetSizeException : Exception
{
    public UnableToReachTargetSizeException() { }

    public UnableToReachTargetSizeException(string? message) : base(message) { }

    public UnableToReachTargetSizeException(string? message, Exception inner) : base(message, inner) { }

    public static void ThrowIf([DoesNotReturnIf(true)] bool predicate,
                               string?                      message = null,
                               [CallerArgumentExpression(nameof(predicate))]
                               string? expression = null)
    {
        if (predicate)
        {
            Throw(message ?? $"Condition failed: {expression}");
        }
    }

    public static void ThrowUnless([DoesNotReturnIf(false)] bool predicate,
                                   string?                       message = null,
                                   [CallerArgumentExpression(nameof(predicate))]
                                   string? expression = null)
    {
        if (!predicate)
        {
            Throw(message ?? $"Condition failed: {expression}");
        }
    }

    [DoesNotReturn]
    public static void Throw() => throw new UnableToReachTargetSizeException();

    [DoesNotReturn]
    public static void Throw(string? message) => throw new UnableToReachTargetSizeException(message);

    [DoesNotReturn]
    public static void Throw(string? message, Exception inner) => throw new UnableToReachTargetSizeException(message, inner);
}


