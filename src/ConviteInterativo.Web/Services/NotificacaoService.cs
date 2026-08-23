using System.Net;
using System.Net.Mail;
using ConviteInterativo.Web.Data.Entities;
using Microsoft.Extensions.Options;

namespace ConviteInterativo.Web.Services;

public interface INotificacaoService
{
    void NotificarResposta(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao novoStatus, byte[]? pdfBytes);
}

public class NotificacaoService(IOptions<SmtpOptions> smtpOptions, ILogger<NotificacaoService> logger) : INotificacaoService
{
    public void NotificarResposta(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao novoStatus, byte[]? pdfBytes)
    {
        // Fire-and-forget — não bloqueia o PRG do convidado. Os dados já foram
        // "bufferizados" (evento/convite/convidado/pdfBytes) pelo chamador antes
        // desta chamada — nada aqui dentro acessa o AppDbContext, que é scoped e
        // morre junto com o request original antes desta Task terminar.
        _ = Task.Run(async () =>
        {
            try
            {
                var opts = smtpOptions.Value;
                var msg = new MailMessage
                {
                    From = new MailAddress(opts.From, "Convite Interativo"),
                    Subject = ConstruirAssunto(evento, convidado, novoStatus),
                    Body = ConstruirCorpo(evento, convite, convidado, novoStatus),
                    IsBodyHtml = false,
                };
                msg.To.Add(evento.EmailAnfitrioes);

                if (novoStatus == StatusConfirmacao.Confirmado && pdfBytes is not null)
                {
                    var nomeArquivo = $"confirmados-{evento.Slug}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";
                    var stream = new MemoryStream(pdfBytes);
                    msg.Attachments.Add(new Attachment(stream, nomeArquivo, "application/pdf"));
                }

                using var client = new SmtpClient(opts.Host, opts.Port)
                {
                    Credentials = new NetworkCredential(opts.Username, opts.Password),
                    EnableSsl = true,
                };
                await client.SendMailAsync(msg);

                logger.LogInformation("Notificação enviada pra {Email} sobre {Nome}", evento.EmailAnfitrioes, convidado.Nome);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar notificação de RSVP");
            }
        });
    }

    private static string ConstruirAssunto(Evento evento, Convidado convidado, StatusConfirmacao status) =>
        status == StatusConfirmacao.Confirmado
            ? $"[{evento.Nome}] {convidado.Nome} confirmou presença"
            : $"[{evento.Nome}] {convidado.Nome} marcou que não vai";

    private static string ConstruirCorpo(Evento evento, Convite convite, Convidado convidado, StatusConfirmacao status)
    {
        var verbo = status == StatusConfirmacao.Confirmado ? "confirmou presença" : "marcou que não pode comparecer";
        return $$"""
            Olá,

            {{convidado.Nome}} (do grupo "{{convite.Nome}}") {{verbo}} para o evento {{evento.Nome}}.

            Data: {{evento.DataHora:dd/MM/yyyy HH:mm}}
            Local: {{evento.Endereco}}

            {{(status == StatusConfirmacao.Confirmado ? "PDF com a lista atualizada de confirmados em anexo." : "")}}

            --
            Sistema Convite Interativo
            """;
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}
