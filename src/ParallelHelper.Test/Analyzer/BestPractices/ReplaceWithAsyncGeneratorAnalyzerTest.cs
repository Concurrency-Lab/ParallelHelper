using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class ReplaceWithAsyncGeneratorAnalyzerTest : AnalyzerTestBase<ReplaceWithAsyncGeneratorAnalyzer> {
    [TestMethod]
    public void ReportsMethodWithAsyncListPopulationInWhileLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void ReportsMethodWithAsyncListPopulationInForLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    var result = new List<int>();
    for(int i = 0; i < 10; i++) {
      await Task.Delay(100);
      result.Add(i);
    }
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void ReportsMethodWithAsyncListPopulationInDoLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    var result = new List<int>();
    int i = 10;
    do {
      await Task.Delay(100);
      result.Add(i);
      i--;
    } while(i > 0);
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void ReportsMethodWithAsyncListPopulationInForeachLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    var result = new List<int>();
    foreach(var i in Enumerable.Range(0, 10)) {
      await Task.Delay(100);
      result.Add(i);
    }
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 3));
    }

    [TestMethod]
    public void ReportsMethodWithAsyncCollectionPopulationInWhileLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    ICollection<int> result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(5, 3));
    }

    [TestMethod]
    public void DoesNotReportMethodWithListPopulationWhenNotReturningEnumerable() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IList<int>> GetAllAsync() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWithListPopulationWhenNotReturningAnything() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task DoItAsync() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      result.Add(i);
      i--;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWithListPopulationWhereAwaitIsOutsideOfTheLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IEnumerable<int>> GetAllAsync() {
    await Task.Delay(100);
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportWithListPopulationWhenMethodIsNotAsync() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

class Test {
  public IEnumerable<int> GetAll() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWithListPopulationWhenMethodIsNotAsync() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

class Test {
  public IEnumerable<int> GetAll() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      result.Add(i);
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWhenTheListIsNotPopulatedInsideTheAsyncLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IList<int>> GetAllAsync() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      i--;
    }
    result.Add(i);
    return result;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMethodWhenTheListIsOnlyReadInsideTheAsyncLoop() {
      const string source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class Test {
  public async Task<IList<int>> GetAllAsync() {
    var result = new List<int>();
    int i = 10;
    while(i > 0) {
      await Task.Delay(100);
      if(result.Contains(i)) {
        break;
      }
      i--;
    }
    return result;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
