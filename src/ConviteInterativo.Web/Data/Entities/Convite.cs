namespace ConviteInterativo.Web.Data.Entities;

public class Convite
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public Evento? Evento { get; set; }
    public required string Nome { get; set; }
    public required string Token { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public ICollection<Convidado> Convidados { get; set; } = new List<Convidado>();
}
