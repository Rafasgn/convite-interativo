using System.ComponentModel.DataAnnotations;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class ConviteInputModel
{
    [Required(ErrorMessage = "Informe o nome do grupo.")]
    [StringLength(200, ErrorMessage = "Nome muito longo (máx. 200 caracteres).")]
    public string Nome { get; set; } = string.Empty;
}
