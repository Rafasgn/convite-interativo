using ConviteInterativo.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class EditModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public EventoInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var evento = await db.Eventos.FindAsync(id);
        if (evento is null)
        {
            return NotFound();
        }

        Id = evento.Id;
        Input = new EventoInputModel
        {
            Nome = evento.Nome,
            Slug = evento.Slug,
            DataHora = evento.DataHora,
            Endereco = evento.Endereco,
            LinkMapa = evento.LinkMapa,
            TemaSlug = evento.TemaSlug,
            Anfitrioes = evento.Anfitrioes,
            Homenageado = evento.Homenageado,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.DataHora < DateTime.UtcNow)
        {
            ModelState.AddModelError(nameof(Input.DataHora), "A data do evento não pode ser no passado.");
            return Page();
        }

        if (await db.Eventos.AnyAsync(e => e.Slug == Input.Slug && e.Id != Id))
        {
            ModelState.AddModelError(nameof(Input.Slug), "Já existe um evento com esse slug.");
            return Page();
        }

        var evento = await db.Eventos.FindAsync(Id);
        if (evento is null)
        {
            return NotFound();
        }

        evento.Nome = Input.Nome;
        evento.Slug = Input.Slug;
        evento.DataHora = Input.DataHora;
        evento.Endereco = Input.Endereco;
        evento.LinkMapa = Input.LinkMapa;
        evento.TemaSlug = Input.TemaSlug;
        evento.Anfitrioes = Input.Anfitrioes;
        evento.Homenageado = Input.Homenageado;
        evento.DataAtualizacao = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
