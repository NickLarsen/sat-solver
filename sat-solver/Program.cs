// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using sat_solver;
using sat_solver.io;
using sat_solver.solver.DPLL;

var timer = Stopwatch.StartNew();

if (args.Length < 1)
    throw new ArgumentOutOfRangeException("first argment should be the path of the DIMACS file to solve");
Console.WriteLine($"File: {args[0]}");
string fileArg = args[0].Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
string filePath = Path.GetFullPath(fileArg);
var fileInfo = new FileInfo(filePath);
if (!fileInfo.Exists)
    throw new ArgumentException($"provided file does not exist '{fileInfo.FullName}'");

IDimacsReader fileReader = new DimacsReader(fileInfo);
ISatSolver solver = new SimpleDPLLSolver();
solver.Init(fileReader);
Console.WriteLine($"Literal Count: {solver.LiteralCount}, Clause Count: {solver.ClauseCount}");
Console.WriteLine($"Load time: {timer.Elapsed.TotalSeconds}");

// var result = solver.Solve();
// Console.WriteLine($"Result: {result.Outcome}");

timer.Stop();
Console.WriteLine($"Time: {timer.Elapsed.TotalSeconds}");