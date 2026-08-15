# Usuarios.App - Contexto para o Codex

## Objetivo
API .NET para cadastro e autenticação de usuários.

## Arquitetura
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- RabbitMQ
- BackgroundService para consumo de mensagens
- SMTP Gmail para confirmação de email

## Fluxo de criação de usuário

UsuarioController
    ↓
UsuarioService.CriarConta
    ↓
UsuarioRepository
    ↓
SQL Server

Depois:

UsuarioService
    ↓
IEventPublisher
    ↓
RabbitMQProducer
    ↓
RabbitMQ
    ↓
EmailConsumer
    ↓
Gmail SMTP
    ↓
Email de confirmação

## RabbitMQ

Evento:

UsuarioCriadoEvent

Contém:
- UsuarioId
- Nome
- Email
- Token

Producer:
RabbitMQProducer

Consumer:
EmailConsumer : BackgroundService

## Email

O EmailConsumer:
1. Consome UsuarioCriadoEvent
2. Desserializa JSON
3. Cria link de confirmação
4. Envia email via Gmail SMTP
5. ACK em caso de sucesso
6. NACK em caso de erro

## Estado atual

O envio de email está sendo configurado/testado.

Próxima etapa:
Criar endpoint confirmar-email que:
- recebe token
- procura usuário pelo token
- define Ativo = true
- invalida EmailConfirmacaoToken