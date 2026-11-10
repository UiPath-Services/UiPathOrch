using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace UiPath.PowerShell.Core;

// Which COMMAND a provider call belongs to. One Copy-Item with several source paths calls the
// provider's CopyItem once per path, and the provider needs to tell "the next path of this
// command" from "a new command" (to keep a folder counter and a link report across the paths).
//
// The command's own InvocationInfo is that identity: one object per invocation, the same for
// every path. It is reached through CmdletProvider.Context (internal), so reflection is used,
// and if the PowerShell internals ever move, the dynamic-parameters object stands in. That
// fallback is what was used alone before: it is one object per invocation for a wildcard
// source, which PowerShell resolves inside one provider call per match, but a NEW object per
// element of an array -Path, which made every element look like a new command.
//
// State that must span the paths of one command lives in a table keyed by that token, not in
// provider fields: for an array -Path PowerShell makes a NEW provider instance per element, so
// fields reset however the token is chosen. The table holds its keys weakly, so a finished
// command's state goes with its InvocationInfo.
internal static class ProviderInvocation
{
    private static readonly ConditionalWeakTable<object, object> s_state = new();

    // The state of the command `token` belongs to; isFirst is true on the first path of it.
    public static T StateFor<T>(object? token, out bool isFirst) where T : class, new()
    {
        if (token is null) { isFirst = true; return new T(); }
        bool created = false;
        var state = s_state.GetValue(token, _ => { created = true; return new T(); });
        isFirst = created;
        return (T)state;
    }

    private static readonly PropertyInfo? s_context =
        typeof(CmdletProvider).GetProperty("Context", BindingFlags.Instance | BindingFlags.NonPublic);

    private static PropertyInfo? s_myInvocation;

    public static object? Token(CmdletProvider provider, object? dynamicParameters)
    {
        try
        {
            var context = s_context?.GetValue(provider);
            if (context is not null)
            {
                s_myInvocation ??= context.GetType().GetProperty("MyInvocation",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (s_myInvocation?.GetValue(context) is InvocationInfo invocation)
                {
                    return invocation;
                }
            }
        }
        catch
        {
            // fall through to the dynamic-parameters identity
        }
        return dynamicParameters;
    }
}
