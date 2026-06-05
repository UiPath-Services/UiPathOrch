using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Regression tests for the Get-OrchLog cache duplicate-accumulation bug.
//
// Background: the Orchestrator robot-log API returns Log.Id == 0 for every
// entry, so the per-folder accumulation cache (RobotLogsCache) cannot key by
// id. Instead Log overrides Equals/GetHashCode across ALL content fields so a
// HashSet<Log> deduplicates identical rows by value -- re-fetching an
// overlapping window does not pile the same rows up again.
//
// A thread-safety change (commit 6fd90f0c, "Fix two thread-safety races")
// swapped the backing HashSet<Log> for a ConcurrentBag<Log>, which ignores
// Equals/GetHashCode entirely -- silently dropping the value dedup. Overlapping
// Get-OrchLog queries then doubled the cached set. The HashSet<Log> store was
// restored (the race it guarded against is unreachable: the only writer is
// Get-OrchLog, which fetches folders sequentially).
//
// These tests lock in BOTH halves of the contract:
//   * RobotLogValueEqualityTests  -- Log's value-equality semantics (the dedup
//                                     itself, incl. Path being excluded so the
//                                     Path stamp Fetch applies doesn't defeat it).
//   * RobotLogsCacheShapeTests    -- the cache's backing store is a value-
//                                     deduplicating HashSet<Log>, not a bag, so a
//                                     future swap back to ConcurrentBag fails here.
public class RobotLogValueEqualityTests
{
    private static readonly DateTime FixedTs =
        new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Log MakeLog(
        string? level = "Info",
        string? windowsIdentity = "DOMAIN\\user",
        string? processName = "MyProcess",
        DateTime? timeStamp = null,
        string? message = "hello world",
        string? jobKey = "job-1",
        string? rawMessage = "{\"m\":\"hello world\"}",
        string? robotName = "Robot1",
        string? hostMachineName = "HOST1",
        long? machineId = 7,
        string? machineKey = "mk-1",
        string? runtimeType = "Unattended",
        long? id = 0,
        string? path = "Orch1:\\Shared")
        => new Log
        {
            Level = level,
            WindowsIdentity = windowsIdentity,
            ProcessName = processName,
            TimeStamp = timeStamp ?? FixedTs,
            Message = message,
            JobKey = jobKey,
            RawMessage = rawMessage,
            RobotName = robotName,
            HostMachineName = hostMachineName,
            MachineId = machineId,
            MachineKey = machineKey,
            RuntimeType = runtimeType,
            Id = id,
            Path = path,
        };

    [Fact]
    public void IdenticalLogs_AreEqual_AndShareHashCode()
    {
        var a = MakeLog();
        var b = MakeLog();

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void HashSet_CollapsesIdenticalContentLogs()
    {
        // The exact operation RobotLogsCache.Fetch performs: Add the same
        // content twice. The second Add must be a no-op.
        var set = new HashSet<Log> { MakeLog() };
        bool added = set.Add(MakeLog());

        Assert.False(added);
        Assert.Single(set);
    }

    [Fact]
    public void PathIsNotPartOfEquality_SoStampedLogsStillDedup()
    {
        // Fetch stamps Log.Path = <folder path> on every entry before adding it
        // to the set. Path is [JsonIgnore] and deliberately excluded from
        // Equals/GetHashCode; if it crept into the equality contract, two
        // identical server rows stamped with the same (or different) path could
        // stop deduplicating. Lock the exclusion: logs differing ONLY by Path
        // are still equal and collapse in a HashSet.
        var a = MakeLog(path: "Orch1:\\Shared");
        var b = MakeLog(path: "Orch1:\\Shared\\Sub");

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        var set = new HashSet<Log> { a, b };
        Assert.Single(set);
    }

    [Fact]
    public void DifferingInAnyContentField_KeepsBothInSet()
    {
        var baseline = MakeLog();

        // Each variant differs from baseline in exactly one content field that
        // Equals/GetHashCode considers. None must be treated as a duplicate.
        var variants = new (string Field, Log Log)[]
        {
            ("Level",           MakeLog(level: "Error")),
            ("WindowsIdentity", MakeLog(windowsIdentity: "DOMAIN\\other")),
            ("ProcessName",     MakeLog(processName: "OtherProcess")),
            ("TimeStamp",       MakeLog(timeStamp: FixedTs.AddSeconds(1))),
            ("Message",         MakeLog(message: "goodbye")),
            ("JobKey",          MakeLog(jobKey: "job-2")),
            ("RawMessage",      MakeLog(rawMessage: "{\"m\":\"goodbye\"}")),
            ("RobotName",       MakeLog(robotName: "Robot2")),
            ("HostMachineName", MakeLog(hostMachineName: "HOST2")),
            ("MachineId",       MakeLog(machineId: 8)),
            ("MachineKey",      MakeLog(machineKey: "mk-2")),
            ("RuntimeType",     MakeLog(runtimeType: "Attended")),
            ("Id",              MakeLog(id: 1)),
        };

        foreach (var (field, variant) in variants)
        {
            Assert.False(baseline.Equals(variant),
                $"Logs differing in {field} must not be equal.");

            var set = new HashSet<Log> { baseline, variant };
            Assert.True(set.Count == 2,
                $"A log differing in {field} must not be deduplicated away.");
        }
    }
}

public class RobotLogsCacheShapeTests
{
    // The fix lives in the choice of backing container. Lock it structurally so
    // a future refactor that reintroduces a non-deduplicating store (the
    // ConcurrentBag<Log> regression) fails here rather than silently doubling
    // the cache.
    [Fact]
    public void CacheBackingStore_IsValueDeduplicatingHashSet_NotABag()
    {
        var field = typeof(RobotLogsCache).GetField(
            "_cache", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(field);
        Assert.Equal(
            typeof(ConcurrentDictionary<long, HashSet<Log>>),
            field!.FieldType);

        // Belt-and-suspenders: the per-folder value type must be HashSet<Log>
        // (dedups via Log.Equals/GetHashCode), never ConcurrentBag<Log> (which
        // ignores equality and was the regression).
        var innerValueType = field.FieldType.GetGenericArguments()[1];
        Assert.Equal(typeof(HashSet<Log>), innerValueType);
        Assert.NotEqual(typeof(ConcurrentBag<Log>), innerValueType);
    }

    [Fact]
    public void GetCache_ReturnsHashSet()
    {
        var method = typeof(RobotLogsCache).GetMethod(
            "GetCache", BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(HashSet<Log>), method!.ReturnType);
    }
}
