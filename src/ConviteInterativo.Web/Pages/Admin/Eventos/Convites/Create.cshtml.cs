using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConviteInterativo.Web.Pages.Admin.Eventos.Convites;

public class CreateModel(AppDbContext db, ConviteService conviteService) : PageModel
{
    public Evento Evento { get; set; } = null!;

    [BindProperty]
    public ConviteInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventoId)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        Evento = evento;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventoId)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        Evento = evento;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await conviteService.NomeDisponivelAsync(eventoId, Input.Nome))
        {
            ModelState.AddModelError(nameof(Input.Nome), "Já existe um convite com esse nome neste evento.");
            return Page();
        }

        var convite = await conviteService.CriarAsync(eventoId, Input.Nome, Input.ModoConfirmacao);

        return RedirectToPage("Details", new { eventoId, id = convite.Id });
    }
}
