# HU-02 — Autenticação admin e CRUD de Evento

**Status:** Aprovada

## Objetivo

Ativar a área /Admin com autenticação por senha única (ADR 0004) e implementar o primeiro CRUD real: gerenciamento de Eventos. Ao fim da HU, o admin consegue logar, listar, criar, editar e excluir eventos. Sem CRUD de Convite/Convidado ainda.

## Escopo

### Autenticação

- Pacote novo: BCrypt.Net-Next (última versão estável compatível com .NET 9) — hash de senha.
- Configuração em appsettings.json: nova seção Admin.PasswordHash (string, hash BCrypt da senha). Um comentário no arquivo indica como gerar novo hash (via script utilitário ou linha de C# rodada manualmente). A senha em texto puro NÃO é commitada em lugar nenhum.
- Ferramenta pra gerar hash: cria Services/PasswordHasher.cs com métodos estáticos Hash(string) e Verify(string, string). Usado no login E disponível pra gerar hash inicial (via um endpoint temporário oculto ou instrução no README de como rodar BCrypt.Net.BCrypt.HashPassword("senha") no dotnet fsi ou similar — decide o mais simples).
- Cookie authentication:
  - builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...) no Program.cs
  - Cookie name: ConviteInterativo.Admin
  - Expiração: 7 dias (ExpireTimeSpan = TimeSpan.FromDays(7))
  - SlidingExpiration = true (renova ao usar)
  - LoginPath = "/Admin/Login"
  - AccessDeniedPath = "/Admin/Login" (usuário não autenticado é redirecionado pro login)
  - Cookie HttpOnly (default), Secure em produção, SameSite = Lax
- AuthorizeFolder no Razor Pages:
  - builder.Services.AddRazorPages(options => { options.Conventions.AuthorizeFolder("/Admin"); options.Conventions.AllowAnonymousToPage("/Admin/Login"); });
  - Todas as páginas em /Admin/* exigem autenticação, exceto /Admin/Login.
- app.UseAuthentication() antes de app.UseAuthorization() no pipeline.

### Página de Login

- Pages/Admin/Login.cshtml + .cshtml.cs
- Formulário: um único campo Senha (password input).
- No POST:
  - PasswordHasher.Verify(senhaInformada, config["Admin:PasswordHash"])
  - Se OK: cria ClaimsIdentity com uma claim mínima (ex.: Name = "admin"), gera cookie via HttpContext.SignInAsync, redireciona pra ReturnUrl ou /Admin/Eventos/Index.
  - Se não: exibe mensagem "Senha incorreta" (sem revelar detalhe).
- Rate limiting simples: se falhar 5 vezes em 1 minuto, bloqueia o IP por 10 minutos (usa IMemoryCache pra tracking; builder.Services.AddMemoryCache()).

### Logout

- Pages/Admin/Logout.cshtml.cs (só o .cs, sem view — action que chama HttpContext.SignOutAsync e redireciona pra /Admin/Login).
- Link "Sair" no layout do admin.

### Layout do Admin

- Pages/Admin/_Layout.cshtml — layout compartilhado das páginas admin. Header simples com título "Convite Interativo — Admin", link "Eventos", link "Sair".
- Pages/Admin/_ViewStart.cshtml — define esse layout como default pra páginas em /Admin/.
- Estilo mínimo: CSS inline ou arquivo pequeno em wwwroot/css/admin.css. Sem framework CSS (Bootstrap, Tailwind) por enquanto — MVP.

### CRUD de Evento

Cinco páginas em Pages/Admin/Eventos/:

- Index.cshtml — listagem de eventos (tabela: Nome, Slug, DataHora, ações Editar/Excluir). Botão "Novo Evento" no topo.
- Create.cshtml — formulário de criação. Campos: Nome, Slug (com hint "identificador na URL, ex.: zayan-1-ano"), DataHora, Endereco, LinkMapa (opcional), TemaSlug (select com opções fixas por enquanto: só "pequeno-principe"). Ao salvar: DataCriacao e DataAtualizacao = DateTime.UtcNow.
- Edit.cshtml — mesmo formulário do Create, pré-preenchido. Ao salvar: atualiza DataAtualizacao.
- Delete.cshtml — confirmação antes de excluir. Mostra o nome do evento e aviso "Todos os convites vinculados serão excluídos junto" (cascade delete do ADR/AppDbContext).
- Details.cshtml — visualização somente leitura (opcional; se ficar apertado, pula e deixa pra HU futura).

### Validações

- Nome, Slug, DataHora, Endereco, TemaSlug obrigatórios (via [Required] no ViewModel).
- Slug só aceita caracteres URL-safe: regex ^[a-z0-9-]+$ (letras minúsculas, números, hífens).
- Ao criar/editar, valida unicidade do Slug (query no DbContext antes do save; mensagem: "Já existe um evento com esse slug").
- DataHora não pode ser no passado (validação simples: >= DateTime.UtcNow).
- LinkMapa, se preenchido, deve ser URL válida ([Url] attribute).

### data/convite.db

- Aplicar a migration InicialModelagem em runtime na primeira execução dev (só em Environment.IsDevelopment()): dbContext.Database.Migrate() no Program.cs. Isso cria o arquivo data/convite.db automaticamente ao rodar dotnet run pela primeira vez.
- Em produção, Migrate() também roda (é idempotente — se já tá aplicada, não faz nada).

## Fora do escopo desta HU

- CRUD de Convite e Convidado (HU seguinte)
- Geração de link único (parte do CRUD de Convite)
- Página pública /Convite/{token}
- Animação
- Exportação PDF
- Recuperação de senha, múltiplos usuários, ASP.NET Identity — tudo continua fora conforme ADR 0004
- Testes automatizados de auth ou CRUD (a validação é manual na UI por enquanto)

## Critérios de aceite

- dotnet build compila com 0 warnings e 0 errors
- dotnet run sobe a app; data/convite.db é criado automaticamente na primeira execução
- Acessar /Admin/Eventos sem estar logado redireciona pra /Admin/Login
- Login com senha correta autentica e redireciona pra /Admin/Eventos/Index
- Login com senha errada mostra "Senha incorreta"; após 5 tentativas falhadas em 1 minuto, IP fica bloqueado por 10 minutos
- Botão "Sair" desloga e volta pra /Admin/Login
- CRUD de Evento funcional: criar, listar, editar, excluir (com confirmação)
- Validação de slug único funciona (tentar criar dois eventos com mesmo slug → erro claro)
