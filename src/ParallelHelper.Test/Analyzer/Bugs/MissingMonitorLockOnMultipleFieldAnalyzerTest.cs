using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MissingMonitorLockOnMultipleFieldAnalyzerTest : AnalyzerTestBase<MissingMonitorLockOnMultipleFieldAnalyzer> {
    [TestMethod]
    public void ReportsDoubleReadAccessOnFieldsWithOneWrittenInsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void SetBalance(int balance) {
    if(closed) {
      throw new InvalidOperationException();
    }
    this.balance = balance;
  }

  public int GetBalance() {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      return this.balance;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 8), new DiagnosticResultLocation(12, 5));
    }

    [TestMethod]
    public void ReportsDoubleReadAccessOnFieldsInsideLockWithOneWrittenOutsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void SetBalance(int balance) {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      this.balance = balance;
    }
  }

  public int GetBalance() {
    if(closed) {
      throw new InvalidOperationException();
    }
    return this.balance;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(18, 8), new DiagnosticResultLocation(21, 12));
    }

    [TestMethod]
    public void ReportsReadWriteAccessOnSingleFieldAndReadOnOtherOutsideLockWithBothReadInsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void Withdraw(int amount) {
    if(amount > balance || closed) {
      throw new InvalidOperationException();
    }
    balance -= amount;
  }

  public int GetBalance() {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      return this.balance;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 17), new DiagnosticResultLocation(9, 28), new DiagnosticResultLocation(12, 5));
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnFieldsOutsideLockWithOneWrittenOutsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void SetBalance(int balance) {
    if(closed) {
      throw new InvalidOperationException();
    }
    this.balance = balance;
  }

  public int GetBalance() {
    if(closed) {
      throw new InvalidOperationException();
    }
    return this.balance;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnFieldsInsideLockWithOneWrittenInsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void SetBalance(int balance) {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      this.balance = balance;
    }
  }

  public int GetBalance() {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      return this.balance;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnSingleFieldOutsideLockWithOneWrittenInsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void Withdraw(int amount) {
    lock(syncObject) {
      if(amount > balance) {
        throw new InvalidOperationException();
      }
      balance -= amount;
    }
  }

  public int GetBalance() {
    if(closed) {
      throw new InvalidOperationException();
    }
    return this.balance;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadWriteAccessOnSingleFieldOutsideLockWithTwoReadInsideLock() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private bool closed = false;
  private int balance = 0;

  public void Withdraw(int amount) {
    if(amount > balance) {
      throw new InvalidOperationException();
    }
    balance -= amount;
  }

  public int GetBalance() {
    lock(syncObject) {
      if(closed) {
        throw new InvalidOperationException();
      }
      return this.balance;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnFieldsInsideLockIfOneIsConstIfReadAndWrittenOutside() {
      const string source = @"
using System;

class Sample {
  private readonly object syncObject = new object();
  private const int UPPER_LIMIT = 100;
  private int balance = 0;

  public bool DepositExceedsLimit(int amount) {
    lock(syncObject) {
      return amount + balance > UPPER_LIMIT;
    }
  }

  public void SetBalance(int balance) {
    if(balance > UPPER_LIMIT) {
        throw new InvalidOperationException();
    }
    this.balance = balance;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
