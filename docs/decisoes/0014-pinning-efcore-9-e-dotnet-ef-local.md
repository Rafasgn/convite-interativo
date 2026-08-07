# 0014 — Pinning de EF Core em 9.x e dotnet-ef como ferramenta local ao repo

**Data:** 2026-08-07
**Status:** Aceita

## Contexto

Duas necessidades surgiram ao preparar a migration inicial da HU-01 (modelagem de domínio e persistência).

A primeira: ao adicionar os pacotes Microsoft.EntityFrameworkCore.Sqlite e Microsoft.EntityFrameworkCore.Design, a versão mais recente disponível no NuGet (10.0.10) exige net10.0, incompatível com o projeto, que está em net9.0 (o SDK disponível no ambiente é o .NET 9). Foi necessário pinar explicitamente a versão 9.0.18 de ambos os pacotes.

A segunda: gerar a migration inicial requer a ferramenta de linha de comando dotnet-ef, que não vem embutida no SDK e precisa ser instalada separadamente.

## Decisão

**EF Core pinado em 9.x (versão exata 9.0.18)** para Microsoft.EntityFrameworkCore.Sqlite e Microsoft.EntityFrameworkCore.Design, em vez de aceitar a versão mais recente do NuGet. O upgrade para EF Core 10 só acontece junto com o upgrade do projeto para .NET 10 — não é feito separadamente, já que a versão 10.x da lib exige a versão 10.x do runtime.

**dotnet-ef como ferramenta local ao repo**, via manifesto versionado em .config/dotnet-tools.json (dotnet new tool-manifest + dotnet tool install dotnet-ef --version 9.0.18), em vez de instalação global no ambiente.

## Motivação

O pinning de versão exata (em vez de um range como 9.*) evita que um `dotnet restore` futuro puxe silenciosamente uma versão incompatível com o TargetFramework do projeto — já aconteceu durante o scaffold inicial, quando `9.*` resolveu para a versão mais recente disponível e quebrou a build.

Ferramenta local, não global, por três razões: reprodutibilidade entre ambientes (dev local em WSL, imagem Docker de produção, CI futuro — todos usam a mesma versão de dotnet-ef sem depender de instalação manual prévia); evita conflito de versão com outros projetos .NET que possam existir no mesmo WSL e exigir uma versão diferente de dotnet-ef instalada globalmente; e o restore da ferramenta entra naturalmente no fluxo do Dockerfile via `dotnet tool restore`, sem precisar de um passo de instalação separado na imagem.

## Consequências

Upgrades de EF Core ficam acoplados ao upgrade do TargetFramework do projeto — não se pode subir a versão do EF Core isoladamente sem primeiro migrar para .NET 10.

O Dockerfile (ADR 0002) precisa incluir `dotnet tool restore` no estágio de build antes de qualquer comando que dependa do dotnet-ef (não é necessário para build/publish em si, apenas se migrations forem aplicadas dentro do container).

`.config/dotnet-tools.json` é versionado no repositório — não deve entrar no `.gitignore`.

Qualquer desenvolvedor que clonar o repo precisa rodar `dotnet tool restore` antes de usar comandos `dotnet ef`.
