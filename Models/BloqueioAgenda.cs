using System.ComponentModel.DataAnnotations;

namespace SaaS.Cilios.Models;

// Período em que a agenda fica fechada (folga, feriado, férias, compromisso).
public class BloqueioAgenda
{
    public int Id { get; set; }

    public DateTime Inicio { get; set; }

    // Exclusivo: um dia inteiro vai de 00:00 até 00:00 do dia seguinte.
    public DateTime Fim { get; set; }

    [StringLength(120)]
    public string? Motivo { get; set; }
}
