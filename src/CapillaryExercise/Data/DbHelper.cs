using Microsoft.Data.Sqlite;

namespace CapillaryExercise.Data;

/// <summary>
/// SQLite 数据库帮助类：负责连接创建与表结构初始化。
/// 各 Repository 通过本类获取连接，统一数据库访问入口。
/// </summary>
public class DbHelper
{
    private readonly string _connectionString;

    /// <summary>
    /// 用数据库文件路径构造。路径可为普通 .db 文件，
    /// 也可为 SQLite 共享内存库（如 "file:test?mode=memory&cache=shared"）以供测试使用。
    /// </summary>
    /// <param name="dbPath">数据库文件路径或连接数据源。</param>
    public DbHelper(string dbPath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath
        }.ToString();
    }

    /// <summary>
    /// 创建并打开一个数据库连接。调用方负责释放（using）。
    /// </summary>
    /// <returns>已打开的 SQLite 连接。</returns>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// 初始化数据库表结构（劈刀信息表、操作日志表及索引）。
    /// 使用 IF NOT EXISTS，可安全重复调用。
    /// </summary>
    public void InitializeSchema()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS CapillaryInfo (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode       TEXT    NOT NULL,
                CapillaryType TEXT    NOT NULL,
                Face          TEXT    NOT NULL,
                PosX          INTEGER NOT NULL,
                PosY          INTEGER NOT NULL,
                StoredTime    TEXT    NOT NULL,
                Status        INTEGER NOT NULL,
                WorkOrder     TEXT,
                MachineNo     TEXT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CapillaryInfo_Barcode
                ON CapillaryInfo (Barcode);

            CREATE INDEX IF NOT EXISTS IX_CapillaryInfo_Fifo
                ON CapillaryInfo (CapillaryType, Status, StoredTime);

            CREATE TABLE IF NOT EXISTS OperationLog (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                OperationType TEXT    NOT NULL,
                Barcode       TEXT    NOT NULL,
                CapillaryType TEXT    NOT NULL,
                Face          TEXT    NOT NULL,
                PosX          INTEGER NOT NULL,
                PosY          INTEGER NOT NULL,
                WorkOrder     TEXT    NOT NULL,
                MachineNo     TEXT    NOT NULL,
                Result        TEXT    NOT NULL,
                Message       TEXT    NOT NULL,
                Timestamp     TEXT    NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}
