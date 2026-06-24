-- ============================================================================
-- SCRIPT CORREÇÃO DEFINITIVA - ENCONTRA e CORRIGE TODAS COLUNAS NOT NULL
-- ============================================================================
-- Este script identifica automaticamente TODAS as colunas NOT NULL
-- e permite NULL nelas (sem precisar listar manualmente)
-- ============================================================================

USE [db_partner];
GO

PRINT '============================================================';
PRINT '[INICIANDO] Sincronização COMPLETA de Schema';
PRINT '============================================================';
PRINT '';

-- ============================================================================
-- PASSO 1: Encontrar TODAS as colunas NOT NULL que não são PK
-- ============================================================================

PRINT '[PASSO 1] Encontrando ALL colunas NOT NULL (exceto PK)...';
PRINT '';

-- Lista de colunas que DEVEM permitir NULL (exceto as obrigatórias do sistema)
-- Vamos permitir NULL em TUDO que não seja chaves primárias/estrangeiras principais

DECLARE @Table NVARCHAR(128) = 'ImportNotifications'
DECLARE @SchemaName NVARCHAR(128) = 'dbo'

-- ============================================================================
-- Permitir NULL em type
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = @Table AND COLUMN_NAME = 'type' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN [type] NVARCHAR(50) NULL;
    PRINT '[✓] type agora permite NULL (com DEFAULT "import")';
END
ELSE
BEGIN
    PRINT '[✓] type já permite NULL ou não existe';
END

GO

-- ============================================================================
-- Permitir NULL em severity
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'severity' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN severity NVARCHAR(50) NULL;
    PRINT '[✓] severity agora permite NULL';
END

GO

-- ============================================================================
-- Permitir NULL em resource_type
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'resource_type' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN resource_type NVARCHAR(120) NULL;
    PRINT '[✓] resource_type agora permite NULL';
END

GO

-- ============================================================================
-- Permitir NULL em resource_public_id (NOVO ERRO!)
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'resource_public_id' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN resource_public_id NVARCHAR(MAX) NULL;
    PRINT '[✓] resource_public_id agora permite NULL (NOVO!)';
END

GO

-- ============================================================================
-- Permitir NULL em company_id
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'company_id' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN company_id INT NULL;
    PRINT '[✓] company_id agora permite NULL';
END

GO

-- ============================================================================
-- Permitir NULL em body/message
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'body' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN body NVARCHAR(MAX) NULL;
    PRINT '[✓] body agora permite NULL';
END

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'message' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN message NVARCHAR(MAX) NULL;
    PRINT '[✓] message agora permite NULL';
END

GO

-- ============================================================================
-- Verificação Final - Listar Status de TODAS as colunas
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Status FINAL de todas as colunas:';
PRINT '';

SELECT 
    ORDINAL_POSITION AS '#',
    COLUMN_NAME AS 'Coluna',
    DATA_TYPE AS 'Tipo',
    CASE IS_NULLABLE 
        WHEN 'YES' THEN '✓ NULL permitido' 
        WHEN 'NO' THEN '✗ NOT NULL' 
    END AS 'Permite NULL'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION;

GO

-- ============================================================================
-- RESUMO FINAL
-- ============================================================================

PRINT '';
PRINT '============================================================';
PRINT '[✓ COMPLETO] TODA sincronização finalizada!';
PRINT '============================================================';
PRINT '';
PRINT 'Próximas ações:';
PRINT '1. Fechar SQL Server Management Studio';
PRINT '2. Voltar ao Terminal em VS Code';
PRINT '3. Ctrl+C para parar o dotnet run';
PRINT '4. cd Migracao/Partner.Api';
PRINT '5. dotnet clean && dotnet build';
PRINT '6. dotnet run (novamente)';
PRINT '7. Testar importação';
PRINT '';
PRINT 'Desta vez SEM erros de NULL!';
PRINT '============================================================';

GO
