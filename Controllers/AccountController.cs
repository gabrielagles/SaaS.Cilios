using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SaaS.Cilios.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _config;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IConfiguration config, ILogger<AccountController> logger)
    {
        _config = config;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string? usuario, string? senha)
    {
        var usuarioValido = _config["AppLogin:Usuario"] ?? "";
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";

        var usuarioOk = IgualSeguro(usuario ?? "", usuarioValido);
        var senhaOk = SenhaConfere(senha ?? "");

        if (usuarioOk && senhaOk)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, usuarioValido) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            _logger.LogInformation("Login realizado, vindo de {Ip}", ip);
            return RedirectToAction("Index", "Home");
        }

        _logger.LogWarning("Falha de login vinda de {Ip}. Usuário informado: {Usuario}", ip, Limpar(usuario));

        await Task.Delay(800); 
        ViewBag.Erro = "Login ou senha incorretos.";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private bool SenhaConfere(string senha)
    {
        var hash = _config["AppLogin:SenhaHash"];
        if (!string.IsNullOrWhiteSpace(hash))
        {
            return VerificarHash(senha, hash);
        }

        return IgualSeguro(senha, _config["AppLogin:Senha"] ?? "");
    }

    private static bool VerificarHash(string senha, string armazenado)
    {
        var partes = armazenado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256") return false;
        if (!int.TryParse(partes[1], out var iteracoes) || iteracoes < 10000) return false;

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IgualSeguro(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }

    private static string Limpar(string? texto)
    {
        return new string((texto ?? "").Where(c => !char.IsControl(c)).Take(40).ToArray());
    }
}
