-- ============================================================================
-- SCRIPT SIMPLES - FIX APENAS do message (coluna problemática)
-- ============================================================================
-- A coluna 'message' não pode ser alterada de NVARCHAR() 
-- Precisa ser NVARCHAR(MAX)
--
-- NOTA: 'id' é PRIMARY KEY, DEVE ser NOT NULL - isso é correto!
-- ============================================================================

USE [db_partner];
GO

PRINT '============================================================';
PRINT '[CORRIGINDO] Coluna message (NVARCHAR(MAX))';
PRINT '============================================================';
PRINT '';

-- A coluna message é NVARCHAR(MAX) - alterar corretamente
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ImportNotifications' AND COLUMN_NAME = 'message' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN message NVARCHAR(MAX) NULL;
    PRINT '[✓] message agora permite NULL';
END
ELSE
BEGIN
    PRINT '[✓] message já permite NULL';
END

GO

-- ============================================================================
-- Verificação Final
-- ============================================================================

PRINT '';
PRINT '[VALIDAÇÃO] Status das colunas problemáticas:';
PRINT '';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CASE IS_NULLABLE 
        WHEN 'YES' THEN '✓ NULL permitido'
        WHEN 'NO' THEN '✗ NOT NULL'
    END AS Status
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications' 
  AND COLUMN_NAME IN ('id', 'message')
ORDER BY ORDINAL_POSITION

PRINT '';
PRINT '[NOTA] id = PRIMARY KEY, DEVE ser NOT NULL (é CORRETO!)'
PRINT ''

GO

-- ============================================================================
-- Verificação - colunas NOT NULL (exceto PK)
-- ============================================================================

DECLARE @CountNotNull INT = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ImportNotifications' 
      AND IS_NULLABLE = 'NO' 
      AND COLUMN_NAME NOT IN ('id')
)

PRINT '[FINAL] Colunas NOT NULL (exceto PK): ' + CAST(@CountNotNull AS VARCHAR(10))

IF @CountNotNull = 0
BEGIN
    PRINT ''
    PRINT '✓✓✓ SUCESSO! Schema sincronizado perfeitamente!'
    PRINT '============================================================'
    PRINT 'AGORA:'
    PRINT '1. Voltar ao Terminal'
    PRINT '2. Ctrl+C para parar dotnet'
    PRINT '3. dotnet clean && dotnet build'
    PRINT '4. dotnet run'
    PRINT '5. Testar importação - DEVE FUNCIONAR!'
    PRINT '============================================================'
END
ELSE
BEGIN
    PRINT '[⚠] Ainda há ' + CAST(@CountNotNull AS VARCHAR(10)) + ' coluna(s) NOT NULL'
END

GO
