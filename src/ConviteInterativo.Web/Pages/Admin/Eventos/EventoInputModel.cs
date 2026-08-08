using System.ComponentModel.DataAnnotations;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class EventoInputModel
{
    public static readonly string[] TemasDisponiveis = ["pequeno-principe"];

    [Required(ErrorMessage = "Informe o nome do evento.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o slug.")]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Use apenas letras minúsculas, números e hífens.")]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data e hora do evento.")]
    public DateTime DataHora { get; set; }

    [Required(ErrorMessage = "Informe o endereço.")]
    public string Endereco { get; set; } = string.Empty;

    [Url(ErrorMessage = "Informe uma URL válida.")]
    public string? LinkMapa { get; set; }

    [Required(ErrorMessage = "Selecione o tema.")]
    public string TemaSlug { get; set; } = string.Empty;
}
