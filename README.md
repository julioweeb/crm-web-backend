# CRM API

API REST de um CRM multiempresa, desenvolvida com ASP.NET Core 8, Entity Framework Core e PostgreSQL. O sistema possui interface web em Angular.

- **Demonstração:** [Abrir o CRM](https://crm-web-frontend-eta.vercel.app)
- **API:** [crm-api-lbhm.onrender.com](https://crm-api-lbhm.onrender.com)

## Conta de demonstração

Use a conta abaixo para explorar a aplicação:

- **Empresa:** Demonstração Enterprise
- **E-mail:** `demonstracao@demo.com`
- **Senha:** `User123@`
- **Perfil:** Admiinistrador

> Use somente dados fictícios e permissões limitadas nessa conta. Não publique uma conta administrativa nem credenciais pessoais.

## Funcionalidades

- Cadastro e administração de empresas e usuários.
- Gestão de clientes, contatos, interações e leads.
- Conversão de leads, oportunidades e funil de vendas.
- Tarefas, anotações, dashboard, busca e filtros.
- Controle de acesso por perfil e separação dos dados por empresa.

## Tecnologias

- C# e ASP.NET Core 8
- Entity Framework Core
- PostgreSQL
- Autenticação JWT e hash de senha com BCrypt
- Angular no front-end

## Perfis de acesso

- **Vendedor:** acessa registros atribuídos a ele dentro da empresa.
- **Gerente:** acessa os registros da empresa e pode excluir clientes.
- **Admin:** administra usuários e registros da própria empresa.
- **SuperAdmin:** administra empresas e seus Administradores iniciais - Dono do CRM.

Os dados do CRM são limitados à empresa associada ao usuário autenticado.

## Executar localmente

Pré-requisitos: .NET 8 SDK e acesso a uma instância PostgreSQL.

1. Clone este repositório e abra a pasta do projeto.
2. Configure a conexão com o banco usando User Secrets ou variáveis de ambiente. Não coloque credenciais reais em arquivos versionados.
3. Restaure e execute a API:

```powershell
dotnet restore
dotnet run
```

Também é possível abrir a solução no Visual Studio e iniciar pelo perfil **CrmApi**. Em ambiente local, o Swagger fica disponível em `/swagger` no endereço indicado pelo terminal.

## Segurança

- Não compartilhe senhas, chaves de assinatura ou connection strings.
- Mantenha segredos fora do GitHub e configure-os no serviço de hospedagem.
- Use dados fictícios e uma conta separada das contas administrativas na demonstração.

## Estado do projeto

O sistema está em desenvolvimento. Os testes funcionais realizados pela equipe estão documentados em `docs/CHECKLIST-TESTES.md`. O back-end ainda não possui testes automatizados nem migrations formais do Entity Framework Core.
