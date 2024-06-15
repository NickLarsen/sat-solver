using System.Diagnostics;
using sat_solver.io;
using sat_solver.solvers;

namespace sat_solver;

public class InspectorProgram
{
    public void Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("no folder provided");
            return;
        }

        Console.WriteLine($"Path: {args[0]}");
        string pathArg = args[0].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        var pattern = "*.cnf";
        if (args.Length >= 2)
        {
            pattern = args[1];
        }
        var infos = new List<DimacsFileInfo>();
        var files = Directory.GetFiles(pathArg, pattern);
        foreach(var file in files)
        {
            var timer = Stopwatch.StartNew();
            var fileInfo = new FileInfo(file);
            using var fileReader = new DimacsReader(fileInfo);
            var (l, c) = fileReader.ReadHeader();
            timer.Stop();
            infos.Add(new DimacsFileInfo { 
                FileInfo = fileInfo, 
                LiteralCount = l, 
                ClauseCount = c, 
                ReadDuration = timer.Elapsed 
            });
        }
        foreach(var info in infos.OrderBy(m => m.LiteralCount))
        {
            Console.WriteLine($"lit: {info.LiteralCount, 8}, cla: {info.ClauseCount, 8}, dur: {info.ReadDuration.TotalMilliseconds}, {info.FileInfo.Name}");
        }
    }

    private class DimacsFileInfo
    {
        public required FileInfo FileInfo { get; set; }
        public int LiteralCount { get; set; }
        public int ClauseCount { get; set; }
        public TimeSpan ReadDuration { get; set; }
    }
}