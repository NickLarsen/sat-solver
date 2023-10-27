using sat_solver.io;

namespace sat_solver.solver.DPLL;

public class NoCopyDPLLSolver : ISatSolver
{
    public int LiteralCount => _problem.LiteralCount;
    public int ClauseCount => _problem.ClauseCount;
    public long DPLLCalls { get; private set; }

    private Problem _problem = new Problem(0, 0);

    public void Init(IDimacsReader fileReader)
    {
        DPLLCalls = 0;
        var (literalCount, clauseCount) = fileReader.ReadHeader();
        _problem = new Problem(literalCount, clauseCount);
        LoadClauses(fileReader);
    }
    private void LoadClauses(IDimacsReader fileReader)
    {
        var seen = new HashSet<int>(ClauseCount);
        var a = Array.Empty<int>();
        while(true) {
            var clause = fileReader.ReadNextClause();
            if (clause == null)
                break;
            seen.Clear();
            bool autoSatisfied = false;
            // pre-processing for one time trivial improvements
            // removes duplicate literals in the same clause
            // removes clauses that are trivially satisfied
            foreach(var literal in clause)
            {
                seen.Add(literal); // takes care of deduping same vars
                int opposite = literal * -1;
                autoSatisfied = seen.Contains(opposite);
                if (autoSatisfied) break;
            }
            if (autoSatisfied) continue;
            _problem.AddClause(new Clause(seen.ToArray()));
        }
    }

    public SatSolverResponse Solve()
    {
        return DPLL();
    }

    private SatSolverResponse DPLL()
    {
        DPLLCalls += 1;
        EliminateUnitClauses(_problem);
        AssignPureLiterals(_problem);
        if (_problem.IsFullySatisfied()) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Satisfied,
                SatisfyingAssignment = _problem.GetFinalAssignments(),
            };
        }
        if (_problem.HasConflict()) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Unsatisfied,
                // TODO: proof
            };
        }
        var firstUnassignedIndex = _problem.GetUnassignedVariable();
        _problem.SetLiteral(firstUnassignedIndex, true, isDecision: true);
        var result = DPLL();
        if (result.Outcome == SatSolverOutcome.Unsatisfied) {
            _problem.Rollback();
            _problem.SetLiteral(firstUnassignedIndex, false, isDecision: true);
            result = DPLL();
        }
        return result;
    }

    private void EliminateUnitClauses(Problem problem)
    {
        while (true)
        {
            var unitClausesLiteral = problem.GetUnitClauseLiteral();
            if (!unitClausesLiteral.HasValue) break;
            var literal = unitClausesLiteral.Value;
            bool isPositive = true;
            if (literal < 0)
            {
                literal = -literal;
                isPositive = false;
            }
            problem.SetLiteral(literal, isPositive, isDecision: false);
        }
    }

    private void AssignPureLiterals(Problem problem)
    {
        throw new NotImplementedException();
    }

    public class Problem
    {
        public int LiteralCount { get; }
        public int ClauseCount { get; }

        private List<Clause> _clauses;
        private bool?[] _assignments;
        private Stack<int> _assignmentsOrdered;
        private int _assignmentCount = 0;
        private Random _random = new Random();

        public Problem(int literalCount, int clauseCount)
        {
            LiteralCount = literalCount;
            ClauseCount = clauseCount;
            _clauses = new List<Clause>(clauseCount);
            // adding 1 so literals map to their actual index and zero is blank spot
            _assignments = new bool?[literalCount + 1];
            // the times 2 here is because we're adding zeros to indicate decisions
            _assignmentsOrdered = new Stack<int>(literalCount * 2);
        }

        public void AddClause(Clause clause)
        {
            _clauses.Add(clause);
        }

        public bool IsFullySatisfied()
        {
            return _assignments.Skip(1).All(m => m.HasValue);
        }

        public bool[] GetFinalAssignments()
        {
            // the variable at index 0 should be ignored by the caller
            return _assignments.Select((m, i) => i == 0 ? false : m.Value).ToArray();
        }

        public void SetLiteral(int literal, bool value, bool isDecision = false)
        {
            // if it's a decision, it's a rollback point
            if (isDecision)
                _assignmentsOrdered.Push(0);
            // set the value that was intented
            _assignments[literal] = value;
            _assignmentsOrdered.Push(literal);
            _assignmentCount += 1;
        }

        public void Rollback()
        {
            int i = 1;
            while(i != 0)
            {
                i = _assignmentsOrdered.Pop();
                _assignments[i] = null;
                if (i != 0)
                {
                    _assignmentCount -= 1;
                }
            }
        }

        public bool HasConflict()
        {
            return _clauses.Any(ClauseHasConflict);
        }
        private bool ClauseHasConflict(Clause clause)
        {
            return clause.Literals.All(ValueUnsatisfied);
        }
        private bool ValueUnsatisfied(int literal)
        {
            bool falseValue = false;
            int literalIndex = literal;
            if (literal < 0) 
            {
                falseValue = true;
                literalIndex = -literalIndex;
            }
            return _assignments[literalIndex] == falseValue;
        }

        public int GetUnassignedVariable()
        {
            int remaining = LiteralCount - _assignmentCount;
            if (remaining == 0) return -1;
            int selection = _random.Next(remaining);
            int passed = 0;
            int i = 1;
            for(; i < _assignments.Length; i++)
            {
                if (_assignments[i].HasValue)
                    continue;
                if (passed == selection)
                    break;
                passed += 1;
            }
            return i;
        }

        public int? GetUnitClauseLiteral()
        {
            foreach(var clause in _clauses)
            {
                var literal = GetUnitLiteral(clause);
                if (literal.HasValue)
                {
                    return literal.Value;
                }
            }
            return null;
        }
        private int? GetUnitLiteral(Clause clause)
        {
            // single unassigned with all others unsatisfied
            int? unassigned = null;
            foreach(var literal in clause.Literals)
            {
                int lit = literal;
                bool unsatisfiedValue = false;
                if (lit < 0)
                {
                    lit = -literal;
                    unsatisfiedValue = true;
                }
                bool? assignedValue = _assignments[lit];
                if (assignedValue.HasValue)
                {
                    if (assignedValue.Value != unsatisfiedValue)
                    {
                        return null;
                    }
                }
                else if (unassigned.HasValue)
                {
                    return null;
                }
                else
                {
                    unassigned = lit;
                }
            }
            if (unassigned.HasValue && unassigned != 0)
            {
                return unassigned.Value;
            }
            return null;
        }
    }

    public class Clause
    {
        public int[] Literals { get; }

        public Clause(int[] literals)
        {
            Literals = literals;
        }
    }
}