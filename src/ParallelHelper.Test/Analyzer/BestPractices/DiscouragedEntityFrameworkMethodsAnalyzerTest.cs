using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelHelper.Analyzer.BestPractices;

namespace ParallelHelper.Test.Analyzer.BestPractices {
  [TestClass]
  public class DiscouragedEntityFrameworkMethodsAnalyzerTest : AnalyzerTestBase<DiscouragedEntityFrameworkMethodsAnalyzer> {
    // We define the desired types and methods here to avoid having to reference the EntityFramework package
    // and all its dependencies.
    private const string EntityFrameworkCorePrototypes = @"
using System.Collections.Generic ;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore {
  public interface EntityEntry<TEntity> {}

  public interface DbSet<TEntity> {
    ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
  }

  public interface DbContext {
    Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken = default);
  }
}";
    
    private MetadataReference CreateEntityFrameworkCoreMetadataReference() {
      return CreateAnalyzerCompilationBuilder()
        .AddSourceTexts(EntityFrameworkCorePrototypes)
        .Build()
        .ToMetadataReference();
    }

    public override void VerifyDiagnostic(string source, params DiagnosticResultLocation[] expectedDiagnostics) {
      CreateAnalyzerCompilationBuilder()
        .AddReferences(CreateEntityFrameworkCoreMetadataReference())
        .AddSourceTexts(source)
        .VerifyDiagnostic(expectedDiagnostics);
    }

    [TestMethod]
    public void ReportsDbSetAddAsync() {
      const string source = @"
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

class Test {
  private readonly DbSet<int> _store;

  public async Task<int> DoWork() {
    int result = 1;
    await _store.AddAsync(result);
    return result;
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(9, 11));
    }

    [TestMethod]
    public void ReportsDbContextAddRangeAsync() {
      const string source = @"
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

class Test {
  private readonly DbContext _store;

  public async Task DoWork() {
    await _store.AddRangeAsync(new object[] { 1, 2, 3, 4 });
  }
}";
      VerifyDiagnostic(source, new DiagnosticResultLocation(8, 11));
    }

    [TestMethod]
    public void DoesNotReportAddAsyncOfCustomType() {
      const string source = @"
using System.Threading.Tasks;

class Test {
  private readonly DbSet<int> _store;

  public async Task<int> DoWork() {
    int result = 1;
    await _store.AddAsync(result);
    return result;
  }
}

class DbSet<TEntity> {
  public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) {
    return Task.CompletedTask;
  }
}";
      VerifyDiagnostic(source);
    }
  }
}
