using LargeTextFiles.Domain;

Console.Write("Generate file of size (Mb): ");
if (!uint.TryParse(Console.ReadLine(), out var fileSizeMb))
{
    Console.WriteLine("Invalid input, expected non-negative integer number");
    return;
}

int? percent;
Console.Write("Duplicate strings percent: ");
if (!int.TryParse(Console.ReadLine(), out var approximateDuplicatesPercent))
{
    Console.WriteLine("Could not parse input, the file will be generated with default duplicate string percent of 1");
    percent = null;
}
else
    percent = approximateDuplicatesPercent;

new TextFileGenerator().GenerateInputFile(fileSizeMb, percent);