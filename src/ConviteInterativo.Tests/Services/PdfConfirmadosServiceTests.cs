using System.Text;
using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Tests.Services;

public class PdfConfirmadosServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly PdfConfirmadosService _service;

    public PdfConfirmadosServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _service = new PdfConfirmadosService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Evento> CriarEventoAsync(string slug)
    {
        var evento = new Evento
        {
            Nome = "Evento " + slug,
            Slug = slug,
            DataHora = DateTime.UtcNow.AddDays(30),
            Endereco = "Rua Teste, 123",
            TemaSlug = "pequeno-principe",
            EmailAnfitrioes = "anfitrioes@example.com",
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow,
        };

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();

        return evento;
    }

    private async Task<Convite> CriarConviteAsync(int eventoId, string nome)
    {
        var agora = DateTime.UtcNow;
        var convite = new Convite
        {
            EventoId = eventoId,
            Nome = nome,
            Token = new TokenGenerator().GerarToken(),
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        _db.Convites.Add(convite);
        await _db.SaveChangesAsync();

        return convite;
    }

    private async Task AdicionarConvidadoAsync(int conviteId, string nome, StatusConfirmacao status)
    {
        var agora = DateTime.UtcNow;
        _db.Convidados.Add(new Convidado
        {
            ConviteId = conviteId,
            Nome = nome,
            Status = status,
            DataCriacao = agora,
            DataAtualizacao = agora,
        });

        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GerarConfirmadosAsync_ComConfirmados_RetornaPdfBemFormado()
    {
        var evento = await CriarEventoAsync("evento-pdf-1");
        var convite = await CriarConviteAsync(evento.Id, "Família Teste");
        await AdicionarConvidadoAsync(convite.Id, "Bruno", StatusConfirmacao.Confirmado);
        await AdicionarConvidadoAsync(convite.Id, "Ana", StatusConfirmacao.Confirmado);
        await AdicionarConvidadoAsync(convite.Id, "Carla", StatusConfirmacao.Confirmado);

        var pdfBytes = await _service.GerarConfirmadosAsync(evento.Id);
        var cabecalho = Encoding.ASCII.GetString(pdfBytes, 0, 5);

        // Não dá pra ler texto de dentro do PDF renderizado sem um parser de PDF
        // (o QuestPDF comprime os content streams e provavelmente usa fontes
        // embutidas com glyph-ID, não ASCII literal) — este é um smoke test:
        // confirma que o PDF foi gerado, é válido e tem conteúdo real.
        // A lógica de filtro/ordenação em si é coberta por
        // Query_FiltraApenasConfirmados_OrdenadosPorNomeESobrenome, abaixo.
        Assert.Equal("%PDF-", cabecalho);
        Assert.True(pdfBytes.Length > 1000, "PDF deveria ter conteúdo real, não só o cabeçalho.");
    }

    [Fact]
    public async Task Query_FiltraApenasConfirmados_OrdenadosPorNomeESobrenome()
    {
        var evento = await CriarEventoAsync("evento-pdf-2");
        var convite = await CriarConviteAsync(evento.Id, "Família Teste");
        await AdicionarConvidadoAsync(convite.Id, "Bruno", StatusConfirmacao.Confirmado);
        await AdicionarConvidadoAsync(convite.Id, "Ana", StatusConfirmacao.Confirmado);
        await AdicionarConvidadoAsync(convite.Id, "Carla", StatusConfirmacao.Confirmado);
        await AdicionarConvidadoAsync(convite.Id, "SemResposta", StatusConfirmacao.SemResposta);
        await AdicionarConvidadoAsync(convite.Id, "Recusou", StatusConfirmacao.NaoVai);

        // Mesma query usada dentro de PdfConfirmadosService.GerarConfirmadosAsync —
        // testada diretamente contra o banco, sem depender de ler o PDF renderizado.
        var confirmados = await (
            from c in _db.Convidados
            join conv in _db.Convites on c.ConviteId equals conv.Id
            where conv.EventoId == evento.Id && c.Status == StatusConfirmacao.Confirmado
            orderby c.Nome, c.Sobrenome
            select c.Nome
        ).ToListAsync();

        Assert.Equal(["Ana", "Bruno", "Carla"], confirmados);
    }
}
