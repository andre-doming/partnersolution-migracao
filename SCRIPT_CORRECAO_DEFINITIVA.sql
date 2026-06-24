-- ============================================================================
-- SCRIPT CORREÇÃO DEFINITIVA - AUDITORIA ESQUEMA BANCO
-- ============================================================================
-- Execute este script NO SQL Server Management Studio
-- Banco: db_partner
-- ============================================================================

USE [db_partner];
GO

-- ============================================================================
-- PASSO 1: Verificar e Adicionar Coluna 'type' em ImportNotifications
-- ============================================================================

PRINT '[VERIFICANDO] Coluna type em ImportNotifications...';

-- Verificar se existe
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'type'
)
BEGIN
    PRINT '[ADICIONANDO] Coluna type em ImportNotifications...';
    ALTER TABLE dbo.ImportNotifications
    ADD [type] NVARCHAR(50) NOT NULL DEFAULT 'import';
    PRINT '[SUCESSO] Coluna type adicionada!';
END
ELSE
BEGIN
    PRINT '[OK] Coluna type já existe em ImportNotifications';
END

GO

-- ============================================================================
-- PASSO 2: Verificar Schema Final de ImportNotifications
-- ============================================================================

PRINT '';
PRINT '========== SCHEMA DE ImportNotifications ==========';
EXEC sp_help 'dbo.ImportNotifications';

GO

-- ============================================================================
-- PASSO 3: Validação - Contar Colunas
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Contando colunas em ImportNotifications...';

SELECT COUNT(*) AS TotalColunas
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications';

GO

-- ============================================================================
-- PASSO 4: Listar Todas as Colunas
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Listando todas as colunas...';

SELECT 
    COLUMN_NAME AS 'Nome da Coluna',
    DATA_TYPE AS 'Tipo',
    IS_NULLABLE AS 'Permite NULL'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION;

GO

-- ============================================================================
-- PASSO 5: Verificar Estrutura Completa
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Estrutura da tabela ImportNotifications:';

SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.CHARACTER_MAXIMUM_LENGTH,
    c.IS_NULLABLE,
    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION;

GO

-- ============================================================================
-- RESUMO FINAL
-- ============================================================================

PRINT '';
PRINT '============================================================';
PRINT '[COMPLETO] Script de Correção Executado com Sucesso!';
PRINT '============================================================';
PRINT 'Próximos passos:';
PRINT '1. Abrir ImportQueries.cs (linha ~429)';
PRINT '2. Adicionar coluna ''type'' no INSERT';
PRINT '3. Adicionar parâmetro @Type';
PRINT '4. Abrir ImportWorker.cs (2 localidades)';
PRINT '5. Adicionar Type = "import" ao anonymous object';
PRINT '6. Compilar: dotnet clean && dotnet build';
PRINT '7. Testar importação novamente';
PRINT '============================================================';

GO
