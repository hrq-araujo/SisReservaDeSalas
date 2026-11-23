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
dotnet build --no-restore
dotnet run --project src/SisReservaDeSalas.Web
```

Acesse http://localhost:5212. Para encerrar, pressione `Ctrl+C` no terminal.

## Estado da implementação

A primeira parte contém a solução .NET, a aplicação ASP.NET Core com Razor Pages,
uma página inicial em português e uma página de erro. Ainda não há conexão com
banco de dados, autenticação ou operações de salas e reservas.

O projeto web está em `src/SisReservaDeSalas.Web`. As páginas ficam em `Pages`
e os arquivos de estilo ficam em `wwwroot`. Os serviços de negócio e a persistência
serão acrescentados nas próximas partes, mantendo a arquitetura em camadas.

EF Core foi escolhido na Etapa 2 e será configurado na parte de persistência.
Arquivos gerados durante a compilação (`bin` e `obj`) não são versionados.
