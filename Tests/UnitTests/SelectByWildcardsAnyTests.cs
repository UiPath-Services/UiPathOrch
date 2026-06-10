using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using UiPath.PowerShell.Core;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// SelectByWildcardsAny backs Update-/Copy-OrchUser -UserName matching: it must match EITHER the tenant
// UserName OR the canonical EmailAddress (an Azure AD B2B guest has a mangled tenant UserName but a real
// email), and -- unlike FilterByWildcardsAny -- return EMPTY when no pattern is supplied, so an action
// cmdlet given no -UserName is a no-op instead of apply-to-all.
public class SelectByWildcardsAnyTests
{
    private static readonly User[] Users =
    [
        new User { UserName = "alice@contoso.com",                     EmailAddress = "alice@contoso.com" },
        new User { UserName = "bob_contoso.com#EXT#@x.onmicrosoft.com", EmailAddress = "bob@contoso.com" },
    ];

    private static readonly Func<User?, string?>[] Selectors = [u => u?.UserName, u => u?.EmailAddress];

    [Fact]
    public void Matches_by_email_when_the_tenant_username_is_mangled()
    {
        var hit = Users.SelectByWildcardsAny(Selectors,
            new[] { "bob@contoso.com" }.ConvertToWildcardPatternList()).ToList();
        Assert.Single(hit);
        Assert.Equal("bob_contoso.com#EXT#@x.onmicrosoft.com", hit[0].UserName);
    }

    [Fact]
    public void Matches_by_username()
    {
        var hit = Users.SelectByWildcardsAny(Selectors,
            new[] { "alice@contoso.com" }.ConvertToWildcardPatternList()).ToList();
        Assert.Single(hit);
        Assert.Equal("alice@contoso.com", hit[0].EmailAddress);
    }

    [Fact]
    public void Empty_or_null_patterns_return_empty_not_all()
    {
        Assert.Empty(Users.SelectByWildcardsAny(Selectors, (List<WildcardPattern>?)null));
        Assert.Empty(Users.SelectByWildcardsAny(Selectors, Array.Empty<string>().ConvertToWildcardPatternList()));
    }
}
