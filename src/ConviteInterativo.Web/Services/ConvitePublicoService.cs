using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Services;

public record ConvitePublicoDto(Evento Evento, Convite Convite, List<Convidado> Convidados);

public class ConvitePublicoService(AppDbContext db)
{
    public async Task<ConvitePublicoDto?> CarregarPorTokenAsync(string token)
    {
        var convite = await db.Convites.FirstOrDefaultAsync(c => c.Token == token);
        if (convite is null)
        {
            return null;
        }

        var evento = await db.Eventos.FindAsync(convite.EventoId);
        if (evento is null)
        {
            return null;
        }

        var convidados = await db.Convidados
            .Where(c => c.ConviteId == convite.Id)
            .OrderBy(c => c.Id)
            .ThenBy(c => c.Sobrenome)
            .ToListAsync();

        return new ConvitePublicoDto(evento, convite, convidados);
    }

    /// <summary>
    /// Idempotente por design: sobrescreve status atual sem filtro. No modo
    /// Grupo o representante decide pelo grupo inteiro.
    /// </summary>
    public async Task ConfirmarGrupoAsync(int conviteId, StatusConfirmacao status)
    {
        var agora = DateTime.UtcNow;
        var convidados = await db.Convidados
            .Where(c => c.ConviteId == conviteId)
            .ToListAsync();

        foreach (var convidado in convidados)
        {
            convidado.Status = status;
            convidado.DataConfirmacao = agora;
            convidado.DataAtualizacao = agora;
        }

        await db.SaveChangesAsync();
    }

    public async Task ConfirmarIndividualAsync(int convidadoId, StatusConfirmacao status)
    {
        var convidado = await db.Convidados.FindAsync(convidadoId);
        if (convidado is null)
        {
            return;
        }

        var agora = DateTime.UtcNow;
        convidado.Status = status;
        convidado.DataConfirmacao = agora;
        convidado.DataAtualizacao = agora;

        await db.SaveChangesAsync();
    }
}
