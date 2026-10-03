using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Controllers;

[Authorize]
public class ServicoController : Controller
{
    private readonly AppDbContext _context;

    public ServicoController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var servicos = await _context.Servicos.OrderBy(s => s.Nome).ToListAsync();
        return View(servicos);
    }

    public IActionResult Create()
    {
        return View(new Servico());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Servico servico)
    {
        if (ModelState.IsValid)
        {
            _context.Servicos.Add(servico);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Serviço cadastrado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        return View(servico);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var servico = await _context.Servicos.FindAsync(id);
        if (servico == null) return NotFound();
        return View(servico);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Servico servico)
    {
        if (id != servico.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(servico);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Serviço atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }
        return View(servico);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var servico = await _context.Servicos.FirstOrDefaultAsync(s => s.Id == id);
        if (servico == null) return NotFound();
        return View(servico);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var servico = await _context.Servicos.FindAsync(id);
        if (servico != null)
        {
            var temAgendamentos = await _context.Agendamentos.AnyAsync(a => a.ServicoId == id);
            if (temAgendamentos)
            {
                servico.Ativo = false;
                _context.Update(servico);
                TempData["Sucesso"] = "Este serviço possui agendamentos vinculados, então foi apenas desativado.";
            }
            else
            {
                _context.Servicos.Remove(servico);
                TempData["Sucesso"] = "Serviço excluído com sucesso!";
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
