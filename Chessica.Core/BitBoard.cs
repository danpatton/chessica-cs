using System.Collections;
using System.Numerics;

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

    public Enumerator GetEnumerator() => new(_state);

    IEnumerator<Coord> IEnumerable<Coord>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int Count => BitOperations.PopCount(_state);

    public bool Any => _state != 0;

    public Coord Single
    {
        get
        {
            if (!BitOperations.IsPow2(_state))
            {
                throw new InvalidOperationException();
            }

            return Coord.FromOrdinal(BitOperations.TrailingZeroCount(_state));
        }
    }

    public BitBoard PawnPushMask(Side side)
    {
        return side == Side.White
            ? _state << 8
            : _state >> 8;
    }

    public BitBoard PawnCaptureMask(Side side)
    {
        return PawnLeftCaptureMask(side) | PawnRightCaptureMask(side);
    }

    public BitBoard PawnLeftCaptureMask(Side side)
    {
        return side == Side.White
            ? (_state & ~FileMask(0)) << 7
            : (_state & ~FileMask(7)) >> 7;
    }

    public BitBoard PawnRightCaptureMask(Side side)
    {
        return side == Side.White
            ? (_state & ~FileMask(7)) << 9
            : (_state & ~FileMask(0)) >> 9;
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

    public static BitBoard KingsMoveMask(Coord king) => Mask.KingsMove[king.Ordinal];

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

    public struct Enumerator : IEnumerator<Coord>
    {
        private readonly ulong _value;
        private ulong _mValue;
        private int _current;

        public Enumerator(ulong value)
        {
            _value = value;
            _mValue = value;
            _current = 0;
        }

        public bool MoveNext()
        {
            if (_mValue == 0) return false;
            _current = BitOperations.TrailingZeroCount(_mValue);
            _mValue &= ~(1ul << _current);
            return true;
        }

        public void Reset()
        {
            _mValue = _value;
            _current = 0;
        }

        public Coord Current => Coord.FromOrdinal(_current);

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}