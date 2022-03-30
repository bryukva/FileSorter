using System.Diagnostics;
using LargeTextFiles.Domain;

var path = @"C:\Users\Home\Documents\FineryMarkets\LargeTextFilesApp\LargeTextFiles.Sorter\bin\Debug\net6.0\100mb.txt";
var target = @"C:\Users\Home\Documents\FineryMarkets\LargeTextFilesApp\LargeTextFiles.Sorter\bin\Debug\net6.0\outputSorted.txt";

var sorter = new Sorter();

if (string.IsNullOrWhiteSpace(path))
{
    throw new ArgumentNullException(nameof(path));
}

if (!File.Exists(path))
{
    throw new FileNotFoundException("File not found", path);
}

target = string.IsNullOrWhiteSpace(target) ? "sorted.txt" : target;
Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();
     var stopwatch = new Stopwatch();
     stopwatch.Start();
await sorter.Sort(path, target);
     stopwatch.Stop();
long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;

Console.WriteLine($"{stopwatch.ElapsedMilliseconds / 1000d} s elapsed, {totalBytesOfMemoryUsed/1024d/1024d} Mb memory consumed");

//var results = new List<double>();
// for (var i = 0; i < 1; i++)
// {
//     var stopwatch = new Stopwatch();
//     stopwatch.Start();
//     await sorter.Sort(new FileStream(path, FileMode.Open), new FileStream(target, FileMode.Create),
//         CancellationToken.None);
//     stopwatch.Stop();
//
//     var seconds = stopwatch.ElapsedMilliseconds / 1000d;
//     results.Add(seconds);
// }
//
// Console.WriteLine($"Avg of 1Gb: {results.Average(x => x)}");