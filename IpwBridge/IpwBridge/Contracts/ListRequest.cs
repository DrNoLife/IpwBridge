namespace IpwBridge.Contracts;

public class ListRequest
{
    public string DataType { get; set; } = String.Empty;
    public string FieldsToGet { get; set; } = String.Empty;
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public string SearchAndOr { get; set; } = "AND";
    public string SearchField { get; set; } = "created";
    public string SearchOperation { get; set; } = "GREATEREQUAL";
    public string SearchAfter { get; set; } = String.Empty;
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddDays(-30);
}
