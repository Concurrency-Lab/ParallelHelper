using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ParallelHelper.Extensions {
  /// <summary>
  /// Extension methods to query the analyzer configurations.
  /// </summary>
  public static class AnalyzerConfigOptionsExtensions {
    private const string ConfigKeyFormat = "dotnet_diagnostic.{0}.{1}";

    /// <summary>
    /// Returns the configured value or the default value if there is no configuration available.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="rule">The rule asking for a specific configuration.</param>
    /// <param name="key">The key of the specific configuration.</param>
    /// <param name="defaultValue">The value to return if no configuration was found.</param>
    /// <returns>The configured value or the provided default.</returns>
    public static string GetConfig(this AnalyzerConfigOptions options, DiagnosticDescriptor rule, string key, string defaultValue) {
      if(options.TryGetValue(GetConfigKey(rule, key), out var value)) {
        return value;
      }
      return defaultValue;
    }

    private static string GetConfigKey(DiagnosticDescriptor rule, string key) {
      return string.Format(ConfigKeyFormat, rule.Id, key);
    }
  }
}
