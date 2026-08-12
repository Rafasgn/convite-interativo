using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Services;

public class ConvidadoService(AppDbContext db)
{
    public async Task<List<Convidado>> ListarAsync(int conviteId)
    {
        return await db.Convidados
            .Where(c => c.ConviteId == conviteId)
            .OrderBy(c => c.Nome)
            .ThenBy(c => c.Sobrenome)
            .ToListAsync();
    }

    public async Task<Convidado> AdicionarAsync(int conviteId, string nome, string? sobrenome)
    {
        var agora = DateTime.UtcNow;
        var convidado = new Convidado
        {
            ConviteId = conviteId,
            Nome = nome,
            Sobrenome = sobrenome,
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        db.Convidados.Add(convidado);
        await BumparConviteAsync(conviteId, agora);
        await db.SaveChangesAsync();

        return convidado;
    }

    public async Task<bool> EditarAsync(int conviteId, int convidadoId, string nome, string? sobrenome)
    {
        var convidado = await db.Convidados.FindAsync(convidadoId);
        if (convidado is null || convidado.ConviteId != conviteId)
        {
            return false;
        }

        var agora = DateTime.UtcNow;
        convidado.Nome = nome;
        convidado.Sobrenome = sobrenome;
        convidado.DataAtualizacao = agora;

        await BumparConviteAsync(conviteId, agora);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoverAsync(int conviteId, int convidadoId)
    {
        var convidado = await db.Convidados.FindAsync(convidadoId);
        if (convidado is null || convidado.ConviteId != conviteId)
        {
            return false;
        }

        db.Convidados.Remove(convidado);
        await BumparConviteAsync(conviteId, DateTime.UtcNow);
        await db.SaveChangesAsync();

        return true;
    }

    private async Task BumparConviteAsync(int conviteId, DateTime agora)
    {
        var convite = await db.Convites.FindAsync(conviteId);
        if (convite is not null)
        {
            convite.DataAtualizacao = agora;
        }
    }
}
