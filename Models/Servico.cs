using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaaS.Cilios.Models;

public class Servico
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do serviço")]
    [StringLength(100)]
    [Display(Name = "Nome do Serviço")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escolha uma cor")]
    [Display(Name = "Cor")]
    public string Cor { get; set; } = "#BC7A91";

    [Required(ErrorMessage = "Informe o preço")]
    [Range(0.01, 99999, ErrorMessage = "Informe um preço válido")]
    [Display(Name = "Preço (R$)")]
    [DataType(DataType.Currency)]
    public decimal Preco { get; set; }

    // Quanto tempo o procedimento ocupa na agenda. Cada agendamento guarda uma cópia
    // desse valor no momento em que é criado (igual ao Valor).
    [Required(ErrorMessage = "Informe a duração")]
    [Range(5, 600, ErrorMessage = "A duração deve ficar entre 5 e 600 minutos")]
    [Display(Name = "Duração (minutos)")]
    public int DuracaoMinutos { get; set; } = 60;

    [Display(Name = "Ativo")]
    public bool Ativo { get; set; } = true;

    [NotMapped]
    public string DuracaoTexto => DuracaoHelper.Texto(DuracaoMinutos);
}
