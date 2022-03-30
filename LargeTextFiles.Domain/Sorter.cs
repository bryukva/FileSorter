namespace LargeTextFiles.Domain;

public class Sorter
{
    private readonly IComparer<Line> _comparer = new LineComparer();
    private const long MaxLinesInFile = 2 * 1024 * 1024;
    private const int BatchSize = 10;
    private const string UnsortedFileExtension = "unsorted";
    private const string SortedFileExtension = "sorted";
    private const string TempFileExtension = "_";
    private readonly string _tmpDirPath = Path.Combine(Directory.GetCurrentDirectory(), "tmp");
    private int _tmpFileNumber;

    public async Task Sort(string sourcePath, string targetPath, CancellationToken cancellationToken = default)
    {
        using var source = new StreamReader(sourcePath);
        await using var target = File.OpenWrite(targetPath);
        Directory.CreateDirectory(_tmpDirPath);

        try
        {
            var files = await SplitFile(source, cancellationToken);

            if (files.Count == 1)
            {
                var unsortedFilePath = files.First();
                await SortFile(File.OpenRead(unsortedFilePath), target);
                File.Delete(unsortedFilePath);
            }
            else
            {
                var sortedFiles = await SortFiles(files);

                var done = false;
                var result = sortedFiles.Count / BatchSize;
                while (!done)
                {
                    done = result <= 0;
                    result /= BatchSize;
                }

                await MergeFiles(sortedFiles, target, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Directory.Delete(_tmpDirPath);
        }
    }

    private async Task<IReadOnlyCollection<string>> SplitFile(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new ArgumentNullException(nameof(reader));
        }
        
        var filenames = new List<string>();
        var strings = new List<string>();
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            strings.Add(line);

            if (strings.Count < MaxLinesInFile) 
                continue;
            filenames.Add(await WriteFile(strings, cancellationToken));
            strings.Clear();
        }

        if (strings.Any())
        {
            filenames.Add(await WriteFile(strings, cancellationToken));
        }

        return filenames;
    }

    private async Task<string> WriteFile(List<string> strings, CancellationToken cancellationToken)
    {
        var filename = Path.Combine(_tmpDirPath,$"{++_tmpFileNumber}.{UnsortedFileExtension}");
        await using var writer = new StreamWriter(filename);
        strings.ForEach(s => writer.WriteLine(s));
        return filename;
    }

    private async Task MergeFiles(IReadOnlyList<string> sortedFiles, Stream target, CancellationToken cancellationToken)
    {
        var allFilesProcessed = false;
        while (!allFilesProcessed)
        {
            var cnt = sortedFiles.Count;
            var finalRun = cnt <= BatchSize;

            if (finalRun)
            {
                await Merge(sortedFiles, target, cancellationToken);
                return;
            }

            var effectiveBatchSize = BatchSize;
            
            if (cnt % BatchSize != 0 && cnt % BatchSize / (double) BatchSize < 0.5*BatchSize)
            {
                effectiveBatchSize -= (int)(0.5 * BatchSize - (cnt % BatchSize));
            }

            var runs = sortedFiles.Chunk(effectiveBatchSize);
            var chunkCounter = 0;
            foreach (var files in runs)
            {
                var fileName = Path.Combine(_tmpDirPath, $"{++chunkCounter}.{SortedFileExtension}");
                var tmpFileName = Path.Combine(_tmpDirPath, $"{fileName}{TempFileExtension}");
                if (files.Length == 1)
                {
                    File.Move(files.First(), fileName);
                    continue;
                }

                var outputStream = File.OpenWrite(tmpFileName);
                await Merge(files, outputStream, cancellationToken);
                File.Move(tmpFileName, fileName, true);
            }

            sortedFiles = Directory.GetFiles(_tmpDirPath, $"*.{SortedFileExtension}")
                .OrderBy(x =>
                {
                    var filename = Path.GetFileNameWithoutExtension(x);
                    return int.Parse(filename);
                })
                .ToArray();

            if (sortedFiles.Count > 1)
            {
                continue;
            }

            allFilesProcessed = true;
        }
    }
    
    private async Task Merge(
        IReadOnlyList<string> filesToMerge,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        var (streamReaders, rows) = await InitializeStreamReaders(filesToMerge);
        var finishedStreamReaders = new List<int>(streamReaders.Length);
        var done = false;
        await using var outputWriter = new StreamWriter(outputStream);

        while (!done)
        {
            rows.Sort((row1, row2) => _comparer.Compare(row1.Value, row2.Value));
            var valueToWrite = rows[0].Value;
            var streamReaderIndex = rows[0].ReaderNumber;
            await outputWriter.WriteLineAsync(valueToWrite.ToString().AsMemory(), cancellationToken);

            if (streamReaders[streamReaderIndex].EndOfStream)
            {
                var indexToRemove = rows.FindIndex(x => x.ReaderNumber == streamReaderIndex);
                rows.RemoveAt(indexToRemove);
                finishedStreamReaders.Add(streamReaderIndex);
                done = finishedStreamReaders.Count == streamReaders.Length;
                continue;
            }

            var value = new Line(await streamReaders[streamReaderIndex].ReadLineAsync() ?? string.Empty);
            rows[0] = new Row(value, streamReaderIndex);
        }

        Parallel.ForEach(streamReaders, x =>
        {
            x.Dispose();
        });
        
        Parallel.ForEach(filesToMerge, File.Delete);
    }
    
    private static async Task<(StreamReader[] StreamReaders, List<Row> rows)> InitializeStreamReaders(
        IReadOnlyList<string> sortedFiles)
    {
        var streamReaders = new StreamReader[sortedFiles.Count];
        var rows = new List<Row>(sortedFiles.Count);
        for (var i = 0; i < sortedFiles.Count; i++)
        {
            var sortedFilePath = sortedFiles[i];
            var sortedFileStream = File.OpenRead(sortedFilePath);
            streamReaders[i] = new StreamReader(sortedFileStream);
            var value = new Line(await streamReaders[i].ReadLineAsync() ?? string.Empty);
            var row = new Row(value, i);
            rows.Add(row);
        }

        return (streamReaders, rows);
    }
    
    private async Task SortFile(Stream unsortedFile, Stream target)
    {
        var fileRows = new List<string>(); 
        using var streamReader = new StreamReader(unsortedFile);
        while (!streamReader.EndOfStream)
        {
            fileRows.Add(await streamReader.ReadLineAsync() ?? string.Empty);
        }

        var arr = fileRows.Select(x => new Line(x)).ToArray();
        Array.Sort(arr, _comparer);
        await using var streamWriter = new StreamWriter(target);

        var strArray = arr.Select(x => x.ToString()).ToList();
        foreach (var str in strArray)
        {
            await streamWriter.WriteLineAsync(str);
        }
    }
    
    private async Task<IReadOnlyList<string>> SortFiles(
        IReadOnlyCollection<string> unsortedFiles)
    {
        var sortedFiles = new List<string>(unsortedFiles.Count);
        foreach (var unsortedFile in unsortedFiles)
        {
            var sortedFile = unsortedFile.Replace(UnsortedFileExtension, SortedFileExtension);
            await SortFile(File.OpenRead(unsortedFile), File.OpenWrite(sortedFile));
            File.Delete(unsortedFile);
            sortedFiles.Add(sortedFile);
        }
        return sortedFiles;
    }
}