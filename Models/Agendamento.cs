using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaaS.Cilios.Models;

public class Agendamento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da cliente")]
    [StringLength(150)]
    [Display(Name = "Cliente")]
    public string ClienteNome { get; set; } = string.Empty;

    [Display(Name = "Telefone")]
    [StringLength(20)]
    public string? ClienteTelefone { get; set; }

    [Required(ErrorMessage = "Selecione o serviço")]
    [Display(Name = "Serviço")]
    public int ServicoId { get; set; }

    [ForeignKey(nameof(ServicoId))]
    public Servico? Servico { get; set; }

    [Required(ErrorMessage = "Informe a data e horário")]
    [Display(Name = "Data e Hora")]
    [DataType(DataType.DateTime)]
    public DateTime DataHora { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Informe o valor")]
    [Range(0, 99999, ErrorMessage = "Informe um valor válido")]
    [Display(Name = "Valor (R$)")]
    [DataType(DataType.Currency)]
    public decimal Valor { get; set; }

    [Display(Name = "Concluído")]
    public bool Concluido { get; set; } = false;

    // Preenchido automaticamente quando o agendamento é criado offline
    // e sincronizado depois (fica marcado pra referência/depuração).
    public bool CriadoOffline { get; set; } = false;
}
