using sat_solver.io;

namespace sat_solver.test.helpers;

public class TestDimacsReader : IDimacsReader
{
    private readonly int _literalCount;
    private readonly int _clauseCount;
    private readonly List<List<int>> _clauses;
    private int _currentReadIndex = 0;

    public TestDimacsReader(int literalCount, int clauseCount, List<List<int>> clauses)
    {
        _literalCount = literalCount;
        _clauseCount = clauseCount;
        _clauses = clauses;
    }

    public (int literalCount, int clauseCount) ReadHeader()
    {
        return (_literalCount, _clauseCount);
    }

    public IReadOnlyList<int>? ReadNextClause()
    {
        if (_currentReadIndex == _clauses.Count) 
            return null;
        return _clauses[_currentReadIndex++];
    }
}