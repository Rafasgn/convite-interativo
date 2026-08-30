namespace ConviteInterativo.Web.Data.Entities;

public class Evento
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public required string Slug { get; set; }
    public DateTime DataHora { get; set; }
    public int DiasConfirmacao { get; set; }
    public required string Endereco { get; set; }
    public string? LinkMapa { get; set; }
    public required string TemaSlug { get; set; }
    public string? Anfitrioes { get; set; }
    public string? Homenageado { get; set; }
    public required string EmailAnfitrioes { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public ICollection<Convite> Convites { get; set; } = new List<Convite>();
}
