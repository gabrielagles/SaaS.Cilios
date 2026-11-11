using System.ComponentModel.DataAnnotations;

namespace SaaS.Cilios.Models;

// Regras da agenda. Só existe 1 linha nessa tabela (o sistema é de um único estúdio).
public class ConfiguracaoAgenda
{
    public int Id { get; set; }

    [Range(5, 600, ErrorMessage = "Entre 5 e 600 minutos")]
    [Display(Name = "Rápido (min)")]
    public int PresetRapidoMin { get; set; } = 30;

    [Range(5, 600, ErrorMessage = "Entre 5 e 600 minutos")]
    [Display(Name = "Médio (min)")]
    public int PresetMedioMin { get; set; } = 60;

    [Range(5, 600, ErrorMessage = "Entre 5 e 600 minutos")]
    [Display(Name = "Demorado (min)")]
    public int PresetDemoradoMin { get; set; } = 120;

    [Range(0, 120, ErrorMessage = "Entre 0 e 120 minutos")]
    [Display(Name = "Intervalo entre atendimentos (min)")]
    public int IntervaloMin { get; set; } = 10;

    [Range(0, 72, ErrorMessage = "Entre 0 e 72 horas")]
    [Display(Name = "Antecedência mínima (horas)")]
    public int AntecedenciaHoras { get; set; } = 2;

    [Range(1, 365, ErrorMessage = "Entre 1 e 365 dias")]
    [Display(Name = "Agendar até (dias à frente)")]
    public int DiasAFrente { get; set; } = 60;

    [Range(5, 120, ErrorMessage = "Entre 5 e 120 minutos")]
    [Display(Name = "Espaço entre horários oferecidos (min)")]
    public int PassoMin { get; set; } = 30;

    [Range(0, 168, ErrorMessage = "Entre 0 e 168 horas")]
    [Display(Name = "Cliente cancela até (horas antes)")]
    public int CancelamentoHoras { get; set; } = 12;

    [StringLength(20)]
    [Display(Name = "WhatsApp para contato")]
    public string? WhatsApp { get; set; }
}
