using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Positional;
using TPositional = UiPath.PowerShell.Positional.Email_License;

namespace UiPath.PowerShell.Commands;

[Cmdlet(VerbsCommon.Add, "PmLicenseToPmUser", SupportsShouldProcess = true)]
[OutputType(typeof(Entities.UpdateLicensedGroupResponse))]
public class AddPmLicenseToPmUserCmdlet : OrchestratorPSCmdlet
{
    // code を管理
    //private Dictionary<(OrchDriveInfo drive, NuLicensedGroup group), HashSet<string>>? _parameterSets;
    private Dictionary<(OrchDriveInfo drive, PmDirectoryEntityInfo group), HashSet<string>>? _parameterSets;

    [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(UserNameCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LicenseCompleter))]
    [SupportsWildcards]
    public string[]? License { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter<TPositional>))]
    public string[]? Path { get; set; }

    private class UserNameCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                yield return new CompletionResult(PathTools.EscapePSText("Please enter at least one character to search."));
                yield break;
            }

            var drives = ResolveOrchDrives(fakeBoundParameters);

            // パラメータで選択済みのユーザー名は、候補から除外する
            var wpUserName = CreateWPListFromParameter(commandAst, "UserName", TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            bool bFound = false;
            foreach (var drive in drives)
            {
                var users = drive.SearchDirectory(wordToComplete);
                if (users is null) continue;

                foreach (var user in users
                    .Where(u => u.type == 0)
                    .ExcludeByWildcards(e => e?.identityName, wpUserName)
                    .OrderBy(e => e.identityName))
                {
                    bFound = true;
                    string tiphelp = TipHelp(user);
                    yield return new CompletionResult(PathTools.EscapePSText(user?.identityName), user?.identityName, CompletionResultType.Text, tiphelp);
                }
            }
            if (!bFound)
            {
                yield return new CompletionResult($"\"No results matching '{wordToComplete}'\".");
            }
        }
    }

    private class LicenseCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var drives = ResolvePmDrives(fakeBoundParameters);

            var wpEmail = CreateWPListFromOtherParameters(commandAst, "Email", TPositional.Parameters);
            var wpLicense = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

            var wp = CreateWPFromWordToComplete(wordToComplete);

            //var results = ParallelResults3.GroupBy(drives, drive => drive.PmUsers.Get());

            //foreach (var drive in drives)
            //{
            //    var licenses = drive.OrchAPISession.GetPmLicensedUsersAvailableLicenses();

            //    foreach (var license in licenses ?? [])
            //    {
            //        if (license.allocated == license.total) continue;
            //        if (AvailableUserBundlesItems.Items.TryGetValue(license.code ?? "", out var licenseName))
            //        {
            //            yield return new CompletionResult(PathTools.EscapePSText(licenseName), licenseName, CompletionResultType.Text, licenseName);
            //        }
            //        else
            //        {
            //            yield return new CompletionResult(PathTools.EscapePSText(license.code), license.code, CompletionResultType.Text, license.code);
            //        }
            //    }
            //}
            yield break;
        }
    }

    protected override void ProcessRecord()
    {
        _parameterSets ??= [];

        var drives = SessionState.EnumPmDrives(Path);

        Email = Email.Split1stValueByUnescapedCommas()?.ToArray();
        var wpLicense = License.Split1stValueByUnescapedCommas().ConvertToWildcardPatternList();

        foreach (var drive in drives)
        {
            foreach (var email in Email ?? [])
            {
                // licensed user でなければ 
                // POST /portal_/api/license/accountant/UserLicense/users
                // を呼ぶ

                var existingLicensedUsers = drive.PmLicensedUsers.Get();
                var targetUser = existingLicensedUsers.FirstOrDefault(e => string.Compare(e.name, email, true) == 0);

                if (targetUser is null)
                {
                    var resolvedUser = drive.PmBulkResolveByName("user", [email], e => e, null);
                    if (resolvedUser == null || !resolvedUser.Any())
                    {
                        // 指定のユーザーは存在しない
                        continue;
                    }

                    AddLicensedUserCommand payload = new()
                    {
                        userIds = [resolvedUser.First().Value!.identifier!]
                    };
                    drive.OrchAPISession.PutLicensedUser(payload);
                }












                //var groups = drive.SearchPmDirectory(groupName);
                //if (groups is null) continue;

                //foreach (var group in groups
                //    .Where(g => g.objectType == "DirectoryGroup" || g.objectType == "LocalGroup")
                //    .OrderBy(e => e.identityName))
                //{
                //    //var licenseGroups = drive.GetPmLicensedGroups();
                //    //var targetGroups = licenseGroups.SelectByWildcards(g => g?.name, wpGroupName);

                //    //foreach (var group in targetGroups.OrderBy(g => g.name))
                //    {
                //        if (!_parameterSets.TryGetValue((drive, group), out var codes))
                //        {
                //            codes = [];
                //            _parameterSets[(drive, group)] = codes;
                //        }

                //        var availableUserBundles = drive.GetPmUserLicenseGroupsAvailableLicenses(group.identifier, group.identityName!);
                //        codes.UnionWith(availableUserBundles?.availableUserBundles?
                //            .SelectByWildcards(b => b?.name, wpLicense)?
                //            .Select(b => b.code!) ?? []);
                //    }
                //}
            }
        }
    }

    //protected override void EndProcessing()
    //{
    //}
}
