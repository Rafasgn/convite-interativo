using System.Security.Claims;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace ConviteInterativo.Web.Pages.Admin;

public class LoginModel(IConfiguration configuration, IMemoryCache cache) : PageModel
{
    private const int MaxTentativas = 5;
    private static readonly TimeSpan JanelaTentativas = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(10);

    [BindProperty]
    public string Senha { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Erro { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        var chaveBloqueio = $"login-bloqueio:{ip}";
        var chaveFalhas = $"login-falhas:{ip}";

        if (cache.TryGetValue(chaveBloqueio, out _))
        {
            Erro = "Muitas tentativas. Tente novamente em alguns minutos.";
            return Page();
        }

        var hash = configuration["Admin:PasswordHash"];
        if (hash is not null && PasswordHasher.Verify(Senha, hash))
        {
            cache.Remove(chaveFalhas);

            var claims = new List<Claim> { new(ClaimTypes.Name, "admin") };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/Admin/Eventos/Index");
        }

        var falhas = (cache.TryGetValue<int>(chaveFalhas, out var atual) ? atual : 0) + 1;
        cache.Set(chaveFalhas, falhas, JanelaTentativas);

        if (falhas >= MaxTentativas)
        {
            cache.Set(chaveBloqueio, true, DuracaoBloqueio);
            cache.Remove(chaveFalhas);
        }

        Erro = "Senha incorreta.";
        return Page();
    }
}
