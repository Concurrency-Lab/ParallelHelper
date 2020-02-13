using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Util;
using System.Linq;

namespace ParallelHelper.Test.Util {
  [TestClass]
  public class MonitorAnalysisTest {
    [TestMethod]
    public void IsMonitorWaitReturnsFalseWhenWaitOfNoneMonitorType() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}

public static class Monitor {
  public void Wait(object obj) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorWait(invocation));
    }

    [TestMethod]
    public void IsMonitorWaitReturnsFalseWhenTypeOfWaitCannotBeResolved() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorWait(invocation));
    }

    [TestMethod]
    public void IsMonitorWaitReturnsTrueForMonitorWaitInvocation() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Wait(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMonitorWait(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseReturnsFalseWhenPulseOfNoneMonitorType() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}

public static class Monitor {
  public void Pulse(object obj) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorPulse(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseReturnsFalseWhenTypeOfPulseCannotBeResolved() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorPulse(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseReturnsTrueForMonitorPulseInvocation() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.Pulse(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMonitorPulse(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseAllReturnsFalseWhenPulseAllOfNoneMonitorType() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }
}

public static class Monitor {
  public void PulseAll(object obj) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorPulseAll(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseAllReturnsFalseWhenTypeOfPulseAllCannotBeResolved() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorPulseAll(invocation));
    }

    [TestMethod]
    public void IsMonitorPulseAllReturnsTrueForMonitorPulseAllInvocation() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    lock(syncObject) {
      Monitor.PulseAll(syncObject);
    }
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMonitorPulseAll(invocation));
    }

    [TestMethod]
    public void IsMonitorTryEnterReturnsFalseWhenTryEnterOfNoneMonitorType() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.TryEnter(syncObject);
  }
}

public static class Monitor {
  public void TryEnter(object obj) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorTryEnter(invocation));
    }

    [TestMethod]
    public void IsMonitorTryEnterReturnsFalseWhenTypeOfTryEnterCannotBeResolved() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.TryEnter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorTryEnter(invocation));
    }

    [TestMethod]
    public void IsMonitorTryEnterReturnsTrueForMonitorTryEnterInvocation() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.TryEnter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMonitorTryEnter(invocation));
    }

    [TestMethod]
    public void IsMonitorEnterReturnsFalseWhenEnterOfNoneMonitorType() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter(syncObject);
  }
}

public static class Monitor {
  public void Enter(object obj) {}
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorEnter(invocation));
    }

    [TestMethod]
    public void IsMonitorEnterReturnsFalseWhenTypeOfEnterCannotBeResolved() {
      const string source = @"
public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.IsMonitorEnter(invocation));
    }

    [TestMethod]
    public void IsMonitorEnterReturnsTrueForMonitorEnterInvocation() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.IsMonitorEnter(invocation));
    }

    [TestMethod]
    public void TryGetSyncObjectFromMonitorMethodReturnsTrueWhenOneArgumentIsPassedToMonitorEnter() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetSyncObjectFromMonitorMethod(invocation, out var _));
    }

    [TestMethod]
    public void TryGetSyncObjectFromMonitorMethodReturnsSetsSyncObjectWhenOneArgumentIsPassedToMonitorEnter() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter(syncObject);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetSyncObjectFromMonitorMethod(invocation, out var syncObject));
      Assert.IsInstanceOfType(syncObject, typeof(IFieldSymbol));
    }

    [TestMethod]
    public void TryGetSyncObjectFromMonitorMethodReturnsTrueWhenTwoArgumentsArePassedToMonitorTryEnter() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.TryEnter(syncObject, 10);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetSyncObjectFromMonitorMethod(invocation, out var _));
    }

    [TestMethod]
    public void TryGetSyncObjectFromMonitorMethodReturnsSetsSyncObjectWhenTwoArgumentsArePassedToMonitorTryEnter() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.TryEnter(syncObject, 10);
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsTrue(analysis.TryGetSyncObjectFromMonitorMethod(invocation, out var syncObject));
      Assert.IsInstanceOfType(syncObject, typeof(IFieldSymbol));
    }

    [TestMethod]
    public void TryGetSyncObjectFromMonitorMethodReturnsFalseWhenNoArgumentIsPassedToMonitorEnter() {
      const string source = @"
using System.Threading;

public class Test {
  private readonly object syncObject = new object();

  public void Run() {
    Monitor.Enter();
  }
}";
      var semanticModel = CompilationFactory.GetSemanticModel(source);
      var invocation = semanticModel.SyntaxTree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();
      var analysis = new MonitorAnalysis(semanticModel, default);
      Assert.IsFalse(analysis.TryGetSyncObjectFromMonitorMethod(invocation, out var _));
    }
  }
}
