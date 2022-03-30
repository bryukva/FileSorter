using System.Diagnostics;
using LargeTextFiles.Domain;

Console.Write("File to sort: ");
var path = Console.ReadLine();

Console.Write("Sorted file name: ");
var target = Console.ReadLine();

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

Console.Write("Sorting started");

var currentProcess = Process.GetCurrentProcess();
var stopwatch = new Stopwatch();
stopwatch.Start();
await sorter.Sort(path, target);
stopwatch.Stop();
var totalBytesOfMemoryUsed = currentProcess.WorkingSet64;

Console.WriteLine($"{stopwatch.ElapsedMilliseconds / 1000d} s elapsed, {totalBytesOfMemoryUsed/1024d/1024d} Mb memory consumed");