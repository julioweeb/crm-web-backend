# CRM – API (back-end)

API REST em **ASP.NET Core 8**, com **EF Core 8**, **PostgreSQL/Supabase** e autenticação **JWT**. O front-end Angular fica em repositório separado (`crm-frontend`).

## Funcionalidades

Usuários e permissões, empresas, clientes, contatos, histórico de interações, leads e conversão em cliente, oportunidades e funil, tarefas, anotações, dashboard, busca, filtros e paginação.

### Perfis de acesso

- **Vendedor**: acessa apenas registros próprios dentro da empresa.
- **Gerente**: acessa registros da empresa e pode excluir clientes.
- **Admin**: acessa registros da empresa e gerencia usuários dessa empresa.

O escopo da empresa é obtido do usuário autenticado. O SuperAdmin cadastra empresas e o primeiro Admin pela tela Empresas; o vínculo é automático. Admins comuns continuam limitados à própria empresa e cadastram os demais usuários por Usuários. O SuperAdmin também só acessa dados de CRM da empresa vinculada ao seu próprio usuário.

## Como rodar localmente

Pré-requisitos: .NET 8 SDK e um projeto no Supabase. Configure a connection string com **User Secrets** (na pasta do projeto):

```bash
dotnet user-secrets set "ConnectionStrings:Padrao" "Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"
```

Consulte [`docs/SUPABASE.md`](docs/SUPABASE.md) para montar a connection string. Não coloque a senha no repositório nem a envie em mensagens.

No Visual Studio, selecione o perfil **CrmApi** e inicie com **F5**. Ou, no terminal:

```bash
dotnet restore
dotnet run
```

O Swagger fica em `/swagger` no endereço exibido pelo projeto (localmente, normalmente `http://localhost:5080/swagger`). Em Development, há valores padrão para a chave JWT e para o Admin inicial; troque a senha padrão antes de usar dados reais.

## Banco de dados e multiempresa

Na primeira execução em banco vazio, `Program.cs` cria as tabelas. A inicialização também aplica SQL idempotente para garantir `Empresas`, `Usuarios.EmpresaId`, índice e chave estrangeira, e habilita RLS nas tabelas, inclusive `Empresas`. Em banco existente, os usuários não são transferidos automaticamente: confira o vínculo `EmpresaId` antes do login.

O primeiro Admin sem usuários é associado à empresa definida por Seed:EmpresaNome (em Development há o padrão Empresa inicial). Em Development, SuperAdmin:Email assume o e-mail do Admin inicial se não estiver configurado; em produção, defina SuperAdmin__Email para uma conta ativa já vinculada a uma empresa. Essa conta ganha o papel SuperAdmin sem alteração manual no Supabase.

O projeto ainda não usa migrations formais do EF Core. Para futuras mudanças estruturais, planeje migrations com baseline para o banco atual. Veja [`docs/SUPABASE.md`](docs/SUPABASE.md).

## Segurança

- Senhas armazenadas com BCrypt; JWT valida assinatura, emissor, audiência e validade.
- A cada requisição autenticada, a API confere no banco se o usuário está ativo, continua associado à empresa do token, se a empresa está ativa e se o perfil não mudou. Isso adiciona uma consulta ao banco por requisição autenticada.
- Login limitado a 10 tentativas por minuto por IP.
- DTOs de entrada evitam alteração de campos de responsável/perfil por *mass assignment*.
- Consultas e vínculos da API respeitam empresa e perfil. A revisão estática dos endpoints está registrada no checklist; o teste funcional confirmado até agora cobriu a listagem de clientes entre duas empresas.
- RLS sem policies bloqueia acesso às tabelas pelo endpoint REST público do Supabase. A API conecta como `postgres` e aplica o isolamento por empresa na própria aplicação; RLS não substitui essa regra da API.
- Mantenha segredos em User Secrets ou variáveis de ambiente.

## Checklist

Testes manuais funcionais e de isolamento: [`docs/CHECKLIST-TESTES.md`](docs/CHECKLIST-TESTES.md).

## Limitações conhecidas

- Sem migrations formais e sem testes automatizados de back-end.
- Sem refresh token; validade padrão do JWT é 8 horas, mas usuário/empresa/perfil desativados ou alterados são recusados na validação.
- A conta SuperAdmin lista/cria empresas; cada empresa nova recebe um Admin inicial. Transferir uma conta existente entre empresas continua sendo uma operação administrativa separada.
