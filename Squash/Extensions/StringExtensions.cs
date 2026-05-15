using System.Text;
using System.Security.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace Squash.Extensions;

public static class StringExtensions
{
    extension(string str)
    {
        public string CreateGuidFrom([StringSyntax(StringSyntaxAttribute.GuidFormat)] string? format)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(str));
            
            Span<byte> guidBytes = stackalloc byte[16];
            
            bytes[..16].CopyTo(guidBytes);

            return new Guid(guidBytes).ToString(format);
        }
    }
}
