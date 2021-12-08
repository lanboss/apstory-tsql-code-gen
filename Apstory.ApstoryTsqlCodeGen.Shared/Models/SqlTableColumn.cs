namespace Apstory.ApstoryTsqlCodeGen.Shared.Models
{
    public class SqlTableColumn
    {
        public string ColumnName { get; set; }
        public int ColumnId { get; set; }
        public string ColumnType { get; set; }
        public bool IsNullable { get; set; }
        public string ForeignTable { get; set; }
    }
}
