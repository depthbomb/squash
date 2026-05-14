using System.Diagnostics.CodeAnalysis;

namespace Squash.Exceptions;

public class MissingSevenZipBinaryException : Exception
{
    public MissingSevenZipBinaryException() { }

    public MissingSevenZipBinaryException(string? message) : base(message) { }

    public MissingSevenZipBinaryException(string? message, Exception inner) : base(message, inner) { }

    public static void ThrowIf([DoesNotReturnIf(true)] bool predicate)
    {
        if (predicate)
        {
            Throw();
        }
    }

    public static void ThrowUnless([DoesNotReturnIf(false)] bool predicate)
    {
        if (!predicate)
        {
            Throw();
        }
    }

    [DoesNotReturn]
    private static void Throw() => throw new MissingSevenZipBinaryException();

    [DoesNotReturn]
    private static void Throw(string? message) => throw new MissingSevenZipBinaryException(message);

    [DoesNotReturn]
    private static void Throw(string? message, Exception inner) => throw new MissingSevenZipBinaryException(message, inner);
}


