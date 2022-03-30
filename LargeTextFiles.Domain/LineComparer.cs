namespace LargeTextFiles.Domain;

public class LineComparer : IComparer<Line>
{
    public int Compare(Line first, Line second)
    {
        var res = string.CompareOrdinal(first.TextPart, second.TextPart);
        if (res == 0)
        {
            res = first.NumberPart.CompareTo(second.NumberPart);
        }
        return res;
    }
}