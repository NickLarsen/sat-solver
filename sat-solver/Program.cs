using System.Diagnostics;
using sat_solver;

internal class Program
{
    private static void Main(string[] args)
    {
        bool debugging = Debugger.IsAttached;
        string target = "solve";
        string[] targetArgs = new[] {
            "../problems/2023/03de316ba1e90305471a3b8620cb9cd7-satsgi-n23himBHm26-p0-q248.cnf.xz"
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