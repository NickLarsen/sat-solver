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
        Assert.Throws<InvalidOperationException>(() => problem.Rollback());
    }

    [Fact]
    public void HasConflict2Literals()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 2 }));
        // this checks all 9 combinations of 2 variables for T, F, U(nassigned)
        Assert.False(problem.HasConflict()); // (U, U)
        problem.SetLiteral(1, false, isDecision: true);
        Assert.False(problem.HasConflict()); // (F, U)
        problem.Rollback();
        problem.SetLiteral(1, true, isDecision: true);
        Assert.False(problem.HasConflict()); // (T, U)
        problem.Rollback(); // (U, U)
        problem.SetLiteral(2, true, isDecision: true);
        Assert.False(problem.HasConflict()); // (U, T)
        problem.SetLiteral(1, false, isDecision: true);
        Assert.False(problem.HasConflict()); // (F, T)
        problem.Rollback(); // (U, T)
        problem.SetLiteral(1, true, isDecision: true);
        Assert.False(problem.HasConflict()); // (T, T)
        problem.Rollback(); // (U, T)
        problem.Rollback(); // (U, U)
        problem.SetLiteral(2, false, isDecision: true);
        Assert.False(problem.HasConflict()); // (U, F)
        problem.SetLiteral(1, true, isDecision: true);
        Assert.False(problem.HasConflict()); // (T, F)
        problem.Rollback(); // (U, F)
        problem.SetLiteral(1, false, isDecision: true);
        Assert.True(problem.HasConflict()); // (F, F)
    }

    [Fact]
    public void GetUnassignedVariableNoAssignments()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        int seen1 = 0;
        int seen2 = 0;
        for(int i = 0; i < 1000; i++)
        {
            int value = problem.GetUnassignedVariable();
            switch (value)
            {
                case 1:
                    seen1++;
                    break;
                case 2:
                    seen2++;
                    break;
                case -1:
                    Assert.Fail("received no unassigned flag when should not have");
                    break;
                default:
                    Assert.Fail($"received unexpected value from GetUnassignedVariable {value}");
                    break;
            }
        }
        // the chance for the values to fall outside this range assuming fair flips
        // is approximately 1 in 809,028,370,860,459 so if this fails there's a good
        // chance the flip isn't fair
        Assert.InRange(seen1, 375, 625);
        Assert.InRange(seen2, 375, 625);
    }

    [Fact]
    public void GetUnassignedVariableOneAssignmentOutOfTwo()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.SetLiteral(1, true, isDecision: true);
        int seen1 = 0;
        int seen2 = 0;
        int trials = 1000;
        for(int i = 0; i < trials; i++)
        {
            int value = problem.GetUnassignedVariable();
            switch (value)
            {
                case 1:
                    seen1++;
                    break;
                case 2:
                    seen2++;
                    break;
                case -1:
                    Assert.Fail("received no unassigned flag when should not have");
                    break;
                default:
                    Assert.Fail($"received unexpected value from GetUnassignedVariable {value}");
                    break;
            }
        }
        Assert.Equal(0, seen1);
        Assert.Equal(trials, seen2);
    }

    [Fact]
    public void GetUnassignedVariableWhenNoUnassignedReturnsMinusOne()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.SetLiteral(1, true, isDecision: true);
        problem.SetLiteral(2, true, isDecision: true);
        int selection = problem.GetUnassignedVariable();
        Assert.Equal(-1, selection);
    }

    [Fact]
    public void GetUnitClauseLiteral2Clause()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 2 }));
        // this checks all 9 combinations of 2 variables for T, F, U(nassigned)
        Assert.Null(problem.GetUnitClauseLiteral()); // (U, U) 2 unassigned = not unit
        problem.SetLiteral(1, false, isDecision: true); // (F, U)
        Assert.Equal(2, problem.GetUnitClauseLiteral()); // (F, U)
        problem.Rollback(); // (U, U)
        problem.SetLiteral(1, true, isDecision: true); // (T, U)
        Assert.Null(problem.GetUnitClauseLiteral()); // (T, U) satisfied = not unit
        problem.Rollback(); // (U, U)
        problem.SetLiteral(2, false, isDecision: true); // (U, F)
        Assert.Equal(1, problem.GetUnitClauseLiteral()); // (U, F)
        problem.Rollback(); // (U, U)
        problem.SetLiteral(2, true, isDecision: true); // (U, T)
        Assert.Null(problem.GetUnitClauseLiteral()); // (U, T) satisfied = not unit
        problem.Rollback(); // (U, U)
        problem.SetLiteral(1, false, isDecision: true); // (F, U)
        problem.SetLiteral(2, false, isDecision: true); // (F, F)
        Assert.Null(problem.GetUnitClauseLiteral()); // (F, F) conflict = not unit
    }

    [Fact]
    public void GetUnitClauseLiteral3Clause()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 1);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 2, 3 }));
        Assert.Null(problem.GetUnitClauseLiteral()); // (U, U, U) 3 unassigned = not unit
        problem.SetLiteral(1, false, isDecision: true); // (F, U, U)
        Assert.Null(problem.GetUnitClauseLiteral()); // (F, U, U) 2 unassigned = not unit
        problem.SetLiteral(3, false, isDecision: true); // (F, U, F)
        Assert.Equal(2, problem.GetUnitClauseLiteral()); // (F, U, F) unit!
        problem.Rollback();
        problem.SetLiteral(3, true, isDecision: false); // (F, U, T)
        Assert.Null(problem.GetUnitClauseLiteral()); // (F, U, T) satisfied = not unit
    }
}