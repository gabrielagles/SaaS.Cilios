using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Controllers;

[Authorize]
public class AgendamentoController : Controller
{
    private readonly AppDbContext _context;

    public AgendamentoController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Agendamento
    public async Task<IActionResult> Index()
    {
        var agendamentos = await _context.Agendamentos
            .Include(a => a.Servico)
            .OrderBy(a => a.DataHora)
            .ToListAsync();
        return View(agendamentos);
    }

    // GET: /Agendamento/Create
    public async Task<IActionResult> Create()
    {
        await CarregarServicosAsync();
        return View(new Agendamento());
    }

    // POST: /Agendamento/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Agendamento agendamento)
    {
        ModelState.Remove(nameof(Agendamento.Servico));

        if (ModelState.IsValid)
        {
            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Agendamento criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        await CarregarServicosAsync();
        return View(agendamento);
    }

    // GET: /Agendamento/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var agendamento = await _context.Agendamentos.FindAsync(id);
        if (agendamento == null) return NotFound();

        await CarregarServicosAsync();
        return View(agendamento);
    }

    // POST: /Agendamento/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Agendamento agendamento)
    {
        if (id != agendamento.Id) return NotFound();

        ModelState.Remove(nameof(Agendamento.Servico));

        if (ModelState.IsValid)
        {
            _context.Update(agendamento);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Agendamento atualizado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        await CarregarServicosAsync();
        return View(agendamento);
    }

    // GET: /Agendamento/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var agendamento = await _context.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (agendamento == null) return NotFound();
        return View(agendamento);
    }

    // POST: /Agendamento/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var agendamento = await _context.Agendamentos.FindAsync(id);
        if (agendamento != null)
        {
            _context.Agendamentos.Remove(agendamento);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Agendamento excluído com sucesso!";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Agendamento/Relatorio?mes=7&ano=2026
    public async Task<IActionResult> Relatorio(int? mes, int? ano)
    {
        var hoje = DateTime.Now;
        int mesSelecionado = mes ?? hoje.Month;
        int anoSelecionado = ano ?? hoje.Year;

        var agendamentosDoMes = await _context.Agendamentos
            .Include(a => a.Servico)
            .Where(a => a.DataHora.Month == mesSelecionado && a.DataHora.Year == anoSelecionado)
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        ViewBag.Mes = mesSelecionado;
        ViewBag.Ano = anoSelecionado;
        ViewBag.TotalGeral = agendamentosDoMes.Sum(a => a.Valor);
        ViewBag.TotalConcluidos = agendamentosDoMes.Where(a => a.Concluido).Sum(a => a.Valor);
        ViewBag.TotalPendentes = agendamentosDoMes.Where(a => !a.Concluido).Sum(a => a.Valor);
        ViewBag.QuantidadeAtendimentos = agendamentosDoMes.Count;

        ViewBag.PorServico = agendamentosDoMes
            .GroupBy(a => a.Servico!.Nome)
            .Select(g => new { Servico = g.Key, Total = g.Sum(a => a.Valor), Quantidade = g.Count() })
            .OrderByDescending(g => g.Total)
            .ToList();

        return View(agendamentosDoMes);
    }

    // GET: /Agendamento/Calendario?mes=1&ano=2026
    public async Task<IActionResult> Calendario(int? mes, int? ano)
    {
        var hoje = DateTime.Now;
        int mesSelecionado = mes ?? hoje.Month;
        int anoSelecionado = ano ?? hoje.Year;

        var primeiroDiaMes = new DateTime(anoSelecionado, mesSelecionado, 1);
        var ultimoDiaMes = primeiroDiaMes.AddMonths(1).AddDays(-1);

        var inicioGrade = primeiroDiaMes.AddDays(-(int)primeiroDiaMes.DayOfWeek);
        var fimGrade = ultimoDiaMes.AddDays(6 - (int)ultimoDiaMes.DayOfWeek);

        var agendamentosDoPeriodo = await _context.Agendamentos
            .Include(a => a.Servico)
            .Where(a => a.DataHora.Date >= inicioGrade && a.DataHora.Date <= fimGrade)
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        var contagemPorDia = agendamentosDoPeriodo
            .GroupBy(a => a.DataHora.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var dias = new List<DiaCalendario>();
        for (var d = inicioGrade; d <= fimGrade; d = d.AddDays(1))
        {
            dias.Add(new DiaCalendario
            {
                Data = d,
                DoMesAtual = d.Month == mesSelecionado,
                QuantidadeAgendamentos = contagemPorDia.TryGetValue(d, out var qtd) ? qtd : 0,
                Hoje = d.Date == hoje.Date
            });
        }

        var agendaPorDia = agendamentosDoPeriodo
            .Where(a => a.DataHora.Month == mesSelecionado && a.DataHora.Year == anoSelecionado)
            .GroupBy(a => a.DataHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AgendaDoDia { Data = g.Key, Agendamentos = g.OrderBy(a => a.DataHora).ToList() })
            .ToList();

        var vm = new CalendarioViewModel
        {
            Mes = mesSelecionado,
            Ano = anoSelecionado,
            Dias = dias,
            AgendaPorDia = agendaPorDia
        };

        return View(vm);
    }

    // GET: /Agendamento/AgendaDia?data=2026-01-16
    public async Task<IActionResult> AgendaDia(DateTime? data)
    {
        var diaSelecionado = (data ?? DateTime.Now).Date;

        var agendamentosDoMes = await _context.Agendamentos
            .Include(a => a.Servico)
            .Where(a => a.DataHora.Month == diaSelecionado.Month && a.DataHora.Year == diaSelecionado.Year)
            .OrderBy(a => a.DataHora)
            .ToListAsync();

        var agendamentosDoDia = agendamentosDoMes
            .Where(a => a.DataHora.Date == diaSelecionado)
            .ToList();

        var outrosDias = agendamentosDoMes
            .Where(a => a.DataHora.Date != diaSelecionado)
            .GroupBy(a => a.DataHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AgendaDoDia { Data = g.Key, Agendamentos = g.OrderBy(a => a.DataHora).ToList() })
            .ToList();

        var vm = new AgendaDiaViewModel
        {
            DiaSelecionado = diaSelecionado,
            AgendamentosDoDia = agendamentosDoDia,
            OutrosDias = outrosDias
        };

        return View(vm);
    }

    private async Task CarregarServicosAsync()
    {
        var servicos = await _context.Servicos
            .Where(s => s.Ativo)
            .OrderBy(s => s.Nome)
            .ToListAsync();
        ViewBag.Servicos = servicos;
    }
}
