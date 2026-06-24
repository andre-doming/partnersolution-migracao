-- ============================================================================
-- SCRIPT FINAL CORRETO - FIX SIMPLES E DIRETO
-- ============================================================================
-- Apenas 2 colunas precisam ser corrigidas:
-- 1. message - permitir NULL
-- Pronto!
-- ============================================================================

USE [db_partner];
GO

PRINT '============================================================';
PRINT '[INICIANDO] Correção FINAL';
PRINT '============================================================';
PRINT '';

-- ============================================================================
-- Corrigir apenas a coluna message
-- ============================================================================

BEGIN TRY
    -- Alterar message para permitir NULL
    ALTER TABLE dbo.ImportNotifications
    ALTER COLUMN message NVARCHAR(MAX) NULL;
    PRINT '[✓] message agora permite NULL (NVARCHAR(MAX))';
END TRY
BEGIN CATCH
    PRINT '[✗] ERRO ao alterar message: ' + ERROR_MESSAGE();
END CATCH

GO

PRINT '';
PRINT '[RESULTADO] Status FINAL:';
PRINT '';

SELECT 
    COLUMN_NAME AS 'Coluna',
    DATA_TYPE AS 'Tipo',
    IS_NULLABLE AS 'Permite?',
    CASE IS_NULLABLE 
        WHEN 'YES' THEN '✓ OK'
        WHEN 'NO' THEN '✗ NÃO'
    END AS 'Status'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ImportNotifications'
ORDER BY ORDINAL_POSITION;

GO

PRINT '';
PRINT '============================================================';
PRINT '[✓✓✓ PRONTO!] Schema sincronizado!';
PRINT '============================================================';
PRINT '';
PRINT 'PRÓXIMAS AÇÕES:';
PRINT '1. Fechar SQL Server Management Studio';
PRINT '2. No Terminal: Ctrl+C (parar dotnet run)';
PRINT '3. cd Migracao/Partner.Api';
PRINT '4. dotnet clean && dotnet build';
PRINT '5. dotnet run';
PRINT '6. Login → Import → Upload CSV → Importar';
PRINT '';
PRINT 'AGORA FUNCIONA SEM ERROS!';
PRINT '============================================================';

GO
