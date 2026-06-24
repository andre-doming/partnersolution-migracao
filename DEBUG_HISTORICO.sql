-- ============================================================================
-- DEBUG: HISTÓRICO DE IMPORTAÇÕES VAZIO
-- ============================================================================

-- 1️⃣ Verificar se as notificações foram criadas
PRINT '=== 1. NOTIFICAÇÕES EXISTENTES ==='
SELECT 
    n.id,
    n.import_job_id,
    n.user_id,
    n.title,
    n.status,
    n.created_at_utc,
    j.id AS JobId,
    j.public_id
FROM dbo.ImportNotifications n
LEFT JOIN dbo.ImportJobs j ON n.import_job_id = j.id
ORDER BY n.created_at_utc DESC;

-- 2️⃣ Verificar se os jobs foram criados
PRINT '=== 2. JOBS EXISTENTES ==='
SELECT 
    j.id,
    j.public_id,
    j.feature,
    j.status,
    j.total_rows,
    j.success_rows,
    j.error_rows,
    j.created_by_user_id,
    j.started_at_utc
FROM dbo.ImportJobs j
ORDER BY j.id DESC;

-- 3️⃣ Contar registros
PRINT '=== 3. CONTAGEM ==='
SELECT 
    'ImportNotifications' AS Tabela,
    COUNT(*) AS Total
FROM dbo.ImportNotifications
UNION ALL
SELECT 
    'ImportJobs',
    COUNT(*)
FROM dbo.ImportJobs;

-- 4️⃣ Procurar notificações de um usuário específico (substitua o valor)
PRINT '=== 4. NOTIFICAÇÕES DO USUÁRIO 2 ==='
SELECT 
    n.id,
    n.import_job_id,
    n.user_id,
    n.title,
    n.message,
    n.status,
    n.created_at_utc
FROM dbo.ImportNotifications n
WHERE n.user_id = 2
ORDER BY n.created_at_utc DESC;

-- 5️⃣ Verificar se o JOIN funciona
PRINT '=== 5. JOIN TEST (User 2) ==='
SELECT
    n.id AS NotificationId,
    n.import_job_id AS ImportJobId,
    n.user_id AS UserId,
    n.title AS Title,
    n.message AS Message,
    n.status AS Status,
    n.created_at_utc AS CreatedAtUtc,
    n.read_at_utc AS ReadAtUtc,
    j.public_id AS ImportJobPublicId
FROM dbo.ImportNotifications n
JOIN dbo.ImportJobs j ON n.import_job_id = j.id
WHERE n.user_id = 2
ORDER BY n.created_at_utc DESC;

-- 6️⃣ Procurar últimas 5 notificações de qualquer usuário
PRINT '=== 6. ÚLTIMAS 5 NOTIFICAÇÕES (QUALQUER USUÁRIO) ==='
SELECT TOP 5
    n.id,
    n.import_job_id,
    n.user_id,
    n.title,
    n.status,
    j.public_id
FROM dbo.ImportNotifications n
LEFT JOIN dbo.ImportJobs j ON n.import_job_id = j.id
ORDER BY n.created_at_utc DESC;

-- 7️⃣ Usuários com notificações
PRINT '=== 7. USUÁRIOS COM NOTIFICAÇÕES ==='
SELECT DISTINCT
    n.user_id,
    COUNT(*) AS Total
FROM dbo.ImportNotifications n
GROUP BY n.user_id
ORDER BY n.user_id;

-- 8️⃣ Listar todos os usuários com suas IDs (do banco legado)
PRINT '=== 8. USUÁRIOS DO SISTEMA ==='
SELECT TOP 10
    id,
    email,
    ativo
FROM tb_usuario
ORDER BY id DESC;
