using System.Diagnostics;
using sat_solver;

internal class Program
{
    private static void Main(string[] args)
    {
        bool debugging = Debugger.IsAttached;
        string target = "solve";
        string[] targetArgs = new[] {
            "no-copy",
            "../problems/2023/0a4ed112f2cdc0a524976a15d1821097-cliquecoloring_n12_k9_c8.cnf.xz"
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

        switch (target)
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
    }
}