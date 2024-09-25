namespace IpwBridge.Contracts;

public class IpwCrudRequest
{
    public string Datatype { get; set; } = String.Empty;
    public ModelOptions Model { get; set; }
    public string JsonData { get; set; } = String.Empty;
    public int? ObjectId { get; set; }
}

public enum ModelOptions
{
    Create,
    Update,
    Delete,
    CreateCopy
}