# 0015 — Hardening pra deploy — path do SQLite, ForwardedHeaders, USER non-root

**Data:** 2026-08-07
**Status:** Aceita parcialmente (apenas decisão 1 implementada; decisões 2 e 3 pendentes até primeiro deploy real)

## Contexto

Durante os testes finais da HU-02, três problemas de paridade dev/prod foram identificados que o ADR 0002 não cobriu.

## Decisão 1 (implementada): renomear o diretório do SQLite de data/ pra db/

WSL sobre /mnt/c é case-insensitive (herda do NTFS), fazendo data/ colidir com Data/ (pasta padrão do EF pra entidades, migrations, DbContext) — o .db ia parar dentro de Data/ misturado com código-fonte. Em produção Linux nativo (case-sensitive) seriam pastas diferentes, quebrando a paridade dev/prod prometida no ADR 0002.

Também: Directory.CreateDirectory antes do Migrate() no Program.cs, pra o SQLite não crashar com "Error 14: unable to open database file" na primeira execução (o driver não cria pasta pai sozinho).

## Decisão 2 (pendente até primeiro deploy): app.UseForwardedHeaders()

Adicionar app.UseForwardedHeaders() no Program.cs configurado com XForwardedFor | XForwardedProto.

Motivo: Railway e Fly.io terminam TLS no edge deles, a app fala HTTP interno. UseHttpsRedirection sem ForwardedHeaders gera loop de redirect ou warnings de "unknown scheme". Implementar antes do primeiro deploy real (não agora — sem impacto em dev local).

## Decisão 3 (pendente até primeiro deploy): USER app no Dockerfile

Adicionar USER app no Dockerfile antes do ENTRYPOINT. As imagens mcr.microsoft.com/dotnet/aspnet já vêm com esse user (uid 1654) configurado.

Motivo: defesa em profundidade — se a app for comprometida, o atacante roda como user não-privilegiado dentro do container. Implementar junto com a decisão 2.

## Consequências

As decisões 2 e 3 ficam registradas mas não implementadas até o momento do primeiro deploy. Quando implementadas, o status vira "Aceita" com nota da data.
