using Microsoft.EntityFrameworkCore;
using SaaS.Cilios.Data;
using SaaS.Cilios.Models;

namespace SaaS.Cilios.Services;

public static class AgendaRegras
{
    // Devolve os agendamentos que se sobrepõem ao intervalo [inicio, inicio + duracaoMin).
    // Dois horários se sobrepõem quando um começa antes de o outro terminar E termina depois
    // de o outro começar. Um terminar às 10h00 e outro começar às 10h00 NÃO é conflito.
    public static async Task<List<Agendamento>> ConflitosAsync(
        AppDbContext context, DateTime inicio, int duracaoMin, int? ignorarId = null)
    {
        var fim = inicio.AddMinutes(duracaoMin);

        // Busca uma janela de 1 dia pra trás e pra frente e faz o filtro fino em memória,
        // assim não dependemos de o banco traduzir "DataHora + duração" em SQL.
        var janelaInicio = inicio.Date.AddDays(-1);
        var janelaFim = inicio.Date.AddDays(2);

        var candidatos = await context.Agendamentos
            .Include(a => a.Servico)
            .Where(a => a.DataHora >= janelaInicio && a.DataHora < janelaFim)
            .ToListAsync();

        return candidatos
            .Where(a => a.Id != ignorarId)
            .Where(a => a.DataHora < fim && a.DataHora.AddMinutes(a.DuracaoMinutos) > inicio)
            .OrderBy(a => a.DataHora)
            .ToList();
    }
}
