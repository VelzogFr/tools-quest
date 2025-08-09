using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using QuestTool.App.Models;

namespace QuestTool.App.Services
{
    public sealed class SchemaService
    {
        private readonly DatabaseService _db;
        public SchemaService(DatabaseService db) { _db = db; }

        public async Task<TableSchema> LoadTableSchemaAsync(string tableName)
        {
            var columns = await LoadColumnsAsync(tableName);
            var identities = await LoadIdentityColumnsAsync(tableName);

            foreach (var c in columns)
            {
                c.IsIdentity = identities.Contains(c.Name, StringComparer.OrdinalIgnoreCase);
            }

            return new TableSchema
            {
                TableName = tableName,
                Columns = columns.OrderBy(c => c.IsIdentity).ThenBy(c => c.Name).ToList()
            };
        }

        private async Task<List<ColumnSchema>> LoadColumnsAsync(string table)
        {
            var inv = _db.ProviderInvariant.ToLowerInvariant();
            using (var conn = _db.CreateConnection())
            {
                // Open via DbConnection if available, else fallback to sync
                var dbConn = conn as System.Data.Common.DbConnection;
                if (dbConn != null) await dbConn.OpenAsync(); else conn.Open();
                if (inv.IndexOf("sqlclient", StringComparison.Ordinal) >= 0)
                {
                    var rows = await conn.QueryAsync<(string COLUMN_NAME, string DATA_TYPE, string IS_NULLABLE)>(
                        "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t",
                        new { t = table });
                    return rows.Select(r => new ColumnSchema
                    {
                        Name = r.COLUMN_NAME,
                        DataType = r.DATA_TYPE,
                        IsNullable = string.Equals(r.IS_NULLABLE, "YES", StringComparison.OrdinalIgnoreCase)
                    }).ToList();
                }
                if (inv.IndexOf("mysql", StringComparison.Ordinal) >= 0)
                {
                    var rows = await conn.QueryAsync<(string COLUMN_NAME, string DATA_TYPE, string IS_NULLABLE)>(
                        "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t",
                        new { t = table });
                    return rows.Select(r => new ColumnSchema
                    {
                        Name = r.COLUMN_NAME,
                        DataType = r.DATA_TYPE,
                        IsNullable = string.Equals(r.IS_NULLABLE, "YES", StringComparison.OrdinalIgnoreCase)
                    }).ToList();
                }
                var rowsPg = await conn.QueryAsync<(string column_name, string data_type, string is_nullable)>(
                    "SELECT column_name, data_type, is_nullable FROM information_schema.columns WHERE table_name = @t",
                    new { t = table });
                return rowsPg.Select(r => new ColumnSchema
                {
                    Name = r.column_name,
                    DataType = r.data_type,
                    IsNullable = string.Equals(r.is_nullable, "YES", StringComparison.OrdinalIgnoreCase)
                }).ToList();
            }
        }

        private async Task<HashSet<string>> LoadIdentityColumnsAsync(string table)
        {
            var inv = _db.ProviderInvariant.ToLowerInvariant();
            using (var conn = _db.CreateConnection())
            {
                var dbConn = conn as System.Data.Common.DbConnection;
                if (dbConn != null) await dbConn.OpenAsync(); else conn.Open();
                if (inv.IndexOf("sqlclient", StringComparison.Ordinal) >= 0)
                {
                    var sql = @"SELECT c.name AS COLUMN_NAME
                                FROM sys.identity_columns ic
                                JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                                JOIN sys.tables t ON t.object_id = c.object_id
                                WHERE t.name = @t";
                    var rows = await conn.QueryAsync<string>(sql, new { t = table });
                    return new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
                }
                if (inv.IndexOf("mysql", StringComparison.Ordinal) >= 0)
                {
                    var sql = @"SELECT COLUMN_NAME
                                FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t AND EXTRA LIKE '%auto_increment%'";
                    var rows = await conn.QueryAsync<string>(sql, new { t = table });
                    return new HashSet<string>(rows, StringComparer.OrdinalIgnoreCase);
                }
                var sqlPg = @"SELECT a.attname AS column_name
                              FROM pg_class t
                              JOIN pg_attribute a ON a.attrelid = t.oid
                              JOIN pg_namespace s ON t.relnamespace = s.oid
                              WHERE t.relkind = 'r' AND t.relname = @t AND a.attnum > 0 AND (a.attidentity IN ('a','d') OR a.atthasdef)
                              AND (
                                a.attidentity IN ('a','d')
                                OR EXISTS (
                                  SELECT 1 FROM pg_get_expr(d.adbin, d.adrelid) AS def
                                  JOIN pg_attrdef d ON d.adrelid = t.oid AND d.adnum = a.attnum
                                  WHERE pg_get_expr(d.adbin, d.adrelid) LIKE 'nextval%'
                                )
                              )";
                var rowsPg = await conn.QueryAsync<string>(sqlPg, new { t = table });
                return new HashSet<string>(rowsPg, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}