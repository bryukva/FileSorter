namespace LargeTextFiles.Domain;

public class Sorter1
{
    private readonly LineComparer _comparer = new();
    private const int MaxLinesInMemory = 1024 * 1024 * 32;
    private const int MaxLinesInBuffers = MaxLinesInMemory * 4;
    private const string _tmpFolder = "tmp";

    private readonly long _initialFileLength;
    private readonly List<ChunkInfoItem> _listOfCurrentItemsToProcess = new();
    private readonly LinkedList<ChunkInfoItem> _results = new();

        private long _initialCountOfLines;
        private long _outputCountOfLines;

        public Sorter()
        {
        }

        public void Sort(string filePath, string outputFilePath)
        {
            Directory.CreateDirectory(_tmpFolder);

            SplitAndSort(filePath);

            MergeSorted(outputFilePath);
        }

        private void SplitAndSort(string filePath)
        {

            try
            {
                using var streamReader = new StreamReader(filePath);

                var item = ReadAndSortInitialChunk(streamReader);
                _results.AddFirst(item);

                if (_initialCountOfLines < MaxLinesInMemory) //it is possible to sort right in memory
                {
                    return;
                }

                string? line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    _initialCountOfLines++;

                    var linePart = LinePreProcessingFunc(line);

                    var node = _results.First;

                    while (node != null)
                    {
                        var chunkInfoItem = node.Value;

                        var compareToFirst = _comparer.Compare(linePart, chunkInfoItem.FirstPart);
                        var compareToLast = _comparer.Compare(linePart, chunkInfoItem.LastPart);

                        if (compareToFirst == 0 && compareToLast < 0 || (compareToFirst < 0 && compareToLast < 0))
                        {
                            chunkInfoItem.Buffer.Add(linePart);
                            chunkInfoItem.FirstPart = linePart;
                            AddCurrentItemIfNotExist(chunkInfoItem);
                            return;
                        }
                        
                        if (compareToFirst > 0 && compareToLast < 0)
                        {
                            chunkInfoItem.Buffer.Add(linePart);
                            AddCurrentItemIfNotExist(chunkInfoItem);
                            return;
                        }

                        if (compareToLast == 0 && compareToFirst > 0)
                        {
                            chunkInfoItem.Buffer.Add(linePart);
                            chunkInfoItem.LastPart = linePart;
                            AddCurrentItemIfNotExist(chunkInfoItem);
                            return;
                        }

                        if (compareToFirst > 0 && compareToLast > 0)
                        {
                            if (node.Next == null)
                            {
                                var newAfterChunkInfo = new ChunkInfoItem();
                                newAfterChunkInfo.Buffer.Add(linePart);
                                newAfterChunkInfo.FirstPart = linePart;
                                newAfterChunkInfo.LastPart = linePart;

                                _results.AddAfter(node, newAfterChunkInfo);
                                AddCurrentItemIfNotExist(newAfterChunkInfo);
                                return;
                            }

                            node = node.Next;
                            continue;
                        }

                        node = node.Next;
                    }

                    if (ShouldStartProcessingOfMemoryData())
                    {
                        ProcessCurrentItems();
                    }
                }

                ProcessCurrentItems();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void ProcessCurrentItems()
        {
            Parallel.ForEach(_listOfCurrentItemsToProcess, x =>
            {
                if (x.CountOfLinesInFile == 0)
                {
                    ProcessNewChunk(x);
                }
                else
                {
                    ProcessExistingChunkWithBuffer(x);
                }
            });

            ProcessWrongOrderOfChunks();

            _listOfCurrentItemsToProcess.Clear();
        }

        private ChunkInfoItem ReadAndSortInitialChunk(StreamReader streamReader)
        {
            var item = new ChunkInfoItem();

            string line;
            while ((line = streamReader.ReadLine()) != null)
            {
                _initialCountOfLines++;

                var linePart = LinePreProcessingFunc(line);
                item.Buffer.Add(linePart);

                if (item.Buffer.Count >= MaxLinesInMemory)
                {
                    break;
                }
            }

            item.Buffer.Sort(_comparer);

            item.FirstPart = item.Buffer.FirstOrDefault();
            item.LastPart = item.Buffer.LastOrDefault();
            item.CountOfLinesInFile = item.Buffer.Count;

            WriteBufferToFile(item);

            return item;
        }

        private static void WriteBufferToFile(ChunkInfoItem item)
        {
            item.StringFilePath = Path.Combine(_tmpFolder, Guid.NewGuid().ToString());
            item.NumberFilePath = Path.Combine(_tmpFolder, Guid.NewGuid().ToString());

            using (var stringWriter = new StreamWriter(item.StringFilePath))
            {
                using (var numberWriter = new StreamWriter(item.NumberFilePath))
                {
                    foreach (var pair in item.Buffer)
                    {
                        stringWriter.WriteLine(pair.TextPart);
                        numberWriter.WriteLine(pair.NumberPart);
                    }
                }
            }

            item.StringFileLength = new FileInfo(item.StringFilePath).Length;
            item.NumberFileLength = new FileInfo(item.NumberFilePath).Length;
            item.Buffer.Clear();
        }

        private bool ShouldStartProcessingOfMemoryData()
        {
            return _listOfCurrentItemsToProcess.Sum(x => x.Buffer.Count) >= MaxLinesInBuffers || 
                   _listOfCurrentItemsToProcess.Max(x => x.Buffer.Count) >= MaxLinesInMemory;
        }

        private void AddCurrentItemIfNotExist(ChunkInfoItem chunkInfoItem)
        {
            if (!_listOfCurrentItemsToProcess.Contains(chunkInfoItem))
            {
                _listOfCurrentItemsToProcess.Add(chunkInfoItem);
            }
        }

        private void ProcessWrongOrderOfChunks()
        {
            var node = _results.First;

            if (node == null)
                return;
            
            var second = node.Next;

            while (second != null)
            {
                if (_comparer.Compare(node.LastPart, second.FirstPart) > 0)
                {
                    first = SortChunks(first, second);
                }
                else
                {
                    first = second;
                }

                second = first.Next;
            }
        }

        private void ProcessNewChunk(ChunkInfoItem treeItemToProcess)
        {
            treeItemToProcess.Buffer.Sort(_comparer);

            treeItemToProcess.FirstPart = treeItemToProcess.Buffer.FirstOrDefault();
            treeItemToProcess.LastPart = treeItemToProcess.Buffer.LastOrDefault();
            treeItemToProcess.CountOfLinesInFile = treeItemToProcess.Buffer.Count;

            WriteBufferToFile(treeItemToProcess);
        }

        private void ProcessExistingChunkWithBuffer(ChunkInfoItem treeItemToProcess)
        {
            // additional chunk - find node - read old - join lines - sort - split to 2 chunks, write and update linked list
            var node = _linkedlist.Find(treeItemToProcess);
            if (node == null)
            {
                throw new InvalidOperationException("Cannot find mandatory node");
            }

            var parts = new List<Line>((int)treeItemToProcess.CountOfLinesInFile + treeItemToProcess.Buffer.Count);

            using (var stringReader = new StreamReader(treeItemToProcess.StringFilePath))
            {
                using (var numberReader = new StreamReader(treeItemToProcess.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new Line(stringLine, numberLine));
                    }
                }
            }

            parts.AddRange(treeItemToProcess.Buffer);
            treeItemToProcess.Buffer.Clear();

            parts.Sort(_comparer);

            var firstPartOfList = parts.Count / 2;
            var lastPartOfList = parts.Count - firstPartOfList;

            var firstList = parts.GetRange(0, firstPartOfList);
            var lastList = parts.GetRange(firstPartOfList, lastPartOfList);

            var firstItem = new ChunkInfoItem
            {
                FirstPart = firstList.FirstOrDefault(),
                LastPart = firstList.LastOrDefault(),
                CountOfLinesInFile = firstList.Count,
                Buffer = firstList,
            };

            WriteBufferToFile(firstItem);

            var lastItem = new ChunkInfoItem
            {
                FirstPart = lastList.FirstOrDefault(),
                LastPart = lastList.LastOrDefault(),
                CountOfLinesInFile = lastList.Count,
                Buffer = lastList,
            };

            WriteBufferToFile(lastItem);

            _linkedlist.AddBefore(node, firstItem);
            _linkedlist.AddAfter(node, lastItem);

            _linkedlist.Remove(node);

            File.Delete(node.Value.StringFilePath);
            File.Delete(node.Value.NumberFilePath);

            Console.WriteLine($"{DateTime.Now}. Processed buffer and split chunk to two new. Chunks count: {_linkedlist.Count}");
        }

        private LinkedListNode<ChunkInfoItem> SortChunks(LinkedListNode<ChunkInfoItem> first, LinkedListNode<ChunkInfoItem> second)
        {
            var parts = new List<Line>();

            // read first chunk 
            using (var stringReader = new StreamReader(first.Value.StringFilePath))
            {
                using (var numberReader = new StreamReader(first.Value.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new Line(stringLine, numberLine));
                    }
                }
            }

            // read second chunk 
            using (var stringReader = new StreamReader(second.Value.StringFilePath))
            {
                using (var numberReader = new StreamReader(second.Value.NumberFilePath))
                {
                    string stringLine;
                    while ((stringLine = stringReader.ReadLine()) != null)
                    {
                        var numberLine = numberReader.ReadLine();
                        parts.Add(new Line(stringLine, numberLine));
                    }
                }
            }

            parts.Sort(_comparer);

            var firstPartOfList = parts.Count / 2;
            var lastPartOfList = parts.Count - firstPartOfList;

            var firstList = parts.GetRange(0, firstPartOfList);
            var lastList = parts.GetRange(firstPartOfList, lastPartOfList);

            var newFirstItem = new ChunkInfoItem
            {
                FirstPart = firstList.FirstOrDefault(),
                LastPart = firstList.LastOrDefault(),
                CountOfLinesInFile = firstList.Count,
                Buffer = firstList,
            };

            WriteBufferToFile(newFirstItem);

            var newLastItem = new ChunkInfoItem
            {
                FirstPart = lastList.FirstOrDefault(),
                LastPart = lastList.LastOrDefault(),
                CountOfLinesInFile = lastList.Count,
                Buffer = lastList,
            };

            WriteBufferToFile(newLastItem);

            var result = _linkedlist.AddBefore(first, newFirstItem);

            _linkedlist.AddAfter(second, newLastItem);

            _linkedlist.Remove(first);
            _linkedlist.Remove(second);

            File.Delete(first.Value.StringFilePath);
            File.Delete(second.Value.NumberFilePath);

            Console.WriteLine($"{DateTime.Now}. Sorting existing chunks to ensure correct order.");
            
            return result;
        }

        private static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        private void MergeSorted(string outputFilePath)
        {
            const int linesCountProgressToInform = 1000 * 1000 * 5;
            
            using (var writer = new StreamWriter(outputFilePath))
            {
                // there possible preloading of buffers
                foreach (var treeItem in _linkedlist)
                {
                    var stringItems = File.ReadAllLines(treeItem.StringFilePath);
                    var numberItems = File.ReadAllLines(treeItem.NumberFilePath);

                    for (int i = 0; i < stringItems.Length; i++)
                    {
                        writer.WriteLine(numberItems[i] + ". " + stringItems[i]);
                        _outputCountOfLines++;

                        if (_outputCountOfLines % linesCountProgressToInform == 0)
                        {
                            Console.WriteLine($"{DateTime.Now}. Processed count of initial lines: {_outputCountOfLines} / {_initialCountOfLines}");
                        }
                    }
               }
            }
        }

        public static Line LinePreProcessingFunc(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return new Line(null, null);
            }

            var indexOfEndOfNumberPart = line.IndexOf(" ", StringComparison.OrdinalIgnoreCase);

            var stringPart = line.Substring(indexOfEndOfNumberPart + 1); // without space
            var numberPart = line.Substring(0, indexOfEndOfNumberPart - 1);

            return new Line(stringPart, numberPart);
        }
    }
}