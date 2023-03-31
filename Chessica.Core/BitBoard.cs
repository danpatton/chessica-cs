using System.Collections;

namespace Chessica.Core;

public struct BitBoard : IEnumerable<Coord>, IEquatable<BitBoard>
{
    private ulong _state;

    public bool Equals(BitBoard other)
    {
        return _state == other._state;
    }

    public override bool Equals(object? obj)
    {
        return obj is BitBoard other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _state.GetHashCode();
    }

    public static bool operator ==(BitBoard left, BitBoard right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BitBoard left, BitBoard right)
    {
        return !left.Equals(right);
    }

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

    public int Count
    {
        get
        {
            var i = (long)_state;
            i -= (i >> 1) & 0x5555555555555555;
            i = (i & 0x3333333333333333) + ((i >> 2) & 0x3333333333333333);
            return (int)((((i + (i >> 4)) & 0xF0F0F0F0F0F0F0F) * 0x101010101010101) >> 56);
        }
    }

    public bool Any => _state != 0;

    public Coord Single
    {
        get
        {
            var bb = _state;
            var ordinal = MultiplyDeBruijnBitPosition[((ulong)((long)bb & -(long)bb) * DeBruijnSequence) >> 58];
            var coord = Coord.FromOrdinal(ordinal);
            if ((_state & ~(1ul << ordinal)) != 0)
            {
                throw new InvalidOperationException();
            }

            return coord;
        }
    }

    public static implicit operator ulong(BitBoard b) => b._state;

    public static implicit operator BitBoard(ulong state) => new() { _state = state };

    public static implicit operator BitBoard(Coord coord) => new() { _state = 1ul << coord.Ordinal };

    public static BitBoard operator &(BitBoard a, BitBoard b) => a._state & b._state;

    public static BitBoard operator |(BitBoard a, BitBoard b) => a._state | b._state;

    public static BitBoard operator ~(BitBoard a) => ~a._state;

    public static BitBoard KnightsMoveMask(Coord knight) => Mask.KnightsMove[knight.Ordinal];

    public static BitBoard BishopsMoveMask(Coord bishop) => Mask.BishopsMove[bishop.Ordinal];

    public static BitBoard RooksMoveMask(Coord rook) => Mask.RooksMove[rook.Ordinal];

    public static BitBoard BoundingBoxMask(Coord x, Coord y) => Mask.BoundingBox[x.Ordinal, y.Ordinal];

    public static BitBoard RankMask(int rank) => Mask.Rank[rank];

    public static BitBoard FileMask(int file) => Mask.File[file];

    public static BitBoard AheadOfRankMask(Side side, int rank)
    {
        return side switch
        {
            Side.White => Mask.AheadOfWhiteRank[rank],
            Side.Black => Mask.AheadOfBlackRank[rank],
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}