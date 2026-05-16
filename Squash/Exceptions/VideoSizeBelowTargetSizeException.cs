using System.Runtime.CompilerServices;

namespace Squash.Exceptions;

[Serializable]
public sealed class VideoSizeBelowTargetSizeException : Exception
{
    public VideoSizeBelowTargetSizeException() { }

    public VideoSizeBelowTargetSizeException(string? message) : base(message) { }

    public VideoSizeBelowTargetSizeException(string? message, Exception inner) : base(message, inner) { }

    public static void ThrowIf(
        [DoesNotReturnIf(true)] bool predicate,
        string?                      message = null,
        [CallerArgumentExpression(nameof(predicate))]
        string? expression = null)
    {
        if (predicate)
        {
            Throw(message ?? $"Condition failed: {expression}");
        }
    }

    public static void ThrowUnless(
        [DoesNotReturnIf(false)] bool predicate,
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
    public static void Throw() => throw new VideoSizeBelowTargetSizeException();

    [DoesNotReturn]
    public static void Throw(string? message) => throw new VideoSizeBelowTargetSizeException(message);

    [DoesNotReturn]
    public static void Throw(string? message, Exception inner) => throw new VideoSizeBelowTargetSizeException(message, inner);
}


