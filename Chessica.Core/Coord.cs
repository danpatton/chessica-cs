namespace Chessica.Core;

public record struct Coord(byte File, byte Rank)
{
    public byte Ordinal => (byte)(Rank * 8 + File);

    public bool IsDarkSquare => (Rank + File) % 2 == 0;

    public static Coord FromOrdinal(byte ordinal)
    {
        if (ordinal > 63) throw new Exception("Invalid ordinal");
        var (rank, file) = Math.DivRem(ordinal, (byte)8);
        return new Coord(file, rank);
    }

    public static Coord FromString(string s)
    {
        s = s.ToLower();
        var file = s[0] - 'a';
        var rank = s[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7)
        {
            throw new Exception($"Malformed coordinate: {s}");
        }

        return new Coord((byte)file, (byte)rank);
    }

    public char FileChar => (char)('a' + File);

    public char RankChar => (char)('1' + Rank);

    public override string ToString() => new(new[] { FileChar, RankChar });

    public IEnumerable<IReadOnlyList<Coord>> MoveSequences(Piece pieceType, Side side, bool attacksOnly = false)
    {
        switch (pieceType)
        {
            case Piece.Pawn:
                return PawnMoveSequences(side, attacksOnly);
            case Piece.Bishop:
                return BishopMoveSequences();
            case Piece.Knight:
                return KnightMoveSequences();
            case Piece.Rook:
                return RookMoveSequences();
            case Piece.Queen:
                return QueenMoveSequences();
            case Piece.King:
                return KingMoveSequences();
            default:
                throw new Exception("Not a long range piece");
        }
    }

    private IEnumerable<IReadOnlyList<Coord>> PawnMoveSequences(Side side, bool attacksOnly)
    {
        // zero-indexed!
        var rankDirection = side == Side.White ? 1 : -1;
        if (!attacksOnly)
        {
            var startingRank = side == Side.White ? (byte)1 : (byte)6;
            if (Rank == startingRank)
            {
                yield return new[] { this with { Rank = (byte)(Rank + rankDirection + rankDirection) } };
            }

            yield return new[] { this with { Rank = (byte)(Rank + rankDirection) } };
        }

        if (File > 0)
        {
            yield return new[] { new Coord { Rank = (byte)(Rank + rankDirection), File = (byte)(File - 1) } };
        }
        if (File < 7)
        {
            yield return new[] { new Coord { Rank = (byte)(Rank + rankDirection), File = (byte)(File + 1) } };
        }
    }

    private IEnumerable<IReadOnlyList<Coord>> BishopMoveSequences()
    {
        var nw = new List<Coord>();
        for (int i = Rank + 1, j = File - 1; i < 8 && j >= 0; ++i, --j)
        {
            nw.Add(new Coord { Rank = (byte)i, File = (byte)j });
        }
        var ne = new List<Coord>();
        for (int i = Rank + 1, j = File + 1; i < 8 && j < 8; ++i, ++j)
        {
            ne.Add(new Coord { Rank = (byte)i, File = (byte)j });
        }
        var sw = new List<Coord>();
        for (int i = Rank - 1, j = File - 1; i >= 0 && j >= 0; --i, --j)
        {
            sw.Add(new Coord { Rank = (byte)i, File = (byte)j });
        }
        var se = new List<Coord>();
        for (int i = Rank - 1, j = File + 1; i >= 0 && j < 8; --i, ++j)
        {
            se.Add(new Coord { Rank = (byte)i, File = (byte)j });
        }

        return new[] { nw, ne, sw, se };
    }

    private IEnumerable<IReadOnlyList<Coord>> KnightMoveSequences()
    {
        if (File > 0)
        {
            if (Rank > 1) yield return new[] { new Coord { File = (byte)(File - 1), Rank = (byte)(Rank - 2)} };
            if (Rank < 6) yield return new[] { new Coord { File = (byte)(File - 1), Rank = (byte)(Rank + 2)} };
        }
        if (File > 1)
        {
            if (Rank > 0) yield return new[] { new Coord { File = (byte)(File - 2), Rank = (byte)(Rank - 1)} };
            if (Rank < 7) yield return new[] { new Coord { File = (byte)(File - 2), Rank = (byte)(Rank + 1)} };
        }
        if (File < 6)
        {
            if (Rank > 0) yield return new[] { new Coord { File = (byte)(File + 2), Rank = (byte)(Rank - 1)} };
            if (Rank < 7) yield return new[] { new Coord { File = (byte)(File + 2), Rank = (byte)(Rank + 1)} };
        }
        if (File < 7)
        {
            if (Rank > 1) yield return new[] { new Coord { File = (byte)(File + 1), Rank = (byte)(Rank - 2)} };
            if (Rank < 6) yield return new[] { new Coord { File = (byte)(File + 1), Rank = (byte)(Rank + 2)} };
        }
    }

    private IEnumerable<IReadOnlyList<Coord>> RookMoveSequences()
    {
        var n = new List<Coord>();
        for (var i = Rank + 1; i < 8; ++i)
        {
            n.Add(this with { Rank = (byte)i });
        }
        var e = new List<Coord>();
        for (var i = File + 1; i < 8; ++i)
        {
            e.Add(this with { File = (byte)i });
        }
        var s = new List<Coord>();
        for (var i = Rank - 1; i >= 0; --i)
        {
            s.Add(this with { Rank = (byte)i });
        }
        var w = new List<Coord>();
        for (var i = File - 1; i >= 0; --i)
        {
            w.Add(this with { File = (byte)i });
        }

        return new[] { n, e, s, w };
    }

    private IEnumerable<IReadOnlyList<Coord>> QueenMoveSequences()
    {
        return BishopMoveSequences().Concat(RookMoveSequences());
    }

    private IEnumerable<IReadOnlyList<Coord>> KingMoveSequences()
    {
        if (Rank > 0)
        {
            yield return new[] { this with { Rank = (byte)(Rank - 1)} };
            if (File > 0) yield return new[] { new Coord { Rank = (byte)(Rank - 1), File = (byte)(File - 1) } };
            if (File < 7) yield return new[] { new Coord { Rank = (byte)(Rank - 1), File = (byte)(File + 1) } };
        }
        if (File > 0) yield return new[] { this with { File = (byte)(File - 1) } };
        if (File < 7) yield return new[] { this with { File = (byte)(File + 1) } };
        if (Rank < 7)
        {
            yield return new[] { this with { Rank = (byte)(Rank + 1)} };
            if (File > 0) yield return new[] { new Coord { Rank = (byte)(Rank + 1), File = (byte)(File - 1) } };
            if (File < 7) yield return new[] { new Coord { Rank = (byte)(Rank + 1), File = (byte)(File + 1) } };
        }
    }

    public static implicit operator Coord(string str) => FromString(str);

    public static implicit operator int(Coord c) => (c.File << 8) | c.Rank;
}