using System.Diagnostics;
using sat_solver.io;
using sat_solver.solvers;
using sat_solver.solvers.dpll;
using sat_solver.solvers.dpll_fast;

namespace sat_solver;

public class SolverProgram
{
    public void Run(string[] args)
    {
        if (args.Length != 1)
            throw new ArgumentOutOfRangeException("unexpected arguments 'file-path'");

        string fileArg = args[0].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        string filePath = Path.GetFullPath(fileArg);
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
        {
            fileInfo = FindInDirectory(fileInfo);
            if (!fileInfo.Exists)
                throw new ArgumentException($"provided file does not exist '{fileInfo.FullName}'");
        }

        Console.WriteLine($"File: {fileInfo.FullName}");
        var loadTimer = Stopwatch.StartNew();
        IDimacsReader fileReader = new DimacsReader(fileInfo);
        ISatSolver solver = new DPLLFastSolver();
        solver.Init(fileReader);
        loadTimer.Stop();
        Console.WriteLine($"Literal Count: {solver.LiteralCount}, Clause Count: {solver.ClauseCount}");
        Console.WriteLine($"Load time: {loadTimer.Elapsed.TotalSeconds}");

        loadTimer.Restart();
        var a = new Timer(_ => Console.WriteLine($"{loadTimer.Elapsed.TotalSeconds:0000.0000}: {solver.DPLLCalls:0,0}"), null, 0, 10000);

        try
        {
            var result = solver.Solve();
            loadTimer.Stop();
            Console.WriteLine($"Result: {result.Outcome}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine($"DPLL calls: {solver.DPLLCalls}");
        }
        finally
        {
            a.Change(0, Timeout.Infinite);
            a.Dispose();
        }
        Console.WriteLine($"Time: {loadTimer.Elapsed.TotalSeconds}");
    }

    private FileInfo FindInDirectory(FileInfo missing)
    {
        if (missing.Directory?.Exists != true) return missing;
        var files = missing.Directory.GetFiles("*.cnf.xz");
        var matching = files.Where(m => m.FullName.StartsWith(missing.FullName)).ToList();
        if (matching.Count == 0)
            return missing;
        else if (matching.Count == 1)
            return matching.First();
        else {
            Console.WriteLine("Multiple matching files, please provide a more specific filename stub.");
            return missing;
        }
    }
}