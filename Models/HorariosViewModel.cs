namespace SaaS.Cilios.Models;

public class DiaHorarioForm
{
    public int DiaSemana { get; set; }
    public bool Ativo { get; set; }
    public string? Inicio1 { get; set; }
    public string? Fim1 { get; set; }
    public string? Inicio2 { get; set; }
    public string? Fim2 { get; set; }
}

public class SemanaForm
{
    public List<DiaHorarioForm> Dias { get; set; } = new();
}

public class HorariosViewModel
{
    public List<DiaHorarioForm> Dias { get; set; } = new();
    public ConfiguracaoAgenda Config { get; set; } = new();
    public List<BloqueioAgenda> Bloqueios { get; set; } = new();
    public bool HorariosSalvos { get; set; }
}
