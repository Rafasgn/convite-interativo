using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Tests;

public class ConviteServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConviteService _service;

    public ConviteServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _service = new ConviteService(_db, new TokenGenerator());
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
            DataCriacao = DateTime.UtcNow,
            DataAtualizacao = DateTime.UtcNow,
        };

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();

        return evento;
    }

    [Fact]
    public async Task NomeDisponivelAsync_NomeJaExisteNoMesmoEvento_RetornaFalse()
    {
        var evento = await CriarEventoAsync("evento-1");
        await _service.CriarAsync(evento.Id, "Família Silva");

        var disponivel = await _service.NomeDisponivelAsync(evento.Id, "Família Silva");

        Assert.False(disponivel);
    }

    [Fact]
    public async Task NomeDisponivelAsync_EditandoOMesmoConvite_IgnoraOProprioId_RetornaTrue()
    {
        var evento = await CriarEventoAsync("evento-2");
        var convite = await _service.CriarAsync(evento.Id, "Família Silva");

        var disponivel = await _service.NomeDisponivelAsync(evento.Id, "Família Silva", convite.Id);

        Assert.True(disponivel);
    }

    [Fact]
    public async Task NomeDisponivelAsync_MesmoNomeEmEventosDiferentes_RetornaTrue()
    {
        var evento1 = await CriarEventoAsync("evento-3");
        var evento2 = await CriarEventoAsync("evento-4");
        await _service.CriarAsync(evento1.Id, "Família Silva");

        var disponivel = await _service.NomeDisponivelAsync(evento2.Id, "Família Silva");

        Assert.True(disponivel);
    }

    [Fact]
    public async Task CriarAsync_TokenEhPreenchidoENaoFicaVazio()
    {
        var evento = await CriarEventoAsync("evento-5");

        var convite = await _service.CriarAsync(evento.Id, "Família Silva");

        Assert.False(string.IsNullOrWhiteSpace(convite.Token));
    }
}
