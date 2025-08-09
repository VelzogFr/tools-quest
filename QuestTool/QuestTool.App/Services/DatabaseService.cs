using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;

namespace QuestTool.App.Services
{
    public sealed class DatabaseService
    {
        public string ProviderInvariant { get; }
        private readonly string _connectionString;
        private readonly DbProviderFactory _factory;

        public DatabaseService(string connectionString, string providerInvariant)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException("connectionString");
            ProviderInvariant = providerInvariant ?? throw new ArgumentNullException("providerInvariant");
            _factory = DbProviderFactories.GetFactory(ProviderInvariant);
        }

        public IDbConnection CreateConnection()
        {
            var conn = _factory.CreateConnection();
            if (conn == null) throw new InvalidOperationException("Impossible de créer la connexion DB");
            conn.ConnectionString = _connectionString;
            return conn;
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var conn = CreateConnection())
            {
                await ((DbConnection)conn).OpenAsync();
                return await conn.ExecuteAsync(sql, param);
            }
        }

        public async Task InsertAsync(string tableName, Dictionary<string, object> values)
        {
            if (values == null || values.Count == 0) return;

            var quotedColumns = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var kvp in values)
            {
                quotedColumns.Add(QuoteIdentifier(kvp.Key));
                parameters.Add(kvp.Key, kvp.Value);
            }

            var columnList = string.Join(", ", quotedColumns);
            var placeholders = "@" + string.Join(", @", values.Keys);

            var sql = string.Format("INSERT INTO {0} ({1}) VALUES ({2});", QuoteIdentifier(tableName), columnList, placeholders);
            await ExecuteAsync(sql, parameters);
        }

        public string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return identifier;
            var inv = ProviderInvariant.ToLowerInvariant();
            if (inv.IndexOf("sqlclient", StringComparison.Ordinal) >= 0)
            {
                // SQL Server
                return "[" + identifier.Replace("]", "]]") + "]";
            }
            if (inv.IndexOf("mysql", StringComparison.Ordinal) >= 0)
            {
                return "`" + identifier.Replace("`", "``") + "`";
            }
            // Default and PostgreSQL
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }
    }
}