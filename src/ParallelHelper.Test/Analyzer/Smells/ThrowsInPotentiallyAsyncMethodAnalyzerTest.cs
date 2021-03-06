﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Smells;

namespace ParallelHelper.Test.Analyzer.Smells {
  [TestClass]
  public class ThrowsInPotentiallyAsyncMethodAnalyzerTest : AnalyzerTestBase<ThrowsInPotentiallyAsyncMethodAnalyzer> {
    [TestMethod]
    public void ReportsNonAsyncMethodWithAsyncSuffixAndReturningTaskThatUsesThrowsStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWorkAsync(int input) {
    throw new ArgumentException(nameof(input));
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 5));
    }

    [TestMethod]
    public void ReportsNonAsyncMethodWithAsyncSuffixAndReturningTaskThatUsesThrowsExpression() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  private string value;

  public Task<int> DoWorkAsync(string value) {
    this.value = value ?? throw new ArgumentException(nameof(value));
    return Task.FromResult(value.Length);
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 27));
    }

    [TestMethod]
    public void DoesNotReportNonAsyncMethodWithoutAsyncSuffixAndReturningTaskThatUsesThrowsStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public Task DoWork(int input) {
    throw new ArgumentException(nameof(input));
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportNonAsyncMethodWithAsyncSuffixAndNotReturningTaskThatUsesThrowsStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public void DoWorkAsync(int input) {
    throw new ArgumentException(nameof(input));
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportAsyncMethodWithAsyncSuffixAndReturningTaskThatUsesThrowsStatement() {
      const string source = @"
using System;
using System.Threading.Tasks;

class Test {
  public async Task DoWorkAsync(int input) {
    throw new ArgumentException(nameof(input));
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
