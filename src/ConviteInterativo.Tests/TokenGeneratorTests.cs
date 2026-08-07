using System.Text.RegularExpressions;
using ConviteInterativo.Web.Services;

namespace ConviteInterativo.Tests;

public class TokenGeneratorTests
{
    private static readonly Regex Base64UrlPattern = new("^[A-Za-z0-9_-]+$");

    [Fact]
    public void GerarToken_RetornaStringBase64UrlValida()
    {
        var token = new TokenGenerator().GerarToken();

        Assert.Matches(Base64UrlPattern, token);
    }

    [Fact]
    public void GerarToken_12Bytes_Retorna16Caracteres()
    {
        var token = new TokenGenerator().GerarToken();

        Assert.Equal(16, token.Length);
    }

    [Fact]
    public void GerarToken_ChamadasEmSequencia_GeramTokensDiferentes()
    {
        var gerador = new TokenGenerator();

        var token1 = gerador.GerarToken();
        var token2 = gerador.GerarToken();

        Assert.NotEqual(token1, token2);
    }
}
