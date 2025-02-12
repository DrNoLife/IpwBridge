using IpwBridge.Contracts.Enums;
using IpwBridge.Contracts.Models;

namespace IpwBridge.Contracts;

public class ListRequest
{
    public string DataType { get; set; } = String.Empty;
    public string FieldsToGet { get; set; } = String.Empty;
    public int Limit { get; set; } = 20;
    public int Offset { get; set; } = 0;
    public SearchConnection SearchAndOr { get; set; } = SearchConnector.And;
    public SearchField SearchField { get; set; } = DefaultSearchFields.Created;
    public SearchOperation SearchOperation { get; set; } = SearchOperator.GreaterEqual;
    public string SearchAfter { get; set; } = String.Empty;
    public DateTime FromDate { get; set; } = DateTime.UtcNow.AddDays(-30);
}
