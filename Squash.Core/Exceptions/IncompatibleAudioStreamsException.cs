namespace Squash.Core.Exceptions;

[Serializable]
public sealed class IncompatibleAudioStreamsException : Exception
{
    public IncompatibleAudioStreamsException() { }

    public IncompatibleAudioStreamsException(string? message) : base(message) { }

    public IncompatibleAudioStreamsException(string? message, Exception inner) : base(message, inner) { }
}
