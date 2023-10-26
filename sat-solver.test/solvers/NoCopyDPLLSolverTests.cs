using sat_solver.io;
using sat_solver.solver.DPLL;

namespace sat_solver.test.solvers;

public class NoCopyDPLLSolverTests
{
    [Fact]
    public void IsFullySatisfied()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(2, true);
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(0, true);
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(1, false);
        Assert.True(problem.IsFullySatisfied());
    }

    [Fact]
    public void Rollback()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        Assert.False(problem.IsFullySatisfied());
        // can only rollback after a decision!
        problem.SetLiteral(2, true, isDecision: true);
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(1, false);
        Assert.True(problem.IsFullySatisfied());
        problem.Rollback();
        Assert.False(problem.IsFullySatisfied());
    }
}