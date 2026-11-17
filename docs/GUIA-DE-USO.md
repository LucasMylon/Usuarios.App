# Guia de uso do Usuarios.App

## Demonstração rápida com Docker

Com o Docker Desktop iniciado, execute no PowerShell, na raiz do projeto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\iniciar-demo.ps1
```

O script gera senhas e chave JWT aleatórias em `.env.demo` na primeira execução, sem pedir credenciais. O arquivo é ignorado pelo Git e reutilizado nas próximas execuções. O Compose de demonstração é separado do Compose original, usa seus próprios volumes e aplica as migrations automaticamente em desenvolvimento. A confirmação de email está desabilitada: novas contas já ficam ativas e nenhum email é enviado. Aguarde a API terminar de iniciar; o primeiro download das imagens pode demorar.

1. Abra http://localhost:5237/swagger e crie uma conta em `POST /api/usuario/Criar`.
2. A conta já fica ativa; nenhum email ou confirmação é necessário.
3. Autentique-se em `POST /api/usuario/autenticar` e use o `accessToken` retornado nas chamadas protegidas com `Authorization: Bearer <token>`.

Se a confirmação for reativada, o Mailpit captura mensagens apenas localmente: elas não chegam a uma caixa de e-mail real. Seus dados são para demonstração; não envie dados reais ou confidenciais. Não utilize essa configuração, a comunicação SMTP sem TLS ou os segredos gerados em produção.

Para inspecionar os serviços ou encerrá-los sem apagar dados:

```powershell
docker compose --env-file .env.demo -f docker-compose.demo.yml ps
docker compose --env-file .env.demo -f docker-compose.demo.yml logs api
docker compose --env-file .env.demo -f docker-compose.demo.yml stop
```

Guarde `.env.demo` enquanto os volumes da demonstração existirem: gerar outra senha não altera a senha já salva no SQL Server ou no RabbitMQ. Se a API de tarefas precisar validar esse JWT, ela deverá usar o mesmo `DEMO_JWT_KEY`, emissor e público. A chave é privada: configure-a na outra API por meio seguro, sem versioná-la ou compartilhá-la publicamente.

## Execução manual com Gmail

As instruções abaixo são uma alternativa à demonstração Docker e exigem configurar os segredos manualmente. Os perfis locais `http` e `IIS Express` habilitam `Desenvolvimento__IgnorarConfirmacaoEmail=true`: nesse modo, contas novas já ficam ativas e não é necessário SMTP ou RabbitMQ. O SQL Server e a chave JWT continuam necessários; contas inativas existentes não são alteradas. Para seguir o fluxo de confirmação descrito abaixo, defina essa opção como `false` no perfil usado. Fora de Development, ignorar a confirmação é proibido.

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

Esse endpoint é usado apenas quando a confirmação está habilitada. No modo rápido local e na demonstração Docker, as contas já são criadas ativas e nenhum token é gerado.

Quando habilitado, o comportamento esperado é ativar a conta e invalidar o token, impedindo sua reutilização. Após uma confirmação bem-sucedida, o campo `Ativo` fica com valor `true` e `EmailConfirmacaoToken` fica `NULL`. Uma segunda tentativa com o mesmo link deve retornar erro, pois o token já foi invalidado.

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

Uma autenticação válida devolve `AccessToken`. Atualmente, o JWT expira após 30 minutos, independentemente do valor de `JwtSettings:ExpirationMinutes`.

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
- o armazenamento atual de senhas usa SHA-256 simples e deve ser migrado futuramente para um algoritmo próprio para senhas, com salt e fator de trabalho, como Argon2id, bcrypt ou PBKDF2.
