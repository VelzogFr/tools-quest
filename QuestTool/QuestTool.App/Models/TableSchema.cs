using System.Collections.Generic;

namespace QuestTool.App.Models
{
    public sealed class TableSchema
    {
        public string TableName { get; set; }
        public List<ColumnSchema> Columns { get; set; }

        public TableSchema()
        {
            TableName = string.Empty;
            Columns = new List<ColumnSchema>();
        }
    }

    public sealed class ColumnSchema
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsIdentity { get; set; }

        public ColumnSchema()
        {
            Name = string.Empty;
            DataType = string.Empty;
        }
    }
}