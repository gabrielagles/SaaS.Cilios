namespace SaaS.Cilios.Models;

// Um período de atendimento num dia da semana. Um dia com pausa de almoço
// tem duas linhas (ex.: 09:00–12:00 e 14:00–18:00).
public class HorarioFuncionamento
{
    public int Id { get; set; }

    // 0 = domingo ... 6 = sábado (mesmo valor do DayOfWeek do .NET)
    public int DiaSemana { get; set; }

    public TimeSpan Inicio { get; set; }
    public TimeSpan Fim { get; set; }
}
