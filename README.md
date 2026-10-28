# CRM – API (back-end)

API REST em **ASP.NET Core 8** com **PostgreSQL (Supabase)**, autenticação **JWT** e perfis de acesso.
O front-end fica em um repositório separado (`crm-frontend`, Angular).

## Funcionalidades
Usuários e permissões · clientes · contatos · histórico de interações · leads (com conversão em cliente) · oportunidades e funil de vendas · tarefas e agendamento · anotações · dashboard · busca, filtros e paginação.

### Perfis de acesso
- **Vendedor**: vê e edita apenas os registros que ele criou.
- **Gerente**: vê tudo e pode excluir clientes.
- **Administrador**: tudo isso + gerenciar usuários.

## Como rodar
Pré-requisitos: .NET 8 SDK e um projeto no Supabase.

1. Abra `appsettings.Development.json` e troque `COLOQUE_SUA_SENHA_AQUI` pela senha do banco no Supabase.
   (Este arquivo está no `.gitignore`. Ao clonar do GitHub, copie `appsettings.Development.example.json` para `appsettings.Development.json` e edite.)
2. Rode:

```bash
dotnet restore
dotnet run        # http://localhost:5080  (Swagger em /swagger)
```

- As tabelas são criadas automaticamente e o admin inicial é `admin@crm.local` / `Admin@12345` (**troque a senha**).
- Em Development, a chave JWT e o admin têm valores padrão caso não estejam configurados.
- Detalhes da connection string, RLS e deploy: [`docs/SUPABASE.md`](docs/SUPABASE.md)

## CORS (integração com o front-end)
A API só aceita chamadas das origens listadas em `Cors:Origens` (padrão `http://localhost:4200`).
Ao publicar o front, adicione a URL dele, por exemplo via variável de ambiente `Cors__Origens__0=https://meu-crm.vercel.app`.

## Segurança
- Senhas com BCrypt; JWT validado (emissor, audiência, validade, assinatura).
- Limite de 10 tentativas/minuto por IP em login e cadastro.
- DTOs de entrada (evita *mass assignment*); responsável do registro vem do token.
- Filtro de visibilidade por perfil em todas as consultas.
- RLS ativado nas tabelas do Supabase (bloqueia a API REST pública).
- Segredos nunca no repositório: user-secrets ou variáveis de ambiente.

## Testes
Checklist manual (funcionais, segurança, responsividade): [`docs/CHECKLIST-TESTES.md`](docs/CHECKLIST-TESTES.md).

## Limitações conhecidas
- Usa `EnsureCreated()`; para evoluir o banco em produção, adote migrations.
- Sem *refresh token* (o JWT expira em 8 h).
- Sem testes automatizados no back-end.
