# 0002 — Hosting em Linux/Docker; SQLite em volume persistente fora do wwwroot/

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

O hosting final ainda não está fechado, mas os candidatos principais são Railway ou Fly.io, ambos rodando containers Linux.

## Decisão

Desenvolvimento local é feito em WSL sobre Windows (Debian/Ubuntu); o alvo de produção é Linux + Docker (Railway ou Fly.io). O projeto precisa funcionar nos dois ambientes sem depender de nada específico de Windows ou IIS.

O arquivo SQLite fica em uma pasta data/ fora do wwwroot/ (que é servida publicamente), com o caminho vindo de appsettings, nunca fixo no código.

Em produção, essa pasta data/ precisa estar montada como volume persistente do container. Sem isso, o filesystem do container é efêmero e o banco é destruído a cada restart ou novo deploy.

## Motivação

wwwroot/ é servida como conteúdo estático, então o arquivo do banco não pode ficar lá.

Caminho configurável via appsettings permite trocar o local do arquivo entre ambiente local (WSL) e produção (container) sem alterar código.

Manter a stack Docker-first evita retrabalho quando o hosting for de fato escolhido entre Railway e Fly.io.

Volume persistente é requisito, não detalhe de implementação: sem ele, qualquer deploy ou reinício apaga os dados do evento.

## Consequências

O projeto ASP.NET Core deve ler a connection string ou caminho do SQLite de configuração, nunca fixo no código.

Precisa de um Dockerfile na solution quando a implementação começar.

A pasta data/ deve ser criada em runtime se não existir, e deve estar no .gitignore.

A configuração de deploy (Railway ou Fly.io) precisa declarar explicitamente o volume persistente apontando para essa pasta, isso entra na checklist de deploy.
