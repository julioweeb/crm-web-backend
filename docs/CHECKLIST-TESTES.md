# Checklist de testes manuais

Use o Swagger (`/swagger`) para a API e o navegador para o front-end. Registre o resultado de cada item (OK / Falhou / Observação).

## 6.1 Testes funcionais
| # | Teste | Resultado esperado |
|---|---|---|
| 1 | Cadastrar usuário com senha fraca (`abc`) | Erro de validação |
| 2 | Cadastrar usuário válido | Entra no sistema como Vendedor |
| 3 | Login com senha errada | "E-mail ou senha inválidos." |
| 4 | Criar, editar e excluir cliente (como Admin) | Lista atualiza |
| 5 | Adicionar contato e interação ao cliente | Aparecem na aba correspondente |
| 6 | Criar lead e **converter** em cliente | Lead vira "Convertido"; cliente e oportunidade são criados |
| 7 | Criar oportunidade e mover no funil (arrastar e seletor) | Coluna e totais mudam |
| 8 | Mover para **Ganha** | Probabilidade vai a 100% e entra no "Ganho no mês" |
| 9 | Criar tarefa com data no passado | Aparece como "Atrasada" e no dashboard |
| 10 | Buscar clientes por nome/e-mail/empresa e filtrar por cidade | Resultados corretos; paginação funciona |
| 11 | Conferir indicadores do dashboard contra os dados | Números coerentes |

## 6.2 Testes de segurança
| # | Teste | Resultado esperado |
|---|---|---|
| 1 | Chamar `/api/clientes` sem token | 401 |
| 2 | Chamar com token expirado/adulterado | 401 e o front volta ao login |
| 3 | Vendedor A tenta `GET /api/clientes/{id}` de um cliente do Vendedor B | 404 |
| 4 | Vendedor tenta `DELETE /api/clientes/{id}` | 403 |
| 5 | Vendedor tenta `GET /api/usuarios` e abrir `/usuarios` no front | 403 / redireciona ao dashboard |
| 6 | Cadastro público enviando `"perfil":"Admin"` no JSON | Ignorado (continua Vendedor) |
| 7 | Mais de 10 logins errados em 1 minuto | 429 |
| 8 | Texto com `<script>` em nome/anotação | Aparece como texto, sem executar |
| 9 | Inspecionar o banco | Coluna `SenhaHash` com hash BCrypt, nunca a senha |
| 10 | Admin tenta rebaixar a si mesmo | Bloqueado |

## 6.3 Testes de responsividade
Use as ferramentas do navegador (F12 → modo dispositivo).
| Dispositivo | Largura | O que verificar |
|---|---|---|
| Computador | 1366 px | Menu lateral fixo, tabelas completas |
| Tablet | 768 px | Menu vira botão ☰; formulários em colunas |
| Celular | 390 px | Tabelas viram cartões; funil rola horizontalmente; botões clicáveis |
