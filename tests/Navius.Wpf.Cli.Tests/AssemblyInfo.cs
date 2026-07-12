using Xunit;

// Integration tests invoke `dotnet run` for the same CLI project. Running them concurrently
// races its build outputs and can fail before the command under test starts.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
