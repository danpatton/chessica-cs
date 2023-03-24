using System.Collections;

namespace Chessica.Core;

public struct BitBoard : IEnumerable<Coord>
{
    private ulong _state;

    public bool IsOccupied(Coord coord)
    {
        var bit = 1ul << coord.Ordinal;
        return (_state & bit) == bit;
    }

    public void Move(Coord from, Coord to)
    {
        if (!IsOccupied(from))
        {
            throw new Exception("Source coord not occupied");
        }

        if (IsOccupied(to))
        {
            throw new Exception("Target coord already occupied");
        }
        _state &= ~(1ul << from.Ordinal);
        _state |= 1ul << to.Ordinal;
    }

    public void Clear(Coord coord)
    {
        _state &= ~(1ul << coord.Ordinal);
    }

    private const ulong DeBruijnSequence = 0x37E84A99DAE458F;

    private static readonly int[] MultiplyDeBruijnBitPosition =
    {
        0, 1, 17, 2, 18, 50, 3, 57,
        47, 19, 22, 51, 29, 4, 33, 58,
        15, 48, 20, 27, 25, 23, 52, 41,
        54, 30, 38, 5, 43, 34, 59, 8,
        63, 16, 49, 56, 46, 21, 28, 32,
        14, 26, 24, 40, 53, 37, 42, 7,
        62, 55, 45, 31, 13, 39, 36, 6,
        61, 44, 12, 35, 60, 11, 10, 9,
    };

    public IEnumerator<Coord> GetEnumerator()
    {
        var bb = _state;
        while (bb != 0)
        {
            var ordinal = MultiplyDeBruijnBitPosition[((ulong)((long)bb & -(long)bb) * DeBruijnSequence) >> 58];
            var coord = Coord.FromOrdinal(ordinal);
            yield return coord;
            bb &= ~(1ul << ordinal);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long Count
    {
        get
        {
            var i = (long)_state;
            i = i - ((i >> 1) & 0x5555555555555555);
            i = (i & 0x3333333333333333) + ((i >> 2) & 0x3333333333333333);
            return (((i + (i >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56;
        }
    }

    public static implicit operator ulong(BitBoard b) => b._state;

    public static implicit operator BitBoard(ulong state) => new() { _state = state };

    public static implicit operator BitBoard(Coord coord) => new() { _state = 1ul << coord.Ordinal };

    public static BitBoard operator &(BitBoard a, BitBoard b) => a._state & b._state;

    public static BitBoard operator |(BitBoard a, BitBoard b) => a._state | b._state;

    public static BitBoard operator ~(BitBoard a) => ~a._state;

    public static BitBoard Rank(int rank)
    {
        return Ranks[rank];
    }

    public static BitBoard File(int file)
    {
        return Files[file];
    }

    public static BitBoard AheadOfRank(Side side, int rank)
    {
        return side switch
        {
            Side.White => AheadOfWhiteRanks[rank],
            Side.Black => AheadOfBlackRanks[rank],
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static readonly ulong[] Ranks =
    {
        0x00000000000000ff,
        0x000000000000ff00,
        0x0000000000ff0000,
        0x00000000ff000000,
        0x000000ff00000000,
        0x0000ff0000000000,
        0x00ff000000000000,
        0xff00000000000000,
    };

    private static readonly ulong[] Files = {
        0x101010101010101,
        0x202020202020202,
        0x404040404040404,
        0x808080808080808,
        0x1010101010101010,
        0x2020202020202020,
        0x4040404040404040,
        0x8080808080808080
    };

    private static readonly ulong[] AheadOfWhiteRanks = {
        0xffffffffffffff00,
        0xffffffffffff0000,
        0xffffffffff000000,
        0xffffffff00000000,
        0xffffff0000000000,
        0xffff000000000000,
        0xff00000000000000,
        0x0000000000000000,
    };

    private static readonly ulong[] AheadOfBlackRanks = {
        0x0000000000000000,
        0x00000000000000ff,
        0x000000000000ffff,
        0x0000000000ffffff,
        0x00000000ffffffff,
        0x000000ffffffffff,
        0x0000ffffffffffff,
        0x00ffffffffffffff
    };
}