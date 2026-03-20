namespace UiPath.PowerShell.Positional;

// These classes are used as type parameters.
// To prevent code bloat, the same class should be reused for the same definition, which is why they are defined together here.
// Actually, in C# (unlike C++) using different classes as type parameters may not cause code bloat...
// Since public type parameters are needed when using completers from .ps1, this approach is fine as-is.

public interface IBoolParameter
{
    public static abstract bool Value { get; }
}

public class True : IBoolParameter
{
    public static bool Value => true;
}

public class False : IBoolParameter
{
    public static bool Value => false;
}

public interface IPositionalParameters
{
    static abstract string[] Parameters { get; }
}

internal class Item1 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["1"];
}

internal class Item10 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["10"];
}

internal class Item30 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["30"];
}

internal class Item40 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["40"];
}

internal class Item100 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["100"];
}

internal class Item180 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["180"];
}

internal class Item500 : IPositionalParameters
{
    public static string[] Parameters { get; } = ["500"];
}

internal class DirectoryTypes : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "DirectoryUser",
        "DirectoryGroup", // Note: local groups cannot be added to local groups (AD groups are allowed)
        "DirectoryRobotUser",
        "DirectoryApplication"
    ];
}

internal class AssetTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Text", "Integer", "Bool"];
}

internal class AuditLogComponentItems : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Assets",
        "Buckets",
        "CloudSnapshots",
        "CloudSubscriptions",
        "Comments",
        "CredentialStores",
        "CredentialsProxies",
        "DirectoryService",
        "Environments",
        "ExecutionMedia",
        "Folders",
        "Jobs",
        "Libraries",
        "Licenses",
        "Machines",
        "Maintenance",
        "Monitoring",
        "Packages",
        "PersonalWorkspaces",
        "Processes",
        "Queues",
        "RemoteControl",
        "Robots",
        "Roles",
        "Triggers",
        "Sessions",
        "Settings",
        //"Actions",
        "Units",
        "Users",
        "Webhooks",
    ];
}

internal class AuditLogActionItems : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Acknowledge",
        "Activate",
        "Assign",
        "Associate",
        "AutomaticallyExploreEnd",
        "BulkComplete",
        "BulkSave",
        "BulkUpload",
        "ChangePassword",
        "ChangeStatus",
        "Convert",
        "Create",
        "CreateBlobFileSas",
        "Deactivate",
        "Delete",
        "DeleteBlobFile",
        "Download",
        "End",
        "ExploreEnd",
        "ExploreStart",
        "Forward",
        "Import",
        "MigrateFolder",
        "Move",
        "PasswordResetAttempt",
        "ResetPassword",
        "Save",
        "Skip",
        "Start",
        "StartDelete",
        "StartJob",
        "StartMigrateFolders",
        "StopJob",
        "Toggle",
        "ToggleUserFolderSubscription",
        "Unassign",
        "Update",
        "Upload",
        "VideoAccess"
    ];
}

internal class BucketOptionsItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["ReadOnly", "AuditReadAccess"];
}

internal class BucketStorageProviderItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Azure", "Amazon"];
}

internal class ExecutionSettingsTraceLevelItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Verbose", "Trace", "Information", "Warning", "Error", "Critical", "Off"];
}

internal class JobProcessTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Undefined", "Process", "TestAutomationProcess"];
}

internal class JobOrderableItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["CreationTime", "Release/Name", "State", "SpecificPriorityValue", "StartTime", "EndTime", "SourceType"];
}

internal class LicenseRobotTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Attended",
        "AttendedStudioWeb",
        "AutomationCloud",
        "AutomationCloudTestAutomation",
        "AutomationKit",
        "Development",
        "Headless",
        "NonProduction",
        "Serverless",
        "ServerlessTestAutomation",
        "StudioPro",
        "StudioX",
        "TestAutomation",
        "Unattended",
        //"CitizenDeveloper",
        //"RpaDeveloper",
        //"RpaDeveloperPro",
        //"Studio",
    ];
}

internal class LogOrderableItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["TimeStamp", "Level"];
}

internal class QueueItemOrderableItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["DueDate", "DeferDate", "StartProcessing", "EndProcessing"];
}

internal class PmUserTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["user", "robot", "directoryUser", "directoryGroup", "robotAccount", "application"];
}

internal class UserCredentialTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Default", "SmartCard", "NCipher", "SafeNet", "NoCredential"];
}

internal class UserUpdatePolicyItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["None", "LatestPatch", "LatestVersion", "SpecificVersion"];
}

internal class Delete_Archive : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Delete", "Archive"];
}

internal class User_Group_Application : IPositionalParameters
{
    public static string[] Parameters { get; } = ["User", "Group", "Application"];
}

internal class MachineSessionStatusItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Available", "Busy", "Disconnected", "Unknown"];
}

internal class JobPriorityItems : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Critical",
        "Highest",
        "VeryHigh",
        "High",
        "MediumHigh",
        "Medium",
        "MediumLow",
        "Low",
        "VeryLow",
        "Lowest"
    ];
}

internal class RemoteControlAccessItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["None", "ReadOnly", "Full"];
}

internal class QueueItemCommitTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["AllOrNothing", "StopOnFirstFailure", "ProcessAllIndependently"];
}

internal class VideoRecordingTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["None", "Failed", "All"];
}

internal class QueueItemVideoRecordingTypeItems : IPositionalParameters
{
    public static string[] Parameters { get; } = ["None", "Failed"];
}

internal class Hour_Day_Week_Month_3Month_6Month_Year_3Year : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Hour",
        "Day",
        "Week",
        "Month",
        "3Months",
        "6Months",
        "Year",
        "3Years"
    ];
}

internal class Day_Week_Month_3Month_6Month_Year_3Year : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "Day",
        "Week",
        "Month",
        "3Months",
        "6Months",
        "Year",
        "3Years"
    ];
}

/*
 * NonProduction: 0,
 * Attended: 1,
 * Unattended: 2,
 * Development: 3,
 * Studio: 3,
 * RpaDeveloper: 3,
 * StudioX: 4,
 * CitizenDeveloper: 4,
 * Headless: 5,
 * RpaDeveloperPro: 6,
 * StudioPro: 6,
 * TestAutomation: 7,
 * AutomationCloud: 8,
 * Serverless: 9,
 * AutomationKit: 10,
 * ServerlessTestAutomation: 11,
 * AutomationCloudTestAutomation: 12,
 * AttendedStudioWeb: 13,
 */
internal class RuntimeTypes : IPositionalParameters
{
    public static string[] Parameters { get; } = [
        "NonProduction",
        "Attended",
        "Unattended",
        "Development",
        "Studio",
        "RpaDeveloper",
        "StudioX",
        "CitizenDeveloper",
        "Headless",
        "StudioPro",
        "RpaDeveloperPro",
        "TestAutomation",
        "AutomationCloud",
        "Serverless",
        "AutomationKit",
        "ServerlessTestAutomation",
        "AutomationCloudTestAutomation",
        "AttendedStudioWeb",
        "Hosting",
        "AssistantWeb",
        "ProcessOrchestration",
        "AgentService",
        "AppTest"
    ];
}

internal class True_False : IPositionalParameters
{
    public static string[] Parameters { get; } = ["True", "False"];
}

internal class SoftStop_Kill : IPositionalParameters
{
    public static string[] Parameters { get; } = ["SoftStop", "Kill"];
}

internal class Id_Version : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Id", "Version"];
}

internal class Any_Foreground_Background : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Any", "Foreground", "Background"];
}

internal class Any_Windows_Portable : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Any", "Windows", "Portable"];
}

internal class Template_Standard_Serverless : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Template", "Standard", "Serverless"];
}

internal class Default_Serverless_AutomationCloudRobot : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Default", "Serverless", "AutomationCloudRobot"];
}

internal class Processes_FolderHierarchy : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Processes", "FolderHierarchy"];
}

internal class DescriptionHere : IPositionalParameters
{
    public static string[] Parameters { get; } = ["'Description here'"];
}

internal class Name : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name"];
}

internal class Modern_Classic : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Modern", "Classic"];
};
