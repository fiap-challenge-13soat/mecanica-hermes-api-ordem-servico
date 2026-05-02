using Xunit;

// Integration tests share a Testcontainers PostgreSQL instance and set process-wide
// environment variables. Parallel execution causes env var races and DB state conflicts.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
