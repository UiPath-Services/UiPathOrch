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

public interface IStaticCandidates
{
    static abstract string[] Items { get; }
}

internal class Item1 : IStaticCandidates
{
    public static string[] Items { get; } = ["1"];
}

internal class Item10 : IStaticCandidates
{
    public static string[] Items { get; } = ["10"];
}

internal class Item30 : IStaticCandidates
{
    public static string[] Items { get; } = ["30"];
}

internal class Item40 : IStaticCandidates
{
    public static string[] Items { get; } = ["40"];
}

internal class Item100 : IStaticCandidates
{
    public static string[] Items { get; } = ["100"];
}

internal class Item180 : IStaticCandidates
{
    public static string[] Items { get; } = ["180"];
}

internal class Item500 : IStaticCandidates
{
    public static string[] Items { get; } = ["500"];
}

internal class DirectoryTypes : IStaticCandidates
{
    public static string[] Items { get; } = [
        "DirectoryUser",
        "DirectoryGroup", // Note: local groups cannot be added to local groups (AD groups are allowed)
        "DirectoryRobotUser",
        "DirectoryApplication"
    ];
}

internal class AssetTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Text", "Integer", "Bool"];
}

internal class AuditLogComponentItems : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class AuditLogActionItems : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class BucketOptionsItems : IStaticCandidates
{
    public static string[] Items { get; } = ["ReadOnly", "AuditReadAccess"];
}

internal class BucketStorageProviderItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Azure", "Amazon"];
}

internal class ExecutionSettingsTraceLevelItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Verbose", "Trace", "Information", "Warning", "Error", "Critical", "Off"];
}

internal class JobProcessTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Undefined", "Process", "TestAutomationProcess"];
}

internal class JobOrderableItems : IStaticCandidates
{
    public static string[] Items { get; } = ["CreationTime", "Release/Name", "State", "SpecificPriorityValue", "StartTime", "EndTime", "SourceType"];
}

internal class LicenseRobotTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class LogOrderableItems : IStaticCandidates
{
    public static string[] Items { get; } = ["TimeStamp", "Level"];
}

internal class QueueItemOrderableItems : IStaticCandidates
{
    public static string[] Items { get; } = ["DueDate", "DeferDate", "StartProcessing", "EndProcessing"];
}

internal class PmUserTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["user", "robot", "directoryUser", "directoryGroup", "robotAccount", "application"];
}

internal class UserCredentialTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Default", "SmartCard", "NCipher", "SafeNet", "NoCredential"];
}

internal class UserUpdatePolicyItems : IStaticCandidates
{
    public static string[] Items { get; } = ["None", "LatestPatch", "LatestVersion", "SpecificVersion"];
}

internal class Delete_Archive : IStaticCandidates
{
    public static string[] Items { get; } = ["Delete", "Archive"];
}

internal class User_Group_Application : IStaticCandidates
{
    public static string[] Items { get; } = ["User", "Group", "Application"];
}

internal class MachineSessionStatusItems : IStaticCandidates
{
    public static string[] Items { get; } = ["Available", "Busy", "Disconnected", "Unknown"];
}

internal class JobPriorityItems : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class RemoteControlAccessItems : IStaticCandidates
{
    public static string[] Items { get; } = ["None", "ReadOnly", "Full"];
}

internal class QueueItemCommitTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["AllOrNothing", "StopOnFirstFailure", "ProcessAllIndependently"];
}

internal class VideoRecordingTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["None", "Failed", "All"];
}

internal class QueueItemVideoRecordingTypeItems : IStaticCandidates
{
    public static string[] Items { get; } = ["None", "Failed"];
}

internal class Hour_Day_Week_Month_3Month_6Month_Year_3Year : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class Day_Week_Month_3Month_6Month_Year_3Year : IStaticCandidates
{
    public static string[] Items { get; } = [
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
internal class RuntimeTypes : IStaticCandidates
{
    public static string[] Items { get; } = [
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

internal class True_False : IStaticCandidates
{
    public static string[] Items { get; } = ["True", "False"];
}

internal class SoftStop_Kill : IStaticCandidates
{
    public static string[] Items { get; } = ["SoftStop", "Kill"];
}

internal class Any_Foreground_Background : IStaticCandidates
{
    public static string[] Items { get; } = ["Any", "Foreground", "Background"];
}

internal class Any_Windows_Portable : IStaticCandidates
{
    public static string[] Items { get; } = ["Any", "Windows", "Portable"];
}

internal class Template_Standard_Serverless : IStaticCandidates
{
    public static string[] Items { get; } = ["Template", "Standard", "Serverless"];
}

internal class Default_Serverless_AutomationCloudRobot : IStaticCandidates
{
    public static string[] Items { get; } = ["Default", "Serverless", "AutomationCloudRobot"];
}

internal class Processes_FolderHierarchy : IStaticCandidates
{
    public static string[] Items { get; } = ["Processes", "FolderHierarchy"];
}

internal class DescriptionHere : IStaticCandidates
{
    public static string[] Items { get; } = ["'Description here'"];
}

internal class Modern_Classic : IStaticCandidates
{
    public static string[] Items { get; } = ["Modern", "Classic"];
};

internal class JobPriority : IStaticCandidates
{
    public static string[] Items { get; } = ["Critical", "Highest", "VeryHigh", "High", "MediumHigh", "Medium", "MediumLow", "Low", "VeryLow", "Lowest"];
};

internal class TestSetExecutionStatusNames : IStaticCandidates
{
    public static string[] Items { get; } = Enum.GetNames(typeof(UiPath.PowerShell.Entities.TestSetExecutionStatus));
};

internal class TestSetExecutionTriggerTypeNames : IStaticCandidates
{
    public static string[] Items { get; } = Enum.GetNames(typeof(UiPath.PowerShell.Entities.TestSetExecutionTriggerType));
};

internal class UnattendedSessionStatus : IStaticCandidates
{
    public static string[] Items { get; } = ["Available", "Busy", "Disconnected", "Unknown"];
};
