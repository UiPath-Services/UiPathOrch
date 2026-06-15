using UiPath.PowerShell.Core;
using Xunit;

namespace UnitTests;

public class ConfigPathOverrideTests
{
    private static readonly object EnvLock = new();

    [Fact]
    public void ConfigDirEnvironmentVariableOverridesDefaultBasePath()
    {
        lock (EnvLock)
        {
            string? originalDir = Environment.GetEnvironmentVariable("UIPATHORCH_CONFIG_DIR");
            string? originalPath = Environment.GetEnvironmentVariable("UIPATHORCH_CONFIG_PATH");
            string debugDir = Path.Combine(Path.GetTempPath(), "uipathorch-debug-config");

            try
            {
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_PATH", null);
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_DIR", debugDir);

                Assert.Equal(debugDir, OrchProvider.GetBasePath());
                Assert.Equal(Path.Combine(debugDir, "UiPathOrchConfig.json"), OrchProvider.GetConfigFilePath());
            }
            finally
            {
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_DIR", originalDir);
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_PATH", originalPath);
            }
        }
    }

    [Fact]
    public void ConfigPathEnvironmentVariableOverridesConfigFilePath()
    {
        lock (EnvLock)
        {
            string? originalDir = Environment.GetEnvironmentVariable("UIPATHORCH_CONFIG_DIR");
            string? originalPath = Environment.GetEnvironmentVariable("UIPATHORCH_CONFIG_PATH");
            string debugConfigPath = Path.Combine(Path.GetTempPath(), "UiPathOrchConfig.debug.json");

            try
            {
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_DIR", null);
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_PATH", debugConfigPath);

                Assert.Equal(debugConfigPath, OrchProvider.GetConfigFilePath());
            }
            finally
            {
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_DIR", originalDir);
                Environment.SetEnvironmentVariable("UIPATHORCH_CONFIG_PATH", originalPath);
            }
        }
    }
}
