using UiPath.PowerShell.Commands;
using UiPath.PowerShell.Entities;
using Xunit;

namespace UnitTests;

// Pins TestPmExternalApplicationCmdlet.Diagnose: the cause it names for a refused client-credentials
// request, read from the application's registration.
public class TestPmExternalApplicationTests
{
    private static readonly DateTime Now = new(2026, 10, 8, 12, 0, 0);

    private static ExternalClient App(bool confidential = true, DateTime?[]? expiries = null) => new()
    {
        name = "MyApp",
        id = "cid",
        isConfidential = confidential,
        secrets = (expiries ?? [null]).Select((e, i) => new Secret { id = i, expiryTime = e }).ToArray(),
        resources =
        [
            new ExternalResource
            {
                name = "UiPath.Orchestrator",
                scopes =
                [
                    new ExternalScope { name = "OR.Jobs", type = TestPmExternalApplicationCmdlet.ApplicationScopeType },
                    new ExternalScope { name = "OR.Assets", type = TestPmExternalApplicationCmdlet.ApplicationScopeType },
                    new ExternalScope { name = "OR.Users", type = TestPmExternalApplicationCmdlet.UserScopeType },
                ],
            },
        ],
    };

    [Fact]
    public void Success_with_registered_scopes_has_no_problem()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(true, null, "cid", App(), true, ["OR.Jobs", "OR.Assets"], Now);
        Assert.Empty(p);
    }

    [Fact]
    public void Unknown_client_id_is_named()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_client", "cid", null, true, ["OR.Jobs"], Now);
        Assert.Single(p);
        Assert.Contains("No external application with App ID cid", p[0]);
    }

    [Fact]
    public void Unreadable_registration_adds_nothing_here()
    {
        // The cmdlet adds its own note; Diagnose has nothing to go on.
        Assert.Empty(TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_client", "cid", null, false, ["OR.Jobs"], Now));
    }

    [Fact]
    public void Non_confidential_application_is_named()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_client", "cid", App(confidential: false), true, ["OR.Jobs"], Now);
        Assert.Single(p);
        Assert.Contains("non-confidential", p[0]);
    }

    [Fact]
    public void Wrong_secret_is_named_on_invalid_client()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_client", "cid", App(), true, ["OR.Jobs"], Now);
        Assert.Single(p);
        Assert.Contains("App Secret does not match", p[0]);
    }

    [Fact]
    public void All_secrets_expired_is_named_instead_of_a_wrong_secret()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_client", "cid", App(expiries: [Now.AddDays(-1), Now.AddDays(-30)]), true, ["OR.Jobs"], Now);
        Assert.Single(p);
        Assert.Contains("Every secret", p[0]);
    }

    [Fact]
    public void One_valid_secret_left_is_not_all_expired()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(true, null, "cid", App(expiries: [Now.AddDays(-1), Now.AddDays(30)]), true, ["OR.Jobs"], Now);
        Assert.Empty(p);
    }

    [Fact]
    public void A_user_scope_and_an_unregistered_scope_are_told_apart()
    {
        var p = TestPmExternalApplicationCmdlet.Diagnose(false, "invalid_scope", "cid", App(), true, ["OR.Jobs", "OR.Users", "OR.Queues"], Now);
        Assert.Equal(2, p.Count);
        Assert.Contains("'OR.Users' is registered on 'MyApp' as a user scope", p[0]);
        Assert.Contains("'OR.Queues' is not registered on 'MyApp'", p[1]);
    }

    [Fact]
    public void Scope_names_compare_ignoring_case()
    {
        Assert.Empty(TestPmExternalApplicationCmdlet.Diagnose(true, null, "cid", App(), true, ["or.jobs"], Now));
    }

    [Fact]
    public void ApplicationScopes_lists_application_scopes_only()
    {
        Assert.Equal(["OR.Jobs", "OR.Assets"], TestPmExternalApplicationCmdlet.ApplicationScopes(App()));
        Assert.Empty(TestPmExternalApplicationCmdlet.ApplicationScopes(null));
    }
}
