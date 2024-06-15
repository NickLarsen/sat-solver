using sat_solver.io;

namespace sat_solver.solvers;

public enum SatSolverOutcome
{
    Unknown,
    Satisfied,
    Unsatisfied,
}

public class SatSolverResponse
{
    public SatSolverOutcome Outcome { get; set; }
    public bool[]? SatisfyingAssignment { get; set; }
    public string? DebugInfo { get; set; }
}

public interface ISatSolver
{
    public int LiteralCount { get; }
    public int ClauseCount { get; }
    public long DPLLCalls { get; }
    void Init(IDimacsReader problem);
    SatSolverResponse Solve();
}