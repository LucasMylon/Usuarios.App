## Visão Geral

Esta API é responsável pelo **cadastro, autenticação e gerenciamento de usuários**, desenvolvida com **ASP.NET** seguindo os princípios de uma arquitetura limpa e desacoplada. O projeto foi estruturado para oferecer segurança, escalabilidade e facilidade de manutenção.

A persistência de dados é realizada por meio do **Entity Framework**, utilizando o padrão ORM para o mapeamento das entidades de usuário, controle de migrations e abstração do acesso ao banco de dados relacional.

Há uma configuração Docker isolada para demonstração local.

O projeto inclui testes automatizados com xUnit e separa as responsabilidades entre camadas.

## Executar a demonstração

Com o Docker Desktop iniciado, execute na raiz do projeto, no PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-demo.ps1
```

Na primeira execução, o comando cria automaticamente credenciais locais em `.env.demo`, inicia os serviços e aplica as migrations. Não é necessário configurar Gmail, SQL Server, RabbitMQ ou chave JWT manualmente. A primeira inicialização pode demorar enquanto as imagens são baixadas e a API fica pronta.

- API e Swagger: http://localhost:5237/swagger

Crie um usuário no Swagger e autentique-se imediatamente para receber o JWT; a conta já é ativada e nenhum email é enviado. O fluxo de confirmação pode ser reativado apenas em desenvolvimento definindo `Desenvolvimento__IgnorarConfirmacaoEmail=false`.

Esta configuração é somente para demonstração local: não use seus segredos ou SMTP sem criptografia em produção. Mantenha `.env.demo` privado e não o exclua enquanto os volumes estiverem em uso. Veja o [guia de uso](docs/GUIA-DE-USO.md) para detalhes, integração com outra API e execução manual.

## Configuração manual

Nos perfis locais `http` e `IIS Express` do Visual Studio, a opção
`Desenvolvimento:IgnorarConfirmacaoEmail` está habilitada: novas contas são
criadas ativas, sem confirmação, SMTP ou RabbitMQ. Essa opção é permitida apenas
em desenvolvimento e não altera contas anteriormente criadas como inativas.
SQL Server e a chave JWT continuam necessários. Para testar o fluxo com email,
defina a opção como `false` no perfil usado ou no ambiente Docker.

Com a confirmação habilitada, configure `ConnectionStrings:UsuariosAppBD`,
`RabbitMQSettings`, `EmailSettings` e `AppSettings:BaseUrl`. A aplicação valida
essas configurações durante a inicialização e não inicia quando algum valor
obrigatório está ausente ou quando uma porta ou URL é inválida.

Credenciais e connection strings de ambientes compartilhados devem ser
fornecidas por User Secrets, variáveis de ambiente ou um cofre de segredos, e
não adicionadas a arquivos versionados.
