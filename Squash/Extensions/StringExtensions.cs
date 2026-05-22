using System.Security.Cryptography;

namespace Squash.Extensions;

public static class StringExtensions
{
    extension(string value)
    {
        public string CreateGuidFrom([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            
            Span<byte> guidBytes = stackalloc byte[16];
            
            bytes[..16].CopyTo(guidBytes);

            return new Guid(guidBytes).ToString(format);
        }
    }

    extension([NotNullWhen(false)] string? value)
    {
        public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(value);

        public bool IsNullOrEmpty() => string.IsNullOrEmpty(value);
    }
}
