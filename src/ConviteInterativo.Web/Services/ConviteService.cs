using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Services;

public class ConviteService(AppDbContext db, TokenGenerator tokenGenerator)
{
    public async Task<bool> NomeDisponivelAsync(int eventoId, string nome, int? ignorarConviteId = null)
    {
        return !await db.Convites.AnyAsync(c =>
            c.EventoId == eventoId &&
            c.Nome == nome &&
            (ignorarConviteId == null || c.Id != ignorarConviteId));
    }

    public async Task<Convite> CriarAsync(int eventoId, string nome)
    {
        var agora = DateTime.UtcNow;
        var convite = new Convite
        {
            EventoId = eventoId,
            Nome = nome,
            Token = tokenGenerator.GerarToken(),
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        db.Convites.Add(convite);
        await db.SaveChangesAsync();

        return convite;
    }
}
