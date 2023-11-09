using sat_solver.solvers;

namespace sat_solver.test.solvers;

public class NoCopyDPLLSolverTests
{
    [Fact]
    public void IsFullySatisfied()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 2);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { -2, 3 }));
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(2, true, isDecision: true);
        Assert.False(problem.IsFullySatisfied());
        problem.SetLiteral(3, true, isDecision: true);
        Assert.True(problem.IsFullySatisfied());
    }

    [Fact]
    public void Rollback()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 2 }));
        Assert.False(problem.IsFullySatisfied());
        // can only rollback after a decision!
        problem.SetLiteral(2, false, isDecision: true);
        Assert.False(problem.IsFullySatisfied());
        // no decision, so it rolls back past this
        problem.SetLiteral(1, true, isDecision: false);
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
        int value = problem.GetUnassignedVariable();
        Assert.Equal(-1, value);
    }

    [Fact]
    public void GetUnassignedVariableOneAssignmentOutOfTwo()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.SetLiteral(1, true, isDecision: true);
        int value = problem.GetUnassignedVariable();
        Assert.Equal(-2, value);
    }

    [Fact]
    public void GetUnassignedVariableOneAssignmentMax3()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 3);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, 3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { -1, -2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 2, 3 }));
        int value = problem.GetUnassignedVariable();
        Assert.Equal(3, value);
    }

    [Fact]
    public void GetUnassignedVariableWhenNoUnassignedThrows()
    {
        var problem = new NoCopyDPLLSolver.Problem(2, 1);
        problem.SetLiteral(1, true, isDecision: true);
        problem.SetLiteral(2, true, isDecision: true);
        Assert.Throws<InvalidOperationException>(() => problem.GetUnassignedVariable());
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

    [Fact]
    public void EliminateUnitClauses()
    {
        var problem = new NoCopyDPLLSolver.Problem(6, 4);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, -2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 2, 3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 3, 4 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 5, 6 }));
        // forces first clause to unit clause
        problem.SetLiteral(1, false, isDecision: true);
        // sets 2, false => clause 2 becomes unit clause
        // sets 3, true => clause 2 and 3 satisfied, leaving clause 4 unsatisfied and unmodified
        NoCopyDPLLSolver.EliminateUnitClauses(problem);
        var currentAssignments = problem.DebugAsssignments;
        Assert.Equal(false, currentAssignments[1]);
        Assert.Equal(false, currentAssignments[2]);
        Assert.Equal(true, currentAssignments[3]);
        Assert.Equal((bool?)null, currentAssignments[4]);
        Assert.Equal((bool?)null, currentAssignments[5]);
        Assert.Equal((bool?)null, currentAssignments[6]);
    }

    [Fact]
    public void GetPureLiterals()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 3);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, -2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 2, 3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { -1, -3 }));
        Assert.Empty(problem.GetPureLiterals());
        problem.SetLiteral(1, true, isDecision: true);
        // 1, true => clause 1 satisfied
        // clause 2, literal 2 is pure
        // clause 3, -1 is assigned but not satisfied, -3 and 3 both exist in unsatisfied clauses
        Assert.Equal(new[] { 2 }, problem.GetPureLiterals());
        problem.SetLiteral(2, true, isDecision: true);
        // now that clause 2 is satisfied, only clause 3, and only -3 available
        Assert.Equal(new[] { -3 }, problem.GetPureLiterals());
    }

    [Fact]
    public void AssignPureLiteralsNoPureLiterals()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 3);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, -2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 2, 3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { -1, -3 }));
        Assert.Empty(problem.GetPureLiterals());
        NoCopyDPLLSolver.AssignPureLiterals(problem);
        var assignments = problem.DebugAsssignments;
        Assert.Equal((bool?)null, assignments[1]);
        Assert.Equal((bool?)null, assignments[2]);
        Assert.Equal((bool?)null, assignments[3]);
    }

    [Fact]
    public void AssignPureLiteralsSomePure()
    {
        var problem = new NoCopyDPLLSolver.Problem(3, 4);
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, -2 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 2, 3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { 1, -3 }));
        problem.AddClause(new NoCopyDPLLSolver.Clause(new[] { -2, 3 }));
        NoCopyDPLLSolver.AssignPureLiterals(problem);
        var assignments = problem.DebugAsssignments;
        Assert.Equal(true, assignments[1]);
        Assert.Equal((bool?)null, assignments[2]);
        Assert.Equal(true, assignments[3]);
    }

    // test for 1, 2, 3, -3, 2, 3, esures there is sufficient rolling back
}