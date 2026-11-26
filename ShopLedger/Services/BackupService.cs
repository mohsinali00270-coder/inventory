using SQLite;
using ShopLedger.Data;

namespace ShopLedger.Services;

public interface IBackupService
{
    Task<string> CreateBackupAsync();
    /// <summary>Replace the live database with the file at sourcePath (validated first).</summary>
    Task RestoreAsync(string sourcePath);
}

public class BackupService : IBackupService
{
    private readonly AppDatabase _db;
    public BackupService(AppDatabase db) => _db = db;

    public async Task<string> CreateBackupAsync()
    {
        var dest = Path.Combine(FileSystem.CacheDirectory, $"shopledger-backup-{DateTime.Now:yyyyMMdd-HHmm}.db3");
        await _db.InitAsync();
        await _db.CloseAsync();                    // flush + release the file
        try { File.Copy(AppDatabase.DbPath, dest, true); }
        finally { await _db.InitAsync(); }
        return dest;
    }

    public async Task RestoreAsync(string sourcePath)
    {
        if (!IsValidBackup(sourcePath))
            throw new InvalidDataException(Loc.T("Err_BadBackup"));

        var safety = Path.Combine(FileSystem.CacheDirectory, "before-restore.db3");
        await _db.InitAsync();
        await _db.CloseAsync();
        try
        {
            if (File.Exists(AppDatabase.DbPath)) File.Copy(AppDatabase.DbPath, safety, true);
            File.Copy(sourcePath, AppDatabase.DbPath, true);
        }
        finally { await _db.InitAsync(); }
    }

    private static bool IsValidBackup(string path)
    {
        try
        {
            using var c = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);
            var n = c.ExecuteScalar<int>(
                "select count(*) from sqlite_master where name in ('Product','Customer','LedgerTransaction','TransactionItem')");
            return n == 4;
        }
        catch { return false; }
    }
}
