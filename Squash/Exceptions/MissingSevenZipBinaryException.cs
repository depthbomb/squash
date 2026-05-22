namespace Squash.Exceptions;

[Serializable]
public sealed class MissingSevenZipBinaryException : Exception
{
    public MissingSevenZipBinaryException() { }

    public MissingSevenZipBinaryException(string? message) : base(message) { }

    public MissingSevenZipBinaryException(string? message, Exception inner) : base(message, inner) { }

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
    public static void Throw() => throw new MissingSevenZipBinaryException();

    [DoesNotReturn]
    public static void Throw(string? message) => throw new MissingSevenZipBinaryException(message);

    [DoesNotReturn]
    public static void Throw(string? message, Exception inner) => throw new MissingSevenZipBinaryException(message, inner);
}


