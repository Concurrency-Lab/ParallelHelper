using System;
using System.Threading;

namespace IntegrationTests.Cli {
  public class BankAccount {
    private readonly object _syncObject = new object();

    private bool _closed;
    private decimal _balance;

    public BankAccount(decimal balance) {
      _balance = balance;
    }

    public void Withdraw(int amount) {
      lock(_syncObject) {
        while (!_closed && amount < _balance) {
          Monitor.Wait(_syncObject);
        }
        if (_closed) {
          throw new InvalidOperationException("account is closed");
        }
        _balance -= amount;
      }
    }

    public void Deposit(int amount) {
      lock(_syncObject) {
        if (_closed) {
          throw new InvalidOperationException("account is closed");
        }
        _balance += amount;
        Monitor.Pulse(_syncObject);
      }
    }

    public void Close() {
      _closed = true;
    }

    public void Open() {
      lock (_syncObject) {
        _closed = false;
        Monitor.PulseAll(_syncObject);
      }
    }
  }
}
