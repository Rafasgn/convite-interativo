using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class IndexModel(AppDbContext db) : PageModel
{
    public List<Evento> Eventos { get; set; } = [];

    public async Task OnGetAsync()
    {
        Eventos = await db.Eventos
            .OrderBy(e => e.DataHora)
            .ToListAsync();
    }
}
