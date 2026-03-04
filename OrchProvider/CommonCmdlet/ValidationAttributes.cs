using System.Collections;
using System.Management.Automation;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
internal class ValidateDictionaryKeyAttribute<TProvider, TValue> : ValidateArgumentsAttribute
    where TProvider : IDictionaryItems<TValue>
{
    private readonly HashSet<string> _validKeys;

    public bool AllowWildcard { get; set; }

    public ValidateDictionaryKeyAttribute()
    {
        _validKeys = new HashSet<string>(
            TProvider.Items.Keys,
            StringComparer.OrdinalIgnoreCase
        );
    }

    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        IEnumerable<object> values = arguments switch
        {
            null => [],
            string s => [s],
            IEnumerable<object> objEnum => objEnum,
            IEnumerable nonGenericEnum => nonGenericEnum.Cast<object>(),
            _ => [arguments]
        };

        foreach (var v in values)
        {
            if (v is not string s)
                throw new ValidationMetadataException($"Invalid value '{v}'. Expected a string.");

            if (_validKeys.Contains(s))
                continue;

            if (AllowWildcard)
            {
                var pattern = new WildcardPattern(s, WildcardOptions.IgnoreCase);
                if (_validKeys.Any(k => pattern.IsMatch(k)))
                    continue;
            }

            throw new ValidationMetadataException(
                $"Invalid value '{s}'. Allowed values are: {string.Join(", ", _validKeys)}"
                + (AllowWildcard ? " or a wildcard pattern matching them." : ".")
            );
        }
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ValidatePositionalParameterAttribute<T> : ValidateArgumentsAttribute
    where T : IPositionalParameters
{
    private static readonly HashSet<string> _validValues = new(
        T.Parameters,
        StringComparer.OrdinalIgnoreCase
    );

    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        IEnumerable<object> values = arguments switch
        {
            null => [],
            string s => [s],
            IEnumerable<object> objEnum => objEnum,
            IEnumerable nonGenericEnum => nonGenericEnum.Cast<object>(),
            _ => [arguments]
        };

        foreach (var v in values)
        {
            if (v is not string s || !_validValues.Contains(s))
            {
                throw new ValidationMetadataException(
                    $"Invalid value '{v}'. Allowed values are: {string.Join(", ", _validValues)}."
                );
            }
        }
    }
}
