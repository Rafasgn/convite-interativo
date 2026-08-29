using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Pages.Convite;

namespace ConviteInterativo.Tests;

public class IndexModelTests
{
    private static Evento CriarEvento(DateTime dataHora) => new()
    {
        Nome = "Festa",
        Slug = "festa",
        DataHora = dataHora,
        Endereco = "Rua Teste, 123",
        TemaSlug = "pequeno-principe",
        EmailAnfitrioes = "anfitrioes@example.com",
    };

    private static Convite CriarConvite(DateTime dataCriacao) => new()
    {
        Nome = "Convite",
        Token = "token-teste",
        DataCriacao = dataCriacao,
    };

    private static Convidado CriarConvidado(StatusConfirmacao status) => new()
    {
        Nome = "Convidado",
        Status = status,
    };

    [Fact]
    public void EhConviteTardio_criado_antes_de_10_dias_retorna_false()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24));
        var convite = CriarConvite(new DateTime(2026, 10, 14));

        Assert.False(IndexModel.EhConviteTardio(convite, evento));
    }

    [Fact]
    public void EhConviteTardio_criado_dentro_de_10_dias_retorna_true()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24));
        var convite = CriarConvite(new DateTime(2026, 10, 16));

        Assert.True(IndexModel.EhConviteTardio(convite, evento));
    }

    [Fact]
    public void PrazoLimite_antecipado_evento_24_10_retorna_14_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24));
        var convite = CriarConvite(new DateTime(2026, 10, 14));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.Equal(new DateTime(2026, 10, 14, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoLimite_tardio_evento_24_10_retorna_22_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24));
        var convite = CriarConvite(new DateTime(2026, 10, 16));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.Equal(new DateTime(2026, 10, 22, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoEncerrado_agora_antes_do_limite_retorna_false()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(60));
        var convite = CriarConvite(DateTime.UtcNow);

        Assert.False(IndexModel.PrazoEncerrado(convite, evento));
    }

    [Fact]
    public void PrazoEncerrado_agora_depois_do_limite_retorna_true()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));

        Assert.True(IndexModel.PrazoEncerrado(convite, evento));
    }

    [Fact]
    public void StatusExibicaoIndividual_sem_resposta_pre_prazo_mostra_data()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24));
        var convite = CriarConvite(new DateTime(2026, 10, 14));
        var convidado = CriarConvidado(StatusConfirmacao.SemResposta);

        var status = IndexModel.StatusExibicaoIndividual(convidado, convite, evento);

        Assert.Equal("Confirme se poderá comparecer até 14 de outubro", status);
    }

    [Fact]
    public void StatusExibicaoIndividual_confirmado_sempre_confirmada()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.Confirmado);

        var status = IndexModel.StatusExibicaoIndividual(convidado, convite, evento);

        Assert.Equal("✓ Presença confirmada", status);
    }

    [Fact]
    public void MostrarBotaoConfirmar_confirmado_pre_prazo_false()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(60));
        var convite = CriarConvite(DateTime.UtcNow);
        var convidado = CriarConvidado(StatusConfirmacao.Confirmado);

        Assert.False(IndexModel.MostrarBotaoConfirmar(convidado, convite, evento));
    }

    [Fact]
    public void MostrarBotaoConfirmar_confirmado_pos_prazo_false()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.Confirmado);

        Assert.False(IndexModel.MostrarBotaoConfirmar(convidado, convite, evento));
    }

    [Fact]
    public void MostrarBotaoRecusar_confirmado_pos_prazo_true()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.Confirmado);

        Assert.True(IndexModel.MostrarBotaoRecusar(convidado, convite, evento));
    }

    [Fact]
    public void MostrarBotaoRecusar_sem_resposta_pos_prazo_false()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.SemResposta);

        Assert.False(IndexModel.MostrarBotaoRecusar(convidado, convite, evento));
    }

    [Fact]
    public void MostrarDadosEvento_recusado_pos_prazo_false()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.NaoVai);

        Assert.False(IndexModel.MostrarDadosEvento(convidado, convite, evento));
    }

    [Fact]
    public void MostrarDadosEvento_confirmado_pos_prazo_true()
    {
        var evento = CriarEvento(DateTime.UtcNow.AddDays(-1));
        var convite = CriarConvite(DateTime.UtcNow.AddDays(-90));
        var convidado = CriarConvidado(StatusConfirmacao.Confirmado);

        Assert.True(IndexModel.MostrarDadosEvento(convidado, convite, evento));
    }
}
