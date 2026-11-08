using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;
using System.Globalization;

namespace SaaS.Cilios.Controllers;

[Authorize]
public class HorariosController : Controller
{
    private readonly AppDbContext _context;

    // Ordem exibida na tela: segunda a domingo (0 = domingo no DayOfWeek do .NET)
    private static readonly int[] OrdemDias = { 1, 2, 3, 4, 5, 6, 0 };
    private static readonly string[] NomesDias =
        { "Domingo", "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado" };

    public HorariosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Horarios
    public async Task<IActionResult> Index()
    {
        return View(await MontarAsync());
    }

    // POST: /Horarios/SalvarSemana
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarSemana(SemanaForm semana)
    {
        var novos = new List<HorarioFuncionamento>();
        string? erro = null;

        foreach (var d in semana.Dias)
        {
            if (!d.Ativo) continue;

            var nome = NomesDias[Math.Clamp(d.DiaSemana, 0, 6)];

            TimeSpan i1 = default, f1 = default;
            var ok1 = TentarHora(d.Inicio1, out i1) && TentarHora(d.Fim1, out f1) && i1 < f1;
            if (!ok1)
            {
                erro = nome + ": informe o início e o fim do atendimento (o início precisa ser antes do fim).";
                break;
            }
            novos.Add(new HorarioFuncionamento { DiaSemana = d.DiaSemana, Inicio = i1, Fim = f1 });

            var temSegundo = !string.IsNullOrWhiteSpace(d.Inicio2) || !string.IsNullOrWhiteSpace(d.Fim2);
            if (!temSegundo) continue;

            TimeSpan i2 = default, f2 = default;
            var ok2 = TentarHora(d.Inicio2, out i2) && TentarHora(d.Fim2, out f2) && i2 < f2;
            if (!ok2)
            {
                erro = nome + ": o segundo período está incompleto ou com o início depois do fim.";
                break;
            }
            if (i2 < f1)
            {
                erro = nome + ": o segundo período precisa começar depois do fim do primeiro.";
                break;
            }
            novos.Add(new HorarioFuncionamento { DiaSemana = d.DiaSemana, Inicio = i2, Fim = f2 });
        }

        if (erro != null)
        {
            ViewBag.ErroSemana = erro;
            return View("Index", await MontarAsync(diasPostados: semana.Dias));
        }

        _context.HorariosFuncionamento.RemoveRange(_context.HorariosFuncionamento);
        _context.HorariosFuncionamento.AddRange(novos);
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = novos.Any()
            ? "Horários de atendimento salvos."
            : "Salvo. Nenhum dia está marcado como dia de atendimento.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Horarios/SalvarRegras
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarRegras([Bind(Prefix = "Config")] ConfiguracaoAgenda config)
    {
        if (!string.IsNullOrWhiteSpace(config.WhatsApp) && config.WhatsApp.Count(char.IsDigit) < 10)
        {
            ModelState.AddModelError("Config.WhatsApp", "Informe o DDD e o número.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await MontarAsync(configPostada: config));
        }

        var existente = await _context.ConfiguracoesAgenda.FirstOrDefaultAsync();
        if (existente == null)
        {
            existente = new ConfiguracaoAgenda();
            _context.ConfiguracoesAgenda.Add(existente);
        }

        existente.PresetRapidoMin = config.PresetRapidoMin;
        existente.PresetMedioMin = config.PresetMedioMin;
        existente.PresetDemoradoMin = config.PresetDemoradoMin;
        existente.IntervaloMin = config.IntervaloMin;
        existente.AntecedenciaHoras = config.AntecedenciaHoras;
        existente.DiasAFrente = config.DiasAFrente;
        existente.PassoMin = config.PassoMin;
        existente.WhatsApp = string.IsNullOrWhiteSpace(config.WhatsApp) ? null : config.WhatsApp.Trim();

        await _context.SaveChangesAsync();
        TempData["Sucesso"] = "Regras da agenda salvas.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Horarios/AdicionarBloqueio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarBloqueio(
        string? dataInicio, string? dataFim, string? horaInicio, string? horaFim, string? motivo)
    {
        if (!DateTime.TryParseExact(dataInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var di))
        {
            TempData["Erro"] = "Informe a data inicial do bloqueio.";
            return RedirectToAction(nameof(Index));
        }

        DateTime inicio;
        DateTime fim;
        var temHora = !string.IsNullOrWhiteSpace(horaInicio) || !string.IsNullOrWhiteSpace(horaFim);

        if (temHora)
        {
            // Com horário: bloqueia só aquela faixa, no dia da data inicial
            TimeSpan hi = default, hf = default;
            var ok = TentarHora(horaInicio, out hi) && TentarHora(horaFim, out hf) && hi < hf;
            if (!ok)
            {
                TempData["Erro"] = "Informe o horário de início e de fim (o início precisa ser antes do fim).";
                return RedirectToAction(nameof(Index));
            }
            inicio = di.Date + hi;
            fim = di.Date + hf;
        }
        else
        {
            // Sem horário: bloqueia os dias inteiros
            var df = di;
            if (!string.IsNullOrWhiteSpace(dataFim) &&
                !DateTime.TryParseExact(dataFim, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out df))
            {
                TempData["Erro"] = "A data final é inválida.";
                return RedirectToAction(nameof(Index));
            }
            if (df.Date < di.Date)
            {
                TempData["Erro"] = "A data final não pode ser antes da data inicial.";
                return RedirectToAction(nameof(Index));
            }
            inicio = di.Date;
            fim = df.Date.AddDays(1);
        }

        var texto = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (texto != null && texto.Length > 120) texto = texto.Substring(0, 120);

        _context.Bloqueios.Add(new BloqueioAgenda { Inicio = inicio, Fim = fim, Motivo = texto });
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = "Bloqueio adicionado.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Horarios/RemoverBloqueio
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverBloqueio(int id)
    {
        var bloqueio = await _context.Bloqueios.FindAsync(id);
        if (bloqueio != null)
        {
            _context.Bloqueios.Remove(bloqueio);
            await _context.SaveChangesAsync();
            TempData["Sucesso"] = "Bloqueio removido.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- apoio ----------

    private async Task<HorariosViewModel> MontarAsync(
        List<DiaHorarioForm>? diasPostados = null, ConfiguracaoAgenda? configPostada = null)
    {
        var salvos = await _context.HorariosFuncionamento
            .OrderBy(h => h.DiaSemana).ThenBy(h => h.Inicio)
            .ToListAsync();

        var config = await _context.ConfiguracoesAgenda.FirstOrDefaultAsync();

        var agora = DateTime.Now;
        var bloqueios = await _context.Bloqueios
            .Where(b => b.Fim > agora)
            .OrderBy(b => b.Inicio)
            .ToListAsync();

        return new HorariosViewModel
        {
            Dias = diasPostados ?? DiasDoBanco(salvos),
            Config = configPostada ?? config ?? new ConfiguracaoAgenda(),
            Bloqueios = bloqueios,
            HorariosSalvos = salvos.Any()
        };
    }

    private static List<DiaHorarioForm> DiasDoBanco(List<HorarioFuncionamento> salvos)
    {
        var lista = new List<DiaHorarioForm>();

        foreach (var dia in OrdemDias)
        {
            var form = new DiaHorarioForm { DiaSemana = dia, Inicio1 = "09:00", Fim1 = "18:00" };

            if (!salvos.Any())
            {
                // Nada configurado ainda: sugere segunda a sábado, das 9h às 18h
                form.Ativo = dia != 0;
            }
            else
            {
                var periodos = salvos.Where(h => h.DiaSemana == dia).OrderBy(h => h.Inicio).ToList();
                form.Ativo = periodos.Any();
                if (periodos.Count > 0)
                {
                    form.Inicio1 = Hora(periodos[0].Inicio);
                    form.Fim1 = Hora(periodos[0].Fim);
                }
                if (periodos.Count > 1)
                {
                    form.Inicio2 = Hora(periodos[1].Inicio);
                    form.Fim2 = Hora(periodos[1].Fim);
                }
            }

            lista.Add(form);
        }

        return lista;
    }

    private static string Hora(TimeSpan t) => $"{(int)t.TotalHours:00}:{t.Minutes:00}";

    private static bool TentarHora(string? texto, out TimeSpan hora)
    {
        hora = default;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        return TimeSpan.TryParseExact(texto.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out hora);
    }
}
