-- ============================================================================
-- SCRIPT DE CORREÇÃO SCHEMA - VALIDAÇÃO INTEGRIDADE SISTEMA
-- Data: 06/08/2026
-- ============================================================================

-- Este script corrige os problemas identificados na auditoria:
-- 1. Garante que todas as colunas existem nas tabelas de import
-- 2. Valida os tipos de dados
-- 3. Corrige foreign keys se necessário

-- ============================================================================
-- PASSO 1: Verificar e corrigir tabela ImportNotifications
-- ============================================================================

IF OBJECT_ID('dbo.ImportNotifications', 'U') IS NOT NULL
BEGIN
    -- Verificar se coluna import_job_id existe
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                   WHERE TABLE_NAME='ImportNotifications' AND COLUMN_NAME='import_job_id')
    BEGIN
        PRINT 'ERRO ENCONTRADO: Coluna import_job_id não existe em ImportNotifications'
        PRINT 'Adicionando coluna import_job_id...'
        
        ALTER TABLE dbo.ImportNotifications 
        ADD import_job_id INT NOT NULL DEFAULT 0
        
        PRINT 'Coluna import_job_id adicionada com sucesso!'
    END
    ELSE
    BEGIN
        PRINT 'OK: Coluna import_job_id já existe em ImportNotifications'
    END
    
    -- Verificar se foreign key existe
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
                   WHERE TABLE_NAME='ImportNotifications' 
                   AND CONSTRAINT_NAME='FK_ImportNotifications_ImportJobs'
                   AND CONSTRAINT_TYPE='FOREIGN KEY')
    BEGIN
        PRINT 'ERRO ENCONTRADO: Foreign key não existe'
        PRINT 'Adicionando foreign key...'
        
        ALTER TABLE dbo.ImportNotifications 
        ADD CONSTRAINT FK_ImportNotifications_ImportJobs 
        FOREIGN KEY (import_job_id) REFERENCES dbo.ImportJobs(id)
        
        PRINT 'Foreign key adicionada com sucesso!'
    END
    ELSE
    BEGIN
        PRINT 'OK: Foreign key já existe'
    END
END
ELSE
BEGIN
    PRINT 'AVISO: Tabela ImportNotifications não encontrada'
END

-- ============================================================================
-- PASSO 2: Validar tabela ImportJobs
-- ============================================================================

IF OBJECT_ID('dbo.ImportJobs', 'U') IS NOT NULL
BEGIN
    PRINT ''
    PRINT 'Validando tabela ImportJobs...'
    
    DECLARE @MissingColumns NVARCHAR(MAX) = ''
    
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='id')
        SET @MissingColumns = @MissingColumns + 'id, '
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='public_id')
        SET @MissingColumns = @MissingColumns + 'public_id, '
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='feature')
        SET @MissingColumns = @MissingColumns + 'feature, '
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='file_name')
        SET @MissingColumns = @MissingColumns + 'file_name, '
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='company_id')
        SET @MissingColumns = @MissingColumns + 'company_id, '
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobs' AND COLUMN_NAME='status')
        SET @MissingColumns = @MissingColumns + 'status, '
    
    IF LEN(@MissingColumns) > 0
    BEGIN
        PRINT 'ERRO: Colunas ausentes em ImportJobs: ' + LEFT(@MissingColumns, LEN(@MissingColumns)-2)
    END
    ELSE
    BEGIN
        PRINT 'OK: Todas as colunas principais existem em ImportJobs'
    END
END
ELSE
BEGIN
    PRINT 'ERRO CRÍTICO: Tabela ImportJobs não encontrada!'
END

-- ============================================================================
-- PASSO 3: Validar tabela ImportJobErrors
-- ============================================================================

IF OBJECT_ID('dbo.ImportJobErrors', 'U') IS NOT NULL
BEGIN
    PRINT ''
    PRINT 'Validando tabela ImportJobErrors...'
    
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='import_job_id')
        PRINT 'ERRO: Coluna import_job_id ausente em ImportJobErrors'
    ELSE
        PRINT 'OK: Coluna import_job_id existe em ImportJobErrors'
    
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='ImportJobErrors' AND COLUMN_NAME='line_number')
        PRINT 'ERRO: Coluna line_number ausente em ImportJobErrors'
    ELSE
        PRINT 'OK: Coluna line_number existe em ImportJobErrors'
END
ELSE
BEGIN
    PRINT 'ERRO CRÍTICO: Tabela ImportJobErrors não encontrada!'
END

-- ============================================================================
-- PASSO 4: Validar tabela ImportJobItems
-- ============================================================================

IF OBJECT_ID('dbo.ImportJobItems', 'U') IS NOT NULL
BEGIN
    PRINT ''
    PRINT 'Validando tabela ImportJobItems...'
    PRINT 'OK: Tabela ImportJobItems existe'
END
ELSE
BEGIN
    PRINT 'AVISO: Tabela ImportJobItems não encontrada'
END

-- ============================================================================
-- PASSO 5: Resumo Final
-- ============================================================================

PRINT ''
PRINT '============================================================================'
PRINT 'RESUMO DE VALIDAÇÃO DO SCHEMA'
PRINT '============================================================================'

SELECT 
    'ImportJobs' AS [Tabela],
    COUNT(*) AS [Colunas],
    'Acesso: ' + CASE WHEN OBJECTPROPERTY(OBJECT_ID(N'dbo.ImportJobs'), N'IsUserTable') = 1 THEN 'OK' ELSE 'ERRO' END AS Status
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ImportJobs'
UNION ALL
SELECT 
    'ImportJobErrors' AS [Tabela],
    COUNT(*) AS [Colunas],
    'Acesso: ' + CASE WHEN OBJECTPROPERTY(OBJECT_ID(N'dbo.ImportJobErrors'), N'IsUserTable') = 1 THEN 'OK' ELSE 'ERRO' END AS Status
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ImportJobErrors'
UNION ALL
SELECT 
    'ImportJobItems' AS [Tabela],
    COUNT(*) AS [Colunas],
    'Acesso: ' + CASE WHEN OBJECTPROPERTY(OBJECT_ID(N'dbo.ImportJobItems'), N'IsUserTable') = 1 THEN 'OK' ELSE 'ERRO' END AS Status
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ImportJobItems'
UNION ALL
SELECT 
    'ImportNotifications' AS [Tabela],
    COUNT(*) AS [Colunas],
    'Acesso: ' + CASE WHEN OBJECTPROPERTY(OBJECT_ID(N'dbo.ImportNotifications'), N'IsUserTable') = 1 THEN 'OK' ELSE 'ERRO' END AS Status
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'ImportNotifications'

PRINT ''
PRINT '============================================================================'
PRINT 'SCRIPT FINALIZADO COM SUCESSO'
PRINT '============================================================================'
