using sat_solver.io;

namespace sat_solver.solvers.dpll;

public class DPLLSolver : ISatSolver
{
    public int LiteralCount { get; private set; }
    public int ClauseCount { get; private set; }
    public long DPLLCalls { get; set; }

    private readonly List<Clause> _clauses = new List<Clause>();

    public void Init(IDimacsReader fileReader)
    {
        DPLLCalls = 0;
        (LiteralCount, ClauseCount) = fileReader.ReadHeader();
        _clauses.Capacity = ClauseCount;
        LoadClauses(fileReader);
    }
    private void LoadClauses(IDimacsReader fileReader)
    {
        while(true) {
            var clause = fileReader.ReadNextClause();
            if (clause == null)
                break;
            _clauses.Add(new Clause { Literals = clause.ToArray() });
        }
    }

    public SatSolverResponse Solve()
    {
        // we're not using the zero index
        var isAssigned = new bool[LiteralCount+1];
        var assignments = new bool[LiteralCount+1];
        var problem = new Problem(_clauses, isAssigned, assignments);
        return DPLL(problem);
    }

    private SatSolverResponse DPLL(Problem problem)
    {
        DPLLCalls += 1;
        problem = EliminateUnitClauses(problem);
        problem = AssignPureLiterals(problem);
        if (problem.IsEmptyClauseList) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Satisfied,
                SatisfyingAssignment = problem.Assignments,
            };
        }
        if (problem.HasEmptyClause) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Unsatisfied,
                // TODO: proof
            };
        }
        var firstUnassignedIndex = FirstUnassigned(problem);
        if (firstUnassignedIndex == -1)
        {
            return new SatSolverResponse {
                Outcome = SatSolverOutcome.Unknown,
                DebugInfo = "did not find first unassigned index, should never happen",
            };
        }
        var positive = problem.SetLiteral(firstUnassignedIndex, true);
        var result = DPLL(positive);
        if (result.Outcome == SatSolverOutcome.Unsatisfied) {
            var negative = problem.SetLiteral(firstUnassignedIndex, false);
            result = DPLL(negative);
        }
        return result;
    }

    private Problem EliminateUnitClauses(Problem problem)
    {
        while(true) {
            var unitClause = problem.Clauses.FirstOrDefault(m => m.IsUnitClause);
            if (unitClause == null) break;
            var literal = unitClause.Literals[0];
            var value = literal > 0;
            literal = Math.Abs(literal);
            problem = problem.SetLiteral(literal, value);            
        }
        return problem;
    }

    private Problem AssignPureLiterals(Problem problem)
    {
        while(true) {
            var pureLiterals = GetPureLiterals(problem);
            if (pureLiterals.Count == 0) break;
            foreach(var (literal, value) in pureLiterals)
            {
                problem = problem.SetLiteral(literal, value);
            }
        }
        return problem;
    }
    private List<(int, bool)> GetPureLiterals(Problem problem)
    {
        var literalStats = new LiteralStats[problem.Assignments.Length];
        foreach(var clause in problem.Clauses)
        {
            foreach(var literal in clause.Literals)
            {
                var value = literal > 0;
                var lit = Math.Abs(literal);
                literalStats[lit].Set(value);
            }
        }
        var result = new List<(int, bool)>();
        for(int i = 1; i < literalStats.Length; i++)
        {
            var stats = literalStats[i];
            if (!stats.IsPureLiteral) continue;
            // this works because if it has a positive, then we want to set it
            // to positive, otherwise negative
            var value = stats.HasPositive;
            result.Add((i, value));
        }
        return result;
    }
    private struct LiteralStats
    {
        public bool HasPositive { get; set; } = false;
        public bool HasNegative { get; set; } = false;
        public readonly bool IsPureLiteral => _hasAnyClauses && (HasPositive ^ HasNegative);
        
        private bool _hasAnyClauses = false;

        public LiteralStats() {}

        public void Set(bool value)
        {
            _hasAnyClauses = true;
            if (value) HasPositive = true;
            else HasNegative = true;
        }
    }

    private int FirstUnassigned(Problem problem)
    {
        for(int i = 1; 1 < problem.IsAssigned.Length; i++)
        {
            if (problem.IsAssigned[i] == false)
                return i;
        }
        return -1;
    }
}