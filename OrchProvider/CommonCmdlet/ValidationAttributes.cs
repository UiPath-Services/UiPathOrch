using System.Collections;
using System.Management.Automation;
using System.Reflection;
using UiPath.PowerShell.Positional;

namespace UiPath.PowerShell.Commands;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ValidateEnumAttribute : ValidateArgumentsAttribute
{
    private readonly string[] _validValues;

    public ValidateEnumAttribute(Type enumType)
    {
        if (!enumType.IsEnum)
            throw new ArgumentException(
                $"Type '{enumType.FullName}' is not an enum.", nameof(enumType));

        _validValues = Enum.GetNames(enumType);
    }

    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        // 配列として渡ってくる想定
        var values = arguments as object[] ?? Array.Empty<object>();
        foreach (var v in values)
        {
            if (v is not string s ||
                !_validValues.Any(x => x.Equals(s, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationMetadataException(
                    $"Invalid value '{v}'. Allowed values are: {string.Join(", ", _validValues)}.");
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ValidateDictionaryKeyAttribute : ValidateArgumentsAttribute
{
    private readonly HashSet<string> _validKeys;

    public bool AllowWildcard { get; set; }

    public ValidateDictionaryKeyAttribute(Type providerType)
    {
        var prop = providerType.GetProperty("Items", BindingFlags.Public | BindingFlags.Static)
            ?? throw new ArgumentException($"Type {providerType.FullName} must have a public static Items property.", nameof(providerType));

        if (prop.GetValue(null) is not IDictionary dict)
            throw new ArgumentException($"The 'Items' property of {providerType.FullName} must be IDictionary.", nameof(providerType));

        _validKeys = new HashSet<string>(
            dict.Keys.Cast<object>().Select(k => k.ToString()!),
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

/// <summary>
/// Validates that string[] parameter values match the values in the specified provider's Items dictionary.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class ValidateDictionaryValueAttribute : ValidateArgumentsAttribute
{
    private readonly HashSet<string> _validValues;

    public ValidateDictionaryValueAttribute(Type providerType)
    {
        // Ensure providerType has a public static 'Items' property
        var prop = providerType.GetProperty(
            "Items",
            BindingFlags.Public | BindingFlags.Static
        ) ?? throw new ArgumentException(
            $"Type {providerType.FullName} must have a public static Items property.",
            nameof(providerType)
        );

        // Ensure Items is IDictionary<string, object>
        if (prop.GetValue(null) is not IDictionary dict)
            throw new ArgumentException(
                $"The 'Items' property of {providerType.FullName} must be IDictionary.",
                nameof(providerType)
            );

        // Extract values via ToString()
        _validValues = new HashSet<string>(
            dict.Values.Cast<object>().Select(v => v.ToString()!),
            StringComparer.OrdinalIgnoreCase
        );
    }

    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        var values = arguments as object[] ?? Array.Empty<object>();
        foreach (var v in values)
        {
            // Use ToString() to compare
            var s = v?.ToString() ?? string.Empty;
            if (!_validValues.Contains(s))
            {
                throw new ValidationMetadataException(
                    $"Invalid value '{s}'. Allowed values are: {string.Join(", ", _validValues)}."
                );
            }
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
