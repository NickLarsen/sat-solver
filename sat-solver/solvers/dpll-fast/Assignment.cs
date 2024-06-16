namespace sat_solver.solvers.dpll_fast;

public struct Assignment
{
    public int Literal { get; }
    public bool Value { get; }
    public bool IsDecisionVariable { get; }

    private Assignment(int literal, bool value, bool isDecisionVariable)
    {
        Literal = literal;
        Value = value;
        IsDecisionVariable = isDecisionVariable;
    }

    public Assignment Opposite()
    {
        return new Assignment(Literal, !Value, IsDecisionVariable);
    }

    public static Assignment MakeDecision(int literal, bool value)
    {
        return new Assignment(literal, value, isDecisionVariable: true);
    }
    public static Assignment MakeImplication(int literal, bool value)
    {
        return new Assignment(literal, value, isDecisionVariable: false);
    }
}