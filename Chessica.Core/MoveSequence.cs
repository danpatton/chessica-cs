namespace Chessica.Core;

public static class MoveSequence
{
    public static IEnumerable<IReadOnlyList<Coord>> MoveSequences(this Coord origin, Piece pieceType, Side side, bool attacksOnly = false)
    {
        switch (pieceType)
        {
            case Piece.Pawn:
                return origin.PawnMoveSequences(side, attacksOnly);
            case Piece.Bishop:
                return origin.BishopMoveSequences();
            case Piece.Knight:
                return origin.KnightMoveSequences();
            case Piece.Rook:
                return origin.RookMoveSequences();
            case Piece.Queen:
                return origin.QueenMoveSequences();
            case Piece.King:
                return origin.KingMoveSequences();
            default:
                throw new Exception("Not a long range piece");
        }
    }

    private static IEnumerable<IReadOnlyList<Coord>> PawnMoveSequences(this Coord origin, Side side, bool attacksOnly)
    {
        var rankDirection = side == Side.White ? 1 : -1;
        if (!attacksOnly)
        {
            // zero-indexed!
            var startingRank = side == Side.White ? 1 : 6;
            if (origin.Rank == startingRank)
            {
                yield return new[] { origin with { Rank = (origin.Rank + rankDirection + rankDirection) } };
            }

            yield return new[] { origin with { Rank = origin.Rank + rankDirection } };
        }

        if (origin.File > 0)
        {
            yield return new[] { new Coord { Rank = origin.Rank + rankDirection, File = origin.File - 1 } };
        }
        if (origin.File < 7)
        {
            yield return new[] { new Coord { Rank = origin.Rank + rankDirection, File = origin.File + 1 } };
        }
    }

    private static IEnumerable<IReadOnlyList<Coord>> BishopMoveSequences(this Coord origin)
    {
        var nw = new List<Coord>();
        for (int i = origin.Rank + 1, j = origin.File - 1; i < 8 && j >= 0; ++i, --j)
        {
            nw.Add(new Coord { Rank = i, File = j });
        }
        var ne = new List<Coord>();
        for (int i = origin.Rank + 1, j = origin.File + 1; i < 8 && j < 8; ++i, ++j)
        {
            ne.Add(new Coord { Rank = i, File = j });
        }
        var sw = new List<Coord>();
        for (int i = origin.Rank - 1, j = origin.File - 1; i >= 0 && j >= 0; --i, --j)
        {
            sw.Add(new Coord { Rank = i, File = j });
        }
        var se = new List<Coord>();
        for (int i = origin.Rank - 1, j = origin.File + 1; i >= 0 && j < 8; --i, ++j)
        {
            se.Add(new Coord { Rank = i, File = j });
        }

        return new[] { nw, ne, sw, se };
    }

    private static IEnumerable<IReadOnlyList<Coord>> KnightMoveSequences(this Coord origin)
    {
        if (origin.File > 0)
        {
            if (origin.Rank > 1) yield return new[] { new Coord { File = origin.File - 1, Rank = origin.Rank - 2} };
            if (origin.Rank < 6) yield return new[] { new Coord { File = origin.File - 1, Rank = origin.Rank + 2} };
        }
        if (origin.File > 1)
        {
            if (origin.Rank > 0) yield return new[] { new Coord { File = origin.File - 2, Rank = origin.Rank - 1} };
            if (origin.Rank < 7) yield return new[] { new Coord { File = origin.File - 2, Rank = origin.Rank + 1} };
        }
        if (origin.File < 6)
        {
            if (origin.Rank > 0) yield return new[] { new Coord { File = origin.File + 2, Rank = origin.Rank - 1} };
            if (origin.Rank < 7) yield return new[] { new Coord { File = origin.File + 2, Rank = origin.Rank + 1} };
        }
        if (origin.File < 7)
        {
            if (origin.Rank > 1) yield return new[] { new Coord { File = origin.File + 1, Rank = origin.Rank - 2} };
            if (origin.Rank < 6) yield return new[] { new Coord { File = origin.File + 1, Rank = origin.Rank + 2} };
        }
    }

    private static IEnumerable<IReadOnlyList<Coord>> RookMoveSequences(this Coord origin)
    {
        var n = new List<Coord>();
        for (var i = origin.Rank + 1; i < 8; ++i)
        {
            n.Add(origin with { Rank = i });
        }
        var e = new List<Coord>();
        for (var i = origin.File + 1; i < 8; ++i)
        {
            e.Add(origin with { File = i });
        }
        var s = new List<Coord>();
        for (var i = origin.Rank - 1; i >= 0; --i)
        {
            s.Add(origin with { Rank = i });
        }
        var w = new List<Coord>();
        for (var i = origin.File - 1; i >= 0; --i)
        {
            w.Add(origin with { File = i });
        }

        return new[] { n, e, s, w };
    }

    private static IEnumerable<IReadOnlyList<Coord>> QueenMoveSequences(this Coord origin)
    {
        return origin.BishopMoveSequences().Concat(origin.RookMoveSequences());
    }

    private static IEnumerable<IReadOnlyList<Coord>> KingMoveSequences(this Coord origin)
    {
        if (origin.Rank > 0)
        {
            yield return new[] { origin with { Rank = origin.Rank - 1} };
            if (origin.File > 0) yield return new[] { new Coord { Rank = origin.Rank - 1, File = origin.File - 1 } };
            if (origin.File < 7) yield return new[] { new Coord { Rank = origin.Rank - 1, File = origin.File + 1 } };
        }
        if (origin.File > 0) yield return new[] { origin with { File = origin.File - 1 } };
        if (origin.File < 7) yield return new[] { origin with { File = origin.File + 1 } };
        if (origin.Rank < 7)
        {
            yield return new[] { origin with { Rank = origin.Rank + 1} };
            if (origin.File > 0) yield return new[] { new Coord { Rank = origin.Rank + 1, File = origin.File - 1 } };
            if (origin.File < 7) yield return new[] { new Coord { Rank = origin.Rank + 1, File = origin.File + 1 } };
        }
    }
}