using System.ComponentModel.DataAnnotations;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class ConvidadoInputModel
{
    [Required(ErrorMessage = "Informe o nome do integrante.")]
    [StringLength(200, ErrorMessage = "Nome muito longo (máx. 200 caracteres).")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Sobrenome muito longo (máx. 200 caracteres).")]
    public string? Sobrenome { get; set; }
}
