# Usando o Supabase como banco de dados

O Supabase é um PostgreSQL gerenciado. Aqui ele guarda os dados; a API (ASP.NET Core) e o Angular ficam hospedados em outros lugares.

## 1. Connection string (Npgsql)

No painel: **Connect → Session pooler** (porta 5432). Converta para o formato do Npgsql:

```
Host=aws-0-REGIAO.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_PROJECT_REF;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true
```

- Use a porta **5432** (modo sessão). Evite a 6543 (modo transação / `pgbouncer=true`): é parâmetro do Prisma e dá problema com o EF Core.
- Sem os colchetes: `[YOUR-PASSWORD]` é só um marcador.
- Senha com `;` ou `=`: redefina para uma sem símbolos (Project Settings → Database).

## 2. Desenvolvimento local (user-secrets)

O projeto já tem `UserSecretsId`; não precisa de `dotnet user-secrets init`.

```bash
dotnet user-secrets set "ConnectionStrings:Padrao" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
dotnet run
```

Na primeira execução a API cria as tabelas do modelo (`CreateTables`) e ativa o RLS. Em um banco existente, `CreateTables` não altera o esquema; a inicialização aplica somente a adição idempotente de `Empresas` e da coluna opcional `Usuarios.EmpresaId`, com FK e índice. Confira em **Table Editor**.

## 3. Segurança: Row Level Security (RLS)

O Supabase expõe o schema `public` por uma API REST pública. Sem RLS, quem tiver a URL do projeto e a chave `anon` (que fica no front de qualquer app Supabase) poderia ler as tabelas.

A API já executa `ENABLE ROW LEVEL SECURITY` em todas as tabelas ao iniciar (veja `Program.cs`). Sem policies, a chave pública não enxerga nada; a API conecta como `postgres`, que ignora o RLS.

Para conferir/aplicar manualmente, cole no **SQL Editor**:

```sql
ALTER TABLE public."Usuarios"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Clientes"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Contatos"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Interacoes"    ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Leads"         ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Oportunidades" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Tarefas"       ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Anotacoes"     ENABLE ROW LEVEL SECURITY;

-- Verificação: todas devem aparecer com rowsecurity = true
SELECT tablename, rowsecurity FROM pg_tables WHERE schemaname = 'public';
```

## 4. Publicar a API (Docker)

O `Dockerfile` (na raiz) serve para Render, Railway, Fly.io etc. Variáveis de ambiente obrigatórias:

| Variável | Valor |
|---|---|
| `ConnectionStrings__Padrao` | a connection string do item 1 |
| `Jwt__Key` | texto aleatório com 32+ caracteres |
| `Seed__AdminEmail` / `Seed__AdminSenha` | administrador inicial (só na 1ª execução) |
| `Cors__Origens__0` | URL do front-end publicado (ex.: `https://meu-crm.vercel.app`) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Depois de publicar, no repositório do front-end, ajuste `API_URL` em `src/app/core/config.ts` para a URL da API (com `/api` no final) e publique o front em Vercel/Netlify.

## 5. Diferenças em relação ao SQL Server

- Busca com `ILike` (sem diferenciar maiúsculas/minúsculas), como era antes.
- Datas sempre em UTC (`Utilitarios.Utc`), exigência do Npgsql para `timestamptz`.
- Tabelas ficam com nomes entre aspas e com maiúscula (`"Clientes"`), por isso o SQL manual precisa das aspas.
