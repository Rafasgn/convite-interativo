# HU-01 — Modelagem inicial do domínio e persistência

**Status:** Concluída

## Objetivo

Modelagem inicial do domínio e persistência. Ao fim desta HU, o sistema tem as entidades definidas, o AppDbContext configurado, a migration inicial gerada e a connection string apontando pra SQLite em data/. Ainda sem CRUD, sem UI, sem lógica de negócio.

## Escopo

### Entidades em Data/Entities/

**Evento**

- Id (int, PK, autoincrement)
- Nome (string, obrigatório)
- Slug (string, obrigatório, único — usado em URLs internas do admin; ex.: "zayan-1-ano")
- DataHora (DateTime, obrigatório — data e hora do evento)
- Endereco (string, obrigatório — endereço textual completo, mostrado na tela de dados)
- LinkMapa (string, opcional — URL pronta do Google Maps, colada pelo admin. Se null, a página monta google.com/maps/search/?api=1&query={Endereco urlencoded}. Se preenchido, usa direto)
- TemaSlug (string, obrigatório — qual pasta em themes/ usar; ex.: "pequeno-principe")
- DataCriacao (DateTime)
- DataAtualizacao (DateTime)
- Relação: 1 Evento → N Convites

**Convite**

- Id (int, PK, autoincrement)
- EventoId (int, FK obrigatória)
- Nome (string, obrigatório — identificador interno do grupo no admin; ex.: "Família Silva", "Vó Carminha e Dinda Nathalia")
- Token (string, obrigatório, único, índice — token opaco Base64Url conforme ADR 0005, gerado por TokenGenerator service antes do SaveChanges)
- DataCriacao (DateTime)
- DataAtualizacao (DateTime)
- Relação: N Convites → 1 Evento; 1 Convite → N Convidados

**Convidado**

- Id (int, PK, autoincrement)
- ConviteId (int, FK obrigatória — cascade delete: apagar Convite apaga Convidados)
- Nome (string, obrigatório)
- Sobrenome (string, opcional — nem todo convidado tem sobrenome próprio, ex.: "Vó Carminha")
- Status (enum StatusConfirmacao { SemResposta, Confirmado, NaoVai }, default SemResposta, persistido no banco como string — não como int — conforme boa prática de legibilidade em SQLite)
- DataConfirmacao (DateTime, opcional — preenchida quando Status muda pra Confirmado ou NaoVai; atualizada a cada mudança, conforme ADR 0006)
- DataCriacao (DateTime)
- DataAtualizacao (DateTime)

### Data/AppDbContext.cs

Três DbSets (Evento, Convite, Convidado) com configuração via Fluent API (não Data Annotations — mais fácil de manter em um único lugar):

- Índice único em Evento.Slug
- Índice único em Convite.Token
- Cascade delete em Convite→Convidados
- Enum StatusConfirmacao mapeado como string (via .HasConversion<string>())
- Sem SeedData ainda — dados de teste virão em HU futura

### Services/TokenGenerator.cs

Implementação do gerador de token conforme ADR 0005: 12 bytes de RandomNumberGenerator.GetBytes() (NÃO System.Random — precisa ser criptograficamente seguro), codificados em Base64Url. Registrado no DI como singleton.

### Program.cs atualizado

- Registro do AppDbContext com SQLite, connection string lida de appsettings
- Registro do TokenGenerator no DI
- Nada de auth ainda — vem em HU futura

### appsettings.json

Nova seção ConnectionStrings.Default apontando pra data/convite.db (path relativo à ContentRoot, funciona igual em dev e no container conforme ADR 0002).

### Migration inicial

Comando: dotnet ef migrations add InicialModelagem (pede aprovação antes de rodar esse comando). Arquivo resultante em Data/Migrations/. Não rodar dotnet ef database update — não criamos o arquivo .db de dev automaticamente, isso fica pra quando o CRUD existir.

## Fora do escopo desta HU

- CRUD de nenhuma entidade
- Auth admin (HU futura)
- UI (Razor Pages continuam sendo os defaults do template)
- Página pública /Convite/{token}
- Exportação PDF
- Testes de integração com banco (só testes unitários do TokenGenerator)

## Critérios de aceite

- dotnet build compila com 0 warnings e 0 erros
- Migration InicialModelagem existe em Data/Migrations/ e tem Up/Down coerentes
- Teste unitário do TokenGenerator valida: (a) gera string Base64Url válida, (b) 12 bytes → 16 caracteres antes de padding, (c) tokens gerados em sequência são diferentes
