namespace sat_solver.solvers.dpll;

class Clause
{
    public required int[] Literals { get; set; }
    public bool IsEmptyClause => Literals.Length == 0;
    public bool IsUnitClause => Literals.Length == 1;
}