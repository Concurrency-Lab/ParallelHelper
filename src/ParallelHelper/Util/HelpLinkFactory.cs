namespace ParallelHelper.Util {
  /// <summary>
  /// Factory class to generate help link URIs.
  /// </summary>
  public static class HelpLinkFactory {
    /// <summary>
    /// Creates a help link URI for the specified diagnostic id.
    /// </summary>
    /// <param name="diagnosticId">The ID of the diagnostic to get the help URI of.</param>
    /// <returns>The help URI to the specified diagnostic.</returns>
    public static string? CreateUri(string diagnosticId) {
      return $"https://ins-hsr.visualstudio.com/_git/ParallelHelper?path=%2Fdoc%2Fanalyzers%2F{diagnosticId}.md&version=GBmaster&fullScreen=true";
    }
  }
}
