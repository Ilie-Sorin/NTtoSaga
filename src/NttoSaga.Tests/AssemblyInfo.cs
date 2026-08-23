using Xunit;

// NTTOSAGA_ROOT e stare globală de proces (variabilă de mediu) — testele trebuie
// să ruleze secvențial pentru ca izolarea per test (TestEnv.FolderNou) să fie sigură.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
