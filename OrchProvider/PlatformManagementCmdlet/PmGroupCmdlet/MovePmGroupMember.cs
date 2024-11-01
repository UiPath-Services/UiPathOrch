using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.GroupName_Type_UserName;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    // WIP
    //[Cmdlet(VerbsCommon.Move, "OrchPmGroupMember", SupportsShouldProcess = true)]
    //[OutputType(typeof(Entities.IdGroup))]
    class RemovePmGroupMember : OrchestratorPSCmdlet
    {
        [Parameter(Position = 0, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<Positional.GroupName_Type_UserName>))]
        [SupportsWildcards]
        public string? SourceGroup { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //[ArgumentCompleter(typeof(TypeCompleter))]
        [SupportsWildcards]
        public string[]? MemberName { get; set; }

        [Parameter(Position = 2, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmGroupNameCompleter<Positional.GroupName_Type_UserName>))]
        [SupportsWildcards]
        public string[]? DestinationGroup { get; set; }

        //[Parameter]
        //public SwitchParameter WarnOnNoMatch { get; set; }

        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.GroupName_Type_UserName>))]
        public string[]? Path { get; set; }

        private static List<Member> GetExistingMembers(List<OrchDriveInfo> drives, List<WildcardPattern>? wpGroupName)
        {
            var results = ParallelResults.ForEach(drives, drive =>
            {
                var groups = drive.GetPmGroups().Values
                    .FilterByWildcards(g => g?.name!, wpGroupName)
                    .OrderBy(g => g?.name);
                return ParallelResults.ForEach(groups, group => drive.GetPmGroup(group?.id));
            });

            List<Member> existingMembers = [];
            foreach (var result in results)
            {
                if (!result.TryGetValue(out var entities)) continue;

                foreach (var group in entities!)
                {
                    if (!group.TryGetValue(out var detailedGroup)) continue;

                    existingMembers.AddRange(detailedGroup?.members ?? []);
                }
            }
            return existingMembers;
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpSourceGroup = new WildcardPattern(SourceGroup, WildcardOptions.IgnoreCase);
            var wpMemberName = MemberName.ConvertToWildcardPatternList();
            var wpDestinationGroup = DestinationGroup.ConvertToWildcardPatternList();

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var drive in drives)
            {
                cancelHandler.Token.ThrowIfCancellationRequested();

                try
                {
                    var existingGroups = drive.GetPmGroups();
                    var srcGroups = existingGroups.Values.Where(g => wpSourceGroup.IsMatch(g.name));
                    var dstGroups = existingGroups.Values.FilterByWildcards(g => g.name, wpDestinationGroup);

                    // source group が一つであることを sure にするのが面倒くさい。。
                    // 複数指定した場合でも、そのまま動かしちゃうのでいいか。。

                    foreach (var srcGroup in srcGroups.OrderBy(g => g.name))
                    {
                        var membersToMove = srcGroup.members?.FilterByWildcards(m => m.name, wpMemberName) ?? [];

                        foreach (var dstGroup in dstGroups.OrderBy(g => g.name))
                        {
                            

                        }
                    }


                        //drive.OrchAPISession.PutPmGroup(group.id, updateGroupCommand);
                        drive._dicPmGroups = null;
                        drive._dicPmGroups_Exception.ClearCache();
                }
                catch (Exception ex)
                {
                    //WriteError(new ErrorRecord(new OrchException(group!.GetPSPath(), ex), "PutIdGroupError", ErrorCategory.InvalidOperation, group));
                }
            }
        }
    }
}
