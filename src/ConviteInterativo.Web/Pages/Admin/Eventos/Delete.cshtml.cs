using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class DeleteModel(AppDbContext db) : PageModel
{
    public Evento? Evento { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Evento = await db.Eventos.FindAsync(id);
        if (Evento is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var evento = await db.Eventos.FindAsync(id);
        if (evento is null)
        {
            return NotFound();
        }

        db.Eventos.Remove(evento);
        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
