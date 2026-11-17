-- Criar tabelas para Usuarios.App

CREATE TABLE PERFIL (
	ID uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
	NOME nvarchar(25) NOT NULL
);

CREATE TABLE USUARIOS (
	ID uniqueidentifier PRIMARY KEY DEFAULT NEWID(),
	NOME nvarchar(150) NOT NULL,
	EMAIL nvarchar(100) NOT NULL UNIQUE,
	SENHA nvarchar(255) NOT NULL,
	PERFIL_ID uniqueidentifier NOT NULL,
	ATIVO bit NOT NULL DEFAULT 1,
	EMAIL_CONFIRMATION_TOKEN nvarchar(max) NULL,
	FOREIGN KEY (PERFIL_ID) REFERENCES PERFIL(ID)
);

CREATE TABLE __EFMigrationsHistory (
	MigrationId nvarchar(150) PRIMARY KEY,
	ProductVersion nvarchar(32) NOT NULL
);

-- Inserir perfis padrão
INSERT INTO PERFIL (NOME) VALUES ('USUARIO');
INSERT INTO PERFIL (NOME) VALUES ('ADMINISTRADOR');

-- Inserir usuário de teste
DECLARE @perfilId uniqueidentifier = (SELECT TOP 1 ID FROM PERFIL WHERE NOME = 'USUARIO');
INSERT INTO USUARIOS (NOME, EMAIL, SENHA, PERFIL_ID, ATIVO)
VALUES ('Usuário Teste', 'teste@example.com', 'Senha123!', @perfilId, 1);

-- Registrar migrations como aplicadas
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20251011175753_Initial', '10.0.0');
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260130233608_ADD_NOVOS_CAMPOS', '10.0.0');
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260815120000_TornarEmailConfirmacaoTokenAnulavel', '10.0.0');
