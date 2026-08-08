# Convite Interativo

Plataforma de convites digitais interativos com animação temática, link único por grupo de convidados, confirmação de presença e painel administrativo.

Primeiro tema: Pequeno Príncipe.

## Estrutura
- `docs/` — documentação, backlog, decisões
- `src/` — código-fonte (.NET)
- `assets/` — imagens, ilustrações, recursos compartilhados entre temas
- `themes/` — temas plugáveis (assets + config)

## Status
MVP em desenvolvimento. Ver `docs/` para roadmap e HUs.

## Senha do admin

O hash em `appsettings.json` (`Admin:PasswordHash`) é só o placeholder de dev (`dev123`). Nunca coloque a senha real em texto puro no repo.

Pra gerar um novo hash (dev ou produção):

```
dotnet run --project tools/HashGen -- <senha>
```

Em produção, sobrescreva via variável de ambiente `Admin__PasswordHash` — não edite o `appsettings.json` commitado.
