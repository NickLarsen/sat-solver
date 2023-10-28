using System.Diagnostics;
using sat_solver;

bool debugging = Debugger.IsAttached;
string target = "solve";
string[] targetArgs = new[] { 
    "no-copy", 
    "../problems/2023/cnf/0297c2a35f116ffd5382aea5b421e6df-Urquhart-s3-b3.shuffled-as.sat03-1556.cnf" 
};

if (!debugging)
{
    if (args.Length == 0)
    {
        Console.WriteLine("no args provided, nothing useful to do");
        return;
    }
    target = args[0];
    targetArgs = args.Skip(1).ToArray();
}

switch(target)
{
    case "solve":
        var solverProgram = new SolverProgram();
        solverProgram.Run(targetArgs);
        break;
    case "inspect":
        var inspectorProgram = new InspectorProgram();
        inspectorProgram.Run(targetArgs);
        break;
    default:
        Console.WriteLine($"Unknown task provided: {args[0]}, please try again.");
        return;
}