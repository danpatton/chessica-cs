namespace Chessica.Core;

public record struct Coord(int File, int Rank)
{
    public int Ordinal => Rank * 8 + File;

    public bool IsDarkSquare => (Rank + File) % 2 == 0;

    public static Coord FromOrdinal(int ordinal)
    {
        if (ordinal is < 0 or > 63) throw new Exception("Invalid ordinal");
        var (rank, file) = Math.DivRem(ordinal, 8);
        return new Coord(file, rank);
    }

    public static Coord FromString(string s)
    {
        s = s.ToLower();
        var file = s[0] - 'a';
        var rank = s[1] - '1';
        if (file is < 0 or > 7 || rank is < 0 or > 7)
        {
            throw new Exception($"Malformed coordinate: {s}");
        }

        return new Coord(file, rank);
    }

    public char FileChar => (char)('a' + File);

    public char RankChar => (char)('1' + Rank);

    public override string ToString() => new(new[] { FileChar, RankChar });

    public static IEnumerable<Coord> All => 
        from rank in Enumerable.Range(0, 8)
        from file in Enumerable.Range(0, 8)
        select new Coord(file, rank);

    public static implicit operator Coord(string str) => FromString(str);

    public static implicit operator int(Coord c) => (c.File << 8) | c.Rank;
}