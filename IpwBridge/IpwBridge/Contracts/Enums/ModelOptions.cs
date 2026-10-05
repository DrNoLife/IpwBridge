namespace IpwBridge.Contracts.Enums;

/// <summary>
/// The built-in models that can be run on an object through <c>/model</c>.
/// </summary>
/// <remarks>
/// Converted to the lowercase model name the API expects (for example <c>create</c>). Datatype-specific models listed by <c>/explain</c> can be passed as strings instead.
/// </remarks>
public enum ModelOptions
{
    /// <summary>Creates a new object.</summary>
    Create,
    /// <summary>Updates an existing object. Requires an object id.</summary>
    Update,
    /// <summary>Deletes an existing object. Requires an object id.</summary>
    Delete,
    /// <summary>Creates a copy of an existing object. Requires an object id.</summary>
    CreateCopy
}
