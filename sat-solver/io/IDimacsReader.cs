namespace sat_solver.io;

public interface IDimacsReader
{
    int LiteralCount { get; }
    int ClauseCount { get; }
    void OpenReader();
    IReadOnlyList<int>? ReadNextClause();
}