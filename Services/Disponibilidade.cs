using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Services;

// Tudo que o cálculo de horários livres precisa, carregado de uma vez só.
public class DadosAgenda
{
    public ConfiguracaoAgenda Config { get; set; } = new();
    public List<HorarioFuncionamento> Horarios { get; set; } = new();
    public List<BloqueioAgenda> Bloqueios { get; set; } = new();
    public List<Agendamento> Agendamentos { get; set; } = new();   // só os não cancelados
}

public static class Disponibilidade
{
    // Carrega os dados necessários para calcular horários entre "de" e "ate".
    public static async Task<DadosAgenda> CarregarAsync(AppDbContext context, DateTime de, DateTime ate)
    {
        var janelaInicio = de.Date.AddDays(-1);
        var janelaFim = ate.Date.AddDays(2);

        var config = await context.ConfiguracoesAgenda.FirstOrDefaultAsync() ?? new ConfiguracaoAgenda();
        var horarios = await context.HorariosFuncionamento.ToListAsync();
        var bloqueios = await context.Bloqueios
            .Where(b => b.Fim > janelaInicio && b.Inicio < janelaFim)
            .ToListAsync();
        var agendamentos = await context.Agendamentos
            .Where(a => a.Status != StatusAgendamento.Cancelado
                        && a.DataHora >= janelaInicio && a.DataHora < janelaFim)
            .ToListAsync();

        return new DadosAgenda
        {
            Config = config,
            Horarios = horarios,
            Bloqueios = bloqueios,
            Agendamentos = agendamentos
        };
    }

    // Horários de início livres num dia, para um serviço com a duração informada.
    //  - só dentro dos períodos de atendimento do dia da semana (o serviço precisa caber inteiro);
    //  - respeita a antecedência mínima e o limite de dias à frente;
    //  - não pode bater com bloqueios;
    //  - mantém o intervalo (folga) entre atendimentos, antes e depois de cada um.
    public static List<DateTime> HorariosLivres(DadosAgenda dados, DateTime dia, int duracaoMin, DateTime agora)
    {
        var resultado = new List<DateTime>();
        var cfg = dados.Config;
        dia = dia.Date;

        var hoje = agora.Date;
        if (dia < hoje || dia > hoje.AddDays(cfg.DiasAFrente)) return resultado;

        var menorInicio = agora.AddHours(cfg.AntecedenciaHoras);
        var passo = Math.Max(5, cfg.PassoMin);
        var folga = Math.Max(0, cfg.IntervaloMin);

        var periodos = dados.Horarios
            .Where(h => h.DiaSemana == (int)dia.DayOfWeek)
            .OrderBy(h => h.Inicio)
            .ToList();

        foreach (var periodo in periodos)
        {
            var fimPeriodo = dia + periodo.Fim;

            for (var inicio = dia + periodo.Inicio;
                 inicio.AddMinutes(duracaoMin) <= fimPeriodo;
                 inicio = inicio.AddMinutes(passo))
            {
                if (inicio < menorInicio) continue;

                var fim = inicio.AddMinutes(duracaoMin);

                var bloqueado = dados.Bloqueios.Any(b => inicio < b.Fim && b.Inicio < fim);
                if (bloqueado) continue;

                var ocupado = dados.Agendamentos.Any(a =>
                    inicio < a.DataHora.AddMinutes(a.DuracaoMinutos + folga) &&
                    a.DataHora < fim.AddMinutes(folga));
                if (ocupado) continue;

                resultado.Add(inicio);
            }
        }

        return resultado.Distinct().OrderBy(x => x).ToList();
    }

    // Dias (sem hora) que têm pelo menos um horário livre.
    public static HashSet<DateTime> DiasComVaga(
        DadosAgenda dados, DateTime de, DateTime ate, int duracaoMin, DateTime agora)
    {
        var dias = new HashSet<DateTime>();
        for (var d = de.Date; d <= ate.Date; d = d.AddDays(1))
        {
            if (HorariosLivres(dados, d, duracaoMin, agora).Any())
            {
                dias.Add(d);
            }
        }
        return dias;
    }
}
