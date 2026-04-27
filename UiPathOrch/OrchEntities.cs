using System.Collections;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities.JsonConverter;

#pragma warning disable IDE1006 // Naming styles

namespace UiPath.PowerShell.Entities;

public class HttpBodyValue<T>
{
    public T? value { get; set; }
}

public class HttpBodyValues<T>
{
    // The following member can get the total element count, but using it would only
    // avoid the waste of fetching zero elements on the next call when there are exactly 1000 elements.
    // It feels safer to process without depending on @odata.count, so we don't use it.
    //[JsonPropertyName("@odata.count")]
    //public int OdataCount { get; set; }
    public T[]? value { get; set; }
}

public class HttpBodyResults<T>
{
    public T[]? results { get; set; }
}

public class OrchPSDrive
{
    public string? Provider { get; set; } = "UiPathOrch";
    public string? Name { get; set; }
    public string? Root { get; set; }
    public string? IdentityUrl { get; set; }
    public bool? IsConfidential { get; set; }
    public string? Description { get; set; }
    public string? Scope { get; set; }
    //public string? BaseUrl { get; set; }
    //public string? TenancyName { get; set; }
    public string? AppId { get; set; }
    public string? RedirectUrl { get; set; }
    public string? HttpListener { get; set; }
    public ProxySettings? ProxySettings { get; set; }
    public double? ApiVersion { get; set; }
    public string? CurrentUser { get; set; }
    public string? CurrentLocation { get; set; }
    public string? PartitionGlobalId { get; set; }
    public int? TenantId { get; set; }
    public string? TenantKey { get; set; }
    public string? AccessToken { get; set; }
    public PSObject? Claims { get; set; }

    private static readonly HashSet<string> _unixTimestampClaims = ["exp", "iat", "nbf", "auth_time"];

    private static PSObject? ParseJwtClaims(string? token)
    {
        if (string.IsNullOrEmpty(token)) return null;

        var parts = token.Split('.');
        if (parts.Length != 3) return null;

        try
        {
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            payload = payload.Replace('-', '+').Replace('_', '/');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

            using var doc = JsonDocument.Parse(json);
            var claims = new PSObject();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                object value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString()!,
                    JsonValueKind.Number when _unixTimestampClaims.Contains(prop.Name)
                        => DateTimeOffset.FromUnixTimeSeconds(prop.Value.GetInt64()).LocalDateTime,
                    JsonValueKind.Number => prop.Value.GetInt64(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Array => prop.Value.EnumerateArray().Select(e => e.ToString()).ToArray(),
                    _ => prop.Value.ToString()
                };
                claims.Properties.Add(new PSNoteProperty(prop.Name, value));
            }
            return claims;
        }
        catch
        {
            return null;
        }
    }

    public OrchPSDrive(OrchDriveInfo drive)
    {
        Name = drive.Name;
        Root = drive.DisplayRoot;
        IdentityUrl = drive._psDrive.IdentityUrl;
        IsConfidential = drive.OrchAPISession.AuthManager.IsConfidentialApp;
        Description = drive.Description;
        Scope = drive._psDrive.Scope;
        AppId = drive._psDrive.AppId;
        RedirectUrl = drive._psDrive.RedirectUrl;
        HttpListener = drive._psDrive.HttpListener;
        ApiVersion = drive.OrchAPISession.ApiVersion;
        CurrentUser = drive.OrchAPISession.AuthManager.IsConfidentialApp ? "N/A" : drive._dicCurrentUser?.UserName;
        CurrentLocation = drive.CurrentLocation;
        PartitionGlobalId = drive._dicPartitionGlobalId;
        TenantId = drive._dicTenantId;
        TenantKey = drive._dicTenantKey;
        AccessToken = drive.OrchAPISession.AuthManager.AccessToken;
        Claims = ParseJwtClaims(AccessToken);
        if (drive._psDrive.Proxy is not null)
        {
            ProxySettings = new()
            {
                UseDefaultWebProxy = drive._psDrive.Proxy.UseDefaultWebProxy,
                Url = drive._psDrive.Proxy.Url,
                BypassProxyOnLocal = drive._psDrive.Proxy.BypassProxyOnLocal,
                UseDefaultCredentials = drive._psDrive.Proxy.UseDefaultCredentials,
                Enabled = drive._psDrive.Proxy.Enabled
            };
            if (drive._psDrive.Proxy.Credentials is not null)
            {
                ProxySettings.Credentials = new()
                {
                    Username = drive._psDrive.Proxy.Credentials.Username
                    // Do not include the Password
                };
            }
        }
    }

    public OrchPSDrive(OrchDuDriveInfo drive) : this(drive.ParentDrive)
    {
        Provider = "UiPathOrchDu";
        Name = drive.Name;
        CurrentLocation = drive.CurrentLocation;
    }

    public OrchPSDrive(OrchTmDriveInfo drive) : this(drive.ParentDrive)
    {
        Provider = "UiPathOrchTm";
        Name = drive.Name;
        CurrentLocation = drive.CurrentLocation;
    }
}

public class RemoteControlStart
{
    public string? uri { get; set; }
    public bool? allowsRemoteControl { get; set; }
}

public class PackageEntryPoint
{
    public string? UniqueId { get; set; }
    public string? Path { get; set; }
    public string? InputArguments { get; set; }
    public string? OutputArguments { get; set; }
    public Int64? Id { get; set; }
}

#region deprecated login method
// LoginModel
public class LoginModel
{
    public string? tenancyName { get; set; }
    public string? usernameOrEmailAddress { get; set; }
    public string? password { get; set; }
}

// ValidationErrorInfo
public class ValidationErrorInfo
{
    public string? message { get; set; }
    public string[]? members { get; set; }
}

// ErrorInfo
public class ErrorInfo
{
    public int? code { get; set; }
    public string? message { get; set; }
    public string? details { get; set; }
    public ValidationErrorInfo[]? validationErrors { get; set; }
}

// AjaxResponse
public class AjaxResponse
{
    public string? result { get; set; }
    public string? targetUrl { get; set; }
    public bool? success { get; set; }
    public ErrorInfo? error { get; set; }
    public bool? unAuthorizedRequest { get; set; }
    public bool? __abp { get; set; }
}

#endregion

#region Tenant Entities

public class SignalR
{
    public string? Url { get; set; }
    public bool? SkipNegotiation { get; set; }
}

// SettingsDto
public class Settings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Scope { get; set; }
}

// ActivitySettingsDto
public class ActivitySettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? ApiVersion { get; set; }
    public SignalR? SignalR { get; set; }
}

// LicenseDto
public class License
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? ExpireDate { get; set; }
    public Int64? HostLicenseId { get; set; } // removed in V19.0
    public Int64? GracePeriodEndDate { get; set; }
    public Int64? GracePeriod { get; set; }
    public string? VersionControl { get; set; }
    public Dictionary<string, Int64>? Allowed { get; set; }
    public Dictionary<string, Int64>? Used { get; set; }
    public bool? AttendedConcurrent { get; set; }
    public bool? DevelopmentConcurrent { get; set; }
    public bool? StudioXConcurrent { get; set; }
    public bool? StudioProConcurrent { get; set; }
    public string[]? LicensedFeatures { get; set; }
    public bool? IsRegistered { get; set; }
    public bool? IsCommunity { get; set; }
    public bool? IsProOrEnterprise { get; set; }
    public string? SubscriptionCode { get; set; }
    public string? SubscriptionPlan { get; set; }
    public bool? IsExpired { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? Code { get; set; }
    public bool? UserLicensingEnabled { get; set; }
}

// UpdateSettingsDto
public class UpdateSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? UpdateServerSource { get; set; }
    public string? UpdateServerUrl { get; set; }
    public int? PollingInterval { get; set; }
}

// ExecutionSettingDefinition
public class ExecutionSettingDefinition
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Scope { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathScope { get; set; } // added by UiPathOrch

    public string? Key { get; set; }
    public string? DisplayName { get; set; }
    public string? ValueType { get; set; }
    public string? DefaultValue { get; set; }
    public string[]? PossibleValues { get; set; }
}

// ExecutionSettingsConfiguration
public class ExecutionSettingsConfiguration
{
    public string? Scope { get; set; }
    public ExecutionSettingDefinition[]? Configuration { get; set; }
}

// ResponseDictionaryDto
public class ResponseDictionary
{
    public string[]? Keys { get; set; }
    public string[]? Values { get; set; }
}

public class ResponseDictionaryItem // added by UiPathOrch
{
    public string? Path { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}

// ODataValueOfString
public class ODataValueOfString
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? value { get; set; }
}

// CredentialStoreDetailsDto
public class CredentialStoreDetails
{
    public bool? IsReadOnly { get; set; }
}

// DefaultCredentialStoreDto
public class DefaultCredentialStore
{
    public string? ResourceType { get; set; }
    public Int64? Id { get; set; }
}

// CredentialStoreDto
public class CredentialStore
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public Int64? ProxyId { get; set; }
    public string? ProxyType { get; set; }
    public string? HostName { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? AdditionalConfiguration { get; set; }
    public CredentialStoreDetails? Details { get; set; }
    public DefaultCredentialStore[]? DefaultCredentialStores { get; set; }
}

// WebhookEventDto
public class WebhookEvent
{
    public string? EventType { get; set; }
}

// WebhookDto
public class Webhook
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; }
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public bool? Enabled { get; set; }
    public string? Secret { get; set; }
    public bool? SubscribeToAllEvents { get; set; }
    public bool? AllowInsecureSsl { get; set; }
    public WebhookEvent[]? Events { get; set; }
    public string? EventType { get; set; }
}

// WebhookEventTypeDto
public class WebhookEventType
{
    public string? Name { get; set; }
    public string? Group { get; set; }
}

// BusinessRuleDto
public class BusinessRule
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Id { get; set; } // uuid
    public string? Name { get; set; }
    public string? Description { get; set; }
    public BusinessRuleVersion? CurrentVersion { get; set; }
    public int? FoldersCount { get; set; }
    public Tag[]? Tags { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// BusinessRuleVersionDto
public class BusinessRuleVersion
{
    public string? BusinessRuleKey { get; set; } // uuid
    public string? VersionNumber { get; set; }
    public Int64? CreatorUserId { get; set; }
    public bool? IsActive { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? Id { get; set; } // uuid
}

// PingEventDto (response of POST /odata/Webhooks({key})/UiPath.Server.Configuration.OData.Ping)
public class WebhookPingResult
{
    public string? Type { get; set; }
    public string? EventId { get; set; }
    public string? EntityKey { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Timestamp { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? EventTime { get; set; }
    public Int64? EventSourceId { get; set; }
    public int? TenantId { get; set; }
    public Int64? OrganizationUnitId { get; set; }
}

//public enum AlertSeverity
//{
//    Info = 0,
//    Success,
//    Warn,
//    Error,
//    Fatal
//}

// AlertDto
public class Alert
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    public string? NotificationName { get; set; }
    public string? Data { get; set; }
    public string? Component { get; set; }
    public string? Severity { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? State { get; set; }
    public string? UserNotificationId { get; set; }
}

public class LibraryFeed
{
    public string? name { get; set; }
    public string? purpose { get; set; }
    public bool isShared { get; set; }
    public bool isPublic { get; set; }
    public bool isExternal { get; set; }
    public string? feedUrl { get; set; }
    public string? publishUrl { get; set; }
    public string? authenticationType { get; set; }
    public string? apiKey { get; set; }
    public string? basicUserName { get; set; }
    public string? basicPassword { get; set; }
    public string? folderId { get; set; }
    public string[]? supportedProjectTypes { get; set; }
    public string? id { get; set; } // Guid
}

// LibraryDto
public class Library
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Created { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastUpdated { get; set; }
    public string? Owners { get; set; }
    public string? IconUrl { get; set; }
    public string? Summary { get; set; }
    public Int64? PackageSize { get; set; }
    public bool? IsPrerelease { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Published { get; set; }
    public bool? IsLatestVersion { get; set; }
    public string? OldVersion { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Authors { get; set; }
    public string? ProjectType { get; set; }
    public string? Tags { get; set; }
    public bool? IsCompiled { get; set; }
    public string? LicenseUrl { get; set; }
    public string? ProjectUrl { get; set; }
    public Tag[]? ResourceTags { get; set; }
}

// LibraryDto
public class LibraryVersion
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    //public string? Name { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Created { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastUpdated { get; set; }
    public string? Owners { get; set; }
    public string? IconUrl { get; set; }
    public string? Summary { get; set; }
    public Int64? PackageSize { get; set; }
    public bool? IsPrerelease { get; set; }
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Published { get; set; }
    public bool? IsLatestVersion { get; set; }
    public string? OldVersion { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Authors { get; set; }
    public string? ProjectType { get; set; }
    public string? Tags { get; set; }
    public bool? IsCompiled { get; set; }
    public string? LicenseUrl { get; set; }
    public string? ProjectUrl { get; set; }
    public Tag[]? ResourceTags { get; set; }
}

// ODataValueOfIEnumerableOfProcessDto
public class Package
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    public bool? IsActive { get; set; }
    //Arguments	ArgumentMetadata{...}
    public bool? SupportsMultipleEntryPoints { get; set; }
    public string? MainEntryPointPath { get; set; }
    public bool? RequiresUserInteraction { get; set; }
    public bool? IsAttended { get; set; }
    public string? TargetFramework { get; set; }
    //EntryPoints	[
    public string? Title { get; set; }
    public string? Version { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Published { get; set; }
    public bool? IsLatestVersion { get; set; }
    public string? OldVersion { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? Authors { get; set; }
    public string? ProjectType { get; set; }
    public string? Tags { get; set; }
    public bool? IsCompiled { get; set; }
    public string? LicenseUrl { get; set; }
    public string? ProjectUrl { get; set; }
    //ResourceTags	[
}

// BulkItemDtoOfString
public class BulkItemDtoOfString
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Key { get; set; }
    public string? Status { get; set; }
    public string? Body { get; set; }
}

// MachineVpnSettingsDto
public class MachineVpnSettings
{
    public string? cidr { get; set; }
}

// UpdateInfoDto
// TODO: confirm case
public class UpdateInfo
{
    public string? UpdateStatus { get; set; }
    public string? Reason { get; set; }
    public string? TargetUpdateVersion { get; set; }
    public bool? IsCommunity { get; set; }
    public string? StatusInfo { get; set; }

}

public class ExtendedMachine
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public long? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? LicenseKey { get; set; } // Guid
    public string? ClientSecret { get; set; }
    public string? Scope { get; set; }
    public int? NonProductionSlots { get; set; }
    public int? UnattendedSlots { get; set; }
    public int? HeadlessSlots { get; set; }
    public int? TestAutomationSlots { get; set; }
    public int? AutomationCloudSlots { get; set; }
    public int? AutomationCloudTestAutomationSlots { get; set; }
    public string? Key { get; set; }
    public string? EndpointDetectionStatus { get; set; }
    public MachinesRobotVersion[]? RobotVersions { get; set; }
    public List<RobotUser>? RobotUsers { get; set; }
    public string? AutomationType { get; set; }
    public string? TargetFramework { get; set; }
    public UpdatePolicy? UpdatePolicy { get; set; }
    public Tag[]? Tags { get; set; }
    public MaintenanceWindow? MaintenanceWindow { get; set; }
    public MachineVpnSettings? VpnSettings { get; set; }
    public UpdateInfo? UpdateInfo { get; set; }

}

// CreatedMachine // added by UiPathOrch
// Needed to switch view definitions
public class CreatedMachine : ExtendedMachine
{
}

// Receives the result from the API when creating a SecretKey
public class MachineClientSecretResponse // added by UiPathOrch
{
    public Int64? id { get; set; }
    public string? secret { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
}

// Output AddMachineSecretKeyResponse from a cmdlet
public class MachineSecretKey // added by UiPathOrch
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    public string? ClientId { get; set; } // Guid
    public Int64? SecretId { get; set; }
    public string? ClientSecret { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Scope { get; set; }
}

//public class CreatedMachineSecretKey : ExtendedMachine
//{
//    public Int64? SecretId { get; set; }
//    public DateTime? SecretCreationTime { get; set; }
//}

// UserRobotsDto
public class UserRobots
{
    public string? UserName { get; set; }
    public string[]? RobotNames { get; set; }
}

// UserRoleDto
public class UserRole
{
    public Int64? UserId { get; set; }
    public int? RoleId { get; set; }
    public string? UserName { get; set; }
    public string? RoleName { get; set; }
    public string? RoleType { get; set; }
    public Int64? Id { get; set; }
}

// OrganizationUnitDto
public class OrganizationUnit
{
    public string? DisplayName { get; set; }
    public Int64? Id { get; set; }
}

// AttendedRobotDto
public class AttendedRobot
{
    public string? UserName { get; set; }
    public ExecutionSettings? ExecutionSettings { get; set; }
    public Int64? RobotId { get; set; }
    public string? RobotType { get; set; }
}

// ExecutionSettings // added by UiPathOrch
public class ExecutionSettings
{
    public string? TracingLevel { get; set; }
    public bool? StudioNotifyServer { get; set; }
    public bool? LoginToConsole { get; set; }

    // Can be either string or int depending on the Orchestrator version.
    [JsonConverter(typeof(StringOrIntConverter))]
    public int? ResolutionWidth { get; set; }

    // Can be either string or int depending on the Orchestrator version.
    [JsonConverter(typeof(StringOrIntConverter))]
    public int? ResolutionHeight { get; set; }

    // Can be either string or int depending on the Orchestrator version.
    [JsonConverter(typeof(StringOrIntConverter))]
    public int? ResolutionDepth { get; set; }

    public bool? FontSmoothing { get; set; }
    public bool? AutoDownloadProcess { get; set; }
}

// UnattendedRobotDto
public class UnattendedRobot
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public string? CredentialType { get; set; }
    public string? CredentialExternalName { get; set; }
    public ExecutionSettings? ExecutionSettings { get; set; }
    public bool? LimitConcurrentExecution { get; set; }
    public Int64? RobotId { get; set; }
    public int? MachineMappingsCount { get; set; }
}

public class UserNotificationSubscription
{
    public bool? Queues { get; set; }
    public bool? Robots { get; set; }
    public bool? Jobs { get; set; }
    public bool? Schedules { get; set; }
    public bool? Tasks { get; set; }
    public bool? QueueItems { get; set; }
    public bool? Insights { get; set; }
    public bool? CloudRobots { get; set; }
    public bool? Serverless { get; set; }
    public bool? Export { get; set; }
    public bool? RateLimitsDaily { get; set; }
    public bool? RateLimitsRealTime { get; set; }
    public bool? AutopilotForRobotsDetectedIssues { get; set; }
    public bool? Webhooks { get; set; }
}

// added by UiPathOrch
// Appears to be undocumented. The proper class name is unknown, so this is a placeholder.
public class AvailableVersions
{
    public string[]? availableVersions { get; set; }
}

// UserDto
public class User
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? UserName { get; set; }
    public string? Domain { get; set; }
    public string? DirectoryIdentifier { get; set; } // Guid
    public string? FullName { get; set; }
    public string? EmailAddress { get; set; }
    public bool? IsEmailConfirmed { get; set; } // deprecated in V18.0? removed in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastLoginTime { get; set; }
    public bool? IsActive { get; set; } // deprecated in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? AuthenticationSource { get; set; }
    public string? Password { get; set; } // deprecated in V19.0
    public bool? IsExternalLicensed { get; set; }
    public UserRole[]? UserRoles { get; set; }
    public string[]? RolesList { get; set; }
    // TODO: ExternalRoles is missing.
    public string[]? LoginProviders { get; set; }
    public List<OrganizationUnit>? OrganizationUnits { get; set; } // deprecated in V19.0
    public int? TenantId { get; set; }
    public string? TenancyName { get; set; }
    public string? TenantDisplayName { get; set; }
    public string? TenantKey { get; set; }
    public string? Type { get; set; }
    public string? ProvisionType { get; set; }
    public string? LicenseType { get; set; }
    public AttendedRobot? RobotProvision { get; set; }
    public UnattendedRobot? UnattendedRobot { get; set; }
    public UserNotificationSubscription? NotificationSubscription { get; set; }
    public string? Key { get; set; } // Guid
    public bool? MayHaveUserSession { get; set; }
    public bool? MayHaveRobotSession { get; set; }
    public bool? MayHaveUnattendedSession { get; set; }
    public bool? MayHavePersonalWorkspace { get; set; }
    public bool? RestrictToPersonalWorkspace { get; set; }
    public bool? BypassBasicAuthRestriction { get; set; } // TODO: Huh? Was this removed in api v18.0? Is it safe to exclude?
    public UpdatePolicy? UpdatePolicy { get; set; }
    public string? AccountId { get; set; }
    public bool? HasOnlyInheritedPrivileges { get; set; }
    public bool? ExplicitMayHaveUserSession { get; set; }
    public bool? ExplicitMayHaveRobotSession { get; set; }
    public bool? ExplicitMayHavePersonalWorkspace { get; set; }
    public bool? ExplicitRestrictToPersonalWorkspace { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    public Int64? CreatorUserId { get; set; }
}

public class Item_IdName // added by UiPathOrch The proper class name is unknown.
{
    public Int64? id { get; set; }
    public string? name { get; set; }
}

public class UP_RoleEntry // added by UiPathOrch The proper class name is unknown.
{
    public string? type { get; set; }
    public Item_IdName? value { get; set; }
}

public class UP_Roles // added by UiPathOrch The proper class name is unknown.
{
    public UP_RoleEntry[]? @explicit { get; set; }
    public UP_RoleEntry[]? inherited { get; set; }
    public UP_RoleEntry[]? effective { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonTools.jsoOneLine);
    }
}

public class Item_TypeValue // added by UiPathOrch The proper class name is unknown.
{
    public string? type { get; set; }
    public string? value { get; set; }
}

public class UP_Access // added by UiPathOrch The proper class name is unknown.
{
    public Item_TypeValue? @explicit { get; set; }
    public Item_TypeValue[]? inherited { get; set; }
    public Item_TypeValue? effective { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonTools.jsoOneLine);
    }
}

public class UP_PermissionEntry // added by UiPathOrch The proper class name is unknown.
{
    public string? type { get; set; }
    public bool? value { get; set; }
    public Item_IdName[]? groups { get; set; }
}

public class UP_ProjectPermission // added by UiPathOrch The proper class name is unknown.
{
    public UP_PermissionEntry? @explicit { get; set; }
    public UP_PermissionEntry? inherited { get; set; }
    public UP_PermissionEntry? effective { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonTools.jsoOneLine);
    }
}

public class UP_UpdatePolicyValue // added by UiPathOrch The proper class name is unknown.
{
    public string? type { get; set; }
    public string? specificVersion { get; set; }
}

public class UP_UpdatePolicyEntry // added by UiPathOrch The proper class name is unknown.
{
    public string? type { get; set; }
    public UP_UpdatePolicyValue? value { get; set; }
}

public class UP_UpdatePolicy // added by UiPathOrch The proper class name is unknown.
{
    public UP_UpdatePolicyEntry? @explicit { get; set; }
    public UP_UpdatePolicyEntry[]? inherited { get; set; }
    public UP_UpdatePolicyEntry? effective { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonTools.jsoOneLine);
    }
}

public class UserPrivilege // added by UiPathOrch The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? UserName { get; set; } // added by UiPathOrch

    public Int64 userId { get; set; }
    public UP_Roles? roles { get; set; }
    public UP_Access? access { get; set; }
    public UP_ProjectPermission? attendedSession { get; set; }
    public UP_ProjectPermission? personalWorkspace { get; set; }
    public UP_UpdatePolicy? updatePolicy { get; set; }
}

// DirectoryObjectDto
public class DirectoryObject
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    //  0: User, 1: Group, 2: Machine, 3: Robot, 4: ExternalApplication
    public int? type { get; set; }
    public string? source { get; set; }
    public string? domain { get; set; }
    public string? identifier { get; set; } // "source|guid"
    public string? identityName { get; set; }
    public string? displayName { get; set; }
}

// DirectoryEntityInfo
public class PmDirectoryEntityInfo
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? source { get; set; }
    public string? identifier { get; set; } // Guid
    public string? identityName { get; set; }
    public string? displayName { get; set; }
    public string? email { get; set; }
    public string? domain { get; set; }
    public int? type { get; set; }
    public string? objectType { get; set; }
}

public class Preferences // added by UiPathOrch
{
    public string? RedesignPreference { get; set; }
    public string? RemoteControlPreference { get; set; }
}

// no documentation, added by UiPathOrch.
// Please be aware that this class inherits from the User class.
public class ExtendedUser : User
{
    public Folder? PersonalWorkspace { get; set; }
    public string? PersonalWorskpaceFeedId { get; set; } // ISSUE: TYPO
    public Int64? VirtualFolderId { get; set; }
    public Preferences? Preferences { get; set; }
}

// UserEntityDto
public class UserEntity
{
    public string? FullName { get; set; }
    public string? AuthenticationSource { get; set; }
    public string? UserName { get; set; }
    public bool? IsInherited { get; set; }
    public Int64[]? AssignedToFolderIds { get; set; }
    public bool? MayHaveAttended { get; set; }
    public bool? MayHaveUnattended { get; set; }
    public string? Type { get; set; }
    public Int64? Id { get; set; }
}

public class MachinesRobotVersion
{
    public long? Count { get; set; }
    public string? Version { get; set; }
    public long? MachineId { get; set; }
}

// RobotUserDto
public class RobotUser
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Machine { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathMachine { get; set; } // added by UiPathOrch
    public string? UserName { get; set; }
    public Int64? RobotId { get; set; }
    public bool? HasTriggers { get; set; }
}


// UpdatePolicyDto
public class UpdatePolicy
{
    public string? Type { get; set; }
    public string? SpecificVersion { get; set; }
}

// SessionMaintenanceModeParameters
public class SessionMaintenanceModeParameters
{
    public Int64? sessionId { get; set; }
    // "Default" or "Enabled"
    public string? maintenanceMode { get; set; }
    // "SoftStop" or "Kill"
    public string? stopJobsStrategy { get; set; }
}

// MaintenanceWindowDto
public class MaintenanceWindow
{
    public bool? Enabled { get; set; }
    public string? JobStopStrategy { get; set; }
    public string? CronExpression { get; set; }
    public string? TimezoneId { get; set; }
    public int? Duration { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? NextExecutionTime { get; set; }
}

// MachineFolderDto
public class MachineFolder
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? LicenseKey { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? Scope { get; set; }
    public int? NonProductionSlots { get; set; }
    public int? UnattendedSlots { get; set; }
    public int? HeadlessSlots { get; set; }
    public int? TestAutomationSlots { get; set; }
    public int? AutomationCloudSlots { get; set; }
    public int? AutomationCloudTestAutomationSlots { get; set; }
    public string? Key { get; set; }
    public string? EndpointDetectionStatus { get; set; }
    public MachinesRobotVersion[]? RobotVersions { get; set; }
    public RobotUser[]? RobotUsers { get; set; }
    public string? AutomationType { get; set; }
    public string? TargetFramework { get; set; }
    public UpdatePolicy? UpdatePolicy { get; set; }
    public string? ClientSecret { get; set; }
    public Tag[]? Tags { get; set; }
    public MaintenanceWindow? MaintenanceWindow { get; set; }
    public MachineVpnSettings? VpnSettings { get; set; }
    public Int64? Id { get; set; }
    public bool? IsAssignedToFolder { get; set; }
    public bool? HasMachineRobots { get; set; }
    public bool? IsInherited { get; set; }
    public bool? PropagateToSubFolders { get; set; }
    public string? InheritedFromFolderName { get; set; }
    public UpdateInfo? UpdateInfo { get; set; }
}

// MachinesFolderAssociationsDto
public class MachinesFolderAssociations
{
    public Int64? FolderId { get; set; }
    public Int64[]? AddedMachineIds { get; set; }
    public Int64[]? RemovedMachineIds { get; set; }
}

// UpdateMachinesToFolderAssociationsRequest
public class UpdateMachinesToFolderAssociationsRequest
{
    public MachinesFolderAssociations? associations { get; set; }
}

// SimpleFolderDto
public class SimpleFolder
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathName { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? FullyQualifiedName { get; set; }
}

// AssetFoldersShareDto
public class AssetFoldersShare
{
    public List<Int64>? AssetIds { get; set; }
    public List<Int64>? ToAddFolderIds { get; set; }
    public List<Int64>? ToRemoveFolderIds { get; set; }
}

public enum RoleType
{
    Mixed, Tenant, Folder
}

public enum RoleScope
{
    Global, Folder, GlobalOrFolder
}

// PermissionDto
public class Permission
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsGranted { get; set; }
    public int? RoleId { get; set; }
    public string? Scope { get; set; }
    public Int64? Id { get; set; }
}

// Role
public class Role
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; } // According to the swagger doc, this should be int, but...
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
    public string? Groups { get; set; }
    public bool? IsStatic { get; set; }
    public bool? IsEditable { get; set; }
    public List<Permission>? Permissions { get; set; }
}

// SimpleRoleDto
public class SimpleRole
{
    public string? Origin { get; set; }
    public string? RoleType { get; set; }
    public SimpleFolder? InheritedFromFolder { get; set; }
    public string? Name { get; set; }
    public Int64? Id { get; set; }
}

// UserRolesDto
public class UserRoles
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public UserEntity? UserEntity { get; set; }
    public List<SimpleRole>? Roles { get; set; }
    public bool? HasAlertsEnabled { get; set; }
}

public enum UserType
{
    User, Robot, DirectoryUser, DirectoryGroup, DirectoryRobot, DirectoryExternalApplication
}

// FolderRolesDto
public class FolderRoles
{
    public Int64? FolderId { get; set; }
    public List<Int64>? RoleIds { get; set; }
}

// DomainUserAssignmentDto
public class DomainUserAssignment
{
    public string? Domain { get; set; }
    public string? UserName { get; set; }
    public string? DirectoryIdentifier { get; set; }
    public string? UserType { get; set; }
    public List<FolderRoles>? RolesPerFolder { get; set; }
}

#endregion

#region Folder Entities

// FolderDto
public class Folder
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? FullName { get; set; } // added by UiPathOrch

    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? DisplayName { get; set; }
    public string? FullyQualifiedName { get; set; }
    public string? FullyQualifiedNameOrderable { get; set; }
    public string? Description { get; set; }
    public string? FolderType { get; set; }
    public string? ProvisionType { get; set; }
    public string? PermissionModel { get; set; }
    public Int64? ParentId { get; set; }
    public string? ParentKey { get; set; }
    public bool? IsActive { get; set; }
    public string? FeedType { get; set; }
}

// MachineRuntimeDto
public class MachineRuntime
{
    public string? Type { get; set; }
    public int? Total { get; set; }
    public int? Connected { get; set; }
    public int? Available { get; set; }
}

// AutopilotForRobotsSettingsDto
public class AutopilotForRobotsSettings
{
    public bool? Enabled { get; set; }
    public bool? HealingEnabled { get; set; }
}

// MachineDto
public class Machine
{
    public long? Id { get; set; } // integer with format int64
    public string? LicenseKey { get; set; } // string with minLength = 0, maxLength = 255
    public string? Name { get; set; } // string with minLength = 0, maxLength = 450
    public string? Description { get; set; } // string with minLength = 0, maxLength = 500

    // Enum for Machine Type (Standard / Template)
    public string? Type { get; set; }

    // Enum for Machine Scope (Default / Shared / PW / Cloud / Serverless)
    public string? Scope { get; set; }

    public int? NonProductionSlots { get; set; } // integer with format int32
    public int? UnattendedSlots { get; set; } // integer with format int32
    public int? HeadlessSlots { get; set; } // integer with format int32
    public int? TestAutomationSlots { get; set; } // integer with format int32
    public int? HostingSlots { get; set; } // added in V19.0
    public int? AppTestSlots { get; set; } // added in V19.0
    public int? AutomationCloudSlots { get; set; } // integer with format int32
    public int? AutomationCloudTestAutomationSlots { get; set; } // integer with format int32

    public string? Key { get; set; } // Guid

    // Enum for Endpoint Detection Status (NotAvailable / Mixed / Enabled)
    public string? EndpointDetectionStatus { get; set; }

    public MachinesRobotVersion[]? RobotVersions { get; set; } // Array of MachinesRobotVersionDto
    public RobotUser[]? RobotUsers { get; set; } // Array of RobotUserDto

    // Enum for Automation Type (Any / Foreground / Background)
    public string? AutomationType { get; set; }

    // Enum for Target Framework (Any / Windows / Portable)
    public string? TargetFramework { get; set; }

    // Enum for Serverless Licensing Model (RobotUnits / LicenseSlots), readOnly
    public string? ServerlessLicensingModel { get; set; }

    public UpdatePolicy? UpdatePolicy { get; set; } // Reference to UpdatePolicyDto
    public string? ClientSecret { get; set; } // string for client secret

    public Tag[]? Tags { get; set; } // Array of TagDto
    public MaintenanceWindow? MaintenanceWindow { get; set; } // Reference to MaintenanceWindowDto
    public MachineVpnSettings? VpnSettings { get; set; } // Reference to MachineVpnSettingsDto
}

// SimpleReleaseDto
public class SimpleRelease
{
    public long? Id { get; set; }
    public string? Key { get; set; }
    public string? ProcessKey { get; set; }
    public string? ProcessVersion { get; set; }
    public bool? IsLatestVersion { get; set; }
    public bool? IsProcessDeleted { get; set; }
    public string? Description { get; set; }
    public string? Name { get; set; }
    public long? EnvironmentId { get; set; }
    public string? EnvironmentName { get; set; }
    public Environment? Environment { get; set; }
    public long? EntryPointId { get; set; }
    public string? EntryPointPath { get; set; }
    public EntryPoint? EntryPoint { get; set; }
    public string? InputArguments { get; set; }
    public string? ProcessType { get; set; }
    public bool? SupportsMultipleEntryPoints { get; set; }
    public bool? RequiresUserInteraction { get; set; }
    public string? MinRequiredRobotVersion { get; set; } // added in V19.0
    public bool? IsAttended { get; set; }
    public bool? IsCompiled { get; set; }
    public string? AutomationHubIdeaUrl { get; set; }
    public ReleaseVersion? CurrentVersion { get; set; }
    public ReleaseVersion[]? ReleaseVersions { get; set; }
    public ArgumentMetadata? Arguments { get; set; }
    public ProcessSettings? ProcessSettings { get; set; }
    public VideoRecordingSettings? VideoRecordingSettings { get; set; }
    public bool? AutoUpdate { get; set; }
    public bool? HiddenForAttendedUser { get; set; }
    public string? FeedId { get; set; } // Guid
    public string? JobPriority { get; set; }
    public int? SpecificPriorityValue { get; set; }
    public long? OrganizationUnitId { get; set; }
    public string? OrganizationUnitFullyQualifiedName { get; set; }
    public string? TargetFramework { get; set; }
    public string? RobotSize { get; set; }
    public Tag[]? Tags { get; set; }
    public string? RemoteControlAccess { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public long? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public long? CreatorUserId { get; set; }
}

public class NameValuePair
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}

public class ResourceOverwrite
{
    public string? ResourceType { get; set; }
    public string? ResourceKey { get; set; } // Guid
    public string? EntityId { get; set; } // Guid
    public string? EntityDisplayName { get; set; }
    public Int64? EntityFolderId { get; set; }
    public NameValuePair[]? Properties2 { get; set; }
    // Properties // unknown type
}

// JobErrorDto
public class JobError
{
    public string? code { get; set; }
    public string? title { get; set; }
    public string? detail { get; set; }
    public string? category { get; set; }
    public int? status { get; set; }
}

// JobDto
public class Job
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StartTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? EndTime { get; set; }
    public string? State { get; set; }
    public string? JobPriority { get; set; }
    public int? SpecificPriorityValue { get; set; }
    public SimpleRobot? Robot { get; set; }
    public SimpleRelease? Release { get; set; }

    [JsonConverter(typeof(SafeArrayConverter<ResourceOverwrite>))]
    public ResourceOverwrite[]? ResourceOverwrites { get; set; }
    public string? Source { get; set; }
    public string? SourceType { get; set; }
    public string? BatchExecutionKey { get; set; }
    public string? Info { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? StartingScheduleId { get; set; }
    public string? ReleaseName { get; set; }
    public string? Type { get; set; }
    public string? InputArguments { get; set; }
    public string? OutputArguments { get; set; }
    public string? HostMachineName { get; set; }
    public bool? HasMediaRecorded { get; set; }
    public bool? HasVideoRecorded { get; set; }
    public string? PersistenceId { get; set; }
    public int? ResumeVersion { get; set; }
    public string? StopStrategy { get; set; }
    public string? RuntimeType { get; set; }
    public bool? RequiresUserInteraction { get; set; }
    public int? ReleaseVersionId { get; set; }
    public string? EntryPointPath { get; set; }
    public int? OrganizationUnitId { get; set; }
    public string? OrganizationUnitFullyQualifiedName { get; set; }
    public string? Reference { get; set; }
    public string? ProcessType { get; set; }
    public Machine? Machine { get; set; }
    public string? ProfilingOptions { get; set; }
    public bool? ResumeOnSameContext { get; set; }
    public string? LocalSystemAccount { get; set; }
    public string? OrchestratorUserIdentity { get; set; }
    public string? RemoteControlAccess { get; set; }
    public string? StartingTriggerId { get; set; } // Guid
    public Int64? MaxExpectedRunningTimeSeconds { get; set; }
    public string? ServerlessJobType { get; set; }
    public string? ParentJobKey { get; set; } // Guid // added in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? ResumeTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public JobError? JobError { get; set; } // added in V19.0 TODO
    public string? ErrorCode { get; set; } // added in V19.0
    public string? FpsProperties { get; set; } // added in V19.0
    public string? TraceId { get; set; } // added in V19.0
    public string? ParentSpanId { get; set; } // added in V19.0
    // public string? ProjectKey { get; set; } // Guid // deprecated?
    // public Int64? ParentOperationId { get; set; } // undocumented
    public AutopilotForRobotsSettings? AutopilotForRobots { get; set; }
    public string? FpsContext { get; set; } // added in V19.0
    //public bool? EnableAutopilotHealing { get; set; } // deprecated
}

public enum _RuntimeType
{
    NonProduction, Attended, Unattended, Development, Studio, RpaDeveloper, StudioX, CitizenDeveloper, Headless, RpaDeveloperPro, StudioPro, TestAutomation, AutomationCloud, Serverless, AutomationKit, ServerlessTestAutomation, AutomationCloudTestAutomation, AttendedStudioWeb
}

public class Log
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Level { get; set; }
    public string? WindowsIdentity { get; set; }
    public string? ProcessName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? TimeStamp { get; set; }
    public string? Message { get; set; }
    public string? JobKey { get; set; }
    public string? RawMessage { get; set; }
    public string? RobotName { get; set; }
    public string? HostMachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? MachineKey { get; set; }
    public string? RuntimeType { get; set; }
    public Int64? Id { get; set; }

    // There is no property that can be used as a dictionary key. Id always returns zero.
    // Therefore, these logs are managed with HashSet<Log>.
    // Override Equals() and GetHashCode() to prevent the same row from appearing multiple times in the HashSet<Log>.
    public override bool Equals(object? obj)
    {
        if (obj is Log other)
        {
            return Level == other.Level &&
                   WindowsIdentity == other.WindowsIdentity &&
                   ProcessName == other.ProcessName &&
                   TimeStamp == other.TimeStamp &&
                   Message == other.Message &&
                   JobKey == other.JobKey &&
                   RawMessage == other.RawMessage &&
                   RobotName == other.RobotName &&
                   HostMachineName == other.HostMachineName &&
                   MachineId == other.MachineId &&
                   MachineKey == other.MachineKey &&
                   RuntimeType == other.RuntimeType &&
                   Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        int hash = HashCode.Combine(Level, WindowsIdentity, ProcessName, TimeStamp, Message, JobKey, RawMessage, RobotName);

        // Compute additional hash codes from remaining properties and combine
        return HashCode.Combine(hash, HostMachineName, MachineId, MachineKey, RuntimeType, Id);
    }
}

// AuditLogEntityDto
public class AuditLogEntity
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathId { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public Int64? AuditLogId { get; set; }
    public string? CustomData { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Hashtable? CustomDataExpanded { get; set; } // added by UiPathOrch

    public Int64? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? Action { get; set; }
}

// AuditLogDto
public class AuditLog
{
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? Parameters { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? ExecutionTime { get; set; }
    public string? Component { get; set; }
    public string? DisplayName { get; set; }
    public Int64? EntityId { get; set; }
    public string? OperationText { get; set; }
    public string? UserName { get; set; }
    public string? UserType { get; set; }
    public AuditLogEntity[]? Entities { get; set; }
    public AuditLogEntity[]? Details { get; set; } // added by UiPathOrch
    public string? ExternalClientId { get; set; }
    public Int64? UserId { get; set; }
    public bool? UserIsDeleted { get; set; }
}

// EnvironmentDto
public class Environment
{
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public SimpleRobot[]? Robots { get; set; }
    public string? Type { get; set; } // deprecated
}

public class Robot
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? LicenseKey { get; set; }
    public string? MachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? ExternalName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostingType { get; set; }
    public string? ProvisionType { get; set; }
    public string? Password { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public Int64? UserId { get; set; }
    public bool? Enabled { get; set; }
    public string? CredentialType { get; set; }
    public Environment[]? Environments { get; set; }
    public string? RobotEnvironments { get; set; }
    public ExecutionSettings? ExecutionSettings { get; set; }
    public User? User { get; set; }
    public bool? IsExternalLicensed { get; set; }
    public bool? LimitConcurrentExecution { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// RobotsFromFolderModel
public class RobotsFromFolderModel
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? UserType { get; set; }
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
    public string? LicenseKey { get; set; }
    public string? MachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? ExternalName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostingType { get; set; }
    public string? ProvisionType { get; set; }
    public string? Password { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public Int64? UserId { get; set; }
    public bool? Enabled { get; set; }
    public string? CredentialType { get; set; }
    public Environment[]? Environments { get; set; }
    public string? RobotEnvironments { get; set; }
    public ExecutionSettings? ExecutionSettings { get; set; }
    //public User? User { get; set; }
    public bool? IsExternalLicensed { get; set; }
    public bool? LimitConcurrentExecution { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// ExtendRobotDto
public class ExtendedRobot
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Machine { get; set; } // added by UiPathOrch

    public long? Id { get; set; }
    public User? User { get; set; }
    public string? LicenseKey { get; set; }
    public string? MachineName { get; set; }
    public long? MachineId { get; set; }
    public string? Name { get; set; } // required
    public string? Username { get; set; }
    public string? ExternalName { get; set; }
    public string? Description { get; set; }
    // NonProduction, Attended, Unattended, Development, Studio, RpaDeveloper, StudioX,
    // CitizenDeveloper, Headless, StudioPro, RpaDeveloperPro, TestAutomation, AutomationCloud,
    // Serverless, AutomationKit, ServerlessTestAutomation, AutomationCloudTestAutomation,
    // AttendedStudioWeb, Hosting, AssistantWeb
    public string? Type { get; set; } // required

    // Standard, Floating
    public string? HostingType { get; set; } // required

    // Standard, Floating
    public string? ProvisionType { get; set; }

    public string? Password { get; set; }
    public long? CredentialStoreId { get; set; }
    public long? UserId { get; set; }
    public bool? Enabled { get; set; }

    // Default, SmartCard, NCipher, SafeNet, NoCredential
    public string? CredentialType { get; set; }

    public List<Environment>? Environments { get; set; }

    // Comma-separated environment names
    public string? RobotEnvironments { get; set; }

    public bool IsExternalLicensed { get; set; }

    // Key-value pairs for execution settings
    public Dictionary<string, string>? ExecutionSettings { get; set; }

    public bool? LimitConcurrentExecution { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public long? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public long? CreatorUserId { get; set; }
}

public class SetMachineRobotsCmd // added by UiPathOrch. The proper class name is unknown.
{
    public long? MachineId { get; set; }
    public long? FolderId { get; set; }
    public List<long>? AddedRobotIds { get; set; }
    public List<long>? RemovedRobotIds { get; set; }
}

// SimpleRobotDto
public class SimpleRobot
{
    public string? LicenseKey { get; set; }
    public string? MachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? ExternalName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostingType { get; set; }
    public string? ProvisionType { get; set; }
    public string? Password { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public Int64? UserId { get; set; }
    public bool? Enabled { get; set; }
    public string? CredentialType { get; set; }
    public Environment[]? Environments { get; set; }
    public string? RobotEnvironments { get; set; }
    public string[]? ExecutionSettings { get; set; }
    public bool? IsExternalLicensed { get; set; }
    public bool? LimitConcurrentExecution { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}

// RobotLicenseDto
public class RobotLicense
{
    public Int64? RobotId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? Timestamp { get; set; }
    public Int64? Id { get; set; }
}

// RobotWithLicenseDto
public class RobotWithLicense
{
    public RobotLicense? License { get; set; }
    public User? User { get; set; }
    public string? LicenseKey { get; set; }
    public string? MachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public string? ExternalName { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostingType { get; set; }
    public string? ProvisionType { get; set; }
    public string? Password { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public Int64? UserId { get; set; }
    public bool? Enabled { get; set; }
    public string? CredentialType { get; set; }
    public Environment[]? Environments { get; set; }
    public string? RobotEnvironments { get; set; }
    //public ExecutionSettings? ExecutionSettings { get; set; } ////////////////
    public bool? IsExternalLicensed { get; set; }
    public bool? LimitConcurrentExecution { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}


// LicenseNamedUserDto
public class LicenseNamedUser
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // Added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? RobotType { get; set; } // Added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathRobotType { get; set; } // Added by UiPathOrch
    public string? Key { get; set; }
    public string? UserName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastLoginDate { get; set; }
    public int? MachinesCount { get; set; }
    public bool? IsLicensed { get; set; }
    public bool? IsExternalLicensed { get; set; }
    public Int64? ActiveRobotId { get; set; }
    public string[]? MachineNames { get; set; }
    public string[]? ActiveMachineNames { get; set; }
}

// LicenseRuntimeDto
public class LicenseRuntime
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // Added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? RobotType { get; set; } // Added by UiPathOrch /////////////////////// What about PathRobotType?
    public string? Key { get; set; }
    public Int64? MachineId { get; set; }
    public string? MachineName { get; set; }
    public string? HostMachineName { get; set; }
    public string? ServiceUserName { get; set; }
    public string? MachineType { get; set; }
    public int? Runtimes { get; set; }
    public int? RobotsCount { get; set; }
    public int? ExecutingCount { get; set; }
    public bool? IsOnline { get; set; }
    public bool? IsLicensed { get; set; }
    public bool? Enabled { get; set; }
    public string? MachineScope { get; set; }
}

// ConsumptionLicenseStatsModel
public class ConsumptionLicenseStatsModel
{
    public string? type { get; set; }
    public Int64? used { get; set; }
    public Int64? total { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? timestamp { get; set; }
}

public enum RobotType
{
    NonProduction,
    Attended,
    Unattended,
    RpaDeveloper,
    CitizenDeveloper,
    Headless,
    RpaDeveloperPro,
    TestAutomation,
    AutomationCloud,
    Serverless,
    AutomationKit,
    ServerlessTestAutomation,
    AutomationCloudTestAutomation,
    AttendedStudioWeb
}

// LicenseStatsModel
public class LicenseStatsModel
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public RobotType? robotType { get; set; }
    public Int64? count { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? timestamp { get; set; }
}

// CountStats
public class CountStats
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? title { get; set; }
    public Int64? count { get; set; }
    public bool? hasPermissions { get; set; }
}

// SessionDto
public class Session
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public RobotWithLicense? Robot { get; set; }
    public string? HostMachineName { get; set; }
    public Int64? MachineId { get; set; }
    public string? MachineName { get; set; }
    public string? State { get; set; }
    public Job? JobDto { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? ReportingTime { get; set; }
    public string? Info { get; set; }
    public bool? IsUnresponsive { get; set; }
    public string? LicenseErrorCode { get; set; }
    public Int64? OrganizationUnitId { get; set; }
    public string? FolderName { get; set; }
    public string? RobotSessionType { get; set; }
    public string? Version { get; set; }
    public string? Source { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DebugModeExpirationDate { get; set; }
    public UpdateInfo? UpdateInfo { get; set; }
    public string? InstallationId { get; set; }
    public string? Platform { get; set; }
    public string? EndpointDetection { get; set; }
}

// RobotsToggleEnabledStatusRequest
public class RobotsToggleEnabledStatusRequest
{
    public Int64[]? robotIds { get; set; }
    public bool? enabled { get; set; }
}

// MachineSessionRuntimeDto
public class MachineSessionRuntime
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? SessionId { get; set; }
    public Int64? MachineId { get; set; }
    public string? MachineName { get; set; }
    public string? MaintenanceMode { get; set; }
    public string? HostMachineName { get; set; }
    public string? RuntimeType { get; set; }
    public string? MachineType { get; set; }
    public string? MachineScope { get; set; }
    public string? Status { get; set; }
    public bool? IsUnresponsive { get; set; }
    public Int64? Runtimes { get; set; }
    public Int64? UsedRuntimes { get; set; }
    public string? ServiceUserName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? ReportingTime { get; set; }
    public string? Version { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DebugModeExpirationDate { get; set; }
    public string? Platform { get; set; }
    public string? EndpointDetection { get; set; }
    public int? TriggersCount { get; set; }
}

// EntryPointDataVariationDto
public class EntryPointDataVariation
{
    public string? Content { get; set; }
    public string? ContentType { get; set; }
    public Int64? Id { get; set; }
}

// EntryPointDto
public class EntryPoint
{
    public Int64? Id { get; set; }
    public string? UniqueId { get; set; }
    public string? Path { get; set; }
    public string? InputArgumets { get; set; }
    public string? OutputArguments { get; set; }
    EntryPointDataVariation? DataVariation { get; set; }
}

// ReleaseVersionDto
public class ReleaseVersion
{
    public Int64? Id { get; set; }
    public Int64? ReleaseId { get; set; }
    public string? VersionNumber { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? ReleaseName { get; set; }
}

public class ArgumentMetadata
{
    public string? Input { get; set; }
    public string? Output { get; set; }
}

// MachineRobotDto
public class MachineRobot
{
    public long? MachineId { get; set; }
    public string? MachineName { get; set; }
    public long? RobotId { get; set; }
    public string? RobotUserName { get; set; }
}

// StartProcessDto
public class StartProcess
{
    public string? ReleaseKey { get; set; }
    public string? Strategy { get; set; }
    public long[]? RobotIds { get; set; }
    public long[]? MachineSessionIds { get; set; }
    public int? NoOfRobots { get; set; }
    public int? JobsCount { get; set; }
    public string? Source { get; set; }
    public string? JobPriority { get; set; }
    public int? SpecificPriorityValue { get; set; }
    public string? RuntimeType { get; set; }
    public string? InputArguments { get; set; }
    public string? EnvironmentVariables { get; set; } // added in V19.0
    public string? Reference { get; set; }
    public MachineRobot? MachineRobots { get; set; }
    public string? TargetFramework { get; set; }
    public bool? ResumeOnSameContext { get; set; }
    public string? BatchExecutionKey { get; set; } // Guid
    public bool? RequiresUserInteraction { get; set; }
    public string? StopProcessExpression { get; set; }
    public string? StopStrategy { get; set; }
    public string? KillProcessExpression { get; set; }
    public string? RemoteControlAccess { get; set; }
    public string? AlertPendingExpression { get; set; }
    public string? AlertRunningExpression { get; set; }
    public bool? RunAsMe { get; set; }
    public string? ParentOperationId { get; set; }
    public AutopilotForRobotsSettings? AutopilotForRobots { get; set; }
    // public bool? EnableAutopilotHealing { get; set; } // deprecated in V18.0? removed in V19.0
    public string? ProfilingOptions { get; set; }
    public string? FpsContext { get; set; } // added in V19.0
    public string? FpsProperties { get; set; } // added in V19.0
    public string? TraceId { get; set; } // added in V19.0
    public string? ParentSpanId { get; set; } // added in V19.0
    public string? EntryPointPath { get; set; } // added in V19.0
}

// ProcessSettingsDto
public class ProcessSettings
{
    public bool? AlwaysRunning { get; set; }
    public bool? AutoStartProcess { get; set; }
    public bool? ErrorRecordingEnabled { get; set; }
    public int? Quality { get; set; }
    public int? Frequency { get; set; }
    public int? Duration { get; set; }
    public AutopilotForRobotsSettings? AutopilotForRobots { get; set; }
    // public bool? EnableAutopilotHealing { get; set; } // deprecated
}

// VideoRecordingSettingsDto
public class VideoRecordingSettings
{
    public string? VideoRecordingType { get; set; }
    public string? QueueItemVideoRecordingType { get; set; }
    public int? MaxDurationSeconds { get; set; }
}

// ReleaseDto
public class Release
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? ProcessKey { get; set; }
    public string? ProcessVersion { get; set; }
    public bool? IsLatestVersion { get; set; }
    public bool? IsProcessDeleted { get; set; }
    public string? Description { get; set; }
    public string? Name { get; set; }
    public Int64? EnvironmentId { get; set; }
    public string? EnvironmentName { get; set; }
    public Environment? Environment { get; set; }
    public Int64? EntryPointId { get; set; }
    public string? EntryPointPath { get; set; }
    public EntryPoint? EntryPoint { get; set; } // Documented in swagger doc, but does not seem to be returned. Keeping it just in case.
    public string? InputArguments { get; set; }
    public string? EnvironmentVariables { get; set; } // added in V19.0
    public string? ProcessType { get; set; }
    public bool? SupportsMultipleEntryPoints { get; set; }
    public bool? RequiresUserInteraction { get; set; }
    public string? MinRequiredRobotVersion { get; set; } // added in V19.0
    public bool? IsAttended { get; set; }
    public bool? IsCompiled { get; set; }
    public string? AutomationHubIdeaUrl { get; set; }
    public ReleaseVersion? CurrentVersion { get; set; }
    public ReleaseVersion[]? ReleaseVersions { get; set; }
    public ArgumentMetadata? Arguments { get; set; }
    public ProcessSettings? ProcessSettings { get; set; }
    public VideoRecordingSettings? VideoRecordingSettings { get; set; }
    public bool? AutoUpdate { get; set; }
    public bool? HiddenForAttendedUser { get; set; }
    public string? FeedId { get; set; }
    //[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? JobPriority { get; set; }
    public int? SpecificPriorityValue { get; set; }
    public string? FolderKey { get; set; } // Guid // added in V19.0
    public Int64? OrganizationUnitId { get; set; }
    public string? OrganizationUnitFullyQualifiedName { get; set; }
    public string? TargetFramework { get; set; }
    //[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? RobotSize { get; set; }
    public Tag[]? Tags { get; set; }
    public string? RemoteControlAccess { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }

    [JsonConverter(typeof(SafeArrayConverter<ResourceOverwrite>))]
    public ResourceOverwrite[]? ResourceOverwrites { get; set; } // ISSUE: unknown. not mentioned in swagger.
    public string? RetentionAction { get; set; } // ISSUE: this is not mentioned in swagger.
    public int? RetentionPeriod { get; set; } // ISSUE: this is not mentioned in swagger.
    public Int64? RetentionBucketId { get; set; } // ISSUE: this is not mentioned in swagger.

    public string? StaleRetentionAction { get; set; } // ISSUE: this is not mentioned in swagger.
    public int? StaleRetentionPeriod { get; set; } // ISSUE: this is not mentioned in swagger.
    public Int64? StaleRetentionBucketId { get; set; } // ISSUE: this is not mentioned in swagger.
}

// added by UiPathOrch
public class InputArgument
{
    public string? name { get; set; }
    public string? type { get; set; }
    public bool? required { get; set; }
    public bool? hasDefault { get; set; }
}

// ReleaseRetentionSettingDto
public class ReleaseRetentionSetting
{
    public Int64? ReleaseId { get; set; }
    public string? Action { get; set; }
    public int? Period { get; set; }
    public Int64? BucketId { get; set; }
    public string? Type { get; set; } // added in V19.0
}

// SubtypedPackageResourceDto
public class PropertyItem
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsExpression { get; set; }
}

public class MetadataInfo
{
    public string? SubType { get; set; }
    public string? ActivityName { get; set; }
    public string? BindingsVersion { get; set; }
    public string? SolutionsSupport { get; set; }
}

public class SubtypedPackageResource
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Release { get; set; } // added by UiPathOrch

    public int Id { get; set; }
    public string? ResourceId { get; set; }
    public string? ResourceName { get; set; }
    public string? ResourceType { get; set; }
    public string? Comment { get; set; }
    public string? ResourceKey { get; set; }
    public string? FolderFullyQualifiedName { get; set; }
    public int FolderId { get; set; }
    public string? FolderType { get; set; }
    public string? FolderProvisionType { get; set; }
    public string? ValidationResult { get; set; }
    public string? ValidationError { get; set; }
    public bool IsOverwritable { get; set; }
    public List<PropertyItem>? Properties { get; set; }
    public MetadataInfo? Metadata { get; set; }
}

public class CustomKeyValuePair
{
    public string? Key { get; set; }
    public string? Value { get; set; }
}

// AssetRobotValueDto
public class AssetRobotValue
{
    public Int64? RobotId { get; set; }
    public string? RobotName { get; set; }
    public string? KeyTrail { get; set; }
    public string? ValueType { get; set; }
    public string? StringValue { get; set; }
    public bool? BoolValue { get; set; }
    public int? IntValue { get; set; }
    public string? Value { get; set; }
    public string? CredentialUsername { get; set; }
    public string? CredentialPassword { get; set; }
    public string? ExternalName { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public CustomKeyValuePair[]? KeyValueList { get; set; }
    public Int64? Id { get; set; }
}

// AssetUserValueDto
public class AssetUserValue
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Name { get; set; } /// added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathName { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public Int64? UserId { get; set; }
    public string? UserName { get; set; }
    public Int64? MachineId { get; set; }
    public string? MachineName { get; set; }
    public string? ValueType { get; set; }
    public string? StringValue { get; set; }
    public bool? BoolValue { get; set; }
    public int? IntValue { get; set; }
    public string? Value { get; set; }
    public string? CredentialUsername { get; set; }
    public string? CredentialPassword { get; set; }
    public string? SecretValue { get; set; } // v20+ — populated only when ValueType == "Secret"
    public Int64? CredentialStoreId { get; set; }
    public string? ExternalName { get; set; }
    public CustomKeyValuePair[]? KeyValueList { get; set; }
}

// AccessibleFoldersDto
public class AccessibleFoldersDto
{
    public SimpleFolder[]? AccessibleFolders { get; set; }
    public int TotalFoldersCount { get; set; }
}

// TagDto
public class Tag
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Value { get; set; }
    public string? DisplayValue { get; set; }

    // TODO: This needs to be removed... It causes incorrect ConvertTo-Json results.
    // Instead, the same thing should be written in the view definition file.
    public override string? ToString()
    {
        if (string.IsNullOrEmpty(DisplayName)) return null;
        if (string.IsNullOrEmpty(Value)) return DisplayName;
        return $"{DisplayName}={DisplayValue}";
    }
}

// AssetDto
public class Asset
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public bool? CanBeDeleted { get; set; }
    public string? ValueScope { get; set; }
    public string? ValueType { get; set; }
    public string? Value { get; set; }
    public string? StringValue { get; set; }
    public bool? BoolValue { get; set; }
    public int? IntValue { get; set; }
    public string? CredentialUsername { get; set; }
    public string? CredentialPassword { get; set; }
    public string? SecretValue { get; set; } // v20+ — populated only when ValueType == "Secret"
    public string? ExternalName { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public CustomKeyValuePair[]? KeyValueList { get; set; }
    public bool? HasDefaultValue { get; set; }
    public bool? AllowDirectApiAccess { get; set; } // v20+
    public string? Description { get; set; }
    public AssetRobotValue[]? RobotValues { get; set; } // deprecated since v19.0
    //public AssetUserValue[]? UserValues { get; set; }
    public List<AssetUserValue>? UserValues { get; set; }
    public Tag[]? Tags { get; set; }
    public int? FoldersCount { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// QueueDefinitionDto
public class QueueDefinition
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? MaxNumberOfRetries { get; set; }
    public bool? AcceptAutomaticallyRetry { get; set; }
    public bool? RetryAbandonedItems { get; set; }
    public bool? EnforceUniqueReference { get; set; }
    public bool? Encrypted { get; set; }
    public string? SpecificDataJsonSchema { get; set; }
    public string? OutputDataJsonSchema { get; set; }
    public string? AnalyticsDataJsonSchema { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? ProcessScheduleId { get; set; }
    public int? SlaInMinutes { get; set; }
    public int? RiskSlaInMinutes { get; set; }
    public Int64? ReleaseId { get; set; }
    public bool? IsProcessInCurrentFolder { get; set; }
    public int? FoldersCount { get; set; }
    public Int64? OrganizationUnitId { get; set; }
    public string? OrganizationUnitFullyQualifiedName { get; set; }
    public string? RetentionAction { get; set; }
    public int? RetentionPeriod { get; set; }
    public Int64? RetentionBucketId { get; set; }
    public string? RetentionBucketName { get; set; }
    public string? StaleRetentionAction { get; set; }
    public int? StaleRetentionPeriod { get; set; }
    public Int64? StaleRetentionBucketId { get; set; }
    public string? StaleRetentionBucketName { get; set; }
    public Tag[]? Tags { get; set; }
}

// QueueRetentionSettingDto
public class QueueRetentionSetting
{
    public Int64 QueueDefinitionId { get; set; }
    public string? Action { get; set; }
    public int? Period { get; set; }
    public Int64? BucketId { get; set; }
    public string? Type { get; set; } // added in V19.0
}

// QueueFoldersShareDto
public class QueueFoldersShare
{
    public List<Int64>? QueueIds { get; set; }
    public List<Int64>? ToAddFolderIds { get; set; }
    public List<Int64>? ToRemoveFolderIds { get; set; }
}

public enum QueuePriority { Low = 1, Normal = 2, High = 3 };

public enum LogLevel
{
    Trace = 0,
    Debug,
    Info,
    Warn,
    Error,
    Fatal
}

// SimpleUserDto
public class SimpleUser
{
    public long? Id { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? UserName { get; set; }
    public string? Domain { get; set; }
    public string? DirectoryIdentifier { get; set; }
    public string? FullName { get; set; }
    public string? EmailAddress { get; set; }
    public bool? IsEmailConfirmed { get; set; } // deprecated in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastLoginTime { get; set; }
    public bool? IsActive { get; set; } // deprecated in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? AuthenticationSource { get; set; }
    public string? Password { get; set; } // deprecated in V19.0
    public bool IsExternalLicensed { get; set; }
    public UserRole[]? UserRoles { get; set; }
    public string[]? RolesList { get; set; }
    public string[]? LoginProviders { get; set; }
    public OrganizationUnit[]? OrganizationUnits { get; set; } // deprecated in V19.0
    public int? TenantId { get; set; }
    public string? TenancyName { get; set; }
    public string? TenantDisplayName { get; set; }
    public string? TenantKey { get; set; }
    public string? Type { get; set; }
    public string? ProvisionType { get; set; }
    public string? LicenseType { get; set; }
    public AttendedRobot? RobotProvision { get; set; }
    public UnattendedRobot? UnattendedRobot { get; set; }
    public UserNotificationSubscription? NotificationSubscription { get; set; }
    public string? Key { get; set; } // Guid
    public bool? MayHaveUserSession { get; set; }
    public bool? MayHaveRobotSession { get; set; }
    public bool? MayHaveUnattendedSession { get; set; }
    public bool? MayHavePersonalWorkspace { get; set; }
    public bool? RestrictToPersonalWorkspace { get; set; }
    public UpdatePolicy? UpdatePolicy { get; set; }
    public string? AccountId { get; set; }
    public bool? HasOnlyInheritedPrivileges { get; set; }
    public bool? ExplicitMayHaveUserSession { get; set; } // added in V19.0
    public bool? ExplicitMayHaveRobotSession { get; set; } // added in V19.0
    public bool? ExplicitMayHavePersonalWorkspace { get; set; } // added in V19.0
    public bool? ExplicitRestrictToPersonalWorkspace { get; set; } // added in V19.0
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public long? LastModifierUserId { get; set; }
    public long? CreatorUserId { get; set; }
}

// ProcessingExceptionDto
public class ProcessingException
{
    public string? Reason { get; set; }
    public string? Details { get; set; }
    public string? Type { get; set; }
    public string? AssociatedImageFilePath { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
}

// QueueItemDto
public class QueueItem
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Name { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathName { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public Int64? QueueDefinitionId { get; set; }
    public QueueDefinition? QueueDefinition { get; set; }
    public ProcessingException? ProcessingException { get; set; }
    public bool? Encrypted { get; set; }
    public Dictionary<string, object>? SpecificContent { get; set; }
    public Dictionary<string, object>? Output { get; set; }
    public Dictionary<string, object>? Analytics { get; set; }
    public string? OutputData { get; set; }
    public string? AnalyticsData { get; set; }
    public string? Status { get; set; }
    public string? ReviewStatus { get; set; }
    public Int64? ReviewerUserId { get; set; }
    public SimpleUser? ReviewerUser { get; set; }
    public string? Key { get; set; }
    public string? Reference { get; set; }
    public string? ProcessingExceptionType { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DueDate { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? RiskSlaDate { get; set; }
    public string? Priority { get; set; }
    public SimpleRobot? Robot { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DeferDate { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StartProcessing { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? EndProcessing { get; set; }
    public int? SecondsInPreviousAttempts { get; set; }
    public Int64? AncestorId { get; set; }
    public string? AncestorUniqueKey { get; set; }
    public int? RetryNumber { get; set; }
    public Int64? ManualAncestorId { get; set; }
    public string? ManualAncestorUniqueKey { get; set; }
    public int? ManualRetryNumber { get; set; }
    public string? UniqueKey { get; set; }
    //HasVideoRecorded boolean // deprecated
    public string? SpecificData { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string? Progress { get; set; }
    public string? RowVersion { get; set; }
    public Int64 OrganizationUnitId { get; set; }
    // public string? OrganizationUnitFullyQualifiedName //deprecated
}

// QueueItemDataDto
// DeferDate etc. are DateTime, but defined as string for CSV import compatibility.
public class QueueItemData4CsvImport
{
    public string? Name { get; set; }
    public string? Priority { get; set; }
    public Dictionary<string, object>? SpecificContent { get; set; }
    public string? DeferDate { get; set; }
    public string? DueDate { get; set; }
    public string? RiskSlaDate { get; set; }
    public string? Reference { get; set; }
    public string? Progress { get; set; }
    public string? Source { get; set; }
}

// TransactionDataDto
public class TransactionData
{
    public string? Name { get; set; }
    public string? RobotIdentifier { get; set; } // Guid
    public Dictionary<string, string?>? SpecificContent { get; set; }
    public DateTimeOffset? DeferDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string? Reference { get; set; }
    public string? ReferenceFilterOption { get; set; }
    public string? ParentOperationId { get; set; }
}

// BulkAddQueueItemsRequest
public class BulkAddQueueItemsRequest4CsvImport
{
    public string? queueName { get; set; }
    public string? commitType { get; set; }
    public QueueItemData4CsvImport[]? queueItems { get; set; }
}

// QueueItemDataDto
public class QueueItemData
{
    public string? Name { get; set; }
    public string? Priority { get; set; }
    public Dictionary<string, object>? SpecificContent { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DeferDate { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DueDate { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? RiskSlaDate { get; set; }
    public string? Reference { get; set; }
    public string? Progress { get; set; }
    public string? Source { get; set; }
    public string? ParentOperationId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    internal Int64? Id { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    internal string? Key { get; set; } // added by UiPathOrch
}

// BulkAddQueueItemsRequest
public class BulkAddQueueItemsRequest
{
    public string? queueName { get; set; }
    public string? commitType { get; set; }
    public QueueItemData[]? queueItems { get; set; }
}

public class RetryQueueItem
{
    public Int64? Id { get; set; }
    public string? RowVersion { get; set; }
}

public class RetryQueueItemRequest
{
    public IEnumerable<RetryQueueItem>? queueItems { get; set; }
    public string? status { get; set; }
}

// BulkOperationResponseDto
public class BulkOperationResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Queue { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathQueue { get; set; } // added by UiPathOrch

    public bool? Success { get; set; }
    public string? Message { get; set; }
    public Int64[]? FailedItems { get; set; }
}

//public class QueueItem
//{
//    public string? ReviewerStatus;
//    public DateTime? DueDate;
//    public QueuePriority Priority;
//    public DateTime? DeferDate;
//    public DateTime? StartProcessing;
//    public int? RetryNumber;
//    public DateTime? CreationTime;
//    public string? Progress;
//    public Dictionary<string, string>? SpecificContent;
//    public Dictionary<string, string>? Output;
//    public Dictionary<string, string>? Analytics;
//}

// FailedQueueItemDto
public class FailedQueueItem
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? QueueName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Category { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? CsvPath { get; set; } // added by UiPathOrch
    public int? Ordinal { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

// BulkOperationResponseDtoOfFailedQueueItemDto
// Issue? Members were lowercase in the swagger doc, but actually capitalized. Fixed in the v19.0 swagger.json.
public class BulkOperationResponseDtoOfFailedQueueItem
{
    public bool? Success { get; set; }
    public string? Message { get; set; }
    public FailedQueueItem[]? FailedItems { get; set; }
}


// LongVersionedEntity
public class LongVersionedEntity
{
    public string? RowVersion { get; set; }
    public Int64? Id { get; set; }
}

// QueueItemDeleteBulkRequest
public class QueueItemDeleteBulkRequest
{
    public List<LongVersionedEntity>? queueItems { get; set; }
}

// BulkOperationResponseDtoOfInt64
public class BulkOperationResponseOfInt64
{
    public bool? Success { get; set; }
    public string? Message { get; set; }
    public Int64[]? FailedItems { get; set; }
}

// QueueItemCommentDto
public class QueueItemComment
{
    //    public Int64 Id { get; set; }
    public string? Text { get; set; }
    public Int64? QueueItemId { get; set; }
    //public DateTime? CreationTime { get; set; } // deprecated
    //public Int64? UserId { get; set; }
    //public string? UserName { get; set; }
}

// PersonalWorkspaceDto
public class PersonalWorkspace
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public Int64? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastLogin { get; set; }
    public Int64[]? ExploringUserIds { get; set; }
}

public class EntitySummary
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Category { get; set; } // added by UiPathOrch
    public string? Type { get; set; }
    public string? Name { get; set; }
    public Int64? Count { get; set; }
}

public class EntitiesSummary
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64 Id { get; set; }
    public EntitySummary[]? DeletableEntities { get; set; }
    public EntitySummary[]? StoppableJobs { get; set; }
}

#endregion

#region ProcessSchedule

// RobotExecutorDto
public class RobotExecutor
{
    public string? MachineName { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Int64? Id { get; set; }
}

// MachineRobotSessionDto
public class MachineRobotSession
{
    public Int64? MachineId { get; set; }
    public string? MachineName { get; set; }
    public Int64? RobotId { get; set; }
    public string? RobotUserName { get; set; }
    public Int64? SessionId { get; set; }
    public string? SessionName { get; set; }
}

// Used to serialize MachineRobots for Get-OrchTrigger -ExportCsv
public class MachineRobotSessionForSerialize // added by UiPathOrch
{
    public string? UserName { get; set; }
    public string? MachineName { get; set; }
    public string? SessionName { get; set; }
}

// ProcessScheduleDto
public class ProcessSchedule
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public bool? Enabled { get; set; }
    public string? Name { get; set; }
    public Int64? ReleaseId { get; set; }
    public string? ReleaseKey { get; set; }
    public string? ReleaseName { get; set; }
    public string? EntryPointPath { get; set; } // added in V19.0
    public string? PackageName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? EnvironmentId { get; set; }
    public string? JobPriority { get; set; }
    public int? SpecificPriorityValue { get; set; }
    public string? RuntimeType { get; set; }
    public string? StartProcessCron { get; set; }
    public string? StartProcessCronDetails { get; set; }
    public string? StartProcessCronSummary { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StartProcessNextOccurrence { get; set; }
    public int? StartStrategy { get; set; }
    public RobotExecutor[]? ExecutorRobots { get; set; }
    public string? StopProcessExpression { get; set; }
    public string? StopStrategy { get; set; }
    public string? KillProcessExpression { get; set; }
    public string? ExternalJobKey { get; set; }
    public string? ExternalJobKeyScheduler { get; set; }
    public string? TimeZoneId { get; set; }
    public string? TimeZoneIana { get; set; }
    public bool? UseCalendar { get; set; } // deprecated
    public Int64? CalendarId { get; set; }
    public string? CalendarName { get; set; }
    public string? CalendarKey { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StopProcessDate { get; set; }
    public string? InputArguments { get; set; }
    public Int64? QueueDefinitionId { get; set; }
    public string? QueueDefinitionName { get; set; }
    public bool? ActivateOnJobComplete { get; set; }
    public Int64? ItemsActivationThreshold { get; set; }
    public Int64? ItemsPerJobActivationTarget { get; set; }
    public int? MaxJobsForActivation { get; set; }
    public bool? ResumeOnSameContext { get; set; }
    public string? Description { get; set; }
    public MachineRobotSession[]? MachineRobots { get; set; }
    public Tag[]? Tags { get; set; }
    public string? AlertPendingExpression { get; set; }
    public string? AlertRunningExpression { get; set; }
    public bool? RunAsMe { get; set; }
    public int? ConsecutiveJobFailuresThreshold { get; set; }
    public int? JobFailuresGracePeriodInHours { get; set; }
    public bool? IsConnected { get; set; }
}
#endregion

#region HttpTrigger

public class TriggerRelease
{
    public Int64? Id { get; set; }
    public string? Name { get; set; }
}

public abstract class TriggerBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Type { get; set; }
    public Int64? OrganizationUnitId { get; set; }
    public string? OrganizationUnitFullyQualifiedName { get; set; }
    public bool? Enabled { get; set; }
    public string? ReleaseKey { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? JobCount { get; set; }
    public int? JobPriority { get; set; }
    public bool? RunAsMe { get; set; }
    public string? RuntimeType { get; set; }
    public string? TargetFramework { get; set; }
    public bool? ResumeOnSameContext { get; set; }
    public bool? RequiresUserInteraction { get; set; }
    public string? StopStrategy { get; set; }
    public int? StopJobAfterSeconds { get; set; }
    public int? KillJobAfterSeconds { get; set; }
    public int? AlertPendingJobAfterSeconds { get; set; }
    public int? AlertRunningJobAfterSeconds { get; set; }
    public string? RemoteControlAccess { get; set; }
    public int? ConsecutiveJobFailuresThreshold { get; set; }
    public int? JobFailuresGracePeriodInHours { get; set; }
    public string? InputArguments { get; set; }
    public bool? Visible { get; set; }
    public Int64? AuditEntityId { get; set; }
    public string? Id { get; set; }
    public TriggerRelease? Release { get; set; }
    public MachineRobotSession[]? MachineRobots { get; set; }
    public Tag[]? Tags { get; set; }
}

public class HttpTrigger : TriggerBase
{
    // TODO: How should Properties be defined?
    // Should it be placed in the base class as Dictionary<string, object>?
    public string? CallingMode { get; set; }
    public string? Method { get; set; }
    public string? Slug { get; set; }
    public string? SuccessCallbackUrl { get; set; }
    public string? FailureCallbackUrl { get; set; }
    public string? Secret { get; set; }
    public string? CallbackMode { get; set; }
    public bool? AllowInsecureSsl { get; set; }
}

public class ApiTrigger : TriggerBase
{
    public string? ExternalReference { get; set; }
    public string? ApiTriggerType { get; set; }
    public bool? IsConnected { get; set; }
    public string? TriggerActionId { get; set; }
    public string? EventId { get; set; }
    public string? ConnectorKey { get; set; }
    public string? ConnectionId { get; set; }
    public Int64? ConnectionFolderId { get; set; }
    public string? Operation { get; set; }
    public string? ObjectName { get; set; }
    public string? FilterContext { get; set; }
    public string? FilterExpression { get; set; }
    public bool? IsDeleted { get; set; }

    public ApiTriggerProperties? Properties { get; set; }
}

public class ApiTriggerProperties
{
    public string? EventTriggerId { get; set; }
    public string? EventTriggerEventId { get; set; }
    public string? EventTriggerConnectorKey { get; set; }
    public string? EventTriggerConnectionId { get; set; }
    public long? EventTriggerConnectionFolderId { get; set; }
    public string? EventTriggerOperation { get; set; }
    public string? EventTriggerObjectName { get; set; }
    public string? EventTriggerFilterExpression { get; set; }
}

public class EnableHttpTrigger
{
    public bool? enabled { get; set; }
    public string[]? triggerIds { get; set; }
}
#endregion

#region Connection

// GetConnectionResponse from Connection Service v1 (/connections_/api/v1/Connections)
public class Connection
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch (folder context)

    public string? id { get; set; } // uuid
    public string? name { get; set; }
    public string? owner { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updateTime { get; set; }
    // Live API returns the enum as a string ("Enabled", ...), not the int from the swagger spec.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ConnectionState? state { get; set; }
    public string? apiBaseUri { get; set; }
    public Int64? elementInstanceId { get; set; }
    public Connector? connector { get; set; }
    public bool? isDefault { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastUsedTime { get; set; }
    public string? connectionIdentity { get; set; }
    public int? pollingIntervalInMinutes { get; set; }
    public ConnectionFolder? folder { get; set; }
    public string? elementVersion { get; set; }
    public string? disabledReason { get; set; }
    public bool? byoaConnection { get; set; }
    public Dictionary<string, string?>? configuration { get; set; }
}

public enum ConnectionState
{
    Enabled = 1,
    Disabled = 2,
    Expired = 3,
    Failed = 4
}

// GetConnectorResponse from Connection Service v1
public class Connector
{
    public Int64? id { get; set; }
    public string? key { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public bool? isPrivate { get; set; }
    public bool? hasEvents { get; set; }
    public string[]? eventTypes { get; set; }
    public string? lifeCycleStage { get; set; }
    public string? tags { get; set; }
    public string? type { get; set; }
    public string? tier { get; set; }
    public Dictionary<string, bool?>? flags { get; set; }
    public bool? enabled { get; set; }
    public int? connectionCount { get; set; }
    public string? documentationUrl { get; set; }
    public Int64? customConnectorId { get; set; }
    public bool? isOwner { get; set; }
    public string[]? categories { get; set; }
    public string? disabledReason { get; set; }
}

// Folder schema from Connection Service v1.
// Renamed to ConnectionFolder to avoid collision with Orchestrator's Folder class.
public class ConnectionFolder
{
    public string? key { get; set; }
    public string[]? permissions { get; set; }
    public bool? personalWorkspace { get; set; }
    public string? name { get; set; }
    public string? path { get; set; }
    public string? folderPathAsGuid { get; set; }
    public string? folderType { get; set; }
}

#endregion

#region TestCaseDefinitionDto

// TestEnvironmentDto
public class TestEnvironment
{
    public string? Name { get; set; }
    public Int64? Id { get; set; }
}

// TestSetPackageDto
public class TestSetPackage
{
    public Int64? TestSetId { get; set; }
    public string? TestSet { get; set; }
    public string? VersionMask { get; set; }
    public string? PackageIdentifier { get; set; }
    public bool? IncludePrerelease { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}

// TestCaseDto
public class TestCase
{
    public bool? Enabled { get; set; }
    public Int64? DefinitionId { get; set; }
    public TestCaseDefinition? Definition { get; set; }
    public Int64? ReleaseId { get; set; }
    public string? VersionNumber { get; set; }
    public Int64? TestSetId { get; set; }
    //TestSet
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}

// TestSetInputArgumentDto
public class TestSetInputArgument
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Value { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}

// TestSetDto
public class TestSet
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? SourceType { get; set; }
    public Int64? OrganizationUnitId { get; set; }
    public Int64? EnvironmentId { get; set; }
    public TestEnvironment? Environment { get; set; }
    public int? TestCaseCount { get; set; }
    public Int64? RobotId { get; set; }
    public bool? EnableCoverage { get; set; }
    public TestSetPackage[]? Packages { get; set; }
    public TestCase[]? TestCases { get; set; }
    public bool? Enabled { get; set; }
    public TestSetInputArgument[]? InputArguments { get; set; }
    public bool? IsDeleted { get; set; }
    public Int64? DeleterUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DeletionTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

//public class TestSetForEdit // added by UiPathOrch
//{
//    public string? Name { get; set; }
//    public string? Description { get; set; }

//}

public class TestCaseDefinition
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public string? PackageIdentifier { get; set; }
    public string? UniqueId { get; set; }
    public string? AppVersion { get; set; }
    public string? CreatedVersion { get; set; }
    public string? LatestVersion { get; set; }
    public string? LatestPrereleaseVersion { get; set; }
    public string? FeedId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// TestSetExecutionAttachmentDto
public class TestSetExecutionAttachment
{
    public Int64? TestSetExecutionId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string[]? Tags { get; set; }
    public Int64? Id { get; set; }
}

// TestCaseAssertionDto
public class TestCaseAssertion
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? TestSetExecutionName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathTestSetExecutionName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? ScreenshotPath { get; set; } // added by UiPathOrch
    public string? Message { get; set; }
    public string? Payload { get; set; }
    public bool? Succeeded { get; set; }
    public Int64? TestCaseExecutionId { get; set; }
    public bool? HasScreenshot { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? Id { get; set; }
}

// TestCaseExecutionAttachmentDto
public class TestCaseExecutionAttachment
{
    public Int64? TestCaseExecutionId { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public string[]? Tags { get; set; }
    public Int64? Id { get; set; }
}

// TestCaseExecutionDto
public class TestCaseExecution
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? TestSetExecutionName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathTestSetExecutionName { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public Int64? JobId { get; set; }
    public string? JobKey { get; set; }
    public Int64? TestSetExecutionId { get; set; }
    public TestSetExecution? TestSetExecution { get; set; }
    public Int64? TestCaseId { get; set; }
    public TestCase? TestCase { get; set; }
    public Int64? ReleaseVersionId { get; set; }
    public string? VersionNumber { get; set; }
    public string? EntryPointPath { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StartTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
    public TestCaseAssertion[]? TestCaseAssertions { get; set; }
    public string? DataVariationIdentifier { get; set; }
    public TestCaseExecutionAttachment[]? TestCaseExecutionAttachments { get; set; }
    public string? OutputArguments { get; set; }
    public string? InputArguments { get; set; }
    public string? Info { get; set; }
    public string? HostMachineName { get; set; }
    public string? RuntimeType { get; set; }
    public string? RobotName { get; set; }
    public bool? HasAssertions { get; set; }
    public int? RunId { get; set; }
    public string? TestCaseType { get; set; }
    public int? ExecutionOrder { get; set; }
    public string? TestManagerTestCaseId { get; set; } // Guid
}

public enum TestSetExecutionStatus // added by UiPathOrch
{
    Pending = 0,
    Running,
    Cancelling,
    Passed,
    Failed,
    Cancelled
}

public enum TestSetExecutionTriggerType // added by UiPathOrch
{
    Manual = 0,
    Scheduled,
    ExternalTool
}

// TestSetExecutionDto
public class TestSetExecution
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public Int64? TestSetId { get; set; }
    public Int64? OrganizationUnitId { get; set; }
    public TestSet? TestSet { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? StartTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? EndTime { get; set; }
    public string? Status { get; set; }
    public Int64? ScheduleId { get; set; }
    public string? TriggerType { get; set; }
    public string? BatchExecutionKey { get; set; }
    public string? CoverageStatus { get; set; }
    public int RunId { get; set; }
    public TestCaseExecution[]? TestCaseExecutions { get; set; }
    public TestSetExecutionAttachment[]? Attachments { get; set; }
    public bool? EnforceExecutionOrder { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
}

// TestSetScheduleDto
public class TestSetSchedule
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public bool? Enabled { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Int64? TestSetId { get; set; }
    public string? TestSetName { get; set; }
    public string? TimeZoneId { get; set; }
    public string? TimeZoneIana { get; set; }
    public Int64? CalendarId { get; set; }
    public string? CalendarName { get; set; }
    public string? CronExpression { get; set; }
    public string? CronDetails { get; set; }
    public string? CronSummary { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? NextOccurrence { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DisableDate { get; set; }
    public string? ExternalJobKey { get; set; }
    public string? ExternalJobKeyScheduler { get; set; }
}

// TestDataQueueDto
public class TestDataQueue
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ContentJsonSchema { get; set; }
    public int? ItemsCount { get; set; }
    public int? ConsumedItemsCount { get; set; }
    public bool? IsDeleted { get; set; }
    public Int64? DeleterUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? DeletionTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public Int64? LastModifierUserId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    public Int64? CreatorUserId { get; set; }
    public Int64? Id { get; set; }
}

// TestDataQueueItemODataDto
public class TestDataQueueItem
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathTestDataQueue { get; set; } // added by UiPathOrch
    public Int64? TestDataQueueId { get; set; }
    public string? ContentJson { get; set; }
    public bool? IsConsumed { get; set; }
    public Int64? Id { get; set; }
}

#endregion

#region Maintenance

public class MaintenanceStateLog
{
    public string? state { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? timeStamp { get; set; }
}

// MaintenanceSetting
public class MaintenanceSetting
{
    public string? state { get; set; }
    public MaintenanceStateLog[]? maintenanceLogs { get; set; }
    public int jobStopsAttempted { get; set; }
    public int jobKillsAttempted { get; set; }
    public int triggersSkipped { get; set; }
    public int systemTriggersSkipped { get; set; }
}

#endregion

#region ExecutionMedia

// ExecutionMediaDto
public class ExecutionMedia
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? StorageLocation { get; set; }
    public string? Name { get; set; }
    public Int64? JobId { get; set; }
    public string? ReleaseName { get; set; }
}

#endregion

#region Buckets

// BucketDto
public class Bucket
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Identifier { get; set; } // Guid
    public string? StorageProvider { get; set; }
    public string? StorageContainer { get; set; }
    public string? StorageParameters { get; set; }
    public Int64? CredentialStoreId { get; set; }
    public string? Password { get; set; }
    public string? ExternalName { get; set; }
    public string? Options { get; set; }
    public int? FoldersCount { get; set; }
    public Tag[]? Tags { get; set; }
}

// BlobFileDto
public class BlobFile
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Bucket { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathBucket { get; set; } // added by UiPathOrch
    public string? Id { get; set; }
    public string? FullPath { get; set; }
    public string? ContentType { get; set; }
    public Int64? Size { get; set; }
    public bool? IsDirectory { get; set; }
}

// BucketFoldersShareDto
public class BucketFoldersShare
{
    public List<Int64>? BucketIds { get; set; }
    public List<Int64>? ToAddFolderIds { get; set; }
    public List<Int64>? ToRemoveFolderIds { get; set; }
}

// BlobFileAccessDto
public class BlobFileAccess
{
    public string? Uri { get; set; }
    public string? Verb { get; set; }
    public bool? RequiresAuth { get; set; }
    public ResponseDictionary? Headers { get; set; }
}
#endregion

#region Calendar

// ExcludedDateNamed
public class ExcludedDateNamed // added by UiPathOrch
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    public string? PathName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? ExcludedDate { get; set; }
}

// ExtendedCalendarDto
public class ExtendedCalendar
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Name { get; set; }
    public string? TimeZoneId { get; set; }

    [JsonConverter(typeof(DateTimeArrayJsonConverter))]
    public DateTime[]? ExcludedDates { get; set; }

    public string? Key { get; set; }
}

#endregion

#region TaskCatalog

// TaskCatalogDto
public class TaskCatalog
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public Int64? Id { get; set; }
    public string? Key { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? CreationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? LastModificationTime { get; set; }
    public int? FoldersCount { get; set; }
    public bool? Encrypted { get; set; }
    public Tag[]? Tags { get; set; }
    public string? RetentionAction { get; set; }
    public int? RetentionPeriod { get; set; }
    public Int64? RetentionBucketId { get; set; }
    public string? RetentionBucketName { get; set; }
}

#endregion

#region Identity

// UserProfileDto
public class UserProfile
{
    public string? partitionName { get; set; }
    public string? partitionGlobalId { get; set; }
    public string? userName { get; set; }
    public string? name { get; set; }
    public bool? isAdmin { get; set; }
    public string? fullName { get; set; }
}

// RobotAccountDto
public class PmRobotAccount
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? name { get; set; }
    public string? displayName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastLoginTime { get; set; }
    public string[]? groupIds { get; set; } // Guid
}

public class PmRobotAccountExpanded // added by UiPathOrch
{
    public string? Path { get; set; }
    public string? RobotAccount { get; set; }
    public string? PathRobotAccount { get; set; }
    public string? groupId { get; set; } // Guid
    public string? groupName { get; set; }
}

// CreateRobotAccountCommand
public class CreateRobotAccountCommand
{
    public string? partitionGlobalId { get; set; }
    public string? name { get; set; }
    public string? displayName { get; set; }
    public List<string>? groupIDsToAdd { get; set; } // Guid
}

// UpdateRobotAccountCommand
public class UpdateRobotAccountCommand
{
    public string? partitionGlobalId { get; set; }
    public string? displayName { get; set; }
    public List<string>? groupIDsToAdd { get; set; } // Guid
    public List<string>? groupIDsToRemove { get; set; } // Guid
}

// CreateExternalClientCommand
public class CreateExternalClientCommand
{
    public string? partitionGlobalId { get; set; }
    public string? name { get; set; }
    public bool? isConfidential { get; set; }
    public string? redirectUri { get; set; }
    public ExternalScope[]? scopes { get; set; }
    //public xxx? clientCertificates
}

// CreateUserCommandBase
public class CreateUserCommandBase
{
    public string? id { get; set; } // Guid
    public string? userName { get; set; }
    public string? email { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? displayName { get; set; }
    public string? type { get; set; }
    public bool? bypassBasicAuthRestriction { get; set; }
    public long? legacyId { get; set; }
    public bool? invitationAccepted { get; set; }
}

// class CreateUsersCommand
public class CreateUsersCommand
{
    public List<CreateUserCommandBase>? users { get; set; }
    public string? partitionGlobalId { get; set; }
    public string[]? groupIDs { get; set; } // Guid
}

public class BulkCreateResult
{
    public bool? succeeded { get; set; }
    public string[]? errors { get; set; }
}

public class BulkCreateResponse
{
    public BulkCreateResult? result { get; set; }
    public PmUser[]? users { get; set; }
}

// UserDto
public class PmUser
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? userName { get; set; }
    public string? email { get; set; }
    public bool? emailConfirmed { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? displayName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastModificationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastLoginTime { get; set; }
    public string[]? groupIDs { get; set; }
    public Int64 legacyId { get; set; }
    public bool? isActive { get; set; }
    public bool? bypassBasicAuthRestriction { get; set; }
    public Int64? type { get; set; } // ISSUE: Defined as string in swagger
    public bool? invitationAccepted { get; set; }

    // Are the following properties specific to AD integration?
    public string? userId { get; set; }
    public string[]? userBundleCodes { get; set; }
    public bool? useExternalLicense { get; set; }
    public bool? inheritedFromGroup { get; set; }
}

// UpdateUserCommand
public class UpdateUserCommand
{
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? displayName { get; set; }
    public string? email { get; set; }
    public bool? isActive { get; set; }
    public string? password { get; set; }
    public string[]? groupIDsToAdd { get; set; } // Guid
    public string[]? groupIDsToRemove { get; set; } // Guid
    public bool? bypassBasicAuthRestriction { get; set; }
    public bool? invitationAccepted { get; set; }
    public Dictionary<string, string>? extensionUserAttributesToAddOrUpdate { get; set; }
    public string[]? extensionUserAttributesToRemove { get; set; }
}

public class RemoveUserCommand
{
    public string? partitionGlobalId { get; set; }
    public string[]? userIds { get; set; }
    public bool? deleteCurrentUser { get; set; }
    public bool? isHostMode { get; set; }
}

public class AddLicensedUserCommand
{
    public string[]? userIds { get; set; }
}

public class KeyValuePair(string? key, string? value) // added by UiPathOrch
{
    public string? key { get; set; } = key;
    public string? value { get; set; } = value;
}

public class UpdatePmUserSettingPayload // added by UiPathOrch
{
    public List<KeyValuePair>? settings { get; set; }
    public string? partitionGlobalId { get; set; }
    public string? userId { get; set; }
}

// added by UiPathOrch
public class NuLicensedGroup // The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? id { get; set; } // Guid
    public string? name { get; set; }
    public string[]? userBundleLicenses { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string[]? userBundleLicenseNames { get; set; } // added by UiPathOrch
    public bool? useExternalLicense { get; set; }
    public bool? orphan { get; set; }
}

// added by UiPathOrch
public class NuLicensedUser // The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public string? id { get; set; }
    public string? email { get; set; }
    public string? name { get; set; }
    public string? surname { get; set; }
    public string? displayName { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastInUse { get; set; }
    public string[]? userBundleLicenses { get; set; }
    public bool? orphan { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string[]? userBundleLicenseNames { get; set; } // added by UiPathOrch
}

// added by UiPathOrch
public class NuLicensedGroupMember : NuLicensedUser
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? GroupName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathGroupName { get; set; } // added by UiPathOrch
}

// added by UiPathOrch
public class AvailableUserBundle // The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? code { get; set; }
    public string? name { get; set; } // added by UiPathOrch
    public int? allocated { get; set; }
    public int? total { get; set; }
    public int? inUse { get; set; }
}

// added by UiPathOrch
public class AvailableUserBundles // The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? GroupName { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathGroupName { get; set; } // added by UiPathOrch

    public AvailableUserBundle[]? availableUserBundles { get; set; }

    public string[]? allocatedUserBundles { get; set; }
    public bool? useExternalLicense { get; set; }
}

// added by UiPathOrch
public class UpdateLicensedGroupCommand // The proper class name is unknown.
{
    public string[]? ubls { get; set; }
    public string? id { get; set; } // Guid
    public bool? useExternalLicense { get; set; }
}

// added by UiPathOrch
public class UpdateLicensedGroupResponse // The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? GroupName { get; set; }// added by UiPathOrch

    public string? groupId { get; set; } // Guid
    public string? organizationId { get; set; } // Guid
    public bool? useExternalLicense { get; set; }
    public string[]? userBundleCodes { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string[]? userBundleLicenseNames { get; set; } // added by UiPathOrch
}

// added by UiPathOrch — TenantAllocation response (per-tenant license allocation).
// Endpoint: GET /api/licensing/tenantAllocations?accountGlobalId={org}
public class TenantAllocation
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public TenantAllocationTenant? tenant { get; set; }
    public string[]? services { get; set; }
    public int? unattendedRobot { get; set; }
    public int? unattendedHostingRobot { get; set; }
    public int? nonProductionRobot { get; set; }
    public int? testingRobot { get; set; }
    public int? appTestRobot { get; set; }
    public int? aiRobot { get; set; }
    public int? gpus { get; set; }
    public int? dataServiceUnit { get; set; }
    public int? robotUnits { get; set; }
    public int? aiUnits { get; set; }
    public int? agentUnits { get; set; }
    public int? platformUnits { get; set; }
    public int? performanceTesting { get; set; }
    public int? heals { get; set; }
    public int? testHeals { get; set; }
    public int? medicalRecordSummarizationUnits { get; set; }
    public int? lendingUnits { get; set; }
}

// added by UiPathOrch — nested tenant object inside TenantAllocation
public class TenantAllocationTenant
{
    public string? name { get; set; }
    public string? id { get; set; } // Guid
    public string? color { get; set; }
    public string? region { get; set; }
    public object[]? tenantServiceInstances { get; set; } // unknown inner shape; structure preserved via JsonElement
    public string? status { get; set; }
    public string? tenantUrl { get; set; }
    public string? tenant_url { get; set; } // duplicate of tenantUrl in API payload; preserved verbatim
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createdOn { get; set; }
    public string? updatedBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updatedOn { get; set; }
    public bool? isCanaryTenant { get; set; }
    public string? environment { get; set; }
    public string? operationId { get; set; }
}

// added by UiPathOrch — product allocation row (per-SKU Total / Allocated).
// One entry of /api/license/management/account/available → productAllocations[].
public class ProductAllocation
{
    public string? code { get; set; }
    // total / allocated are double? because consumable SKUs (AIU/AGU/RU/FCCU/HEAL/LU/MRSU/PLTU/SPR/TEU)
    // can return fractional usage, e.g. AIU allocated=1451.8.
    public double? total { get; set; }
    public double? allocated { get; set; }
    public bool? isConsumable { get; set; }
    public long? startDate { get; set; } // Unix epoch seconds
    public long? endDate { get; set; } // Unix epoch seconds
}

// added by UiPathOrch — full /api/license/management/account/available response
// (the Robots & Services tab dashboard: product totals + entitlement usage + service catalog).
public class LicenseInventory
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public ProductAllocation[]? productAllocations { get; set; }
    public AvailableUserBundle[]? userLicensingBundles { get; set; }
    public EntitlementUsage[]? entitlementUsages { get; set; }
    public AvailableService[]? availableServices { get; set; }
    public MlKey[]? mlKeys { get; set; }
}

// added by UiPathOrch — entitlement pool usage entry inside LicenseInventory
public class EntitlementUsage
{
    public string? entitlement { get; set; }
    public EntitlementPoolSummary[]? poolSummary { get; set; }
}

// added by UiPathOrch — pool usage detail inside EntitlementUsage
public class EntitlementPoolSummary
{
    public string? poolName { get; set; }
    public long? totalQuantity { get; set; }
    public long? consumedQuantity { get; set; }
    public long? startTime { get; set; } // Unix epoch seconds
    public long? endTime { get; set; } // Unix epoch seconds
    public string? refreshType { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? hasNextAvailable { get; set; }
}

// added by UiPathOrch — service catalog entry inside LicenseInventory
public class AvailableService
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? serviceLicenseStatus { get; set; }
    public string? provisioningMode { get; set; }
    public string[]? supportedRegions { get; set; }
    public string? defaultRegion { get; set; }
    public string? entitlement { get; set; }
    public bool? isVisible { get; set; }
    public bool? isAlwaysProvision { get; set; }
}

// added by UiPathOrch — account-level license contract.
// Endpoint: GET /api/license/management/account?accountGlobalId={org}&accountUserId={sub}
// The wrapper response holds {mlKeys, accountLicense}; mlKeys is promoted here so that
// a single object carries the complete response.
public class AccountLicense
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public MlKey[]? mlKeys { get; set; } // promoted from wrapper response root

    public string? accountId { get; set; } // Guid
    public string? bundleCode { get; set; }
    public long? endDate { get; set; } // Unix epoch seconds
    public int? gracePeriod { get; set; }
    public string? licenseCode { get; set; }
    public string? licenseStatus { get; set; }
    public AccountLicenseProduct[]? products { get; set; }
    public AccountLicenseTemplate[]? templates { get; set; }
    public AccountLicenseEntitlement[]? entitlements { get; set; }
    public object[]? entitlementOverrides { get; set; } // always empty in observed responses; shape unknown
    public long? startDate { get; set; } // Unix epoch seconds
    public string? subscriptionCode { get; set; }
    public string? subscriptionPlan { get; set; }
    public string? payload { get; set; } // embedded JSON string mirroring other fields; preserved verbatim
    public string? type { get; set; }
    public string? parentLicenseCode { get; set; }
    public string? activationVersion { get; set; }
    public bool? needsLicenseReactivation { get; set; }
    public bool? enableMigration { get; set; }
    public int? migrationDeadlineDays { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? migrationStartedAt { get; set; }
    public string? accountType { get; set; }
}

// added by UiPathOrch — wrapper for /api/license/management/account response
internal class AccountLicenseResponse
{
    public MlKey[]? mlKeys { get; set; }
    public AccountLicense? accountLicense { get; set; }
}

// added by UiPathOrch — nested product entry inside AccountLicense
public class AccountLicenseProduct
{
    public string? code { get; set; }
    public long? quantity { get; set; }
    public string? type { get; set; }
    public ProductFeature[]? features { get; set; }
    public ProductConsumptionDistribution[]? consumptionDistributions { get; set; }
}

// added by UiPathOrch — feature flag entry inside AccountLicenseProduct.features
public class ProductFeature
{
    public string? code { get; set; }
    public string? type { get; set; }
    public string? value { get; set; }
}

// added by UiPathOrch — consumption interval entry inside AccountLicenseProduct.consumptionDistributions
public class ProductConsumptionDistribution
{
    public long? startDate { get; set; } // Unix epoch seconds
    public long? endDate { get; set; } // Unix epoch seconds
    public long? quantity { get; set; }
    public bool? isIntervalFromTrial { get; set; }
}

// added by UiPathOrch — nested template entry inside AccountLicense
public class AccountLicenseTemplate
{
    public string? productCode { get; set; }
    public AccountLicenseTemplateProduct[]? products { get; set; }
}

// added by UiPathOrch — nested product inside AccountLicenseTemplate
public class AccountLicenseTemplateProduct
{
    public string? code { get; set; }
    public string? type { get; set; }
}

// added by UiPathOrch — nested entitlement entry inside AccountLicense
public class AccountLicenseEntitlement
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? value { get; set; }
    public string? type { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? startsOn { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? endsOn { get; set; }
}

// added by UiPathOrch — nested ML service key
public class MlKey
{
    public string? licenseType { get; set; }
    public string? mlServiceKey { get; set; }
}

// GroupDto
public class PmGroup
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? id { get; set; } // Guid
    public string? name { get; set; }
    public string? displayName { get; set; }
    public int? type { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastModificationTime { get; set; }
    public PmGroupMember[]? members { get; set; }
    public string? mappingRole { get; set; }
    public string? scope { get; set; }
    public string[]? userBundleLicenses { get; set; } // undocumented

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string[]? userBundleLicenseNames { get; set; } // added by UiPathOrch

    // public string? userBundleLeases { get; set; } // undocumented. Contents are unknown.
    public bool? useExternalLicense { get; set; } // undocumented
    public bool? orphan { get; set; } // undocumented
}

// CreateGroupCommand
public class CreateGroupCommand
{
    public string? partitionGlobalId { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string[]? directoryUserMemberIDs { get; set; }
}

// UpdateGroupCommand
public class UpdateGroupCommand
{
    public string? partitionGlobalId { get; set; }
    public string? name { get; set; }
    public List<string>? directoryUserIDsToAdd { get; set; } // Guid
    public List<string>? directoryUserIDsToRemove { get; set; } // Guid
}

// BulkResolveByNameCommand
public class BulkResolveByNameCommand
{
    public string[]? entityNames { get; set; }
    public string? entityType { get; set; }
    public string? scope { get; set; }
}

public abstract class PmGroupMember
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathGroupName { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? groupName { get; set; } // added by UiPathOrch

    public string? objectType { get; set; }
    public string? source { get; set; }
    public string? identifier { get; set; } // Guid
    public string? name { get; set; }
    public string? displayName { get; set; }
    public string? email { get; set; }
}

// objectType = "DirectoryUser"
public class DirectoryUser : PmGroupMember
{
    public string? firstName { get; set; }
    public string? lastName { get; set; }
    public string? jobTitle { get; set; }
    public string? companyName { get; set; }
    public string? city { get; set; }
    public string? department { get; set; }
    //public string? extensionUserAttributes // TODO/////////////
    public string? externalId { get; set; }
}

// objectType = "DirectoryGroup"
public class DirectoryGroup : PmGroupMember
{
}

// objectType = "DirectoryRobotUser"
public class DirectoryRobotUser : PmGroupMember
{
}

// objectType = "DirectoryApplication"
public class DirectoryApplication : PmGroupMember
{
    public string? applicationId { get; set; }
}

// ExternalScopeDto
public class ExternalScope
{
    public string? name { get; set; }
    public string? displayName { get; set; }
    public string? description { get; set; }
    public int? type { get; set; }
}

// ExternalResourceDto
public class ExternalResource
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? name { get; set; }
    public string? displayName { get; set; }
    public string? description { get; set; }
    public ExternalScope[]? scopes { get; set; }
}

// SecretDto
public class Secret
{
    public Int64 id { get; set; }
    public string? description { get; set; }
    public string? secret { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? expiryTime { get; set; }
}

// ExternalClientDto
public class ExternalClient
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? name { get; set; }
    public string? id { get; set; }
    public string? secret { get; set; }
    public bool? isConfidential { get; set; }
    public string? redirectUri { get; set; }
    public ExternalResource[]? resources { get; set; }
    public Secret[]? secrets { get; set; }
}

// Used as the return value of Copy-PmExternalApplication.
// Needed to change the default view.
public class ExternalClientCreated : ExternalClient
{
}

// ExternalIdentityProviderDto
public class PmExternalIdentityProvider
{
    public int? id { get; set; }
    public string? partitionGlobalId { get; set; }
    public string? displayName { get; set; }
    public string? displayIcon { get; set; }
    public string? authenticationScheme { get; set; }
    public string? clientId { get; set; }
    public string? clientSecret { get; set; }
    public string? authority { get; set; }
    public string? logoutUrl { get; set; }
    public bool? isActive { get; set; }
    public string? settings { get; set; }
    public bool? isExclusive { get; set; }
    public string? certMetaData { get; set; }
}

// DirectoryConnectionDto
public class PmDirectoryConnection
{
    public Int64? id { get; set; }
    public int? partitionId { get; set; }
    public string? type { get; set; }
    public string? configuration { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? creationTime { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastModificationTime { get; set; }
}

// AuthenticationSettingDto
public class PmAuthenticationSetting
{
    public PmExternalIdentityProvider? externalIdentityProviderDto { get; set; }
    public PmDirectoryConnection? directoryConnectionDto { get; set; }
    public string? authenticationSettingType { get; set; }
    public string? hostConnectionType { get; set; }
}

// AuthenticationSettingRoot // added by UiPathOrch
public class PmAuthenticationRoot
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public PmExternalIdentityProvider? externalIdentityProviderDto { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public Hashtable? settingsExpanded { get; set; }

    public PmAuthenticationSetting? directoryConnectionDto { get; set; }
    public string? authenticationSettingType { get; set; }
    public string? hostConnectionType { get; set; }
}

// IEquatable is needed because this is managed with HashSet<T>.
public class PmAuditLog : IEquatable<PmAuditLog> // added by UiPathOrch. The proper class name is unknown.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createdOn { get; set; }
    public string? category { get; set; }
    public string? action { get; set; }
    public string? auditLogDetails { get; set; }
    public Hashtable? auditLogDetailsExpanded { get; set; } // added by UiPathOrch
    public string? userName { get; set; }
    public string? email { get; set; }
    public string? message { get; set; }
    public string? detailsVersion { get; set; }
    public string? source { get; set; }

    // IEquatable<T> implementation
    public bool Equals(PmAuditLog? other)
    {
        if (other is null)
            return false;

        // Do not consider Path and auditLogDetailsExpanded
        return createdOn == other.createdOn &&
               category == other.category &&
               action == other.action &&
               auditLogDetails == other.auditLogDetails &&
               userName == other.userName &&
               email == other.email &&
               message == other.message &&
               detailsVersion == other.detailsVersion &&
               source == other.source;
    }

    public override bool Equals(object? obj) => Equals(obj as PmAuditLog);

    public override int GetHashCode()
    {
        int hash = HashCode.Combine(createdOn, category, action, auditLogDetails, userName, email, message, detailsVersion);
        return HashCode.Combine(hash, source);
    }
}

// DirectoryScope
public class DirectoryScope
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public string? name { get; set; }
    public bool? isDefault { get; set; }
}

// private: return value from PartitionAccessPolicy
public class AccessAllowedMember
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    public string? objectType { get; set; }
    public string? externalId { get; set; } // string?
    public string? source { get; set; }
    public string? identifier { get; set; }
    public string? name { get; set; }
    public string? email { get; set; }
    public string? displayName { get; set; }
}

#endregion


#region Document Understanding

public class ActionDetail // added by UiPathOrch Not in the swagger doc.
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? @namespace { get; set; }
    public string? serviceDisplayName { get; set; }
    public string? esourceType { get; set; }
    public string? resourceAction { get; set; }
    public string? resourceGroup { get; set; }
    public string? description { get; set; }
}

public class DuRole // added by UiPathOrch Not in the swagger doc.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public string? type { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createdOn { get; set; }
    public string? tenantId { get; set; }
    public ActionDetail[]? actionDetails { get; set; }
}

public class RoleAssignmentDto // added by UiPathOrch Not in the swagger doc.
{
    public string? id { get; set; }
    public string? securityPrincipalId { get; set; }
    public string? securityPrincipalType { get; set; }
    public string? type { get; set; }
    public string? scope { get; set; }
    public string? roleId { get; set; }
    public string? roleName { get; set; }
    public string? roleType { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createdOn { get; set; }
    public bool? inherited { get; set; }
    public bool? mutable { get; set; }
}

public class DuUser // added by UiPathOrch Not in the swagger doc.
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Project { get; set; } // added by UiPathOrch

    public string? securityPrincipalId { get; set; }
    public RoleAssignmentDto[]? roleAssignmentDtos { get; set; }
    public string? displayName { get; set; }
    public string? email { get; set; }
    public string? type { get; set; }
    public string? source { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    private string? _name; // added by UiPathOrch
    internal string? Name // Probably never null, but just in case.
    {
        get
        {
            if (_name is null)
            {
                _name = (type == "DirectoryUser" && !string.IsNullOrEmpty(email)) ? email : displayName;
                if (string.IsNullOrEmpty(_name)) _name = securityPrincipalId;
            }
            return _name;
        }
    }

    // Request from Mishima-san (KDDI): To allow specifying User Principal Name in Add-DuUser,
    // the following would be needed, but I can't think of a good implementation.
    // Either sacrifice performance or add complex parameters...
    // Personally, neither option is acceptable.
    //[JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    //internal string? UserName { get; set; }// Probably never null, but just in case.
}

public class DuRoleAssignment // added by UiPathOrch
{
    public string? roleId { get; set; }
    public string? scope { get; set; }
    public string? securityPrincipalId { get; set; } // Looks like a Guid, but keeping it as string since it's a user id.
    public int? securityPrincipalType { get; set; }
}

public class UserRoleAssignmentsCmd // added by UiPathOrch
{
    public List<DuRoleAssignment>? roleAssignmentsToAdd { get; set; }
    public List<string>? roleAssignmentsToDelete { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.Project
public class DuProject
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? FullName { get; set; } // added by UiPathOrch

    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? createdOn { get; set; }
    public string? detailsUrl { get; set; }
    public string? digitizationStartUrl { get; set; }
    public string? classifiersDiscoveryUrl { get; set; }
    public string? extractorsDiscoveryUrl { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.GetProjectsResponse
public class DuGetProjectsResponse
{
    public DuProject[]? projects { get; set; }
}

public class CreateDuProjectCmd // added by UiPathOrch
{
    public string? name { get; set; }
    public string? description { get; set; }
    public string? ocrMethod { get; set; }
    public string? ocrUrl { get; set; }
    public string? forceApplyOcr { get; set; }
    public string? type { get; set; }
    public bool? helix { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.DocumentType
public class DuDocumentType
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Project { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? name { get; set; }
    public string? detailsUrl { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.GetDocumentTypesResponse
public class DuGetDocumentTypesResponse
{
    public DuDocumentType[]? documentTypes { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.Classifier
public class DuClassifier
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Project { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? name { get; set; }
    public string? status { get; set; }
    public string? detailsUrl { get; set; }
    public string? syncUrl { get; set; }
    public string? asyncUrl { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.GetClassifiersResponse
public class DuGetClassifiersResponse
{
    public DuClassifier[]? classifiers { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.Extractor
public class DuExtractor
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Project { get; set; } // added by UiPathOrch
    public string? id { get; set; }
    public string? name { get; set; }
    public string? documentTypeId { get; set; }
    public string? status { get; set; }
    public string? detailsUrl { get; set; }
    public string? syncUrl { get; set; }
    public string? asyncUrl { get; set; }
}

// UiPath.DocumentUnderstanding.Framework.Api.Controllers.Model.Discovery.GetExtractorsResponse
public class DuGetExtractorsResponse
{
    public DuExtractor[]? extractors { get; set; }
}

#endregion

#region Test Manager

// UiPath.TestManagementHub.Common.DTOs.PagingModelDto`1.PagingDto
public class TmPaging
{
    public int? total { get; set; }
    public int? page { get; set; }
    public int? pages { get; set; }
    public int? pageSize { get; set; }
    public int? returned { get; set; }
    public bool? previousPage { get; set; }
    public bool? nextPage { get; set; }
}

public class TmPagingModel<T>
{
    public List<T>? data { get; set; }
    public TmPaging? paging { get; set; }
}

public class TmPagingModel2<T>
{
    public int? totalPages { get; set; }
    public int? totalItems { get; set; }
    public int? currentPage { get; set; }
    public int? maxPageSize { get; set; }
    public bool? hasPreviousPage { get; set; }
    public bool? hasNextPage { get; set; }
    public List<T>? data { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.ProjectDto
public class TmProject
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? FullName { get; set; } // added by UiPathOrch

    public string? projectPrefix { get; set; }
    public bool? isAuthorizationEnabled { get; set; }
    public bool? isActive { get; set; }
    public bool? isSapConfigured { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.RequirementDto
public class TmRequirement
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? projectId { get; set; }
    public string? objKey { get; set; }
    public string? containerId { get; set; }
    public string? foreignReference { get; set; }
    public string? connectorRequirementId { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.TestCaseDto
public class TmTestCase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? projectId { get; set; }
    public string? objKey { get; set; }
    public string? version { get; set; }
    public string? preCondition { get; set; }
    public string? inputParams { get; set; }
    public string? automationId { get; set; }
    public string? containerId { get; set; }
    public string? automationTestCaseName { get; set; }
    public string? automationProjectName { get; set; }
    public string? foreignReference { get; set; }
    public string? connectorTestCaseId { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.TestSetDto
public class TmTestSet
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? projectId { get; set; }
    public string? objKey { get; set; }
    public string? version { get; set; }
    public int? numberOfTestCases { get; set; }
    public string? source { get; set; }
    public string? sourceDetails { get; set; }
    public string? externalTestSetId { get; set; }
    public bool? enableCoverage { get; set; }
    public bool? enforceExecutionOrder { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.TestExecutionDto
public class TmTestExecution
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? projectId { get; set; }
    public string? testSetId { get; set; }
    public string? testSetObjKey { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? executionStart { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? executionFinished { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? reportingDate { get; set; }
    public string? source { get; set; }
    public string? sourceDetails { get; set; }
    public bool? enableCoverage { get; set; }
    public string? status { get; set; }
    public bool? hasTestsFromMultipleAutomationProjects { get; set; }
    public string? executionType { get; set; }
    public int? runId { get; set; }
    public bool? enforceExecutionOrder { get; set; }
    public int? duration { get; set; }
    public string? testSetScheduleId { get; set; }
    public string? remoteControlAccess { get; set; }
    public string? autopilotForRobotsSettings { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

public class TmTestExecutionResult
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? TestExecution { get; set; } // added by UiPathOrch

    public TmTestCase? testCase { get; set; }
    public string? id { get; set; }
    public string? testCaseId { get; set; }
    public string? projectId { get; set; }
    public string? automationId { get; set; }
    public string? jobKey { get; set; }
    public string? automationProjectName { get; set; }
    public string? automationTestCaseName { get; set; }
    public string? testExecutionId { get; set; }
    public string? variationId { get; set; }
    public string? testCaseVersion { get; set; }
    public string? externalTestExecutionId { get; set; }
    public string? result { get; set; }
    public bool? hasError { get; set; }
    public string? executionType { get; set; }
    public string? businessResult { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? executionStart { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? executionEnd { get; set; }
    public string? executedBy { get; set; }
    public string? detailLink { get; set; }
    public string? defectId { get; set; }
    public string? hostMachineName { get; set; }
    public string? info { get; set; }
    public string? inputArguments { get; set; }
    public string? outputArguments { get; set; }
    public string? robotName { get; set; }
    public string? runtimeType { get; set; }
    public int? runId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public int? executionOrder { get; set; }
    public string? originalResult { get; set; }
    public string? preCondition { get; set; }
    public string? postCondition { get; set; }
    public bool? isPostConditionMet { get; set; }
    public string? assigneeIdentityProviderId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? dueDate { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.DefectDto
public class TmDefect
{
    public string? priority { get; set; }
    public string? status { get; set; }
    public string? link { get; set; }
    public string? linkLabel { get; set; }
    public string? syncStatus { get; set; }
    public string? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
}

// UiPath.Platform.Rbac.Common.Dtos.RoleDto
public class TmRole
{
    public string? name { get; set; }
    public string? description { get; set; }
    public string[]? permissions { get; set; }
    public bool? isStatic { get; set; }
    public string? resourceType { get; set; }
    public string? id { get; set; }
    public string? tenantId { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
    public bool? isDeleted { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? deleted { get; set; }
    public string? deletedBy { get; set; }
}

// UiPath.TestManagementHub.WebAPI.Controllers.ServerInfo
public class TmServerInfo
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? version { get; set; }
    public string? type { get; set; }
    public string? status { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.ProjectSettingsDto
public class TmProjectSettings
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public string? projectPrefix { get; set; }
    public int? maxNumberOfTestSteps { get; set; }
    public string? projectTimeZone { get; set; }
    public string? id { get; set; }
    public string? projectId { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.EndpointsDto
public class TmEndpoints
{
    public string? AuthLocation { get; set; }
    public string? TaskCaptureEndpoint { get; set; }
    public string? SwaggerEndpoint { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.BasicConfigurationDto
public class TmBasicConfiguration
{
    public string? endpoint { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.IdentityServerConfigurationDto
public class TmIdentityServerConfiguration
{
    public string? authority { get; set; }
    public string? identityServerClientId { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.StorageConfigurationDto
public class TmStorageConfiguration
{
    public int? uploadSizeLimit { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.SearchConfigurationDto
public class TmSearchConfiguration
{
    public string? searchProvider { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.OAuthConnectorConfigurationDto
public class TmOAuthConnectorConfiguration
{
    public string? clientId { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.PlatformConfugurationDto
public class TmPlatformConfuguration
{
    public bool? cloudEnabled { get; set; }
    public bool? integrationEnabled { get; set; }
    public string? accountKey { get; set; }
    public string? accountName { get; set; }
    public string? tenantKey { get; set; }
    public string? tenantName { get; set; }
    public int? tokenTimeoutOffsetMinutes { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.ApplicationConfigurationDtoUiPath.TestManagementHub.Configuration.Abstractions.DTOs.ApplicationConfigurationDto
public class TmApplicationConfiguration
{
    public int? maxObjectCountPerRequest { get; set; }
    public int? maxItemCountPerObject { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.ProjectMigrationConfigurationDto
public class TMProjectMigrationConfiguration
{
    public int? projectMigrationAttachmentMaxSizeInMB { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.AISoluaISolutionsConfiguration
public class TmAISolutionsConfiguration
{
    public string[]? aISolutionsSupportedDocumentTypes { get; set; }
}

// UiPath.TestManagementHub.Configuration.Abstractions.DTOs.ConfigDto
public class TmConfig
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    public TmEndpoints? endpoints { get; set; }
    public TmBasicConfiguration? basicConfiguration { get; set; }
    public TmIdentityServerConfiguration? identityServerConfiguration { get; set; }
    public TmStorageConfiguration? storageConfiguration { get; set; }
    public TmSearchConfiguration? searchConfiguration { get; set; }
    public TmPlatformConfuguration? platformConfiguration { get; set; }
    public TmPlatformConfuguration? azureDevOps { get; set; }
    public TmPlatformConfuguration? jiraServer { get; set; }
    public TmPlatformConfuguration? jiraCloudBasicAuth { get; set; }
    public TmPlatformConfuguration? jiraCloudOAuth { get; set; }
    public TmPlatformConfuguration? serviceNow { get; set; }
    public TmPlatformConfuguration? webHook { get; set; }
    public TmPlatformConfuguration? xRay { get; set; }
    public TmPlatformConfuguration? xrayCloud { get; set; }
    public TmPlatformConfuguration? redmine { get; set; }
    public TmPlatformConfuguration? qtest { get; set; }
    public TmPlatformConfuguration? sap { get; set; }
    public TMProjectMigrationConfiguration? projectMigrationConfiguration { get; set; }
    public TmAISolutionsConfiguration? aISolutionsConfiguration { get; set; }
}

// UiPath.TestManagementHub.UserManagement.Abstractions.DTOs.DirectoryUserDto
public class TmDirectoryUser
{
    public string? localIdentifier { get; set; }
    public string? identifier { get; set; }
    public string? identityName { get; set; }
    public string? objectType { get; set; }
    public string? source { get; set; }
    public string? email { get; set; }
    public string? displayName { get; set; }
    public string? firstName { get; set; }
    public string? lastName { get; set; }
}

// UiPath.TestManagementHub.TestManagement.Abstractions.DTOs.ProjectPermissionDto
public class TmProjectPermission
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Path { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Project { get; set; } // added by UiPathOrch
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? PathProject { get; set; } // added by UiPathOrch
    public string? projectId { get; set; }
    public bool? isOwner { get; set; }
    public string? id { get; set; }
    public TmDirectoryUser? user { get; set; }
    [JsonConverter(typeof(LocalDateTimeConverter))]
    public DateTime? lastUpdated { get; set; }
    public string? lastUpdatedBy { get; set; }
    public string[]? roles { get; set; }
}

#endregion

#region OrchCmdlets specific

public class OrchRolePermissionExpanded
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    public string? PathName { get; set; }
    public bool? IsEditable { get; set; }
    public string? Type { get; set; }
    public string? PermissionName { get; set; }
    public string? Scope { get; set; }
    public bool? View { get; set; }
    public bool? Edit { get; set; }
    public bool? Create { get; set; }
    public bool? Delete { get; set; }
}
#endregion

#pragma warning restore IDE1006 // Naming styles
