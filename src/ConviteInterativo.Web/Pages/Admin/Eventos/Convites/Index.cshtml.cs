using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public record ConviteListItem(int Id, string Nome, int IntegrantesCount, string Token);

public class IndexModel(AppDbContext db) : PageModel
{
    public Evento Evento { get; set; } = null!;
    public List<ConviteListItem> Convites { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int eventoId)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        Evento = evento;

        Convites = await db.Convites
            .Where(c => c.EventoId == eventoId)
            .OrderBy(c => c.Nome)
            .Select(c => new ConviteListItem(c.Id, c.Nome, c.Convidados.Count, c.Token))
            .ToListAsync();

        return Page();
    }
}
