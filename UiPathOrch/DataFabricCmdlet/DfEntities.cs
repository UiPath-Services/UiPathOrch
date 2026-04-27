using System.Text.Json.Serialization;

namespace UiPath.PowerShell.Entities;

// UiPath Data Fabric (DataService) — entity schema returned by GET /dataservice_/api/Entity
public class DfEntity
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? displayName { get; set; }
    public int? entityTypeId { get; set; }
    public string? entityType { get; set; }
    public string? description { get; set; }
    public string? folderId { get; set; }
    public DfField[]? fields { get; set; }
}

public class DfField
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? displayName { get; set; }
    public string? description { get; set; }
    public bool? isPrimaryKey { get; set; }
    public bool? isForeignKey { get; set; }
    public bool? isExternalField { get; set; }
    public bool? isHiddenField { get; set; }
    public bool? isUnique { get; set; }
    public bool? isRequired { get; set; }
    public bool? isEncrypted { get; set; }
    public bool? isSystemField { get; set; }
    public bool? isAttachment { get; set; }
    public bool? isRbacEnabled { get; set; }
    public bool? isModelReserved { get; set; }
    public int? fieldCategoryId { get; set; }
    public string? referenceType { get; set; }
    public DfSqlType? sqlType { get; set; }
}

public class DfSqlType
{
    public string? name { get; set; }
    public int? lengthLimit { get; set; }
}

public class DfQueryResponse
{
    public int? totalRecordCount { get; set; }
    public System.Text.Json.JsonElement[]? value { get; set; }
}
