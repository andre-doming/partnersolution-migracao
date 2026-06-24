-- ============================================================================
-- SCRIPT CORREÇÃO SCHEMA MISMATCH - OPÇÃO 1 (RÁPIDA)
-- ============================================================================
-- Execute este script NO SQL Server Management Studio
-- Banco: db_partner
-- Tempo: 2 minutos
-- ============================================================================

USE [db_partner];
GO

PRINT '============================================================';
PRINT '[INICIANDO] Sincronização de Schema - ImportNotifications';
PRINT '============================================================';
PRINT '';

-- ============================================================================
-- PASSO 1: Adicionar DEFAULT para coluna 'type'
-- ============================================================================

PRINT '[PASSO 1] Adicionando DEFAULT para coluna type...';

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE 
    WHERE CONSTRAINT_NAME = 'DF_ImportNotifications_type'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ADD CONSTRAINT DF_ImportNotifications_type DEFAULT 'import' FOR [type];
    PRINT '[✓] DEFAULT adicionado em type = "import"';
END
ELSE
BEGIN
    PRINT '[✓] DEFAULT já existe em type';
END

GO

-- ============================================================================
-- PASSO 2: Permitir NULL em 'company_id'
-- ============================================================================

PRINT '[PASSO 2] Permitindo NULL em company_id...';

BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN company_id INT NULL;
    PRINT '[✓] company_id agora aceita NULL';
END

GO

-- ============================================================================
-- PASSO 3: Permitir NULL em 'severity'
-- ============================================================================

PRINT '[PASSO 3] Permitindo NULL em severity...';

BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN severity NVARCHAR(50) NULL;
    PRINT '[✓] severity agora aceita NULL';
END

GO

-- ============================================================================
-- PASSO 4: Permitir NULL em 'resource_type'
-- ============================================================================

PRINT '[PASSO 4] Permitindo NULL em resource_type...';

BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN resource_type NVARCHAR(120) NULL;
    PRINT '[✓] resource_type agora aceita NULL';
END

GO

-- ============================================================================
-- PASSO 5: Validação Final - Verificar Estrutura
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Estrutura final da tabela ImportNotifications:';
PRINT '';

SELECT 
    COLUMN_NAME AS 'Nome da Coluna',
    DATA_TYPE AS 'Tipo',
    IS_NULLABLE AS 'Permite NULL',
    COLUMNPROPERTY(OBJECT_ID('dbo.ImportNotifications'), COLUMN_NAME, 'IsIdentity') AS 'É Identity'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION;

GO

-- ============================================================================
-- RESUMO FINAL
-- ============================================================================

PRINT '';
PRINT '============================================================';
PRINT '[✓ COMPLETO] Schema sincronizado com sucesso!';
PRINT '============================================================';
PRINT '';
PRINT 'Próximas ações:';
PRINT '1. Fechar SQL Server Management Studio';
PRINT '2. Abrir Terminal em VS Code';
PRINT '3. cd Migracao';
PRINT '4. dotnet clean && dotnet build';
PRINT '5. dotnet run';
PRINT '6. Fazer login e testar importação';
PRINT '';
PRINT '✅ Não deve haver mais erro de NULL!';
PRINT '============================================================';

GO
