-- Database Backup Script for db_SCADA
-- Run daily via SQL Server Agent or Windows Task Scheduler
-- Usage: sqlcmd -S "(local)\SQLEXPRESS" -i backup_db.sql

DECLARE @BackupPath NVARCHAR(500) = 'C:\Backups\db_SCADA\'
DECLARE @BackupFile NVARCHAR(500)
DECLARE @DateSuffix NVARCHAR(20) = CONVERT(NVARCHAR(8), GETDATE(), 112) + '_' + REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), ':', '')

SET @BackupFile = @BackupPath + 'db_SCADA_' + @DateSuffix + '.bak'

-- Create backup directory if it doesn't exist
EXEC xp_create_subdir @BackupPath

-- Full backup with compression
BACKUP DATABASE [db_SCADA]
TO DISK = @BackupFile
WITH
    COMPRESSION,
    STATS = 10,
    NAME = N'db_SCADA Full Backup',
    DESCRIPTION = N'Automated daily backup'

PRINT 'Backup completed: ' + @BackupFile

-- Clean up backups older than 7 days
DECLARE @CleanupDate DATETIME = DATEADD(DAY, -7, GETDATE())
EXEC xp_delete_files @BackupPath, 'bak', @CleanupDate

PRINT 'Old backups cleaned up (keeping last 7 days)'
GO
