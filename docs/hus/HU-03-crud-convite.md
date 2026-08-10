# HU-03 — CRUD de Convite (grupo)

**Status:** Aprovada

## Contexto

HU-01 (modelagem) e HU-02 (auth admin + CRUD Evento) concluídas. Entidades `Evento`, `Convite` (FK EventoId, Nome, Token único) e `Convidado` (FK ConviteId, cascade) já existem, assim como `AppDbContext` e `Services/TokenGenerator`.

## Objetivo

CRUD de Convite (o "grupo" que recebe um link único), aninhado sob o Evento. Sem cadastro de integrantes (Convidado) ainda — isso é HU-04. Sem página pública `/c/{token}` — isso é HU-05.

## Escopo

### Rotas
`/Admin/Eventos/{eventoId:int}/Convites/{action?}`, resolvido via `@page` com template absoluto por página (sem convenção de rota customizada em Program.cs — ver seção "Resolução da rota aninhada" no relatório de entrega).

### Páginas em Pages/Admin/Eventos/Convites/
- **Index** — lista convites do evento (Nome, Nº integrantes, ações). Botão copiar link habilitado só com integrantes > 0. Editar/Detalhes/Excluir por linha. Botão "Novo convite". Breadcrumb.
- **Create** — form com só Nome. Gera token via `TokenGenerator`, persiste, redireciona pra Details.
- **Edit** — edita só Nome. Não expõe nem regenera token.
- **Details** — nome do grupo, seção Integrantes (placeholder pra HU-04), botão "Gerar link" (reveal, desabilitado sem integrantes), breadcrumb.
- **Delete** — confirmação, mostra contagem de integrantes que serão apagados junto (cascade).

### Partial e InputModel
- `_ConviteForm.cshtml` compartilhada entre Create/Edit (só campo Nome).
- `ConviteInputModel` (Nome, Required, StringLength). Sem Token nem EventoId (vem da rota).

### Validações
- Nome obrigatório.
- Nome único dentro do mesmo Evento (dois eventos diferentes podem repetir nome de grupo).
- Edit permite manter o próprio nome (exclui o próprio Id da checagem).

### Autorização
Herdada de `AuthorizeFolder("/Admin")` já configurado — sem mudança em Program.cs pra isso.

### Guarda de evento inexistente
`eventoId` da rota inexistente → 404 em todas as páginas.

## Fora do escopo
CRUD de Convidado (HU-04), página pública `/c/{token}` (HU-05), regenerar token, envio de WhatsApp.

## Testes
Testes unitários sobre a lógica de unicidade de nome e geração de token, extraída pra um `ConviteService` testável (ver relatório de entrega pra detalhes de arquitetura).

## Critérios de aceite
- `dotnet build` sem warnings novos.
- `dotnet test` — todos os testes passando.
- Sem migration nova (schema já existe desde HU-01).
- Sem dependência NuGet nova.
- Botão "Gerar link" bloqueado sem integrantes, mesmo com token já existente no banco.
- Sem código morto, sem TODO deixado no meio.
