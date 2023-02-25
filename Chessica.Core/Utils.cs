using System.Security.Cryptography;
using System.Text;

namespace Chessica.Core;

public static class Utils
{
    public static string Sha1Sum(byte[] input)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(input);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("X2").ToLower());
        }
        return sb.ToString();
    }
}