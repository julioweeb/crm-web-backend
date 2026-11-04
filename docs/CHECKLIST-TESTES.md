# Checklist manual do CRM

Use o Swagger (`/swagger`) e o front-end. Em testes multiempresa, mantenha duas sessões/logins separados: Admin da Empresa A e Admin da Empresa B. Use apenas dados de teste e remova-os depois. Não compartilhe senhas ou tokens.

## 6.1 Testes funcionais

| # | Teste | Resultado esperado |
|---|---|---|
| 1 | Cadastrar usuário com senha fraca (`abc`) | Rejeitado com mensagem de validação |
| 2 | Admin cria usuário válido pela API | Usuário fica associado à empresa do Admin |
| 3 | Login com senha errada | 401, mensagem genérica |
| 4 | Criar, editar e excluir cliente (como Admin) | Operação permitida e lista atualizada |
| 5 | Adicionar contato e interação a cliente | Aparecem apenas dentro do cliente selecionado |
| 6 | Criar lead e converter | Lead vira Convertido; cliente e oportunidade ficam na mesma empresa/responsável |
| 7 | Criar oportunidade e mover no funil | Estágio e totais atualizados |
| 8 | Mover oportunidade para Ganha | Probabilidade vira 100% e valor aparece no dashboard do mês |
| 9 | Criar tarefa vencida | Aparece como atrasada e no dashboard |
| 10 | Buscar e filtrar clientes/leads/oportunidades | Resultados e paginação corretos |
| 11 | Comparar dashboard aos registros da empresa | Totais coerentes e sem dados da outra empresa |

## 6.2 Isolamento entre empresas

Pré-requisito: Empresa A e Empresa B ativas, cada uma com um Admin e ao menos um registro de teste. O usuário de B deve ser associado a B antes de fazer login. Execute os mesmos passos trocando A por B.

| # | Teste | Resultado esperado |
|---|---|---|
| 1 | `GET /api/clientes`, `/api/leads`, `/api/oportunidades`, `/api/tarefas` como A e como B | Cada lista contém somente dados daquela empresa |
| 2 | Pesquisar por nome/e-mail/documento que exista apenas na outra empresa | Nenhum resultado da outra empresa |
| 3 | `GET` por ID de cliente, lead, oportunidade ou tarefa da outra empresa | 404 |
| 4 | `GET /api/usuarios` | Mostra apenas usuários da empresa do token |
| 5 | Abrir contatos/interações pelo ID de um cliente da outra empresa | 404; nenhum dado filho retornado |
| 6 | Criar oportunidade apontando para cliente da outra empresa | 400, cliente inválido |
| 7 | Criar/editar tarefa com cliente ou oportunidade da outra empresa | 400, vínculo inválido |
| 8 | Criar anotação ligada a cliente, lead ou oportunidade da outra empresa | 400, registro inválido |
| 9 | Converter lead como cada empresa | Cliente/oportunidade criados ficam apenas na empresa do lead |
| 10 | Consultar dashboard de A e B | Contagens, valores e tarefas não se misturam |
| 11 | Como A, tentar consultar/editar ID de usuário de B | 404 ou ausência na listagem |

**Já confirmado pelo usuário:** Admins de duas empresas; `GET /api/clientes` da Empresa B não mostrou o cliente da Empresa A. Os demais itens ainda precisam de execução manual.

## 6.3 Testes de autenticação e autorização

| # | Teste | Resultado esperado |
|---|---|---|
| 1 | Chamar endpoint protegido sem token ou com token expirado/adulterado | 401 |
| 2 | Desativar usuário e reutilizar o token antigo | 401 na próxima requisição autenticada |
| 3 | Alterar o perfil do usuário e reutilizar token antigo | 401; login novamente gera token com novo perfil |
| 4 | Alterar `EmpresaId` e reutilizar token antigo | 401; depois do novo login, token contém a nova empresa |
| 5 | Desativar a empresa e reutilizar token | 401 |
| 6 | Vendedor tenta excluir cliente | 403 |
| 7 | Mais de 10 logins incorretos em um minuto | 429 |
| 8 | Admin tenta remover o próprio perfil Admin/desativar a si mesmo | Bloqueado |
| 9 | Enviar `ResponsavelId`, `EmpresaId` ou perfil indevido em DTO de negócio | Campos não controlados pelo DTO não alteram o registro |
| 10 | Inspecionar `SenhaHash` | BCrypt; senha em texto puro nunca é retornada |

## 6.4 Responsividade

Use F12 → modo dispositivo no navegador.

| Dispositivo | Largura | O que verificar |
|---|---:|---|
| Computador | 1366 px | Menu lateral e tabelas utilizáveis |
| Tablet | 768 px | Menu compacto e formulários legíveis |
| Celular | 390 px | Listas/cartões e botões utilizáveis; funil pode rolar horizontalmente |
