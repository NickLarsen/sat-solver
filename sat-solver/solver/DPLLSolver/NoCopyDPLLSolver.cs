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
            var literal = unitClausesLiteral.Value;
            bool satisfiedValue = true;
            if (literal < 0)
            {
                literal = -literal;
                satisfiedValue = false;
            }
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
                int lit = literal;
                bool satisfiedValue = true;
                if (literal < 0)
                {
                    lit = -lit;
                    satisfiedValue = false;
                }
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
        private int _assignmentCount = 0;
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
            return _clauses.All(ClauseIsSatisfied);
        }
        private bool ClauseIsSatisfied(Clause clause)
        {
            return clause.Literals.Any(LiteralIsSatisfied);
        }
        private bool LiteralIsSatisfied(int literal)
        {
            int lit = literal;
            bool satisfiedValue = true;
            if (lit < 0)
            {
                lit = -lit;
                satisfiedValue = false;
            }
            return _assignments[lit] == satisfiedValue;
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
            _assignmentCount += 1;
            //LogAssignments("set lite", $"{_assignmentCount}, {literal}, {value}, {isDecision}");
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
            //LogAssignments("rollback", $"{_assignmentCount}");
        }

        private void LogAssignments(string reason, string? extra = null)
        {
            var values = _assignments.Skip(1).Select(m => m switch { true => "T", false => "F", null => "U" }).ToArray();
            Console.Write($"{reason}: ");
            for(int i = 0; i < values.Length; i++)
            {
                Console.Write(values[i]);
                Console.Write(',');
                if (i % 10 == 9)
                    Console.Write(' ');
            }
            Console.WriteLine($"    {extra}");
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
                {
                    //LogAssignments("get unas", $"-- {selection}, {passed}, {i}");
                    return i;
                }
                passed += 1;
            }
            throw new InvalidOperationException($"failed GetUnassignedVariable {LiteralCount}, {_assignmentCount}, {selection}");
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
                    unassigned = literal;
                }
            }
            if (unassigned.HasValue && unassigned != 0)
            {
                return unassigned.Value;
            }
            return null;
        }

        public IReadOnlyList<int> GetPureLiterals()
        {
            Array.Clear(_pureLiteralBuffer);
            foreach(var clause in _clauses)
            {
                // if the clause is satisfied, then don't count anything
                if (ClauseIsSatisfied(clause))
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
            // usually when you call this it will be very few pure literals
            // so this just avoid excess resizing
            var result = new List<int>(32);
            for(int literal = 1; literal < _pureLiteralBuffer.Length; literal++)
            {
                var seen = _pureLiteralBuffer[literal];
                if (seen == 1)
                    result.Add(-literal);
                else if (seen == 2)
                    result.Add(literal);
            }
            return result;
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