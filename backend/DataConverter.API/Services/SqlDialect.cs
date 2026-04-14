using DataConverter.API.Models;

namespace DataConverter.API.Services;

public enum ColumnType
{
    Int, BigInt, Float, Decimal, DateTime, Boolean, ShortText, LongText
}

public abstract class SqlDialect
{
    public abstract string QuoteIdentifier(string name);
    public abstract string QuoteValue(string value);
    public abstract string MapType(ColumnType type);
    public virtual string CreateTablePrefix => "CREATE TABLE ";
    public virtual string? CreateTableSuffix => null;
    public virtual AutoIncrementColumnDef? AutoIncrementColumn => null;
}

public record AutoIncrementColumnDef(string Name, string Definition);

public class SqlServerDialect : SqlDialect
{
    public override string QuoteIdentifier(string name) => $"[{name}]";
    public override string QuoteValue(string value) => $"'{value.Replace("'", "''")}'";
    public override string MapType(ColumnType type) => type switch
    {
        ColumnType.Int => "INT",
        ColumnType.BigInt => "BIGINT",
        ColumnType.Float => "FLOAT",
        ColumnType.Decimal => "DECIMAL(18,2)",
        ColumnType.DateTime => "DATETIME",
        ColumnType.Boolean => "BIT",
        ColumnType.ShortText => "NVARCHAR(255)",
        ColumnType.LongText => "NVARCHAR(MAX)",
        _ => "NVARCHAR(255)"
    };
    public override AutoIncrementColumnDef AutoIncrementColumn => new("Id", "INT IDENTITY(1,1) PRIMARY KEY");
}

public class MySqlDialect : SqlDialect
{
    public override string QuoteIdentifier(string name) => $"`{name}`";
    public override string QuoteValue(string value) => $"'{value.Replace("'", "''")}'";
    public override string MapType(ColumnType type) => type switch
    {
        ColumnType.Int => "INT",
        ColumnType.BigInt => "BIGINT",
        ColumnType.Float => "DOUBLE",
        ColumnType.Decimal => "DECIMAL(18,2)",
        ColumnType.DateTime => "DATETIME",
        ColumnType.Boolean => "TINYINT(1)",
        ColumnType.ShortText => "VARCHAR(255)",
        ColumnType.LongText => "TEXT",
        _ => "VARCHAR(255)"
    };
    public override AutoIncrementColumnDef AutoIncrementColumn => new("id", "INT AUTO_INCREMENT PRIMARY KEY");
    public override string CreateTableSuffix => "ENGINE=InnoDB DEFAULT CHARSET=utf8mb4";
}

public class PostgreSqlDialect : SqlDialect
{
    public override string QuoteIdentifier(string name) => $"\"{name}\"";
    public override string QuoteValue(string value) => $"'{value.Replace("'", "''")}'";
    public override string MapType(ColumnType type) => type switch
    {
        ColumnType.Int => "INTEGER",
        ColumnType.BigInt => "BIGINT",
        ColumnType.Float => "DOUBLE PRECISION",
        ColumnType.Decimal => "NUMERIC(18,2)",
        ColumnType.DateTime => "TIMESTAMP",
        ColumnType.Boolean => "BOOLEAN",
        ColumnType.ShortText => "VARCHAR(255)",
        ColumnType.LongText => "TEXT",
        _ => "VARCHAR(255)"
    };
    public override AutoIncrementColumnDef AutoIncrementColumn => new("id", "SERIAL PRIMARY KEY");
}

public static class SqlDialectFactory
{
    public static SqlDialect GetDialect(DatabaseType dbType) => dbType switch
    {
        DatabaseType.MySql => new MySqlDialect(),
        DatabaseType.PostgreSql => new PostgreSqlDialect(),
        _ => new SqlServerDialect()
    };
}
