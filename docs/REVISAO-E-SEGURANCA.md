# Revisão e segurança do Usuarios.App

## Objetivo e data

Revisão realizada em 20 de agosto de 2026 para fortalecer cadastro, autenticação e recuperação de contas preservando a arquitetura Controller → Service → Repository.

## Escopo e arquitetura

Foram analisados API, domínio, Entity Framework Core, SQL Server, RabbitMQ, Gmail SMTP, JWT, configurações, migrations e testes.

O cadastro grava o usuário inativo, publica um evento no RabbitMQ e o consumidor envia a confirmação por SMTP. A autenticação valida senha e ativação antes de emitir JWT. Operações sensíveis usam tokens temporários persistidos separadamente.

## Fluxos implementados

- confirmação de e-mail com token aleatório, hash, validade e uso único;
- autenticação JWT com versão de segurança;
- alteração e redefinição de senha;
- cadastro, confirmação e alteração de telefone;
- alteração de e-mail com confirmação do novo endereço e aviso ao antigo;
- recuperação de e-mail por telefone confirmado, seguida de confirmação do novo e-mail;
- revogação de JWTs antigos após alterações sensíveis.

## Correções aplicadas

- SHA-256 simples substituído por `PasswordHasher` com salt individual;
- senha removida da consulta SQL de autenticação;
- tokens temporários com expiração, uso único e armazenamento em hash;
- códigos SMS com expiração, limite de tentativas e cooldown;
- erros inesperados não expõem detalhes internos;
- publisher confirms habilitados no RabbitMQ;
- testes convertidos de `.Result` para `async/await`;
- JWT passou a respeitar `ExpirationMinutes` e a ser revogável.

## Configuração

Configurações públicas adicionadas:

- `RecoverySettings:LinkExpirationMinutes`;
- `RecoverySettings:SmsCodeExpirationMinutes`;
- `RecoverySettings:MaxCodeAttempts`;
- `RecoverySettings:RequestCooldownSeconds`;
- `SmsSettings:Provider`.

Nenhuma credencial foi versionada. O remetente SMS atual funciona somente em `Development` e deve ser substituído antes de produção.

## Migration

Foi criada `AccountSecurityRecovery`, sem alterar migrations antigas. Ela adiciona telefone, confirmação do telefone, expiração da confirmação de e-mail, versão de segurança e a tabela `USUARIO_TOKENS`. Ela não foi aplicada automaticamente.

Hashes SHA-256 existentes são incompatíveis. Usuários antigos devem redefinir a senha ou, em desenvolvimento, ser recriados.

## Validação

```powershell
dotnet build .\Usuarios.App.sln --no-restore
dotnet test .\UsuariosApp.Tests\UsuariosApp.Tests.csproj --no-build --filter "FullyQualifiedName~SecurityTests"
```

Teste manual recomendado:

1. criar e confirmar conta;
2. autenticar e consultar `Minha-Conta`;
3. confirmar telefone;
4. alterar senha e verificar que o JWT antigo recebe `401`;
5. redefinir senha e tentar reutilizar o token;
6. alterar e-mail e verificar o aviso ao endereço antigo;
7. alterar telefone;
8. recuperar o e-mail pelo telefone e confirmar o novo endereço.

## Limitações e riscos restantes

- não existe provedor SMS de produção;
- SMS é vulnerável a troca de chip e não deve ser a única recuperação em sistemas de alto risco;
- testes de integração ainda dependem de SQL Server, RabbitMQ e SMTP;
- não existe frontend para os links de redefinição;
- SQL e publicação RabbitMQ ainda não formam uma transação atômica; recomenda-se outbox;
- rate limiting distribuído por IP/conta e auditoria persistente ainda são recomendados.

## Próximos passos

1. implementar um provedor SMS real;
2. adicionar outbox transacional;
3. isolar testes de integração;
4. adicionar rate limiting distribuído e monitoramento;
5. considerar passkeys ou aplicativo autenticador como alternativa ao SMS.
