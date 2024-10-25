using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using UiPath.PowerShell.Completer;

using Positional = UiPath.PowerShell.Positional.UserName;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands
{
    // なんだかうまく動かないな。。いったん保留で。
    [Cmdlet(VerbsData.Update, "OrchPmUserSetting")]
    //[OutputType(typeof(Entities.PmUser))]
    class UpdatePmUserSettingCommand : OrchestratorPSCmdlet
    {
        private static (string Locale, string LocaleCode)[] _languages = [
            ("English", "en"),
            ("日本語", "ja"),
            ("Deutsch", "de"),
            ("Español", "es"),
            ("Español (México)", "es-mx"),
            ("Français", "fr"),
            ("한국어", "ko"),
            ("Português", "pt"),
            ("Português (Brasil)", "pt-br"),
            ("Русский", "ru"),
            ("Türkçe", "tr"),
            ("中文(简体)", "zh-CN"),
            ("中文(繁體)", "zh-TW")
        ];

        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(PmUserNameCompleter<Positional.UserName_Language>))]
        [SupportsWildcards]
        public string[]? UserName { get; set; }

        [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
        [ArgumentCompleter(typeof(LanguageCompleter))]
        [SupportsWildcards]
        public string? Language { get; set; }

        [Parameter]
        [ArgumentCompleter(typeof(DriveCompleter<Positional.UserName>))]
        public string[]? Path { get; set; }

        internal class PmUserNameCompleter<TPositional> : OrchArgumentCompleter where TPositional : IPositionalParameters
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var drives = ResolveDrives(fakeBoundParameters);

                // パラメータで選択済みの Name は、候補から除外する
                var wpUserName = CreateWPListFromParameter(commandAst, parameterName, TPositional.Parameters, wordToComplete);

                var wp = CreateWPFromWordToComplete(wordToComplete);

                var results = ParallelResults.ForEach(drives, drive => drive.GetPmUsers());

                foreach (var result in results)
                {
                    if (!result.TryGetValue(out var entities)) continue;

                    foreach (var user in entities!.Values
                        .Where(g => wp.IsMatch(g?.userName))
                        .ExcludeByWildcards(u => u?.userName!, wpUserName)
                        .OrderBy(u => u?.userName))
                    {
                        string tooltip = user.GetPSPath();
                        yield return new CompletionResult(PathTools.EscapePSText(user?.userName), user?.userName, CompletionResultType.Text, tooltip);
                    }
                }
            }
        }

        internal class LanguageCompleter : OrchArgumentCompleter
        {
            public override IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                foreach (var language in _languages)
                {
                    string tooltip = $"{language.Locale} ({language.LocaleCode})";
                    yield return new CompletionResult($"'{language.Locale}'", language.Locale, CompletionResultType.Text, tooltip);
                }
            }
        }

        protected override void ProcessRecord()
        {
            var drives = OrchDriveInfo.EnumOrchDrives(Path);
            var wpUserName = UserName.ConvertToWildcardPatternList();

            var (locale, localeCode) = _languages.FirstOrDefault(l => l.Locale == Language);
            localeCode ??= Language;

            using var results = OrchThreadPool.RunForEach(drives,
                drive => drive.NameColonSeparator,
                drive => drive,
                drive => drive.GetPmUsers().Values);

            using var cancelHandler = new ConsoleCancelHandler();
            foreach (var result in results)
            {
                try
                {
                    var entities = result.GetResult(cancelHandler.Token);
                    if (entities == null) continue;

                    var drive = result.Source;

                    foreach (var user in entities
                        .FilterByWildcards(user => user?.userName, wpUserName))
                    {
                        if (ShouldProcess(user.GetPSPath(), "Update PmUserSetting"))
                        {
                            List<Entities.KeyValuePair> settings = [
                                new Entities.KeyValuePair("UserLanguage.Language", localeCode),
                                new Entities.KeyValuePair("UserLanguage.Date", new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds().ToString())
                            ];

                            UpdatePmUserSettingPayload payload = new()
                            {
                                settings = settings,
                                partitionGlobalId = drive!.GetPartitionGlobalId(),
                                userId = user.id
                            };

                            try
                            {
                                drive!.OrchAPISession.PutPmUserSetting(payload);
                            }
                            catch (Exception ex)
                            {
                                WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "GetPmUserError", ErrorCategory.InvalidOperation, user));
                            }
                        }
                    }
                }
                catch (OrchException ex)
                {
                    WriteError(new ErrorRecord(ex, "GetPmUserError", ErrorCategory.InvalidOperation, ex.Target));
                }
            }
        }
    }
}
