namespace SaaS.Cilios.Models;

public class CalendarioViewModel
{
    public int Mes { get; set; }
    public int Ano { get; set; }
    public List<DiaCalendario> Dias { get; set; } = new();
    public List<AgendaDoDia> AgendaPorDia { get; set; } = new();
}

public class DiaCalendario
{
    public DateTime Data { get; set; }
    public bool DoMesAtual { get; set; }
    public int QuantidadeAgendamentos { get; set; }
    public bool Hoje { get; set; }
}

public class AgendaDoDia
{
    public DateTime Data { get; set; }
    public List<Agendamento> Agendamentos { get; set; } = new();
}

public class AgendaDiaViewModel
{
    public DateTime DiaSelecionado { get; set; }
    public List<Agendamento> AgendamentosDoDia { get; set; } = new();
    public List<AgendaDoDia> OutrosDias { get; set; } = new();
}
