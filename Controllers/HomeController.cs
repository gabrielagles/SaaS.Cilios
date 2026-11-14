using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _context;

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hoje = DateTime.Now.Date;

        var limiteOnline = DateTime.Now.AddDays(-2);
        ViewBag.OnlineRecentes = await _context.Agendamentos.CountAsync(a =>
            a.Origem == OrigemAgendamento.Online
            && a.Status != StatusAgendamento.Cancelado
            && a.ConsentimentoEm != null && a.ConsentimentoEm >= limiteOnline);
        ViewBag.AgendamentosHoje = await _context.Agendamentos
            .Include(a => a.Servico)
            .Where(a => a.Status != StatusAgendamento.Cancelado && a.DataHora.Date == hoje)
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
