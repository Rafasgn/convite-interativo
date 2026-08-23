using System.Runtime.CompilerServices;
using QuestPDF.Infrastructure;

namespace ConviteInterativo.Tests;

internal static class QuestPdfLicenseSetup
{
    // Program.cs configura isso na inicialização real da app, mas os testes rodam
    // num processo próprio que nunca executa Program.cs — sem isso, qualquer teste
    // que gere PDF via QuestPDF falha com "Please configure the QuestPDF license".
    [ModuleInitializer]
    public static void Configurar()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
