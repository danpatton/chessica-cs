namespace Chessica.Core;

public class SliderMoveCalculator
{
    private readonly MagicBitBoardEntry[] _rookMagic;
    private readonly MagicBitBoardEntry[] _bishopMagic;

    public SliderMoveCalculator(int indexBits, Random rng)
    {
        _rookMagic = Coord.All.Select(square => FindRookMagic(square, indexBits, rng)).ToArray();
        _bishopMagic = Coord.All.Select(square => FindBishopMagic(square, indexBits, rng)).ToArray();
    }

    public BitBoard GetRookMoves(Coord square, BitBoard allPieces, BitBoard ownPieces) =>
        _rookMagic[square.Ordinal].Lookup(allPieces, ownPieces);

    public BitBoard GetBishopMoves(Coord square, BitBoard allPieces, BitBoard ownPieces) =>
        _bishopMagic[square.Ordinal].Lookup(allPieces, ownPieces);

    private readonly struct MagicBitBoardEntry
    {
        private readonly BitBoard _mask;
        private readonly ulong _magic;
        private readonly int _indexShift;
        private readonly BitBoard[] _table;

        public MagicBitBoardEntry(BitBoard mask, ulong magic, int indexShift, BitBoard[] table)
        {
            _mask = mask;
            _magic = magic;
            _indexShift = indexShift;
            _table = table;
        }

        public BitBoard Lookup(BitBoard allPieces, BitBoard ownPieces)
        {
            var blockers = allPieces & _mask;
            return _table[blockers.MagicHashIndex(_magic, _indexShift)] & ~ownPieces;
        }
    }

    private static MagicBitBoardEntry FindRookMagic(Coord square, int indexBits, Random rng)
    {
        var indexShift = 64 - indexBits;
        BitBoard boundingBox = ulong.MaxValue;
        if (square.Rank != 0)
        {
            boundingBox &= ~BitBoard.RankMask(0);
        }
        if (square.Rank != 7)
        {
            boundingBox &= ~BitBoard.RankMask(7);
        }
        if (square.File != 0)
        {
            boundingBox &= ~BitBoard.FileMask(0);
        }
        if (square.File != 7)
        {
            boundingBox &= ~BitBoard.FileMask(7);
        }
        var mask = BitBoard.RooksMoveMask(square) & ~(BitBoard)square & boundingBox;
        while (true)
        {
            var magic = (ulong)(rng.NextInt64() & rng.NextInt64() & rng.NextInt64());
            if (TryMakeMagicTable(Piece.Rook, square, mask, magic, indexBits, out var table))
            {
                return new MagicBitBoardEntry(mask, magic, indexShift, table);
            }
        }
    }

    private static MagicBitBoardEntry FindBishopMagic(Coord square, int indexBits, Random rng)
    {
        var indexShift = 64 - indexBits;
        BitBoard boundingBox = ulong.MaxValue;
        if (square.Rank != 0)
        {
            boundingBox &= ~BitBoard.RankMask(0);
        }
        if (square.Rank != 7)
        {
            boundingBox &= ~BitBoard.RankMask(7);
        }
        if (square.File != 0)
        {
            boundingBox &= ~BitBoard.FileMask(0);
        }
        if (square.File != 7)
        {
            boundingBox &= ~BitBoard.FileMask(7);
        }
        var mask = BitBoard.BishopsMoveMask(square) & ~(BitBoard)square & boundingBox;
        while (true)
        {
            var magic = (ulong)(rng.NextInt64() & rng.NextInt64() & rng.NextInt64());
            if (TryMakeMagicTable(Piece.Bishop, square, mask, magic, indexBits, out var table))
            {
                return new MagicBitBoardEntry(mask, magic, indexShift, table);
            }
        }
    }

    private static bool TryMakeMagicTable(Piece piece, Coord square, BitBoard mask, ulong magic, int indexBits, out BitBoard[] table)
    {
        table = new BitBoard[1 << indexBits];
        var indexShift = 64 - indexBits;
        
        foreach (var blockers in mask.SubSets())
        {
            BitBoard moves = 0;
            foreach (var moveSequence in square.MoveSequences(piece, Side.White))
            {
                foreach (var coord in moveSequence)
                {
                    moves |= coord;
                    if (blockers.IsOccupied(coord)) break;
                }
            }

            var index = blockers.MagicHashIndex(magic, indexShift);
            if (table[index].Any)
            {
                if (table[index] != moves)
                {
                    return false;
                }
            }
            else
            {
                table[index] = moves;
            }
        }
        return true;
    }
}