using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Tests;

public class ConvidadoServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConvidadoService _service;

    public ConvidadoServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _service = new ConvidadoService(_db);
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

    private async Task<Convite> CriarConviteAsync(int eventoId, string nome, DateTime? dataAtualizacao = null)
    {
        var agora = dataAtualizacao ?? DateTime.UtcNow;
        var convite = new Convite
        {
            EventoId = eventoId,
            Nome = nome,
            Token = new TokenGenerator().GerarToken(),
            ModoConfirmacao = ModoConfirmacao.Grupo,
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        _db.Convites.Add(convite);
        await _db.SaveChangesAsync();

        return convite;
    }

    [Fact]
    public async Task AdicionarAsync_PrimeiroConvidado_ContagemViraUm()
    {
        var evento = await CriarEventoAsync("evento-1");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");

        await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        var lista = await _service.ListarAsync(convite.Id);
        Assert.Single(lista);
    }

    [Fact]
    public async Task RemoverAsync_UltimoConvidado_ContagemVoltaAZero()
    {
        var evento = await CriarEventoAsync("evento-2");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        var convidado = await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        await _service.RemoverAsync(convite.Id, convidado.Id);

        var lista = await _service.ListarAsync(convite.Id);
        Assert.Empty(lista);
    }

    [Fact]
    public async Task EditarAsync_AtualizaDadosMantendoOMesmoId()
    {
        var evento = await CriarEventoAsync("evento-3");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        var convidado = await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        var resultado = await _service.EditarAsync(convite.Id, convidado.Id, "Ana Maria", "Souza");

        Assert.True(resultado);

        var lista = await _service.ListarAsync(convite.Id);
        var atualizado = Assert.Single(lista);
        Assert.Equal(convidado.Id, atualizado.Id);
        Assert.Equal("Ana Maria", atualizado.Nome);
        Assert.Equal("Souza", atualizado.Sobrenome);
    }

    [Fact]
    public async Task AdicionarAsync_BumpaDataAtualizacaoDoConvite()
    {
        var evento = await CriarEventoAsync("evento-4");
        var dataAntiga = DateTime.UtcNow.AddDays(-10);
        var convite = await CriarConviteAsync(evento.Id, "Família Silva", dataAntiga);

        await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        var recarregado = await _db.Convites.FindAsync(convite.Id);
        Assert.True(recarregado!.DataAtualizacao > dataAntiga);
    }

    [Fact]
    public async Task EditarAsync_BumpaDataAtualizacaoDoConvite()
    {
        var evento = await CriarEventoAsync("evento-5");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        var convidado = await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        var dataAntiga = DateTime.UtcNow.AddDays(-10);
        convite.DataAtualizacao = dataAntiga;
        await _db.SaveChangesAsync();

        await _service.EditarAsync(convite.Id, convidado.Id, "Ana Maria", "Souza");

        var recarregado = await _db.Convites.FindAsync(convite.Id);
        Assert.True(recarregado!.DataAtualizacao > dataAntiga);
    }

    [Fact]
    public async Task RemoverAsync_BumpaDataAtualizacaoDoConvite()
    {
        var evento = await CriarEventoAsync("evento-6");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        var convidado = await _service.AdicionarAsync(convite.Id, "Ana", "Silva");

        var dataAntiga = DateTime.UtcNow.AddDays(-10);
        convite.DataAtualizacao = dataAntiga;
        await _db.SaveChangesAsync();

        await _service.RemoverAsync(convite.Id, convidado.Id);

        var recarregado = await _db.Convites.FindAsync(convite.Id);
        Assert.True(recarregado!.DataAtualizacao > dataAntiga);
    }

    [Fact]
    public async Task EditarAsync_ConvidadoDeOutroConvite_RetornaFalseNaoAltera()
    {
        var evento = await CriarEventoAsync("evento-7");
        var convite1 = await CriarConviteAsync(evento.Id, "Convite 1");
        var convite2 = await CriarConviteAsync(evento.Id, "Convite 2");
        var convidado = await _service.AdicionarAsync(convite1.Id, "Ana", "Silva");

        var resultado = await _service.EditarAsync(convite2.Id, convidado.Id, "Outro Nome", "Outro Sobrenome");

        Assert.False(resultado);

        var recarregado = await _db.Convidados.FindAsync(convidado.Id);
        Assert.Equal("Ana", recarregado!.Nome);
        Assert.Equal("Silva", recarregado.Sobrenome);
    }

    [Fact]
    public async Task RemoverAsync_ConvidadoDeOutroConvite_RetornaFalseNaoRemove()
    {
        var evento = await CriarEventoAsync("evento-8");
        var convite1 = await CriarConviteAsync(evento.Id, "Convite 1");
        var convite2 = await CriarConviteAsync(evento.Id, "Convite 2");
        var convidado = await _service.AdicionarAsync(convite1.Id, "Ana", "Silva");

        var resultado = await _service.RemoverAsync(convite2.Id, convidado.Id);

        Assert.False(resultado);

        var aindaExiste = await _db.Convidados.FindAsync(convidado.Id);
        Assert.NotNull(aindaExiste);
    }
}
