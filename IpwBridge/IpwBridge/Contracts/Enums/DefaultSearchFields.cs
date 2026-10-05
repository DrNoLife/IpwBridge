namespace IpwBridge.Contracts.Enums;

/// <summary>
/// The standard fields every Metazo object has, usable as search fields in a <see cref="ListRequest"/>.
/// </summary>
/// <remarks>
/// Converted to the lowercase field id the API expects (for example <c>ObjectId</c> becomes <c>objectid</c>).
/// </remarks>
public enum DefaultSearchFields
{
    /// <summary>The object id.</summary>
    ObjectId,
    /// <summary>The site the object belongs to.</summary>
    Site,
    /// <summary>The datatype of the object.</summary>
    Type,
    /// <summary>The file size (binfiles).</summary>
    Filesize,
    /// <summary>The object id of the user who created the object.</summary>
    CreatedBy,
    /// <summary>When the object was created.</summary>
    Created,
    /// <summary>The object id of the user who last changed the object.</summary>
    ChangedBy,
    /// <summary>When the object was last changed.</summary>
    Changed,
    /// <summary>The object id of the user who checked the object.</summary>
    CheckedBy,
    /// <summary>When the object was checked.</summary>
    Checked,
    /// <summary>When the object was deleted.</summary>
    DeletedTime,
    /// <summary>The object id of the user who deleted the object.</summary>
    DeletedBy,
    /// <summary>The sort order among siblings.</summary>
    ChildOrder,
    /// <summary>The object id of the parent object.</summary>
    ParentId,
    /// <summary>The object this object is a copy of.</summary>
    CopyOf,
    /// <summary>The language code of the object.</summary>
    Language,
    /// <summary>Whether the object is approved.</summary>
    Approved,
    /// <summary>Whether the object is active.</summary>
    Active,
    /// <summary>Whether the object is read-only.</summary>
    Readonly,
    /// <summary>Whether the object has children.</summary>
    HasChild,
    /// <summary>Whether the object has explicit permissions.</summary>
    HasPermission,
    /// <summary>Whether the object is deleted.</summary>
    Deleted,
    /// <summary>The object this is a future revision of.</summary>
    FutureRevisionOf,
    /// <summary>Whether a future revision exists.</summary>
    HasFutureRevision,
    /// <summary>Whether the object has categories.</summary>
    HasCategory,
    /// <summary>Whether the object has variants.</summary>
    HasVariant,
    /// <summary>The object this is a variant of.</summary>
    VariantOf,
    /// <summary>The object this is an old revision of.</summary>
    OldRevisionOf,
    /// <summary>Whether old revisions exist.</summary>
    HasOldRevision,
    /// <summary>Whether the object is a standard object.</summary>
    Standard,
    /// <summary>Whether the object is hidden on the web.</summary>
    WebHidden,
    /// <summary>Whether the object is hidden in the system.</summary>
    SysHidden,
    /// <summary>Whether the object uses an app.</summary>
    UseApp,
    /// <summary>The access type.</summary>
    AccessType,
    /// <summary>The ORAC permission.</summary>
    OracPermission,
    /// <summary>The access setting.</summary>
    Access
}
