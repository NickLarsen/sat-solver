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
        if (_problem.HasConflict()) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Unsatisfied,
                // TODO: proof
            };
        }
        if (_problem.IsFullySatisfied()) {
            return new SatSolverResponse { 
                Outcome = SatSolverOutcome.Satisfied,
                SatisfyingAssignment = _problem.GetFinalAssignments(),
            };
        }
        var literal = _problem.GetUnassignedVariable();
        _problem.SetLiteral(literal, true, isDecision: true);
        var result = DPLL();
        if (result.Outcome == SatSolverOutcome.Unsatisfied) {
            _problem.Rollback();
            _problem.SetLiteral(literal, false, isDecision: true);
            result = DPLL();
            if (result.Outcome == SatSolverOutcome.Unsatisfied)
            {
                _problem.Rollback();
            }
        }
        return result;
    }

    public static void EliminateUnitClauses(Problem problem)
    {
        while (true)
        {
            var unitClausesLiteral = problem.GetUnitClauseLiteral();
            if (!unitClausesLiteral.HasValue) break;
            var literal = Math.Abs(unitClausesLiteral.Value);
            bool satisfiedValue = unitClausesLiteral.Value > 0;
            problem.SetLiteral(literal, satisfiedValue, isDecision: false);
        }
    }

    public static void AssignPureLiterals(Problem problem)
    {
        while (true)
        {
            var pureLiterals = problem.GetPureLiterals();
            if (pureLiterals.Count == 0)
                break;
            foreach(var literal in pureLiterals)
            {
                int lit = Math.Abs(literal);
                bool satisfiedValue = literal > 0;
                problem.SetLiteral(lit, satisfiedValue, isDecision: false);
            }
        }
    }

    public class Problem
    {
        public int LiteralCount { get; }
        public int ClauseCount { get; }
        public bool?[] DebugAsssignments => _assignments;

        private List<Clause> _clauses;
        private bool?[] _assignments;
        private Stack<int> _assignmentsOrdered;
        private Random _random = new Random();

        private readonly int[] _pureLiteralBuffer;

        public Problem(int literalCount, int clauseCount)
        {
            LiteralCount = literalCount;
            ClauseCount = clauseCount;
            _clauses = new List<Clause>(clauseCount);
            // adding 1 so literals map to their actual index and zero is blank spot
            _assignments = new bool?[literalCount + 1];
            // the times 2 here is because we're adding zeros to indicate decisions
            _assignmentsOrdered = new Stack<int>(literalCount * 2);
            // the times 2 here is because we're storing positive and negative side by side
            _pureLiteralBuffer = new int[literalCount + 1];
        }

        public void AddClause(Clause clause)
        {
            _clauses.Add(clause);
        }

        public bool IsFullySatisfied()
        {
            foreach(var clause in _clauses)
            {
                bool isSatisfied = false;
                foreach(var literal in clause.Literals)
                {
                    int lit = Math.Abs(literal);
                    bool satisfiedValue = literal > 0;
                    if (_assignments[lit] == satisfiedValue)
                    {
                        isSatisfied = true;
                        break;
                    }
                }
                if (!isSatisfied)
                    return false;
            }
            return true;
        }

        public bool[] GetFinalAssignments()
        {
            // the variable at index 0 should be ignored by the caller
            // we can be fully satisfied without assigning all literals
            // if there are cases where the value of some variables simply
            // does not matter, so in those cases, we just want to return
            // a value for all variables and false it is
            return _assignments.Select(m => m ?? false).ToArray();
        }

        public void SetLiteral(int literal, bool value, bool isDecision)
        {
            if (_assignments[literal].HasValue)
                throw new InvalidOperationException($"attempting to set literal that is already set {literal}");
            // if it's a decision, it's a rollback point
            if (isDecision)
                _assignmentsOrdered.Push(0);
            // set the value that was intented
            _assignments[literal] = value;
            _assignmentsOrdered.Push(literal);
        }

        public void Rollback()
        {
            int i = 1;
            while(i != 0)
            {
                i = _assignmentsOrdered.Pop();
                _assignments[i] = null;
            }
        }

        public bool HasConflict()
        {
            foreach(var clause in _clauses)
            {
                bool allUnsatisfied = true;
                foreach(var literal in clause.Literals)
                {
                    bool unsatisfiedValue = literal < 0;
                    int literalIndex = Math.Abs(literal);
                    bool isUnsatisfied = _assignments[literalIndex] == unsatisfiedValue;
                    allUnsatisfied = allUnsatisfied && isUnsatisfied;
                }
                if (allUnsatisfied)
                    return true;
            }
            return false;
        }

        public int GetUnassignedVariable()
        {
            for(int i = 1; i < _assignments.Length; i++)
            {
                if (!_assignments[i].HasValue)
                {
                    return i;
                }
            }
            throw new InvalidOperationException("cannot get unassigned variable when all variables are assigned");
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
                int lit = Math.Abs(literal);
                bool unsatisfiedValue = literal < 0;
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
                    unassigned = literal;
                }
            }
            if (unassigned.HasValue && unassigned != 0)
            {
                return unassigned.Value;
            }
            return null;
        }

        // usually when you call this it will be very few pure literals
        // so this just avoid excess resizing
        private readonly List<int> _pureLiteralsResultsBuffer = new List<int>(32);
        public IReadOnlyList<int> GetPureLiterals()
        {
            Array.Clear(_pureLiteralBuffer);
            foreach(var clause in _clauses)
            {
                // if the clause is satisfied, then don't count anything
                bool isSatisfied = false;
                foreach(var literal in clause.Literals)
                {
                    int lit = Math.Abs(literal);
                    bool satisfiedValue = literal > 0;
                    if (_assignments[lit] == satisfiedValue)
                    {
                        isSatisfied = true;
                        break;
                    }
                }
                if (isSatisfied)
                    continue;
                // if not satisfied, count all unassigned variables
                foreach(var literal in clause.Literals)
                {
                    int lit = Math.Abs(literal);
                    if (_assignments[lit].HasValue)
                        continue;
                    // this works by having 0 be "assigned value"
                    // 1 is negation, 2 is natural
                    // 3 is both negation and natural
                    // bitwise OR is how we combine these values
                    int value = literal < 0 ? 1 : 2;
                    _pureLiteralBuffer[lit] |= value;
                }
            }
            _pureLiteralsResultsBuffer.Clear();
            for(int literal = 1; literal < _pureLiteralBuffer.Length; literal++)
            {
                var seen = _pureLiteralBuffer[literal];
                if (seen == 1)
                    _pureLiteralsResultsBuffer.Add(-literal);
                else if (seen == 2)
                    _pureLiteralsResultsBuffer.Add(literal);
            }
            return _pureLiteralsResultsBuffer;
        }
    }

    public struct Clause
    {
        public int[] Literals { get; }

        public Clause(int[] literals)
        {
            Literals = literals;
        }
    }
}