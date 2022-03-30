namespace LargeTextFiles.Domain;

public static class Extensions
{
    public static int WriteAndCountBytes(this StreamWriter writer, string str)
    {
        writer.WriteLine(str);
        return writer.Encoding.GetByteCount(str) + 2;
    }
}