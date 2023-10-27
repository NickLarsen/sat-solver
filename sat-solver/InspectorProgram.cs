using sat_solver.io;
using sat_solver.solver.DPLL;

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
            var fileInfo = new FileInfo(file);
            using var fileReader = new DimacsReader(fileInfo);
            var (l, c) = fileReader.ReadHeader();
            infos.Add(new DimacsFileInfo { FileInfo = fileInfo, LiteralCount = l, ClauseCount = c });
        }
        foreach(var info in infos.OrderBy(m => m.LiteralCount))
        {
            Console.WriteLine($"lit: {info.LiteralCount, 8}, cla: {info.ClauseCount, 8}, {info.FileInfo.Name}");
        }
    }

    private class DimacsFileInfo
    {
        public FileInfo FileInfo { get; set; }
        public int LiteralCount { get; set; }
        public int ClauseCount { get; set; }
    }
}