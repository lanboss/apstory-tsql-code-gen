namespace Apstory.ApstoryTsqlCodeGen.Shared.Models
{
    public class SqlStoredProcedureParams
    {
        public string RoutineName { get; set; }
        public string ParameterName { get; set; }
        public string TypeName { get; set; }
        public string ColumnType { get; set; }
        public string ParamOrder { get; set; }
        public bool IsNullable { get; set; }
    }
}
