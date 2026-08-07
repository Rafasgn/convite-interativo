using Microsoft.Extensions.FileProviders;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;
// TODO: implementar exportação (ADR 0013)

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// themes/ e assets/ na raiz do repo são a fonte da verdade (ADR 0012) — servidos
// diretamente da pasta física, sem copiar/duplicar em wwwroot/.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "..", "..", "themes")),
    RequestPath = "/themes",
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "..", "..", "assets")),
    RequestPath = "/assets",
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
