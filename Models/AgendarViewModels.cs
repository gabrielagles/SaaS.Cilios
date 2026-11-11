namespace SaaS.Cilios.Models;

public class DiaAgendar
{
    public DateTime Data { get; set; }
    public bool DoMes { get; set; }
    public bool Livre { get; set; }
}

public class AgendarDataViewModel
{
    public Servico Servico { get; set; } = null!;
    public int Mes { get; set; }
    public int Ano { get; set; }
    public List<DiaAgendar> Dias { get; set; } = new();
    public bool TemAnterior { get; set; }
    public bool TemProximo { get; set; }
    public int MesAnterior { get; set; }
    public int AnoAnterior { get; set; }
    public int MesSeguinte { get; set; }
    public int AnoSeguinte { get; set; }
}

public class AgendarHorariosViewModel
{
    public Servico Servico { get; set; } = null!;
    public DateTime Dia { get; set; }
    public List<DateTime> Horarios { get; set; } = new();
}

public class AgendarConfirmarViewModel
{
    public Servico Servico { get; set; } = null!;
    public DateTime Inicio { get; set; }
    public string? Nome { get; set; }
    public string? Telefone { get; set; }
    public string? Erro { get; set; }
}

// Campos enviados pelo formulário da página pública
public class ConfirmarForm
{
    public int ServicoId { get; set; }
    public string? Inicio { get; set; }
    public string? Nome { get; set; }
    public string? Telefone { get; set; }
    public bool Aceite { get; set; }
    public string? Website { get; set; }   // campo-isca: pessoas não veem, robôs preenchem
}
