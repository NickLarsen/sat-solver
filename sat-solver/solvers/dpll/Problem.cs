namespace sat_solver.solvers.dpll;

class Problem
{
    public List<Clause> Clauses { get; }
    public bool IsEmptyClauseList => Clauses.Count == 0;
    public bool HasEmptyClause { get; private set; }
    public bool[] IsAssigned { get; }
    public bool[] Assignments { get; }

    public Problem(List<Clause> clauses, bool[] isAssigned, bool[] assignments)
    {
        Clauses = clauses;
        HasEmptyClause = Clauses.Any(m => m.IsEmptyClause);
        IsAssigned = isAssigned;
        Assignments = assignments;
    }

    public Problem SetLiteral(int literal, bool value)
    {
        var isAssigned = IsAssigned.ToArray();
        isAssigned[literal] = true;
        var assignments = Assignments.ToArray();
        assignments[literal] = value;
        var clauses = ReduceClauses(literal, value);
        return new Problem(clauses, isAssigned, assignments);
    }
    private List<Clause> ReduceClauses(int literal, bool value)
    {
        int sign = value ? 1 : -1;
        int satisfy = sign * literal;
        int remove = satisfy * -1;
        var result = new List<Clause>(Clauses.Count);
        foreach(var clause in Clauses)
        {
            if (clause.Literals.Contains(satisfy)) continue;
            var updated = clause.Literals.Where(m => m != remove).ToArray();
            result.Add(new Clause { Literals = updated });
        }
        return result;
    }
}