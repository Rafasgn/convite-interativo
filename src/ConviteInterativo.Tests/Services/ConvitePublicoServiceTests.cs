using ConviteInterativo.Web.Data;
using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ConviteInterativo.Tests.Services;

public class ConvitePublicoServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ConvitePublicoService _service;

    public ConvitePublicoServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _service = new ConvitePublicoService(_db);
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

    private async Task<Convite> CriarConviteAsync(int eventoId, string nome, ModoConfirmacao modo = ModoConfirmacao.Grupo)
    {
        var agora = DateTime.UtcNow;
        var convite = new Convite
        {
            EventoId = eventoId,
            Nome = nome,
            Token = new TokenGenerator().GerarToken(),
            ModoConfirmacao = modo,
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        _db.Convites.Add(convite);
        await _db.SaveChangesAsync();

        return convite;
    }

    private async Task<Convidado> AdicionarConvidadoAsync(int conviteId, string nome, string? sobrenome = null)
    {
        var agora = DateTime.UtcNow;
        var convidado = new Convidado
        {
            ConviteId = conviteId,
            Nome = nome,
            Sobrenome = sobrenome,
            DataCriacao = agora,
            DataAtualizacao = agora,
        };

        _db.Convidados.Add(convidado);
        await _db.SaveChangesAsync();

        return convidado;
    }

    [Fact]
    public async Task CarregarPorTokenAsync_TokenValido_RetornaDtoCompleto()
    {
        var evento = await CriarEventoAsync("evento-1");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        await AdicionarConvidadoAsync(convite.Id, "Ana", "Silva");

        var dto = await _service.CarregarPorTokenAsync(convite.Token);

        Assert.NotNull(dto);
        Assert.Equal(evento.Id, dto!.Evento.Id);
        Assert.Equal(convite.Id, dto.Convite.Id);
        Assert.Single(dto.Convidados);
    }

    [Fact]
    public async Task CarregarPorTokenAsync_TokenInexistente_RetornaNull()
    {
        var dto = await _service.CarregarPorTokenAsync("token-que-nao-existe");

        Assert.Null(dto);
    }

    [Fact]
    public async Task CarregarPorTokenAsync_NaoCarregaConvidadosDeOutrosConvites()
    {
        var evento = await CriarEventoAsync("evento-2");
        var convite1 = await CriarConviteAsync(evento.Id, "Convite 1");
        var convite2 = await CriarConviteAsync(evento.Id, "Convite 2");
        await AdicionarConvidadoAsync(convite1.Id, "Ana", "Silva");
        await AdicionarConvidadoAsync(convite2.Id, "Bruno", "Souza");

        var dto = await _service.CarregarPorTokenAsync(convite1.Token);

        var convidado = Assert.Single(dto!.Convidados);
        Assert.Equal("Ana", convidado.Nome);
    }

    [Fact]
    public async Task ConfirmarGrupoAsync_Confirmado_AtualizaTodosOsConvidados()
    {
        var evento = await CriarEventoAsync("evento-3");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        await AdicionarConvidadoAsync(convite.Id, "Ana");
        await AdicionarConvidadoAsync(convite.Id, "Bruno");

        await _service.ConfirmarGrupoAsync(convite.Id, StatusConfirmacao.Confirmado);

        var convidados = await _db.Convidados.Where(c => c.ConviteId == convite.Id).ToListAsync();
        Assert.All(convidados, c =>
        {
            Assert.Equal(StatusConfirmacao.Confirmado, c.Status);
            Assert.NotNull(c.DataConfirmacao);
        });
    }

    [Fact]
    public async Task ConfirmarGrupoAsync_NaoVai_AtualizaTodos()
    {
        var evento = await CriarEventoAsync("evento-4");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva");
        await AdicionarConvidadoAsync(convite.Id, "Ana");
        await AdicionarConvidadoAsync(convite.Id, "Bruno");

        await _service.ConfirmarGrupoAsync(convite.Id, StatusConfirmacao.NaoVai);

        var convidados = await _db.Convidados.Where(c => c.ConviteId == convite.Id).ToListAsync();
        Assert.All(convidados, c =>
        {
            Assert.Equal(StatusConfirmacao.NaoVai, c.Status);
            Assert.NotNull(c.DataConfirmacao);
        });
    }

    [Fact]
    public async Task ConfirmarIndividualAsync_AtualizaSoOConvidadoAlvo()
    {
        var evento = await CriarEventoAsync("evento-5");
        var convite = await CriarConviteAsync(evento.Id, "Família Silva", ModoConfirmacao.Individual);
        var ana = await AdicionarConvidadoAsync(convite.Id, "Ana");
        var bruno = await AdicionarConvidadoAsync(convite.Id, "Bruno");

        await _service.ConfirmarIndividualAsync(ana.Id, StatusConfirmacao.Confirmado);

        var anaRecarregada = await _db.Convidados.FindAsync(ana.Id);
        var brunoRecarregado = await _db.Convidados.FindAsync(bruno.Id);

        Assert.Equal(StatusConfirmacao.Confirmado, anaRecarregada!.Status);
        Assert.NotNull(anaRecarregada.DataConfirmacao);
        Assert.Equal(StatusConfirmacao.SemResposta, brunoRecarregado!.Status);
        Assert.Null(brunoRecarregado.DataConfirmacao);
    }

    [Fact]
    public async Task ConfirmarGrupoAsync_NaoTocaConvidadosDeOutroConvite()
    {
        var evento = await CriarEventoAsync("evento-6");
        var convite1 = await CriarConviteAsync(evento.Id, "Convite 1");
        var convite2 = await CriarConviteAsync(evento.Id, "Convite 2");
        await AdicionarConvidadoAsync(convite1.Id, "Ana");
        var bruno = await AdicionarConvidadoAsync(convite2.Id, "Bruno");

        await _service.ConfirmarGrupoAsync(convite1.Id, StatusConfirmacao.Confirmado);

        var brunoRecarregado = await _db.Convidados.FindAsync(bruno.Id);
        Assert.Equal(StatusConfirmacao.SemResposta, brunoRecarregado!.Status);
        Assert.Null(brunoRecarregado.DataConfirmacao);
    }
}
