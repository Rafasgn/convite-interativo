# 0004 — Auth do admin: senha única via appsettings mais cookie, sem ASP.NET Identity

**Data:** 2026-08-06
**Status:** Aceita

## Contexto

A área /admin precisa de proteção, mas no MVP só o dono do projeto vai usá-la. Não há necessidade de múltiplos usuários, papéis ou recuperação de senha.

## Decisão

Proteger /admin com autenticação por cookie, validando contra uma senha única configurada em appsettings (tratada como segredo em produção). Sem ASP.NET Identity no MVP.

## Motivação

Um único operador não justifica o overhead de Identity, como tabelas de usuário, hashing de múltiplas contas e fluxo de registro ou recuperação.

Cookie de autenticação simples já resolve a necessidade real, que é manter a sessão autenticada depois do login.

## Consequências

Se o projeto virar produto multiusuário, a migração para ASP.NET Identity ou outro provider precisa ser planejada como trabalho futuro. O cookie de auth deve ser implementado de forma que essa troca não exija reescrever as páginas admin, só o mecanismo de login.

A senha em appsettings precisa ser tratada como segredo, ou seja, variável de ambiente em produção e nunca commitada em texto puro.
