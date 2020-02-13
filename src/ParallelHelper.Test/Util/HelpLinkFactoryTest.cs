using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Util;

namespace ParallelHelper.Test.Util {
  [TestClass]
  public class HelpLinkFactoryTest {
    [TestMethod]
    public void CreatesGitHubUrlWithGivenDiagnosticId() {
      Assert.AreEqual(
        "https://github.com/Concurrency-Lab/ParallelHelper/tree/master/doc/analyzers/PH_B001.md",
        HelpLinkFactory.CreateUri("PH_B001")
      );
      Assert.AreEqual(
        "https://github.com/Concurrency-Lab/ParallelHelper/tree/master/doc/analyzers/PH_S013.md",
        HelpLinkFactory.CreateUri("PH_S013")
      );
    }
  }
}
