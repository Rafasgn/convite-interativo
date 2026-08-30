using ConviteInterativo.Web.Data.Entities;
using ConviteInterativo.Web.Pages.Convite;

namespace ConviteInterativo.Tests;

public class IndexModelTests
{
    private static Evento CriarEvento(DateTime dataHora, int diasConfirmacao = 15) => new()
    {
        Nome = "Festa",
        Slug = "festa",
        DataHora = dataHora,
        DiasConfirmacao = diasConfirmacao,
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

    // HU-15 — evento 24/10, DiasConfirmacao=15 → prazoNormalData=09/10, gatilhoTardio=02/10.
    // Tabela de referência validada com o usuário; cada linha é uma fronteira da regra.
    [Fact]
    public void PrazoLimite_criado_01_10_normal_retorna_09_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 1));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.False(IndexModel.EhConviteTardio(convite, evento));
        Assert.Equal(new DateTime(2026, 10, 9, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoLimite_criado_02_10_tardio_diasAteEvento_22_retorna_19_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 2));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.True(IndexModel.EhConviteTardio(convite, evento));
        Assert.Equal(new DateTime(2026, 10, 19, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoLimite_criado_17_10_tardio_diasAteEvento_exatamente_7_retorna_19_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 17));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.Equal(new DateTime(2026, 10, 19, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoLimite_criado_18_10_tardio_diasAteEvento_6_retorna_23_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 18));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.Equal(new DateTime(2026, 10, 23, 23, 59, 59), prazo);
    }

    [Fact]
    public void PrazoLimite_criado_23_10_tardio_retorna_23_10_23_59_59()
    {
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 23));

        var prazo = IndexModel.PrazoLimite(convite, evento);

        Assert.Equal(new DateTime(2026, 10, 23, 23, 59, 59), prazo);
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
        var evento = CriarEvento(new DateTime(2026, 10, 24), diasConfirmacao: 15);
        var convite = CriarConvite(new DateTime(2026, 10, 1));
        var convidado = CriarConvidado(StatusConfirmacao.SemResposta);

        var status = IndexModel.StatusExibicaoIndividual(convidado, convite, evento);

        Assert.Equal("Confirme se poderá comparecer até 09 de outubro", status);
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
