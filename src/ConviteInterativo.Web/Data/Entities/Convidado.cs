namespace ConviteInterativo.Web.Data.Entities;

public class Convidado
{
    public int Id { get; set; }
    public int ConviteId { get; set; }
    public Convite? Convite { get; set; }
    public required string Nome { get; set; }
    public string? Sobrenome { get; set; }
    public StatusConfirmacao Status { get; set; } = StatusConfirmacao.SemResposta;
    public DateTime? DataConfirmacao { get; set; }
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }
}
