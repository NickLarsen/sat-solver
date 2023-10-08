namespace sat_solver.io;

public interface IDimacsReader
{
    (int literalCount, int clauseCount) ReadHeader();
    IReadOnlyList<int>? ReadNextClause();
}