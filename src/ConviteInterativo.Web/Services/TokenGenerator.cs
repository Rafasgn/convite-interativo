using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace ConviteInterativo.Web.Services;

public class TokenGenerator
{
    private const int TamanhoEmBytes = 12;

    public string GerarToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TamanhoEmBytes);
        return Base64UrlTextEncoder.Encode(bytes);
    }
}
