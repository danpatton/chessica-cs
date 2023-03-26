namespace Chessica.Core;

public class ZobristHash
{
    private static readonly long BlackToMoveKey;
    private static readonly long[,,] PieceKeys = new long[2, 6, 64];
    private static readonly long[] ShortCastlingKeys = new long[2];
    private static readonly long[] LongCastlingKeys = new long[2];
    private static readonly long[] EnPassantFileKeys = new long[8];

    static ZobristHash()
    {
        var rng = new Random(0);

        BlackToMoveKey = rng.NextInt64();

        foreach (var side in Enum.GetValues<Side>())
        {
            foreach (var piece in Enum.GetValues<Piece>())
            {
                foreach (var coord in Coord.All)
                {
                    PieceKeys[(int)side, (int)piece, coord.Ordinal] = rng.NextInt64();
                }
            }
        }

        foreach (var side in Enum.GetValues<Side>())
        {
            ShortCastlingKeys[(int)side] = rng.NextInt64();
            LongCastlingKeys[(int)side] = rng.NextInt64();
        }

        for (var i = 0; i < 8; ++i)
        {
            EnPassantFileKeys[i] = rng.NextInt64();
        }

        var set = new HashSet<long> { BlackToMoveKey };
        foreach (var key in PieceKeys)
        {
            if (!set.Add(key))
            {
                throw new Exception("Collision");
            }
        }

        foreach (var key in ShortCastlingKeys)
        {
            if (!set.Add(key))
            {
                throw new Exception("Collision");
            }
        }

        foreach (var key in LongCastlingKeys)
        {
            if (!set.Add(key))
            {
                throw new Exception("Collision");
            }
        }

        foreach (var key in EnPassantFileKeys)
        {
            if (!set.Add(key))
            {
                throw new Exception("Collision");
            }
        }
    }

    public long Value { get; private set; }

    public static long Of(BoardState board)
    {
        var sideToMoveHash = board.SideToMove == Side.Black ? BlackToMoveKey : 0L;
        return sideToMoveHash ^ board.WhiteState.HashValue ^ board.BlackState.HashValue;
    }

    public void SetFrom(ZobristHash other)
    {
        Value = other.Value;
    }

    public void FlipPiece(Side side, Piece piece, Coord coord)
    {
        Value ^= PieceKeys[(int)side, (int)piece, coord.Ordinal];
    }

    public void MovePiece(Side side, Piece piece, Coord from, Coord to)
    {
        Value ^= PieceKeys[(int)side, (int)piece, from.Ordinal];
        Value ^= PieceKeys[(int)side, (int)piece, to.Ordinal];
    }

    public void FlipEnPassantFile(int file)
    {
        Value ^= EnPassantFileKeys[file];
    }

    public void FlipLongCastling(Side side)
    {
        Value ^= LongCastlingKeys[(int)side];
    }

    public void FlipShortCastling(Side side)
    {
        Value ^= ShortCastlingKeys[(int)side];
    }
}