# Usando o Supabase como banco de dados

O Supabase fornece o PostgreSQL. Aqui ficam os dados; a API ASP.NET Core e o Angular são executados separadamente.

## 1. Connection string

No Supabase, abra **Connect → Session pooler** e use a porta 5432. Configure o segredo local na pasta do projeto:

```bash
dotnet user-secrets set "ConnectionStrings:Padrao" "Host=aws-0-REGIAO.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_PROJECT_REF;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true"
```

- Substitua os marcadores pelos dados do seu projeto.
- Não cole a senha no código, no Git ou em mensagens.
- O projeto já tem `UserSecretsId`; não precisa executar `dotnet user-secrets init`.
- Use SSL e a porta 5432 do Session pooler. Evite a porta 6543/transação com EF Core.

## 2. Inicialização do banco

Ao iniciar, a API:

1. cria as tabelas do modelo se ainda não existir `Usuarios`;
2. garante, de forma idempotente, a tabela `Empresas`, a coluna opcional `Usuarios.EmpresaId`, o índice e a FK;
3. habilita RLS nas tabelas, incluindo `Empresas`;
4. se ainda não houver usuário e o seed do Admin estiver configurado, cria ou usa a empresa `Seed:EmpresaNome` e associa o Admin a ela.

A inicialização preserva os dados existentes. Ela não atribui automaticamente uma empresa aos usuários antigos sem `EmpresaId`. Antes do login, confirme que cada usuário está vinculado à empresa correta.

### Conferir empresas e usuários

No **SQL Editor**, consulte:

```sql
SELECT "Id", "Nome", "Ativo" FROM public."Empresas" ORDER BY "Id";
SELECT "Id", "Email", "EmpresaId" FROM public."Usuarios" ORDER BY "Id";
```

Não associe todos os usuários sem empresa à mesma empresa sem antes conferir a qual organização pertencem. Exemplo, depois de confirmar o ID correto:

```sql
UPDATE public."Usuarios"
SET "EmpresaId" = 1
WHERE "Email" = 'usuario@exemplo.com';
```

Para criar uma empresa enquanto ainda não há tela no CRM, use o **Table Editor** (`Empresas → Insert row`) ou SQL:

```sql
INSERT INTO public."Empresas" ("Nome", "Ativo", "CriadoEm")
VALUES ('Nome da empresa', TRUE, NOW())
RETURNING "Id";
```

Guarde o ID retornado e associe os usuários apropriados a essa empresa. Não altere `EmpresaId` de usuários reais sem confirmar a migração de responsabilidade pelos dados: os registros do CRM pertencem à empresa do usuário responsável.

## 3. Row Level Security (RLS)

O schema `public` pode ser acessado pela API REST do Supabase. RLS sem policies bloqueia o acesso com a chave `anon`. A API do CRM conecta como `postgres` (dono/bypass de RLS), então a separação entre empresas nas chamadas à API é aplicada pelos controllers e pelo token validado no backend. Não crie policies abertas para contornar erros.

A API habilita RLS ao iniciar. Para conferir manualmente no SQL Editor:

```sql
ALTER TABLE public."Empresas"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Usuarios"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Clientes"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Contatos"      ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Interacoes"    ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Leads"         ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Oportunidades" ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Tarefas"       ENABLE ROW LEVEL SECURITY;
ALTER TABLE public."Anotacoes"     ENABLE ROW LEVEL SECURITY;

SELECT tablename, rowsecurity
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;
```

As tabelas do CRM devem mostrar `rowsecurity = true`.

## 4. Publicar a API (Docker)

Configure estas variáveis no serviço de hospedagem:

| Variável | Uso |
|---|---|
| `ConnectionStrings__Padrao` | Connection string do item 1 |
| `Jwt__Key` | Chave aleatória com pelo menos 32 caracteres |
| `Jwt__Issuer` / `Jwt__Audience` | Valores usados para emitir e validar JWT |
| `Seed__EmpresaNome` | Nome da empresa inicial se o seed criar o primeiro Admin |
| `Seed__AdminEmail` / `Seed__AdminSenha` | Admin inicial, usado somente quando não há usuários |
| `Cors__Origens__0` | Origem do front-end publicado |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

Em produção, configure segredos fortes; os valores padrão de desenvolvimento não devem ser usados. Depois, ajuste `API_URL` no front-end para apontar à API publicada.

## 5. Observações do PostgreSQL

- Busca usa `ILike`, sem diferenciar maiúsculas/minúsculas.
- Datas são normalizadas para UTC por `Utilitarios.Utc`.
- Tabelas e colunas mantêm nomes com maiúsculas; use aspas nos comandos SQL.
- Ainda não há migrations formais. O SQL incremental desta etapa cobre apenas empresa e vínculo usuário–empresa; planeje baseline antes de adotar migrations em produção.
