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

    public IEnumerator<Coord> GetEnumerator()
    {
        for (byte i = 0x00; i < 0x40; ++i)
        {
            var bit = 1ul << i;
            if ((_state & bit) == bit)
            {
                yield return Coord.FromOrdinal(i);
            }
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
}