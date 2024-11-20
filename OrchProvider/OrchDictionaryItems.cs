using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Positional
{
    // これらのクラスは、型パラメータとして使用する。

    // key は必ず string
    internal interface IDictionaryItems<TValue>
    {
        static abstract Dictionary<string, TValue> Items { get; }
    }

    //internal interface IDictionaryItems<TValue>
    //{
    //    static abstract IEnumerable<KeyValuePair<string, TValue>> Items { get; }
    //}

    //internal class JobSourceTypeItems : IKeyValuePairItems<int>
    //{
    //    public static KeyValuePair<string, int>[] Items { get; } =
    //    [
    //        new("Manual", 0),
    //        new("Schedule", 1),
    //        new("Agent", 2),
    //        new("Queue", 3),
    //        new("Event", 4),
    //        new("Studio", 6),
    //        new("Apps", 8),
    //        new("ApiTrigger", 10)
    //    ];
    //}

    internal class DirectoryTypeItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "DirectoryUser",                0 },
            { "DirectoryGroup",               1 },
            { "DirectoryRobot",               3 },
            { "DirectoryExternalApplication", 4 },
        };
    }

    internal class DirectoryTypes : IDictionaryItems<string>
    {
        public static Dictionary<string, string> Items { get; } = new()
        {
            { "DirectoryUser",        "DirectoryUser" },
            { "DirectoryGroup",       "DirectoryGroup" },
            { "DirectoryRobotUser",   "DirectoryRobot" },
            { "Application",          "DirectoryApplication" }
        };
    }

    //internal class DirectoryTypes2 : IDictionaryItems<string>
    //{
    //    public static Dictionary<string, string> Items { get; } = new()
    //    {
    //        { "DirectoryUser",        "user" },
    //        { "DirectoryGroup",       "group" },
    //        { "DirectoryApplication", "application" }
    //    };
    //}

    internal class AlertComponentItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "Robots", 0 },
            { "Transactions", 1 },
            { "Schedules", 2 },
            { "Jobs", 3 },
            { "Process", 4 },
            { "Tasks", 5 },
            { "Queues", 6 },
            { "Folders", 7 },
            { "PersonalWorkspaces", 8 },
            { "TestAutomation", 9 },
            { "Insights", 10 },
            { "CloudRobots", 11 },
            { "ConnectedTriggers", 12 },
            { "Serverless", 13 },
            { "Export", 14 }
        };
    }

    internal class AlertSeverityItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "Info", 0 },
            { "Success", 1 },
            { "Warn", 2 },
            { "Error", 3 },
            { "Fatal", 4 }
        };
    }

    internal class JobSourceTypeItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "Manual", 0 },
            { "Schedule", 1 },
            { "Agent", 2 },
            { "Queue", 3 },
            { "Event", 4 },
            { "Studio", 6 },
            { "Apps", 8 },
            { "ApiTrigger", 10 }
        };
    }

    internal class JobStateItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "Pending", 0 },
            { "Running", 1 },
            { "Stopping", 2 },
            { "Terminating", 3 },
            { "Faulted", 4 },
            { "Successful", 5 },
            { "Stopped", 6 },
            { "Suspended", 7 },
            { "Resumed", 8 }
        };
    }

    internal class QueueItemStatusItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "New", 0 },
            { "InProgress", 1 },
            { "Failed", 2 },
            { "Successful", 3 },
            { "Abondoned", 4 },
            { "Retried", 5 },
            { "Deleted", 6 }
        };
    }

    internal class QueueItemRevisionItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "None", 0 },
            { "InReview", 1 },
            { "Verified", 2 },
            { "Retried", 3 }
        };
    }

    internal class QueueItemPriorityItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "High", 0 },
            { "Normal", 1 },
            { "Low", 2 }
        };
    }

    internal class QueueItemExceptionItems : IDictionaryItems<int>
    {
        public static Dictionary<string, int> Items { get; } = new()
        {
            { "Application", 0 },
            { "Business", 1 },
        };
    }

    internal class AvailableUserBundlesItems : IDictionaryItems<string>
    {
        public static Dictionary<string, string> Items { get; } = new()
        {
            { "ACCU",        "Action Center - Multiuser" },
            { "ACNU",        "Action Center - Named User" },
            { "AKIT",        "Automation Express" },
            { "ATTUCU",      "Attended - Multiuser" },
            { "ATTUNU",      "Attended - Named User" },
            { "CTZDEVCU",    "Citizen Developer - Multiuser" },
            { "CTZDEVNU",    "Citizen Developer - Named User" },
            { "IDU",         "Insights Designer Users" },
            { "PMBU",        "Process Mining Business User" },
            { "PMD",         "Process Mining Developer" },
            { "RPADEVCU",    "RPA Developer - Multiuser" },
            { "RPADEVNU",    "RPA Developer - Named User" },
            { "RPADEVPROCU", "Automation Developer - Multiuser" },
            { "RPADEVPRONU", "Automation Developer - Named User" },
            { "TSTNU",       "Tester - Named User" }
        };
    }
}
