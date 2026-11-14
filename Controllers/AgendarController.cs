using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;
using SaaS.Cilios.Services;
using System.Globalization;
using System.Text.Json;

namespace SaaS.Cilios.Controllers;

// Página pública de agendamento (sem login): /agendar
[AllowAnonymous]
[Route("agendar")]
public class AgendarController : Controller
{
    // Só um agendamento é gravado por vez, pra duas clientes não pegarem o mesmo horário
    private static readonly SemaphoreSlim Trava = new(1, 1);

    private const int MaxFuturosPorTelefone = 2;
    private const string FormatoInicio = "yyyy-MM-dd'T'HH:mm";

    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;

    public AgendarController(AppDbContext context, IConfiguration config, IHttpClientFactory http)
    {
        _context = context;
        _config = config;
        _http = http;
    }

    // 1) Escolher o procedimento
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var servicos = await _context.Servicos.Where(s => s.Ativo).OrderBy(s => s.Nome).ToListAsync();
        var temHorarios = await _context.HorariosFuncionamento.AnyAsync();
        ViewBag.Disponivel = temHorarios && servicos.Any();
        return View(servicos);
    }

    // 2) Escolher o dia (calendário só com dias que têm vaga)
    [HttpGet("data")]
    public async Task<IActionResult> Data(int servicoId, int? mes, int? ano)
    {
        var servico = await ObterServicoAtivoAsync(servicoId);
        if (servico == null) return RedirectToAction(nameof(Index));

        var agora = DateTime.Now;
        var hoje = agora.Date;
        var mesSel = mes ?? hoje.Month;
        var anoSel = ano ?? hoje.Year;
        if (mesSel < 1 || mesSel > 12 || anoSel < hoje.Year - 1 || anoSel > hoje.Year + 2)
        {
            mesSel = hoje.Month;
            anoSel = hoje.Year;
        }

        var primeiro = new DateTime(anoSel, mesSel, 1);
        var ultimo = primeiro.AddMonths(1).AddDays(-1);
        var inicioGrade = primeiro.AddDays(-(int)primeiro.DayOfWeek);
        var fimGrade = ultimo.AddDays(6 - (int)ultimo.DayOfWeek);

        var dados = await Disponibilidade.CarregarAsync(_context, inicioGrade, fimGrade);
        var comVaga = Disponibilidade.DiasComVaga(dados, inicioGrade, fimGrade, servico.DuracaoMinutos, agora);

        var mesAtual = new DateTime(hoje.Year, hoje.Month, 1);
        var limite = hoje.AddDays(dados.Config.DiasAFrente);
        var anterior = primeiro.AddMonths(-1);
        var seguinte = primeiro.AddMonths(1);

        var dias = new List<DiaAgendar>();
        for (var d = inicioGrade; d <= fimGrade; d = d.AddDays(1))
        {
            dias.Add(new DiaAgendar { Data = d, DoMes = d.Month == mesSel, Livre = comVaga.Contains(d) });
        }

        return View(new AgendarDataViewModel
        {
            Servico = servico,
            Mes = mesSel,
            Ano = anoSel,
            Dias = dias,
            TemAnterior = primeiro > mesAtual,
            TemProximo = seguinte <= limite,
            MesAnterior = anterior.Month,
            AnoAnterior = anterior.Year,
            MesSeguinte = seguinte.Month,
            AnoSeguinte = seguinte.Year
        });
    }

    // 3) Escolher o horário
    [HttpGet("horarios")]
    public async Task<IActionResult> Horarios(int servicoId, string? data)
    {
        var servico = await ObterServicoAtivoAsync(servicoId);
        if (servico == null) return RedirectToAction(nameof(Index));

        if (!DateTime.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
        {
            return RedirectToAction(nameof(Data), new { servicoId });
        }

        var livres = await HorariosDoDiaAsync(servico, dia);
        return View(new AgendarHorariosViewModel { Servico = servico, Dia = dia.Date, Horarios = livres });
    }

    // 4) Dados da cliente e confirmação
    [HttpGet("confirmar")]
    public async Task<IActionResult> Confirmar(int servicoId, string? inicio)
    {
        var servico = await ObterServicoAtivoAsync(servicoId);
        if (servico == null) return RedirectToAction(nameof(Index));

        if (!DateTime.TryParseExact(inicio, FormatoInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ini))
        {
            return RedirectToAction(nameof(Data), new { servicoId });
        }

        var livres = await HorariosDoDiaAsync(servico, ini);
        if (!livres.Contains(ini))
        {
            TempData["Erro"] = "Esse horário não está mais disponível. Escolha outro.";
            return RedirectToAction(nameof(Horarios), new { servicoId, data = ini.ToString("yyyy-MM-dd") });
        }

        return View(new AgendarConfirmarViewModel { Servico = servico, Inicio = ini });
    }

    [HttpPost("confirmar")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("agendar")]
    public async Task<IActionResult> ConfirmarPost(ConfirmarForm f)
    {
        // Campo-isca preenchido: é um robô. Não damos nenhuma pista.
        if (!string.IsNullOrEmpty(f.Website)) return RedirectToAction(nameof(Index));

        var servico = await ObterServicoAtivoAsync(f.ServicoId);
        if (servico == null) return RedirectToAction(nameof(Index));

        if (!DateTime.TryParseExact(f.Inicio, FormatoInicio, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio))
        {
            return RedirectToAction(nameof(Data), new { servicoId = servico.Id });
        }

        var nome = (f.Nome ?? "").Trim();
        var digitos = Digitos(f.Telefone);

        string? erro = null;
        if (nome.Length < 2) erro = "Informe o seu nome.";
        else if (nome.Length > 150) erro = "O nome ficou muito longo.";
        else if (digitos.Length < 10 || digitos.Length > 11) erro = "Informe o seu WhatsApp com DDD (ex.: 11 91234-5678).";
        else if (!f.Aceite) erro = "Para confirmar, é preciso concordar com o uso dos seus dados para o agendamento.";
        else if (!await TurnstileOkAsync()) erro = "Não conseguimos confirmar que você não é um robô. Tente de novo.";

        if (erro != null)
        {
            return View("Confirmar", new AgendarConfirmarViewModel
            {
                Servico = servico,
                Inicio = inicio,
                Nome = nome,
                Telefone = f.Telefone,
                Erro = erro
            });
        }

        await Trava.WaitAsync();
        try
        {
            // Confere de novo, já "dentro da trava": o horário pode ter sido tomado nesse meio tempo.
            var livres = await HorariosDoDiaAsync(servico, inicio);
            if (!livres.Contains(inicio))
            {
                TempData["Erro"] = "Esse horário acabou de ser ocupado. Escolha outro.";
                return RedirectToAction(nameof(Horarios), new { servicoId = servico.Id, data = inicio.ToString("yyyy-MM-dd") });
            }

            // Limite de horários futuros por telefone (evita uma pessoa lotar a agenda)
            var agora = DateTime.Now;
            var telefonesFuturos = await _context.Agendamentos
                .Where(a => a.Status != StatusAgendamento.Cancelado && a.DataHora >= agora)
                .Select(a => a.ClienteTelefone)
                .ToListAsync();
            var jaTem = telefonesFuturos.Count(t => t != null && Digitos(t) == digitos);
            if (jaTem >= MaxFuturosPorTelefone)
            {
                return View("Confirmar", new AgendarConfirmarViewModel
                {
                    Servico = servico,
                    Inicio = inicio,
                    Nome = nome,
                    Telefone = f.Telefone,
                    Erro = "Esse número já tem " + MaxFuturosPorTelefone + " horários marcados. Para marcar outro, cancele um deles ou fale com a gente."
                });
            }

            var agendamento = new Agendamento
            {
                ClienteNome = nome,
                ClienteTelefone = FormatarTelefone(digitos),
                ServicoId = servico.Id,
                DataHora = inicio,
                DuracaoMinutos = servico.DuracaoMinutos,
                Valor = servico.Preco,
                Concluido = false,
                Status = StatusAgendamento.Confirmado,
                Origem = OrigemAgendamento.Online,
                CodigoPublico = Guid.NewGuid().ToString("N"),
                ConsentimentoEm = agora
            };

            _context.Agendamentos.Add(agendamento);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Pronto), new { codigo = agendamento.CodigoPublico });
        }
        finally
        {
            Trava.Release();
        }
    }

    // 5) Tudo certo
    [HttpGet("pronto/{codigo}")]
    public async Task<IActionResult> Pronto(string codigo)
    {
        var agendamento = await _context.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.CodigoPublico == codigo);
        if (agendamento == null) return NotFound();

        return View(agendamento);
    }

    // Cancelamento pela própria cliente, pelo link secreto
    [HttpGet("cancelar/{codigo}")]
    public async Task<IActionResult> Cancelar(string codigo)
    {
        var agendamento = await _context.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.CodigoPublico == codigo);
        if (agendamento == null) return NotFound();

        return View(agendamento);
    }

    [HttpPost("cancelar/{codigo}")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("agendar")]
    public async Task<IActionResult> CancelarPost(string codigo)
    {
        var agendamento = await _context.Agendamentos.FirstOrDefaultAsync(a => a.CodigoPublico == codigo);
        if (agendamento == null) return NotFound();

        if (agendamento.Status == StatusAgendamento.Cancelado)
        {
            return RedirectToAction(nameof(Cancelar), new { codigo });
        }

        var config = await _context.ConfiguracoesAgenda.FirstOrDefaultAsync() ?? new ConfiguracaoAgenda();
        var faltam = agendamento.DataHora - DateTime.Now;
        if (faltam < TimeSpan.FromHours(config.CancelamentoHoras))
        {
            TempData["Erro"] = "Já não dá mais para cancelar por aqui. Fale com a gente pelo WhatsApp.";
            return RedirectToAction(nameof(Cancelar), new { codigo });
        }

        agendamento.Status = StatusAgendamento.Cancelado;
        agendamento.CanceladoEm = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["Sucesso"] = "Seu horário foi cancelado.";
        return RedirectToAction(nameof(Cancelar), new { codigo });
    }

    // ---------- apoio ----------

    private Task<Servico?> ObterServicoAtivoAsync(int id) =>
        _context.Servicos.FirstOrDefaultAsync(s => s.Id == id && s.Ativo);

    private async Task<List<DateTime>> HorariosDoDiaAsync(Servico servico, DateTime dia)
    {
        var dados = await Disponibilidade.CarregarAsync(_context, dia.Date, dia.Date.AddDays(1));
        return Disponibilidade.HorariosLivres(dados, dia.Date, servico.DuracaoMinutos, DateTime.Now);
    }

    private static string Digitos(string? texto) =>
        new string((texto ?? "").Where(char.IsDigit).ToArray());

    private static string FormatarTelefone(string d) =>
        d.Length == 11
            ? $"({d.Substring(0, 2)}) {d.Substring(2, 5)}-{d.Substring(7)}"
            : $"({d.Substring(0, 2)}) {d.Substring(2, 4)}-{d.Substring(6)}";

    // Cloudflare Turnstile (CAPTCHA gratuito). Só vale se as chaves estiverem configuradas.
    private async Task<bool> TurnstileOkAsync()
    {
        var segredo = _config["Turnstile:SecretKey"];
        if (string.IsNullOrWhiteSpace(segredo)) return true;

        var token = Request.Form["cf-turnstile-response"].ToString();
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            var cliente = _http.CreateClient();
            cliente.Timeout = TimeSpan.FromSeconds(6);

            var corpo = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = segredo,
                ["response"] = token,
                ["remoteip"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? ""
            });

            var resposta = await cliente.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", corpo);
            var json = await resposta.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var sucesso) && sucesso.GetBoolean();
        }
        catch
        {
            return false;
        }
    }
}
