using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Language;
using UiPath.PowerShell.Completer;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;

namespace UiPath.PowerShell.Commands;

// This doesn't seem to work properly... putting on hold for now.
[Cmdlet(VerbsData.Update, "PmUserSetting")]
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
    [ArgumentCompleter(typeof(PmUserEmailCompleter))]
    [SupportsWildcards]
    [Alias("UserName")]
    public string[]? Email { get; set; }

    [Parameter(Position = 1, ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(LanguageCompleter))]
    [SupportsWildcards]
    public string? Language { get; set; }

    [Parameter(ValueFromPipelineByPropertyName = true)]
    [ArgumentCompleter(typeof(DriveCompleter))]
    public string[]? Path { get; set; }

    internal class LanguageCompleter : OrchArgumentCompleter
    {
        public override IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            foreach (var (Locale, LocaleCode) in _languages)
            {
                string tooltip = $"{Locale} ({LocaleCode})";
                yield return new CompletionResult($"'{Locale}'", Locale, CompletionResultType.Text, tooltip);
            }
        }
    }

    protected override void ProcessRecord()
    {
        var drives = SessionState.EnumPmDrives(Path);
        var wpEmail = Email.ConvertToWildcardPatternList();

        var (locale, localeCode) = _languages.FirstOrDefault(l => l.Locale == Language);
        localeCode ??= Language;

        foreach (var drive in drives)
        {
            try
            {
                var entities = drive.PmUsers.Get();
                if (entities is null) continue;

                foreach (var user in entities
                    .FilterByWildcards(user => user?.email, wpEmail)
                    .OrderBy(user => user.email))
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
                            partitionGlobalId = drive.GetPartitionGlobalId(),
                            userId = user.id
                        };

                        try
                        {
                            drive.OrchAPISession.PutPmUserSetting(payload);
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(new OrchException(user.GetPSPath(), ex), "UpdatePmUserSettingError", ErrorCategory.InvalidOperation, user));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(new OrchException(drive.NameColonSeparator, ex), "GetPmUserError", ErrorCategory.InvalidOperation, drive));
            }
        }
    }
}
