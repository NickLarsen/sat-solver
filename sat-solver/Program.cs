// See https://aka.ms/new-console-template for more information


using sat_solver;

if (args.Length == 0)
{
    Console.WriteLine("no args provided, nothing useful to do");
    return;
}

switch(args[0])
{
    case "solve":
        var solverProgram = new SolverProgram();
        solverProgram.Run(args.Skip(1).ToArray());
        break;
    case "inspect":
        var inspectorProgram = new InspectorProgram();
        inspectorProgram.Run(args.Skip(1).ToArray());
        break;
    default:
        Console.WriteLine($"Unknown task provided: {args[0]}, please try again.");
        return;
}