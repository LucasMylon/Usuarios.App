# Guia de uso do Usuarios.App

## 1. Objetivo

O Usuarios.App é uma API ASP.NET Core para cadastro, confirmação de e-mail e autenticação de usuários. Ela utiliza:

- SQL Server para persistência;
- Entity Framework Core para acesso ao banco e migrations;
- RabbitMQ para publicar o evento de criação do usuário;
- Gmail SMTP para enviar o link de confirmação;
- JWT para autenticar chamadas a endpoints protegidos.

## 2. Pré-requisitos

Instale antes de começar:

- .NET SDK 8;
- Docker Desktop;
- ferramenta `dotnet-ef`;
- uma conta Gmail com verificação em duas etapas e senha de app, caso queira testar o envio real de e-mail.

Confirme as instalações:

```powershell
dotnet --version
docker --version
dotnet ef --version
```

Se `dotnet-ef` não estiver instalado:

```powershell
dotnet tool install --global dotnet-ef
```

## 3. Configurar os segredos locais

Execute os comandos na raiz do repositório. Nunca coloque senhas, chaves JWT ou a connection string real em arquivos versionados.

### 3.1 SQL Server usado pelo Docker

Crie um arquivo `.env` na raiz do projeto:

```dotenv
SQLSERVER_SA_PASSWORD=<SENHA_FORTE_DO_SQL_SERVER>
```

O `.env` já está ignorado pelo Git. A senha deve atender à política de complexidade do SQL Server.

### 3.2 Connection string

Inicialize os User Secrets, caso necessário:

```powershell
dotnet user-secrets init --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Cadastre a connection string substituindo o placeholder pela mesma senha usada no `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:UsuariosAppBD" "Server=localhost,1434;Initial Catalog=UsuariosAppBD;User ID=sa;Password=<SENHA_DO_SQL_SERVER>;TrustServerCertificate=True" --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

### 3.3 Chave JWT

Gere uma chave aleatória de 256 bits no PowerShell:

```powershell
$bytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $rng.GetBytes($bytes)
    [Convert]::ToBase64String($bytes)
}
finally {
    $rng.Dispose()
}
```

Copie o resultado e salve-o nos User Secrets:

```powershell
dotnet user-secrets set "JwtSettings:SecretKey" "<CHAVE_BASE64_GERADA>" --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

O emissor, o público e o tempo de expiração ficam no `appsettings.json` porque não são segredos.

### 3.4 Gmail SMTP

Salve o endereço Gmail e a senha de app nos User Secrets:

```powershell
dotnet user-secrets set "EmailSettings:User" "<SEU_EMAIL_GMAIL>" --project .\Usuarios.App.API\Usuarios.App.API.csproj
dotnet user-secrets set "EmailSettings:Password" "<SENHA_DE_APP_DO_GMAIL>" --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Não use a senha normal da conta Google. O servidor e a porta SMTP públicos já estão configurados no `appsettings.json`.

Para conferir somente os nomes das configurações cadastradas:

```powershell
dotnet user-secrets list --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Evite compartilhar a saída, pois ela mostra os valores dos segredos.

## 4. Iniciar a infraestrutura

Abra o Docker Desktop e execute:

```powershell
docker compose up -d
docker compose ps
```

Serviços locais:

| Serviço | Endereço |
|---|---|
| SQL Server | `localhost,1434` |
| RabbitMQ | `localhost:5672` |
| RabbitMQ Management | `http://localhost:15672` |

O usuário e a senha padrão do RabbitMQ estão no `docker-compose.yml` e devem ser usados somente no ambiente local.

Para encerrar os containers sem excluir seus volumes:

```powershell
docker compose stop
```

## 5. Preparar o banco de dados

Confira as migrations:

```powershell
dotnet ef migrations list --project .\UsuariosApp.Infra.Data\UsuariosApp.Infra.Data.csproj --startup-project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Aplique apenas migrations pendentes:

```powershell
dotnet ef database update --project .\UsuariosApp.Infra.Data\UsuariosApp.Infra.Data.csproj --startup-project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Esse comando altera o banco. Não é necessário executá-lo novamente quando todas as migrations já aparecem como aplicadas.

## 6. Compilar e executar

Restaure e compile a solução:

```powershell
dotnet restore .\Usuarios.App.sln
dotnet build .\Usuarios.App.sln --no-restore
```

Execute a API:

```powershell
dotnet run --project .\Usuarios.App.API\Usuarios.App.API.csproj
```

No ambiente de desenvolvimento, abra:

```text
http://localhost:5236/swagger
```

A API valida as configurações ao iniciar. Se um segredo obrigatório estiver ausente ou inválido, a inicialização será interrompida com uma mensagem indicando o nome da configuração.

## 7. Fluxo de uso da API

### 7.1 Criar uma conta

Endpoint público:

```http
POST /api/usuario/Criar
```

Exemplo com `curl.exe`:

```powershell
curl.exe -i -X POST "http://localhost:5236/api/usuario/Criar" `
  -H "Content-Type: application/json" `
  -d '{"nome":"Usuario Teste","email":"<EMAIL_DE_TESTE>","senha":"<SENHA_DE_TESTE_FORTE>"}'
```

Regras atuais:

- o nome deve possuir entre 8 e 150 caracteres;
- o e-mail deve ser válido e ainda não cadastrado;
- a senha deve conter letra maiúscula, letra minúscula, número e caractere especial.

Após salvar o usuário como inativo, a API publica um evento no RabbitMQ. O `EmailConsumer` lê esse evento e envia o link de confirmação pelo Gmail SMTP.

Use apenas um endereço de e-mail de teste que pertença a você.

### 7.2 Confirmar o e-mail

O link recebido usa este endpoint público:

```http
GET /api/usuario/confirmar-email?token=<TOKEN>
```

O comportamento esperado é ativar a conta e invalidar o token, impedindo sua reutilização.

Após uma confirmação bem-sucedida, o campo `Ativo` fica com valor `true` e `EmailConfirmacaoToken` fica `NULL`. Uma segunda tentativa com o mesmo link deve retornar erro, pois o token já foi invalidado.

### 7.3 Autenticar

Endpoint público:

```http
POST /api/usuario/autenticar
```

```powershell
curl.exe -i -X POST "http://localhost:5236/api/usuario/autenticar" `
  -H "Content-Type: application/json" `
  -d '{"email":"<EMAIL_CADASTRADO>","senha":"<SENHA_CADASTRADA>"}'
```

Uma autenticação válida devolve `AccessToken`. O JWT expira após o período configurado em `JwtSettings:ExpirationMinutes`.

Somente usuários ativos podem autenticar. Antes da confirmação do e-mail, a tentativa deve retornar `401 Unauthorized` com uma mensagem genérica, sem revelar se a conta existe ou se ainda está inativa.

### 7.4 Consultar a própria conta

Endpoint protegido:

```http
GET /api/usuario/Minha-Conta
Authorization: Bearer <JWT>
```

```powershell
curl.exe -i "http://localhost:5236/api/usuario/Minha-Conta" `
  -H "Authorization: Bearer <JWT_RECEBIDO_NO_LOGIN>"
```

Sem o cabeçalho ou com um token inválido/expirado, a resposta esperada é `401 Unauthorized`.

No Swagger, clique em **Authorize** e informe:

```text
Bearer <JWT_RECEBIDO_NO_LOGIN>
```

## 8. Executar os testes

Com a infraestrutura e os segredos de teste configurados:

```powershell
dotnet test .\UsuariosApp.sln --no-build
```

Os testes atuais são de integração e podem inicializar a aplicação completa. Isso significa que podem depender de SQL Server, RabbitMQ e configurações SMTP. Não utilize credenciais nem serviços reais sem revisar previamente o cenário executado.

## 9. Solução de problemas

### A API não conecta em `localhost:5236`

Confirme se ela está em execução e observe o terminal de `dotnet run`. A porta está definida no `launchSettings.json`.

### As tabelas não aparecem

- confirme que o SQL Server está ativo com `docker compose ps`;
- conecte em `localhost,1434`;
- selecione o banco `UsuariosAppBD`;
- atualize a árvore de bancos/tabelas na ferramenta;
- use `dotnet ef migrations list` antes de considerar um novo `database update`.

### A aplicação encerra durante a inicialização

Confira se foram cadastrados:

- `ConnectionStrings:UsuariosAppBD`;
- `JwtSettings:SecretKey`;
- `EmailSettings:User`;
- `EmailSettings:Password`.

Também confirme se SQL Server e RabbitMQ estão ativos.

### O e-mail não chega

- verifique se foi usada uma senha de app do Google;
- confirme que o Gmail informado pertence à senha de app;
- observe o terminal da API e a fila `Usuarios_app` no RabbitMQ Management;
- não publique novamente dados sensíveis ao solicitar ajuda.

## 10. Cuidados de segurança

- não versione `.env`, User Secrets ou credenciais reais;
- não compartilhe senhas, tokens, chaves JWT ou connection strings;
- use credenciais exclusivas para desenvolvimento;
- não reutilize a configuração local em produção;
- substitua as credenciais imediatamente se algum segredo for exposto;
- as senhas são armazenadas com `PasswordHasher`, usando salt individual e fator de trabalho;
- tokens temporários são armazenados somente como hash, expiram e são de uso único;
- códigos de telefone expiram, têm limite de tentativas e intervalo mínimo entre solicitações;
- alterações sensíveis incrementam a versão de segurança e invalidam JWTs antigos.

## 11. Recursos de segurança da conta

### Alterar senha

Envie `senhaAtual` e `novaSenha` para `POST /api/usuario/alterar-senha` com um JWT. Depois da alteração, faça login novamente.

### Recuperar senha esquecida

1. Envie o e-mail para `POST /api/usuario/esqueci-senha`.
2. A resposta será sempre genérica.
3. Envie `token` e `novaSenha` para `POST /api/usuario/redefinir-senha`.

### Confirmar o telefone

O telefone deve usar formato internacional, como `+5511999999999`.

1. Envie `telefone` para `POST /api/usuario/telefone/solicitar-confirmacao` com JWT.
2. Envie `codigo` para `POST /api/usuario/telefone/confirmar` com JWT.

Em desenvolvimento, o código aparece no terminal da API. Isso não é permitido em produção.

### Alterar e-mail

Envie `senhaAtual` e `novoEmail` para `POST /api/usuario/email/solicitar-alteracao` com JWT. O endereço atual permanece válido até a confirmação do link enviado ao novo e-mail. O endereço antigo recebe um aviso.

### Alterar telefone

Envie `senhaAtual` e `novoTelefone` para `POST /api/usuario/telefone/solicitar-alteracao`. Confirme o código em `POST /api/usuario/telefone/confirmar-alteracao`.

### Recuperar e-mail pelo telefone

Esse fluxo exige telefone previamente confirmado:

1. envie `telefone` para `POST /api/usuario/email/recuperar-por-telefone`;
2. envie `telefone`, `codigo` e `novoEmail` para `POST /api/usuario/email/confirmar-recuperacao-por-telefone`;
3. confirme o link recebido no novo endereço.

## 12. Migration e compatibilidade

A migration criada é `AccountSecurityRecovery`. Ela não é aplicada automaticamente:

```powershell
dotnet ef database update --project .\UsuariosApp.Infra.Data\UsuariosApp.Infra.Data.csproj --startup-project .\Usuarios.App.API\Usuarios.App.API.csproj
```

Senhas antigas em SHA-256 não são aceitas pelo novo `PasswordHasher`. Em desenvolvimento, recrie os usuários ou o banco. Com dados reais, conduza os usuários pelo fluxo de redefinição de senha.

## 13. SMS

O `DevelopmentSmsSender` existe somente para testes locais e grava o código no log. Antes de publicar, implemente `ISmsSender` com um provedor real, guarde suas credenciais fora do Git e adicione limites de envio no provedor.
