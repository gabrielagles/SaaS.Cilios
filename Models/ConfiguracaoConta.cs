using System.ComponentModel.DataAnnotations;

namespace SaaS.Cilios.Models;

// Guarda as preferências de exibição do painel (só existe 1 linha nessa tabela,
// já que o sistema hoje é de uso pessoal/privado — não multi-empresa).
public class ConfiguracaoConta
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome a ser exibido")]
    [StringLength(60)]
    [Display(Name = "Seu nome (aparece na saudação)")]
    public string NomeExibicao { get; set; } = "Rafa";

    [Required(ErrorMessage = "Informe o nome do painel")]
    [StringLength(60)]
    [Display(Name = "Nome do painel (aparece no topo)")]
    public string NomePainel { get; set; } = "Cílios Agenda";

    // Caminho relativo dentro de wwwroot, ex: "/uploads/perfil.jpg". Nulo = usa a inicial do nome.
    public string? FotoCaminho { get; set; }

    // Incrementado a cada troca de foto, só pra "furar" o cache do navegador na hora de mostrar a nova imagem.
    public int FotoVersao { get; set; } = 0;
}
