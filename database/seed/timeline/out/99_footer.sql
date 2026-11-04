COMMIT TRANSACTION;
DECLARE @rs NVARCHAR(MAX) = N'';
SELECT @rs = @rs + N'IF IDENT_CURRENT(''dbo.' + t.name + N''') < 99999 DBCC CHECKIDENT(''dbo.' + t.name + N''', RESEED, 99999) WITH NO_INFOMSGS;'
FROM sys.identity_columns ic JOIN sys.tables t ON t.object_id = ic.object_id WHERE SCHEMA_NAME(t.schema_id) = N'dbo';
EXEC sp_executesql @rs;
PRINT N'   identity các bảng đã đẩy lên 100000 (dữ liệu app mới nằm ngoài dải seed).';
DROP TABLE #sch; DROP TABLE #cat; DROP TABLE #alg; DROP TABLE #plan; DROP TABLE #ing;
PRINT N'>> SEED60 hoàn tất: 14011 dòng được nạp. Chạy verify_timeline.sql để kiểm tra chéo.';
