# Convite Interativo — Contexto

## Sobre
Plataforma de convites digitais interativos. Convidado recebe link único no WhatsApp,
abre e vive uma animação temática antes de ver o convite e confirmar presença.
Painel admin cadastra eventos/convites/convidados e gera os links.

Primeiro evento real: festa em 24/10/2026. Envio dos convites: ~20/09/2026.

## Stack
- ASP.NET Core 9 (Razor Pages ou MVC — a decidir)
- Entity Framework Core
- SQLite (MVP; migrar pra Postgres se virar produto)
- HTML/CSS/JS puro no front do convite (sem framework SPA no MVP)

## Convenções
- Tema plugável desde o começo: assets e config por tema em `themes/<slug>/`,
  motor de animação não referencia strings/paths do Pequeno Príncipe diretamente
- Backlog e decisões vão em `docs/` antes da implementação
- Modelagem base: Evento → Convite → Convidados (1 convite = 1 link, sem seleção dinâmica)

## Restrições operacionais
- Trabalhar em modo "manually approve edits" — revisar antes de aplicar
- Usuário controla git (add/commit/push) via Visual Studio, não pelo Claude Code
- Perguntar antes de introduzir dependências/libs novas
