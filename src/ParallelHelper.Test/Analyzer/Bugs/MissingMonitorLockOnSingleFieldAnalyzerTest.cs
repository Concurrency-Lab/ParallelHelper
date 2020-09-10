using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.Bugs;

namespace ParallelHelper.Test.Analyzer.Bugs {
  [TestClass]
  public class MissingMonitorLockOnSingleFieldAnalyzerTest : AnalyzerTestBase<MissingMonitorLockOnSingleFieldAnalyzer> {
    [TestMethod]
    public void ReportsDoubleReadAccessOnFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public bool CanWithDrawAny(int amount1, int amount2) {
    return amount1 <= balance || amount2 <= balance;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 23), new DiagnosticResultLocation(11, 45));
    }

    [TestMethod]
    public void ReportsWriteAccessOutsideLockToFieldWithDoubleReadInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { balance = value; }
    get { return balance; }
  }

  public bool CanWithDrawAny(int amount1, int amount2) {
    lock(syncObject) {
      return amount1 <= balance || amount2 <= balance;
    }
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(6, 11));
    }

    [TestMethod]
    public void ReportsUnaryReadWriteAccessOutsideLockToFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public void Increment() {
    balance++;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 5));
    }

    [TestMethod]
    public void ReportsReadWriteAccessOutsideLockToFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public void Deposit(int amount) {
    balance += amount;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 5));
    }

    [TestMethod]
    public void ReportsMultipleReadAccessesOutsideLockToFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public int GetMaxWithrawal(int amount) {
    if(balance >= amount) {
      return amount;
    }
    return balance;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 8), new DiagnosticResultLocation(14, 12));
    }

    [TestMethod]
    public void ReportsMultipleReadAccessesInConditionalExpressionOutsideLockToFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public int GetMaxWithrawal(int amount) {
    return balance >= amount ? amount : balance;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(11, 12), new DiagnosticResultLocation(11, 41));
    }

    [TestMethod]
    public void DoesReportDoubleReadAccessInsideLockOnFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public bool CanWithDrawAny(int amount1, int amount2) {
    lock(syncObject) {
      return amount1 <= balance || amount2 <= balance;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportReadWriteAccessInsideLockToFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }

  public void Deposit(int amount) {
    lock(syncObject) {
      balance += amount;
    }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnFieldOnlyWrittenAndReadOutsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { balance = value; }
    get { return balance; }
  }

  public bool CanWithDrawAny(int amount1, int amount2) {
    return amount1 <= balance || amount2 <= balance;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSingleReadAccessOnFieldWrittenInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { return balance; }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSingleWriteAccessOnFieldReadInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int Balance {
    set { balance = value; }
    get { lock(syncObject) { return balance; } }
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportMultipleReadAccessesOnFieldWithMultipleReadAccessesInsideLock() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;
  
  public int MaxWithdrawal(int amount) {
    lock(syncObject) {
      if(balance < amount) {
        return balance;
      }
      return amount;
    }
  }
  

  public bool CanWithDrawAny(int amount1, int amount2) {
    return amount1 <= balance || amount2 <= balance;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportSingleWriteAccessOutsideLockWhithSingleWriteAndReadAccessSeperateLocks() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private int balance;

  public int Balance {
    set { lock(syncObject) { balance = value; } }
    get { lock(syncObject) { return balance; } }
  }
  
  public void SetBalance(int balance) {
    this.balance = balance;
  }
}";
      VerifyDiagnostic(source);
    }

    [TestMethod]
    public void DoesNotReportDoubleReadAccessOnFieldWrittenInsideLockForFieldsOfForeignClass() {
      const string source = @"
class BankAccount {
  private readonly object syncObject = new object();
  private volatile BalanceHolder balance = new BalanceHolder();
  
  public int Balance {
    set {
      lock(syncObject) {
        balance = new BalanceHolder();
        balance.Value = value;
      }
    }
    get { return balance.Value; }
  }

  public bool CanWithDrawAny(int amount1, int amount2) {
    var current = balance;
    return amount1 <= current.Value || amount2 <= current.Value;
  }

  private class BalanceHolder {
    public int Value;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
