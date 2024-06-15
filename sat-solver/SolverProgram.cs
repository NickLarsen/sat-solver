using System.Diagnostics;
using sat_solver.io;
using sat_solver.solvers;
using sat_solver.solvers.dpll;

namespace sat_solver;

public class SolverProgram
{
    public void Run(string[] args)
    {
        var timer = Stopwatch.StartNew();

        if (args.Length != 1)
            throw new ArgumentOutOfRangeException("unexpected arguments 'file-path'");
        string fileArg = args[0].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        string filePath = Path.GetFullPath(fileArg);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new ArgumentException($"provided file does not exist '{fileInfo.FullName}'");

        Console.WriteLine($"File: {args[0]}");
        IDimacsReader fileReader = new DimacsReader(fileInfo);
        ISatSolver solver = new DPLLSolver();
        solver.Init(fileReader);
        Console.WriteLine($"Literal Count: {solver.LiteralCount}, Clause Count: {solver.ClauseCount}");
        Console.WriteLine($"Load time: {timer.Elapsed.TotalSeconds}");

        var a = new Timer(_ => Console.WriteLine($"{timer.Elapsed.TotalSeconds:0000.0000}: {solver.DPLLCalls:0,0}"), null, 0, 10000);

        var result = solver.Solve();
        Console.WriteLine($"Result: {result.Outcome}");

        a.Change(0, Timeout.Infinite);
        a.Dispose();
        timer.Stop();
        Console.WriteLine($"Time: {timer.Elapsed.TotalSeconds}");
    }
}