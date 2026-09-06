using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Web.Pages.Admin.Eventos;

public class IndexModel(AppDbContext db, PdfConfirmadosService pdfService) : PageModel
{
    public List<Evento> Eventos { get; set; } = [];

    public Dictionary<int, int> ConfirmadosPorEvento { get; set; } = [];

    public async Task OnGetAsync()
    {
        Eventos = await db.Eventos
            .OrderBy(e => e.DataHora)
            .ToListAsync();

        ConfirmadosPorEvento = await (
            from c in db.Convidados
            join conv in db.Convites on c.ConviteId equals conv.Id
            where c.Status == StatusConfirmacao.Confirmado
            group c by conv.EventoId into g
            select new { EventoId = g.Key, Quantidade = g.Count() }
        ).ToDictionaryAsync(x => x.EventoId, x => x.Quantidade);
    }

    public async Task<IActionResult> OnGetPdfAsync(int eventoId, bool download = false)
    {
        var evento = await db.Eventos.FindAsync(eventoId);
        if (evento is null)
        {
            return NotFound();
        }

        var bytes = await pdfService.GerarConfirmadosAsync(eventoId);

        return download
            ? File(bytes, "application/pdf", $"confirmados-{evento.Slug}.pdf")
            : File(bytes, "application/pdf");
    }
}
