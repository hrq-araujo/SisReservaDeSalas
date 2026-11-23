# Sistema Web de Reserva de Salas Universitárias

Projeto desenvolvido para a disciplina de Construção de Software da Universidade Estadual de Maringá (UEM).

## Sobre o projeto

O projeto propõe o desenvolvimento de um sistema web para gerenciamento de reservas de salas universitárias.

Como cenário inicial, considera-se que o controle dos agendamentos é realizado por meio de planilhas eletrônicas. O sistema busca centralizar esse processo, facilitando a consulta de disponibilidade das salas e evitando conflitos entre reservas.

## Objetivo

Permitir que usuários consultem salas e sua disponibilidade, realizem e cancelem reservas, enquanto usuários administradores podem gerenciar as salas disponíveis no sistema.

## Funcionalidades planejadas

- Autenticação de usuários;
- Visualização das salas cadastradas;
- Consulta de disponibilidade por data e horário;
- Realização de reservas;
- Consulta das reservas realizadas;
- Cancelamento de reservas;
- Cadastro, edição, ativação e desativação de salas;
- Consulta administrativa das reservas;
- Prevenção de conflitos de horário.

## Arquitetura

O sistema utilizará inicialmente uma arquitetura monolítica em camadas, dividida em:

- Interface Web;
- Regras de Negócio;
- Persistência;
- Banco de Dados.

Essa organização busca manter o projeto simples e separar as principais responsabilidades da aplicação.

## Desenvolvimento

O projeto será desenvolvido de forma incremental ao longo das etapas da disciplina.

Na primeira etapa foram definidos o problema, o escopo, as histórias de usuário, o Product Backlog, a arquitetura inicial e o planejamento da primeira sprint.

Os arquivos de implementação e demais componentes do projeto serão adicionados ao repositório durante as próximas etapas do desenvolvimento.

## Executar a aplicação

Pré-requisito: SDK .NET 10 instalado.

Na pasta do repositório, execute:

```shell
dotnet restore
dotnet tool restore
dotnet build --no-restore
dotnet run --project src/SisReservaDeSalas.Web
```

Acesse http://localhost:5212. Para encerrar, pressione `Ctrl+C` no terminal.

## Estado da implementação

A aplicação contém a estrutura web, a página inicial e a configuração da
persistência com EF Core e PostgreSQL. A entidade `Sala` e a migration
`CriarTabelaSalas` estão disponíveis. O login, a saída e os perfis de usuário
e administrador usam ASP.NET Core Identity, com a migration
`AdicionarAutenticacao`. Administradores podem cadastrar salas; usuários
autenticados podem listar salas ativas e consultar seus detalhes. Ainda não há
operações de reservas, edição ou desativação de salas.

O projeto web está em `src/SisReservaDeSalas.Web`. As páginas ficam em `Pages`
e os arquivos de estilo ficam em `wwwroot`. As entidades ficam em `Models`,
e o contexto e as migrations ficam em `Data`. Os serviços de negócio serão
acrescentados junto das funcionalidades, mantendo a arquitetura em camadas.

Arquivos gerados durante a compilação (`bin` e `obj`) não são versionados.

## Configurar o banco de dados

É necessário ter um servidor PostgreSQL em execução para aplicar as migrations
e usar as operações de dados. A página inicial funciona sem o banco.

O endereço padrão em `appsettings.json` aponta para `localhost:5432`, banco
`sis_reserva_de_salas` e usuário `postgres`. Não há senha nesse arquivo.
Para desenvolvimento, configure a conexão completa com User Secrets,
substituindo `SUA_SENHA` pela senha do seu PostgreSQL:

```shell
dotnet user-secrets set "ConnectionStrings:ReservaDb" "Host=localhost;Port=5432;Database=sis_reserva_de_salas;Username=postgres;Password=SUA_SENHA" --project src/SisReservaDeSalas.Web
dotnet ef database update --project src/SisReservaDeSalas.Web -- --environment Development
```

A senha é salva fora do repositório. O usuário do banco deve poder criar o banco
de desenvolvimento; alternativamente, crie o banco antes com um administrador
e conceda ao usuário da aplicação permissão para criar suas tabelas.

As migrations são aplicadas explicitamente pelo comando acima; a aplicação
não cria nem altera tabelas automaticamente ao iniciar.

Para conferir as alterações de esquema sem conectar ao banco:

```shell
dotnet ef migrations script --project src/SisReservaDeSalas.Web
dotnet ef migrations has-pending-model-changes --project src/SisReservaDeSalas.Web
```

## Tecnologias de persistência

- EF Core 10.0.15 para o acesso aos dados e as migrations.
- Npgsql.EntityFrameworkCore.PostgreSQL 10.0.4 como provedor do PostgreSQL.
- Ferramenta local `dotnet-ef` 10.0.15, versionada em `dotnet-tools.json`.

O PostgreSQL complementa a decisão da Etapa 2, que havia deixado o banco em
aberto. Seu suporte a restrições de intervalos será utilizado na implementação
das reservas para impedir sobreposições na mesma sala. Essa regra ainda não foi
implementada nesta parte.

A tabela `Salas` possui identificação (até 100 caracteres), localização (até 150),
capacidade maior que zero, recursos opcionais (até 500) e situação ativa/inativa.
As validações dos formulários serão implementadas junto das telas.

Referências: [provedor Npgsql](https://www.npgsql.org/efcore/),
[ferramentas EF Core](https://learn.microsoft.com/ef/core/cli/dotnet) e
[restrições de intervalos no PostgreSQL](https://www.postgresql.org/docs/current/rangetypes.html#RANGETYPES-CONSTRAINT).

## Login e usuários iniciais

O login usa e-mail e senha de uma conta previamente cadastrada. Não há cadastro
público de contas. O Identity armazena hashes de senhas e mantém os perfis
`Usuario` e `Administrador`. Após cinco tentativas incorretas, a conta fica
bloqueada por cinco minutos. A sessão não é persistente e expira após 30 minutos
de inatividade.

Depois de configurar a conexão e aplicar as migrations, configure as duas contas
iniciais. Substitua os e-mails de exemplo e os valores `SUA_SENHA_FORTE` pelos
dados que deseja usar. As senhas precisam ter pelo menos oito caracteres,
incluindo letra maiúscula, minúscula, número e símbolo.

```shell
dotnet user-secrets set "UsuariosIniciais:Administrador:Email" "admin@exemplo.com" --project src/SisReservaDeSalas.Web
dotnet user-secrets set "UsuariosIniciais:Administrador:Senha" "SUA_SENHA_FORTE" --project src/SisReservaDeSalas.Web
dotnet user-secrets set "UsuariosIniciais:Usuario:Email" "usuario@exemplo.com" --project src/SisReservaDeSalas.Web
dotnet user-secrets set "UsuariosIniciais:Usuario:Senha" "SUA_SENHA_FORTE" --project src/SisReservaDeSalas.Web
dotnet run --project src/SisReservaDeSalas.Web -- --preparar-usuarios
```

O último comando cria as contas e os perfis e encerra a aplicação. Ele não aplica
migrations. Pode ser repetido sem duplicar as contas e não altera a senha de
uma conta existente. Use e-mails diferentes para os dois perfis.

Para abrir o sistema, execute novamente sem `--preparar-usuarios` e clique em
**Entrar**. A página **Minha conta** exige autenticação e mostra o perfil.
A saída usa um formulário protegido contra falsificação de solicitações.

## Testes

```shell
dotnet test
```

Os testes de integração verificam acesso sem login, entrada com cada perfil,
saída, senha inválida, bloqueio por tentativas, recusa de redirecionamento externo
e proteção contra envio de formulários sem token. Também verificam o cadastro
de salas, a rejeição de dados inválidos, a restrição do cadastro ao administrador
e a consulta apenas de salas ativas.
Usam um SQLite temporário em memória, sem precisar configurar contas ou alterar
o PostgreSQL. Eles não verificam a aplicação das migrations no PostgreSQL nem
a concorrência de reservas, que será testada quando essa funcionalidade existir.

Referência de autenticação: [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity).

## Cadastro e consulta de salas (US07 e US02)

Após preparar o banco e as contas, entre como administrador, abra **Salas** e
clique em **Cadastrar sala**. Informe identificação, localização e capacidade
maior que zero. Os recursos são opcionais. A sala é criada ativa e o sistema
abre seus detalhes após salvar.

Contas do perfil usuário podem consultar as salas e seus detalhes, mas não
podem acessar o cadastro, inclusive por uma solicitação direta ao servidor.
Salas inativas não aparecem na listagem nem na consulta de detalhes.

Os formulários enviam dados ao `SalaService`, que valida o cadastro e usa
o `ReservaDbContext` para persistir as informações. As páginas não acessam
o banco diretamente. Esta parte utiliza a tabela já criada pela migration
`CriarTabelaSalas`, sem mudanças no esquema do banco.
