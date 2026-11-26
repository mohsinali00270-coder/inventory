using SQLite;
using ShopLedger.Models;

namespace ShopLedger.Data;

public class AppDatabase
{
    public const string DbFileName = "shopledger.db3";
    public static string DbPath => Path.Combine(FileSystem.AppDataDirectory, DbFileName);

    private readonly string _path;
    private SQLiteAsyncConnection? _conn;

    /// <summary>Production: the app data folder.</summary>
    public AppDatabase() : this(DbPath) { }
    /// <summary>Tests: any file path.</summary>
    internal AppDatabase(string path) { _path = path; }

    private readonly SemaphoreSlim _lock = new(1, 1);

    public SQLiteAsyncConnection Connection =>
        _conn ?? throw new InvalidOperationException("Call InitAsync() first.");

    public async Task InitAsync()
    {
        if (_conn is not null) return;
        await _lock.WaitAsync();
        try
        {
            if (_conn is not null) return;
            var conn = new SQLiteAsyncConnection(_path,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await conn.CreateTableAsync<Product>();
            await conn.CreateTableAsync<StockMovement>();
            await conn.CreateTableAsync<Customer>();
            await conn.CreateTableAsync<LedgerTransaction>();
            await conn.CreateTableAsync<TransactionItem>();
            _conn = conn;
        }
        finally { _lock.Release(); }
    }

    /// <summary>Run several writes atomically (used for sales: stock + balance + items).</summary>
    public async Task InTransactionAsync(Action<SQLiteConnection> work)
    {
        await InitAsync();
        await Connection.RunInTransactionAsync(work);
    }

    /// <summary>Close connection (needed before backup/restore).</summary>
    public async Task CloseAsync()
    {
        if (_conn is null) return;
        await _conn.CloseAsync();
        _conn = null;
    }
}
