using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Controllers;

[Authorize]
public class ContaController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    // Extensões de imagem aceitas no upload da foto de perfil
    private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long TamanhoMaximoBytes = 3 * 1024 * 1024; // 3 MB

    public ContaController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /Conta
    public async Task<IActionResult> Index()
    {
        var config = await _context.ConfiguracoesConta.FirstOrDefaultAsync();
        return View(config ?? new ConfiguracaoConta());
    }

    // POST: /Conta/Editar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(ConfiguracaoConta model, IFormFile? foto)
    {
        // Remove a validação do campo de foto (é opcional e não faz parte do model postado como arquivo)
        ModelState.Remove(nameof(ConfiguracaoConta.FotoCaminho));
        ModelState.Remove(nameof(ConfiguracaoConta.FotoVersao));

        if (foto != null && foto.Length > 0)
        {
            var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(extensao))
            {
                ModelState.AddModelError(string.Empty, "Formato de imagem não suportado. Use JPG, PNG ou WEBP.");
            }
            else if (foto.Length > TamanhoMaximoBytes)
            {
                ModelState.AddModelError(string.Empty, "A imagem precisa ter no máximo 3 MB.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var config = await _context.ConfiguracoesConta.FirstOrDefaultAsync();
        var novaConfig = config == null;
        if (config == null) config = new ConfiguracaoConta();

        config.NomeExibicao = model.NomeExibicao;
        config.NomePainel = model.NomePainel;

        if (foto != null && foto.Length > 0)
        {
            var extensao = Path.GetExtension(foto.FileName).ToLowerInvariant();
            var pastaUploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(pastaUploads);

            var nomeArquivo = "perfil" + extensao;
            var caminhoFisico = Path.Combine(pastaUploads, nomeArquivo);

            using (var stream = new FileStream(caminhoFisico, FileMode.Create))
            {
                await foto.CopyToAsync(stream);
            }

            config.FotoCaminho = "/uploads/" + nomeArquivo;
            config.FotoVersao += 1;
        }

        if (novaConfig)
        {
            _context.ConfiguracoesConta.Add(config);
        }
        else
        {
            _context.Update(config);
        }

        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Configurações atualizadas com sucesso!";
        return RedirectToAction(nameof(Index));
    }
}
