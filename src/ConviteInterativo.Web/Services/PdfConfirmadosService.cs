using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ConviteInterativo.Web.Services;

public class PdfConfirmadosService(AppDbContext db)
{
    public async Task<byte[]> GerarConfirmadosAsync(int eventoId)
    {
        var evento = await db.Eventos.FindAsync(eventoId)
            ?? throw new InvalidOperationException("Evento não encontrado.");

        var confirmados = await (
            from c in db.Convidados
            join conv in db.Convites on c.ConviteId equals conv.Id
            where conv.EventoId == eventoId && c.Status == StatusConfirmacao.Confirmado
            orderby c.Nome, c.Sobrenome
            select new { c.Nome, c.Sobrenome, NomeConvite = conv.Nome }
        ).ToListAsync();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(evento.Nome).FontSize(18).SemiBold();
                    col.Item().Text($"{evento.DataHora:dd/MM/yyyy HH:mm} — {evento.Endereco}").FontSize(10).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).Text($"Confirmados: {confirmados.Count}").SemiBold();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    if (confirmados.Count == 0)
                    {
                        col.Item().Text("Nenhum confirmado ainda.").Italic();
                        return;
                    }

                    foreach (var conf in confirmados)
                    {
                        var display = string.IsNullOrWhiteSpace(conf.Sobrenome)
                            ? $"{conf.Nome} ({conf.NomeConvite})"
                            : $"{conf.Nome} {conf.Sobrenome}";
                        col.Item().Text(display);
                    }
                });

                page.Footer().AlignRight().Text($"Gerado em {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });

        return doc.GeneratePdf();
    }
}
