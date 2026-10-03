using System.ComponentModel.DataAnnotations;

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

    [Display(Name = "Ativo")]
    public bool Ativo { get; set; } = true;
}
