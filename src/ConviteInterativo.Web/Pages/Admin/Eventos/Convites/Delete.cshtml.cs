using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class DeleteModel(AppDbContext db) : PageModel
{
    public Evento Evento { get; set; } = null!;
    public Convite Convite { get; set; } = null!;
    public int IntegrantesCount { get; set; }

    public async Task<IActionResult> OnGetAsync(int eventoId, int id)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        var convite = await db.Convites.FindAsync(id);
        if (convite is null || convite.EventoId != eventoId)
        {
            return NotFound();
        }

        Evento = evento;
        Convite = convite;
        IntegrantesCount = await db.Convidados.CountAsync(c => c.ConviteId == id);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventoId, int id)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        var convite = await db.Convites.FindAsync(id);
        if (convite is null || convite.EventoId != eventoId)
        {
            return NotFound();
        }

        db.Convites.Remove(convite);
        await db.SaveChangesAsync();

        return RedirectToPage("Index", new { eventoId });
    }
}
