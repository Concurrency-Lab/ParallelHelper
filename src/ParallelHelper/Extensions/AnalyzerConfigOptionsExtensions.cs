using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ParallelHelper.Analyzer;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// Parses the provided configuration as <see cref="ClassMemberDescriptor"/>. The required format is: &lt;type&gt;:&lt;method1&gt;,&lt;method2&gt;
    /// wheras types are seperated by a whitespace.
    /// </summary>
    /// <param name="options">The analyzer options to query.</param>
    /// <param name="rule">The rule asking for a specific configuration.</param>
    /// <param name="key">The key of the specific configuration.</param>
    /// <param name="defaultValue">The value to return if no configuration was found.</param>
    /// <returns>The parsed member descriptors from the given configuration.</returns>
    public static IEnumerable<ClassMemberDescriptor> GetConfigAsMemberDescriptors(
      this AnalyzerConfigOptions options, DiagnosticDescriptor rule, string key, string defaultValue
    ) {
      return options.GetConfig(rule, key, defaultValue)
        .Split()
        .Select(ToMethodDescriptor)
        .IsNotNull();
    }

    private static ClassMemberDescriptor? ToMethodDescriptor(string config) {
      var splitByTypeAndMethods = config.Split(':');
      return splitByTypeAndMethods.Length != 2
        ? null
        : new ClassMemberDescriptor(splitByTypeAndMethods[0], splitByTypeAndMethods[1].Split(','));
    }

    private static string GetConfigKey(DiagnosticDescriptor rule, string key) {
      return string.Format(ConfigKeyFormat, rule.Id, key);
    }
  }
}
