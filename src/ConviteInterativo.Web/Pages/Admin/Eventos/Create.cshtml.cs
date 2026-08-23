using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class CreateModel(AppDbContext db) : PageModel
{
    [BindProperty]
    public EventoInputModel Input { get; set; } = new();

    public void OnGet()
    {
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

        if (await db.Eventos.AnyAsync(e => e.Slug == Input.Slug))
        {
            ModelState.AddModelError(nameof(Input.Slug), "Já existe um evento com esse slug.");
            return Page();
        }

        var agora = DateTime.UtcNow;
        db.Eventos.Add(new Evento
        {
            Nome = Input.Nome,
            Slug = Input.Slug,
            DataHora = Input.DataHora,
            Endereco = Input.Endereco,
            LinkMapa = Input.LinkMapa,
            TemaSlug = Input.TemaSlug,
            Anfitrioes = Input.Anfitrioes,
            EmailAnfitrioes = Input.EmailAnfitrioes,
            Homenageado = Input.Homenageado,
            DataCriacao = agora,
            DataAtualizacao = agora,
        });

        await db.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
