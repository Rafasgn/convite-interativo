# 0003 — Razor Pages em vez de MVC

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

O PDF de visão inicial deixava em aberto Razor Pages ou MVC. O projeto tem duas superfícies: um fluxo público simples (animação e confirmação) e uma área admin com CRUDs (eventos, convites, convidados).

## Decisão

Usar Razor Pages para o projeto web.

## Motivação

Menos cerimônia que MVC para esse tipo de fluxo, já que não há poucas rotas complexas nem API pública separada no MVP.

Arquivos co-localizados (.cshtml junto com .cshtml.cs por página) facilitam navegar um projeto pequeno mantido por uma pessoa só.

CRUDs simples da área admin mapeiam bem para páginas (Index, Create, Edit, Delete por entidade) sem precisar de convenções de controller e view separadas.

## Consequências

Rotas seguem a convenção de pastas do Razor Pages, por exemplo /Admin/Eventos/... e /Convite/{token}.

Se no futuro for necessário expor uma API HTTP separada, isso pode conviver com Razor Pages via Minimal APIs, sem precisar migrar para MVC.
