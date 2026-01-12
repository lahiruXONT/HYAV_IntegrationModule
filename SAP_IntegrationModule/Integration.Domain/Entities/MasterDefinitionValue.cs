namespace Integration.Domain.Entities;

public class MasterDefinitionValue : BaseAuditableEntity
{
    public int RecordID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;

    public string MasterGroup { get; set; } = string.Empty;

    public string MasterGroupValue { get; set; } = string.Empty;

    public string MasterGroupValueDescription { get; set; } = string.Empty;

    public string? ParentMasterGroup { get; set; }

    public string? ParentMasterGroupValue { get; set; }

    public string GroupType { get; set; } = string.Empty;

    public string Status { get; set; } = "1";
}

public class MasterDefinition : BaseAuditableEntity
{
    public int RecordID { get; set; }
    public string BusinessUnit { get; set; } = string.Empty;

    public string MasterGroup { get; set; } = string.Empty;

    public string GroupDescription { get; set; } = string.Empty;

    public string Status { get; set; } = "1";
}
