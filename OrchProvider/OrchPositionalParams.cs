namespace UiPath.PowerShell.Positional;

// これらのクラスは、型パラメータとして使用する。
// code bloat を抑止するため、同じ定義に対しては同じクラスを使う必要がある。そのため、ここでまとめて定義しておく。
// と思ったけど、実は C# の場合は（C++ とは違って）違うクラスを型パラメータとして使っても code bloat は発生しないかな。。
// .ps1 から completer を使うときには、public な型パラメータが必要になるから、これはこれで良いか。

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
        "DirectoryGroup", // ローカルグループにローカルグループは追加できないことに注意（ADグループは可能）
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

public class Empty : IPositionalParameters
{
    public static string[] Parameters { get; } = [];
}

internal class GroupName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName"];
}

internal class GroupName_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName", "Destination"];
}

internal class GroupName_License : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName", "License"];
}

internal class Email_License : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Email", "License"];
}

internal class GroupName_UserName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName", "UserName"];
}

internal class GroupName_Type_UserName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName", "Type", "UserName"];
}

internal class GroupName_UserName_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["GroupName", "UserName", "Destination"];
}

internal class Id : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Id"];
}

internal class Id_Level : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Id", "Level"];
}

internal class Id_Version : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Id", "Version"];
}

internal class PackageId_Version : IPositionalParameters
{
    public static string[] Parameters { get; } = ["PackageId", "Version"];
}

internal class Id_Version_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Id", "Version", "Destination"];
}

internal class JobId : IPositionalParameters
{
    public static string[] Parameters { get; } = ["JobId"];
}

internal class JobId_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["JobId", "Destination"];
}

internal class Any_Foreground_Background : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Any", "Foreground", "Background"];
}

internal class Any_Windows_Portable : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Any", "Windows", "Portable"];
}

internal class Key : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Key"];
}

internal class Last : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Last"];
}

internal class Template_Standard_Serverless : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Template", "Standard", "Serverless"];
}

internal class Default_Serverless_AutomationCloudRobot : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Default", "Serverless", "AutomationCloudRobot"];
}

internal class Last_Component_UserName_Action : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Last", "Component", "UserName", "Action"];
}

internal class Last_Severity_Component : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Last", "Severity", "Component"];
}

internal class MachineName_HostMachineName_ServiceUserName_SessionId : IPositionalParameters
{
    public static string[] Parameters { get; } = ["MachineName", "HostMachineName", "ServiceUserName", "SessionId"];
}

internal class Name_Email : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Email"];
}

internal class Name_FullPath_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "FullPath", "Destination"];
}

internal class Name_UserName_MachineName_CredentialUsername_CredentialPassword : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "UserName", "MachineName", "CredentialUsername", "CredentialPassword"];
}

internal class Processes_FolderHierarchy : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Processes", "FolderHierarchy"];
}

internal class DescriptionHere : IPositionalParameters
{
    public static string[] Parameters { get; } = ["'Description here'"];
}

internal class EntityType_Name : IPositionalParameters
{
    public static string[] Parameters { get; } = ["EntityType", "Name"];
}

internal class Type_Name : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Type", "Name"];
}

internal class Type_Name_Role : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Type", "Name", "Role"];
}

internal class Name : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name"];
}

internal class Source_Name : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Source", "Name"];
}

internal class Name_ExcludedDate : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "ExcludedDate"];
}

internal class Name_ImportCsv_CsvEncoding_CommitType : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "ImportCsv", "CsvEncoding", "CommitType"];
}

internal class Name_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Destination"];
}

internal class Name_Id_RowVersion : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Id", "RowVersion"];
}

internal class Name_DirectoryUserMember : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "DirectoryUserMember"];
}

internal class Name_FullPath : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "FullPath"];
}

internal class Name_GroupName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "GroupName"];
}

internal class Name_Link : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Link"];
}

internal class Name_OwnerName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "OwnerName"];
}

internal class Name_PropagateToSubFolders : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "PropagateToSubFolders"];
}

internal class Name_ReleaseName: IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "ReleaseName"];
}

internal class Name_Role : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Role"];
}

internal class Name_RuntimeType_JobsCount : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "RuntimeType", "JobsCount"];
}

internal class Name_SecretId : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "SecretId"];
}

internal class Name_UserName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "UserName"];
}

internal class FullName_Username : IPositionalParameters
{
    public static string[] Parameters { get; } = ["FullName", "Username"];
}

internal class Name_Id : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Id"];
}

internal class Name_ValueType : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "ValueType"];
}

internal class Name_Version : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Name", "Version"];
}

internal class Path : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Path"];
}

internal class RobotType : IPositionalParameters
{
    public static string[] Parameters { get; } = ["RobotType"];
}

internal class RobotType_Key : IPositionalParameters
{
    public static string[] Parameters { get; } = ["RobotType", "Key"];
}

internal class Scope_DisplayName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Scope", "DisplayName"];
}

internal class Source_Path: IPositionalParameters
{
    public static string[] Parameters { get; } = ["Source", "Path"];
}

public class SourceGroupName_UserName_DestinationGroupName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["SourceGroupName", "UserName", "DestinationGroupName"];
}

public class Status : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Status"];
}

internal class Type_UserName_Roles : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Type", "UserName", "Roles"];
}

internal class Type_UserName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Type", "UserName"];
}

public class Email : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Email"];
}

public class Email_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Email", "Destination"];
}

public class UserName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName"];
}

internal class UserName_Destination : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName", "Destination"];
}

internal class UserName_FullName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName", "FullName"];
}

internal class UserName_FullName_Type : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName", "FullName", "Type"];
}

internal class Email_Language : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Email", "Language"];
}

internal class UserName_GroupName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName", "GroupName"];
}

internal class UserName_Roles : IPositionalParameters
{
    public static string[] Parameters { get; } = ["UserName", "Roles"];
}

internal class ValueType_Name_Value_UserName_MachineName : IPositionalParameters
{
    public static string[] Parameters { get; } = ["ValueType", "Name", "Value", "UserName", "MachineName"];
};

internal class Modern_Classic : IPositionalParameters
{
    public static string[] Parameters { get; } = ["Modern", "Classic"];
};
