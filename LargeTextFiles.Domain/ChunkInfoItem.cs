using System;
using System.Collections.Generic;
using System.Diagnostics;
using LargeTextFiles.Domain;

namespace LargeTextFiles.Domain;

public class ChunkInfoItem
{
    public string? StringFilePath = null;
    public string? NumberFilePath = null;

    public long StringFileLength = 0;
    public long NumberFileLength = 0;

    public long CountOfLinesInFile = 0;
       
    public Line FirstPart;
    public Line LastPart;

    public readonly List<Line> Buffer = new List<Line>();
        
    public long CountLines => CountOfLinesInFile + Buffer.Count;

    public long ChunksLength => StringFileLength + NumberFileLength;
}