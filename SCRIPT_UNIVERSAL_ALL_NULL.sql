-- ============================================================================
-- SCRIPT UNIVERSAL - PERMITE NULL EM TODAS AS COLUNAS NÃO-ESSENCIAIS
-- ============================================================================
-- Este script AUTOMATICAMENTE encontra e corrige TODAS as colunas NOT NULL
-- que NÃO são chaves primárias
-- ============================================================================

USE [db_partner];
GO

PRINT '============================================================';
PRINT '[INICIANDO] Limpeza TOTAL de ImportNotifications';
PRINT '============================================================';
PRINT '';

-- ============================================================================
-- Usar cursor para iterar TODAS as colunas NOT NULL (exceto PK)
-- ============================================================================

DECLARE @ColumnName NVARCHAR(MAX)
DECLARE @ColumnType NVARCHAR(MAX)
DECLARE @SQL NVARCHAR(MAX)

DECLARE cursor_cols CURSOR FOR
SELECT 
    c.COLUMN_NAME,
    c.DATA_TYPE + 
    CASE 
        WHEN c.DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar') 
            THEN '(' + CAST(c.CHARACTER_MAXIMUM_LENGTH AS NVARCHAR(10)) + ')'
        WHEN c.DATA_TYPE IN ('numeric', 'decimal')
            THEN '(' + CAST(c.NUMERIC_PRECISION AS NVARCHAR(10)) + ',' + CAST(c.NUMERIC_SCALE AS NVARCHAR(10)) + ')'
        ELSE ''
    END AS ColumnType
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE 
    c.TABLE_NAME = 'ImportNotifications'
    AND c.IS_NULLABLE = 'NO'
    AND c.COLUMN_NAME NOT IN ('id')  -- Não alterar PK
ORDER BY c.ORDINAL_POSITION

OPEN cursor_cols

FETCH NEXT FROM cursor_cols INTO @ColumnName, @ColumnType

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Construir comando ALTER
    SET @SQL = 'ALTER TABLE dbo.ImportNotifications ALTER COLUMN [' + @ColumnName + '] ' + @ColumnType + ' NULL'
    
    BEGIN TRY
        EXECUTE sp_executesql @SQL
        PRINT '[✓] ' + @ColumnName + ' agora permite NULL'
    END TRY
    BEGIN CATCH
        PRINT '[✗] ERRO ao alterar ' + @ColumnName + ': ' + ERROR_MESSAGE()
    END CATCH
    
    FETCH NEXT FROM cursor_cols INTO @ColumnName, @ColumnType
END

CLOSE cursor_cols
DEALLOCATE cursor_cols

GO

-- ============================================================================
-- Verificação Final
-- ============================================================================

PRINT ''
PRINT '[VALIDAÇÃO] Verificando status FINAL:'
PRINT ''

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CASE IS_NULLABLE 
        WHEN 'YES' THEN '✓ NULL OK'
        WHEN 'NO' THEN '✗✗✗ AINDA NÃO NULL! ✗✗✗'
    END AS Status
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION

-- Verificação final - há ainda colunas NOT NULL?
DECLARE @CountNotNull INT = (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ImportNotifications' AND IS_NULLABLE = 'NO' AND COLUMN_NAME != 'id'
)

PRINT ''
IF @CountNotNull = 0
BEGIN
    PRINT '[✓✓✓ SUCESSO!] TODAS as colunas opcionais agora permitem NULL'
    PRINT '============================================================'
    PRINT '[✓ PRONTO] Execute agora:'
    PRINT '1. Voltar ao Terminal'
    PRINT '2. Ctrl+C para parar'
    PRINT '3. dotnet clean && dotnet build'
    PRINT '4. dotnet run'
    PRINT '5. Testar importação'
    PRINT '============================================================'
END
ELSE
BEGIN
    PRINT '[⚠ AVISO] Ainda há ' + CAST(@CountNotNull AS VARCHAR(10)) + ' coluna(s) NOT NULL!'
    PRINT 'Se erro persistir, copie a saída acima e envie para verificação'
END

GO
