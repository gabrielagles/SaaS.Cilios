using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;
using SaaS.Cilios.Services;

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
    public async Task<IActionResult> Index(bool cancelados = false)
    {
        var consulta = _context.Agendamentos.Include(a => a.Servico).AsQueryable();
        if (!cancelados)
        {
            consulta = consulta.Where(a => a.Status != StatusAgendamento.Cancelado);
        }

        var agendamentos = await consulta.OrderBy(a => a.DataHora).ToListAsync();
        ViewBag.MostrarCancelados = cancelados;
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
    public async Task<IActionResult> Create(Agendamento agendamento, bool encaixar = false)
    {
        ModelState.Remove(nameof(Agendamento.Servico));

        agendamento.Status = StatusAgendamento.Confirmado;
        agendamento.Origem = OrigemAgendamento.Admin;
        agendamento.CanceladoEm = null;

        // Antes de gravar, confere se o horário já está ocupado. A Rafaela pode "encaixar"
        // mesmo assim marcando a opção que aparece junto do aviso.
        if (ModelState.IsValid && !encaixar)
        {
            var conflitos = await AgendaRegras.ConflitosAsync(_context, agendamento.DataHora, agendamento.DuracaoMinutos);
            if (conflitos.Any())
            {
                ViewBag.Conflitos = conflitos;
                await CarregarServicosAsync();
                return View(agendamento);
            }
        }

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
    public async Task<IActionResult> Edit(int id, Agendamento agendamento, bool encaixar = false)
    {
        if (id != agendamento.Id) return NotFound();

        ModelState.Remove(nameof(Agendamento.Servico));

        if (ModelState.IsValid && !encaixar)
        {
            var conflitos = await AgendaRegras.ConflitosAsync(_context, agendamento.DataHora, agendamento.DuracaoMinutos, agendamento.Id);
            if (conflitos.Any())
            {
                ViewBag.Conflitos = conflitos;
                await CarregarServicosAsync();
                return View(agendamento);
            }
        }

        if (ModelState.IsValid)
        {
            var existente = await _context.Agendamentos.FindAsync(id);
            if (existente == null) return NotFound();

            // Só copia o que o formulário edita. Status, origem e "criado offline" ficam como estavam.
            existente.ClienteNome = agendamento.ClienteNome;
            existente.ClienteTelefone = agendamento.ClienteTelefone;
            existente.ServicoId = agendamento.ServicoId;
            existente.DataHora = agendamento.DataHora;
            existente.DuracaoMinutos = agendamento.DuracaoMinutos;
            existente.Valor = agendamento.Valor;
            existente.Concluido = agendamento.Concluido;

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
            .Where(a => a.Status != StatusAgendamento.Cancelado
                        && a.DataHora.Month == mesSelecionado && a.DataHora.Year == anoSelecionado)
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
            .Where(a => a.Status != StatusAgendamento.Cancelado
                        && a.DataHora.Date >= inicioGrade && a.DataHora.Date <= fimGrade)
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
            .Where(a => a.Status != StatusAgendamento.Cancelado && a.DataHora.Date == diaSelecionado)
            .ToList();

        var canceladosDoDia = agendamentosDoMes
            .Where(a => a.Status == StatusAgendamento.Cancelado && a.DataHora.Date == diaSelecionado)
            .ToList();

        var outrosDias = agendamentosDoMes
            .Where(a => a.Status != StatusAgendamento.Cancelado && a.DataHora.Date != diaSelecionado)
            .GroupBy(a => a.DataHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new AgendaDoDia { Data = g.Key, Agendamentos = g.OrderBy(a => a.DataHora).ToList() })
            .ToList();

        var vm = new AgendaDiaViewModel
        {
            DiaSelecionado = diaSelecionado,
            AgendamentosDoDia = agendamentosDoDia,
            CanceladosDoDia = canceladosDoDia,
            OutrosDias = outrosDias
        };

        return View(vm);
    }

    // POST: /Agendamento/Cancelar/5  (o horário fica livre, mas o registro continua no histórico)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id, string? retorno)
    {
        var agendamento = await _context.Agendamentos.FindAsync(id);
        if (agendamento == null) return NotFound();

        if (agendamento.Status != StatusAgendamento.Cancelado)
        {
            agendamento.Status = StatusAgendamento.Cancelado;
            agendamento.CanceladoEm = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Agendamento cancelado. O horário ficou livre.";
        }

        return VoltarPara(retorno, agendamento.DataHora);
    }

    // POST: /Agendamento/Reativar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reativar(int id, string? retorno)
    {
        var agendamento = await _context.Agendamentos.FindAsync(id);
        if (agendamento == null) return NotFound();

        if (agendamento.Status == StatusAgendamento.Cancelado)
        {
            var conflitos = await AgendaRegras.ConflitosAsync(
                _context, agendamento.DataHora, agendamento.DuracaoMinutos, agendamento.Id);

            if (conflitos.Any())
            {
                TempData["Erro"] = "Não deu pra reativar: esse horário já foi ocupado por outro agendamento.";
            }
            else
            {
                agendamento.Status = StatusAgendamento.Confirmado;
                agendamento.CanceladoEm = null;
                await _context.SaveChangesAsync();
                TempData["Sucesso"] = "Agendamento reativado.";
            }
        }

        return VoltarPara(retorno, agendamento.DataHora);
    }

    private IActionResult VoltarPara(string? retorno, DateTime data)
    {
        if (retorno == "dia")
        {
            return RedirectToAction(nameof(AgendaDia), new { data = data.ToString("yyyy-MM-dd") });
        }
        return RedirectToAction(nameof(Index));
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
