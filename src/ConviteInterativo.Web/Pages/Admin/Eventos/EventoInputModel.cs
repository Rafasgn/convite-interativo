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
    public DateTime DataHora { get; set; } = DateTime.Now.Date.AddDays(1).AddHours(19);

    [Required(ErrorMessage = "Informe o limite de dias para confirmação.")]
    [Range(1, 90, ErrorMessage = "Informe um valor entre 1 e 90 dias.")]
    [Display(Name = "Limite de Dias para confirmação")]
    public int DiasConfirmacao { get; set; } = 15;

    [Required(ErrorMessage = "Informe o endereço.")]
    public string Endereco { get; set; } = string.Empty;

    [Url(ErrorMessage = "Informe uma URL válida.")]
    public string? LinkMapa { get; set; }

    [Required(ErrorMessage = "Selecione o tema.")]
    public string TemaSlug { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Anfitriões")]
    public string? Anfitrioes { get; set; }

    [Required(ErrorMessage = "Informe o email dos anfitriões.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [StringLength(200)]
    [Display(Name = "Email dos anfitriões")]
    public string EmailAnfitrioes { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Homenageado")]
    public string? Homenageado { get; set; }
}
