using System.Diagnostics;
using sat_solver.io;
using sat_solver.solvers;

namespace sat_solver;

public class SolverProgram
{
    public void Run(string[] args)
    {
        var timer = Stopwatch.StartNew();

        if (args.Length < 2)
            throw new ArgumentOutOfRangeException("missing arguments 'solver file-path'");
        Console.WriteLine($"Solver: {args[0]}");
        string solverName = args[0];
        Console.WriteLine($"File: {args[1]}");
        string fileArg = args[1].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        string filePath = Path.GetFullPath(fileArg);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new ArgumentException($"provided file does not exist '{fileInfo.FullName}'");

        IDimacsReader fileReader = new DimacsReaderBuffered(fileInfo);
        ISatSolver solver = GetSolverInstance(solverName);
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

    private static ISatSolver GetSolverInstance(string solverName)
    {
        return solverName switch
        {
            "simple" => new SimpleDPLLSolver(),
            "no-copy" => new NoCopyDPLLSolver(),
            "cdcl" => new CDCLSolver(),
            _ => throw new ArgumentException("Unknown solver specified", nameof(solverName)),
        };
    }
}