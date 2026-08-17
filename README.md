## Visão Geral

Esta API é responsável pelo **cadastro, autenticação e gerenciamento de usuários**, desenvolvida com **ASP.NET** seguindo os princípios de uma arquitetura limpa e desacoplada. O projeto foi estruturado para oferecer segurança, escalabilidade e facilidade de manutenção.

A persistência de dados é realizada por meio do **Entity Framework**, utilizando o padrão ORM para o mapeamento das entidades de usuário, controle de migrations e abstração do acesso ao banco de dados relacional.

A aplicação é totalmente **containerizada com Docker**, permitindo a padronização dos ambientes de desenvolvimento, testes e produção, além de facilitar o deploy e a integração contínua.

A qualidade e confiabilidade do código são garantidas através de **testes automatizados com xUnit**, cobrindo regras de negócio, validações e fluxos críticos de autenticação, assegurando o correto funcionamento da API e prevenindo regressões.

O projeto segue princípios de **SOLID**, com clara separação de responsabilidades entre camadas, priorizando testabilidade, segurança e manutenibilidade.

## Configuração obrigatória

Antes de iniciar a API, configure `ConnectionStrings:UsuariosAppBD`,
`RabbitMQSettings`, `EmailSettings` e `AppSettings:BaseUrl`. A aplicação valida
essas configurações durante a inicialização e não inicia quando algum valor
obrigatório está ausente ou quando uma porta ou URL é inválida.

Credenciais e connection strings de ambientes compartilhados devem ser
fornecidas por User Secrets, variáveis de ambiente ou um cofre de segredos, e
não adicionadas a arquivos versionados.
