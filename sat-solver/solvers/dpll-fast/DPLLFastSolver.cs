using sat_solver.io;

namespace sat_solver.solvers.dpll_fast;

public class DPLLFastSolver : ISatSolver
{
    public int LiteralCount { get; private set; }
    public int ClauseCount { get; private set; }
    public long DPLLCalls { get; private set; }

    private bool?[] _assignmentValues = Array.Empty<bool?>();
    private Stack<Assignment> _assignments = new Stack<Assignment>();
    private Clause[] _clauses = Array.Empty<Clause>();


    public void Init(IDimacsReader problemReader)
    {
        DPLLCalls = 0;
        (LiteralCount, ClauseCount) = problemReader.ReadHeader();
        _assignmentValues = new bool?[LiteralCount+1];
        _assignments = new Stack<Assignment>();
        _clauses = new Clause[ClauseCount];
        ReadClauses(problemReader);
    }
    private void ReadClauses(IDimacsReader problemReader)
    {
        int i = 0;
        while(true) 
        {
            var literals = problemReader.ReadNextClause();
            if (literals == null) break;
            _clauses[i] = new Clause(literals);
            i++;
        }
        if (i != ClauseCount)
        {
            throw new Exception("did not receive the correct amount of clauses");
        }
    }

    public SatSolverResponse Solve()
    {
        // this is an implementation of DPLL
        SatSolverResponse response;
        while(true)
        {
            DPLLCalls++;
            response = UnitPropagate();
            if (response.Outcome == SatSolverOutcome.Satisfied)
            {
                break;
            }
            else if (response.Outcome == SatSolverOutcome.Unsatisfied)
            {
                var assignment = Backtrack();
                if (!assignment.HasValue)
                    break;
                var opposite = assignment.Value.Opposite();
                PushAssignment(opposite);
            }
            else if (response.Outcome == SatSolverOutcome.Unknown)
            {
                int nextLiteral = PickNextLiteral();
                var assignment = Assignment.MakeDecision(nextLiteral, value: true);
                PushAssignment(assignment);
            }
        }
        return response;
    }

    private Assignment? Backtrack()
    {
        Assignment? result = null;
        while(_assignments.Count > 0)
        {
            var assignment = PopAssignment();
            if (assignment.IsDecisionVariable && assignment.Value == true)
            {
                result = assignment;
                break;
            }
        }
        int currentLevel = _assignments.Count;
        for(int i = 0; i < _clauses.Length; i++)
        {
            var clause = _clauses[i];
            if (clause.SatisfiedByLevel > currentLevel)
            {
                clause.SatisfiedByLevel = null;
            }
        }
        return result;
    }

    private void PushAssignment(Assignment assignment)
    {
        _assignmentValues[assignment.Literal] = assignment.Value;
        _assignments.Push(assignment);
    }
    private Assignment PopAssignment()
    {
        var assignment = _assignments.Pop();
        _assignmentValues[assignment.Literal] = null;
        return assignment;
    }

    private int PickNextLiteral()
    {
        foreach(var clause in _clauses)
        {
            if (clause.SatisfiedByLevel == null)
            {
                foreach(var literal in clause.Literals)
                {
                    int lit = Math.Abs(literal);
                    if (!_assignmentValues[lit].HasValue)
                        return lit;
                }
                throw new Exception("invalid state, clause has no unassigned literals");
            }
        }
        throw new Exception("unable to pick next literal because all clauses are satisfied");
    }

    private SatSolverResponse UnitPropagate()
    {
        bool hasUnsatisfiedClause = false;
        bool assignmentWasAdded = true;
        while(assignmentWasAdded)
        {
            assignmentWasAdded = false;
            hasUnsatisfiedClause = false;
            for(int i = 0; i < _clauses.Length; i++)
            {
                var clause = _clauses[i];
                if (clause.SatisfiedByLevel.HasValue)
                    continue;
                int unassignedLits = 0;
                int lastUnassignedLiteral = 0;
                foreach(var literal in clause.Literals)
                {
                    int lit = Math.Abs(literal);
                    bool? litValue = _assignmentValues[lit];
                    if (!litValue.HasValue)
                    {
                        unassignedLits++;
                        lastUnassignedLiteral = literal;
                    }
                    else
                    {
                        bool litExpected = literal > 0;
                        if (litValue.Value == litExpected)
                        {
                            clause.SatisfiedByLevel = _assignments.Count;
                            break;
                        }
                    }
                }
                if (clause.SatisfiedByLevel.HasValue)
                    continue;
                if (unassignedLits == 1)
                {
                    // this was a pure literal so make the assignment
                    int lit = Math.Abs(lastUnassignedLiteral);
                    bool litValue = lastUnassignedLiteral > 0;
                    var assignment = Assignment.MakeImplication(lit, litValue);
                    PushAssignment(assignment);
                    assignmentWasAdded = true;
                }
                else if (unassignedLits == 0)
                {
                    return new SatSolverResponse() 
                    {
                        Outcome = SatSolverOutcome.Unsatisfied,
                        DebugInfo = "empty clause",
                    };
                }
                hasUnsatisfiedClause = true;
            }
        }
        if (hasUnsatisfiedClause)
        {
            return new SatSolverResponse()
            {
                Outcome = SatSolverOutcome.Unknown,
            };
        }
        return new SatSolverResponse()
        {
            Outcome = SatSolverOutcome.Satisfied,
        };
    }
}