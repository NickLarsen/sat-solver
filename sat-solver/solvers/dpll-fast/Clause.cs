namespace sat_solver.solvers.dpll_fast;

public class Clause 
{
    public int? SatisfiedByLevel { get; set; }
    public int[] Literals { get; }

    public Clause(IReadOnlyList<int> literals)
    {
        this.Literals = literals
            .OrderBy(m => Math.Abs(m))
            .ToArray();
    }
}