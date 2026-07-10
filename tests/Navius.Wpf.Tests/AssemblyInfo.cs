// WPF tests share one process-wide Application and STA state; parallel test
// classes race on creating it. Run collections serially.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
