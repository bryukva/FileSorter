using System.Text;

namespace LargeTextFiles.Domain;

public class TextFileGenerator
{
    private const string OutputFilePath = @"output.txt";
    private static string[] _textCorpus = Array.Empty<string>();
    private static string[] _duplicateStrings = Array.Empty<string>();
    private const ushort MinWordsCount = 1;
    private const ushort MaxWordsCount = 5;
    private const int DuplicatePoolSize = 20;
    private const long FileSizeUnitMultiplier = 1024*1024;
    private const decimal ApproximateDuplicateProbability = 1m;
    private const string RowsDelimiter = "\r\n";
    private static readonly Random Rnd = new();
    private static int _dupCount;
    private static int _count;
        
    //TODO: to appsettings
    public TextFileGenerator(string textCorpusFilePath = @"words.txt")
    {
        var textCorpusFileContent = File.ReadAllText(textCorpusFilePath, Encoding.UTF8) ?? throw new FileLoadException("Could not load text corpus file");
        _textCorpus = textCorpusFileContent.Split(RowsDelimiter) ?? throw new FileLoadException("Could not parse text corpus file");
        _duplicateStrings = Enumerable.Range(0, DuplicatePoolSize).Select(_ => GenerateStringPart()).ToArray();
    }

    public void GenerateInputFile(uint fileSizeMb, decimal? approximateDuplicateProbability)
    {
        approximateDuplicateProbability ??= ApproximateDuplicateProbability;
        long totalLength = 0;
            
        using (var fs = new FileStream(OutputFilePath, FileMode.Create))
        using (var sw = new StreamWriter(fs))
        {
            if (totalLength < fileSizeMb)
                totalLength += _duplicateStrings.Select(x => GetLine(GetNumberPart, x)).ToList().Sum(str => sw.WriteAndCountBytes(str));
                
            while (totalLength < fileSizeMb * FileSizeUnitMultiplier)
            {
                var str = GetLine(approximateDuplicateProbability.Value);
                totalLength += sw.WriteAndCountBytes(str);
            }
        }

        var fileStats = new FileInfo(OutputFilePath);
            
        Console.WriteLine($"{DateTime.Now}. File created: {fileStats.Length/FileSizeUnitMultiplier} Mb, total strings: {_count}, duplicate strings: {_dupCount}");
    }

    private static int GetNumberPart => Rnd.Next(ushort.MinValue, ushort.MaxValue);
        
    private static string GetLine (decimal approximateDuplicateProbability)
    {
        var numberPart = GetNumberPart;
        var stringPart = DuplicateOrGenerateStringPart(approximateDuplicateProbability);
        return GetLine(numberPart, stringPart);
    }
        
    private static string GetLine (int numberPart, string stringPart) => $"{numberPart.ToString()}. {stringPart}";

    private static string DuplicateOrGenerateStringPart(decimal approximateDuplicateProbability)
    {
        if (approximateDuplicateProbability > 0 && (Rnd.Next(0, 10000) < approximateDuplicateProbability*100))
        {
            _dupCount++;
            return _duplicateStrings[Rnd.Next(0, DuplicatePoolSize)];
        }
        _count++;
        return GenerateStringPart();
    }

    private static string GenerateStringPart()
    {
        var countWords = Rnd.Next(MinWordsCount, MaxWordsCount);

        var str = "";
        for (var i = 0; i < countWords; i++)
        {
            str += $"{_textCorpus[Rnd.Next(0, _textCorpus.Length)]} ";
        }

        return str;
    }
}