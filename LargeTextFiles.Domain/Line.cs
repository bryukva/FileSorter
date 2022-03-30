namespace LargeTextFiles.Domain;

public struct Line
{
    public Line(int numberPart, string stringPart)
    {
        NumberPart = numberPart;
        TextPart = stringPart;
    }
    
    public Line(string line)
    {
        var prefix = line.Split('.').First();
        NumberPart = int.Parse(prefix);
        TextPart = line.Remove(0, prefix.Length + 1);
    }
    public int NumberPart { get; set; }
    public string TextPart { get; set; }
    public override string ToString() => $"{NumberPart}.{TextPart}";
}