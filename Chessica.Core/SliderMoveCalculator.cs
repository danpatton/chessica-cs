namespace Chessica.Core;

public class SliderMoveCalculator
{
    private readonly MagicBitBoardEntry[] _rookMagic;
    private readonly MagicBitBoardEntry[] _bishopMagic;

    private SliderMoveCalculator(MagicBitBoardEntry[] rookMagic, MagicBitBoardEntry[] bishopMagic)
    {
        _rookMagic = rookMagic;
        _bishopMagic = bishopMagic;
    }

    public static SliderMoveCalculator Generate(int indexBits, Random rng)
    {
        var rookMagic = Coord.All.Select(square => FindRookMagic(square, indexBits, rng)).ToArray();
        var bishopMagic = Coord.All.Select(square => FindBishopMagic(square, indexBits, rng)).ToArray();
        return new SliderMoveCalculator(rookMagic, bishopMagic);
    }

    public static ulong[] GenerateBishopMagics(int indexBits, Random rng)
    {
        return Coord.All.Select(square => FindBishopMagic(square, indexBits, rng)).Select(m => m.Magic).ToArray();
    }

    public static ulong[] GenerateRookMagics(int indexBits, Random rng)
    {
        return Coord.All.Select(square => FindRookMagic(square, indexBits, rng)).Select(m => m.Magic).ToArray();
    }

    public static SliderMoveCalculator Hardcoded()
    {
        var rookMagic = BuildMagicRookTables(RookMagics);
        var bishopMagic = BuildMagicBishopTables(BishopMagics);
        return new SliderMoveCalculator(rookMagic, bishopMagic);
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

        public ulong Magic => _magic;

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

    private static BitBoard GetBoxMask(Coord square)
    {
        BitBoard box = ulong.MaxValue;
        if (square.Rank != 0)
        {
            box &= ~BitBoard.RankMask(0);
        }
        if (square.Rank != 7)
        {
            box &= ~BitBoard.RankMask(7);
        }
        if (square.File != 0)
        {
            box &= ~BitBoard.FileMask(0);
        }
        if (square.File != 7)
        {
            box &= ~BitBoard.FileMask(7);
        }

        return box;
    }

    private static MagicBitBoardEntry FindRookMagic(Coord square, int indexBits, Random rng)
    {
        var indexShift = 64 - indexBits;
        while (true)
        {
            var mask = BitBoard.RooksMoveMask(square) & ~(BitBoard)square & GetBoxMask(square);
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
        var mask = BitBoard.BishopsMoveMask(square) & ~(BitBoard)square & GetBoxMask(square);
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

    private static MagicBitBoardEntry[] BuildMagicBishopTables(IEnumerable<(int, ulong)> hardcodedMagics)
    {
        var entries = new List<MagicBitBoardEntry>();
        var i = 0;
        foreach (var (indexBits, magic) in hardcodedMagics)
        {
            var indexShift = 64 - indexBits;
            var square = Coord.FromOrdinal(i++);
            var mask = BitBoard.BishopsMoveMask(square) & ~(BitBoard)square & GetBoxMask(square);
            if (!TryMakeMagicTable(Piece.Bishop, square, mask, magic, indexBits, out var tables))
            {
                throw new Exception("WTF");
            }

            entries.Add(new MagicBitBoardEntry(mask, magic, indexShift, tables));
        }

        return entries.ToArray();
    }

    private static MagicBitBoardEntry[] BuildMagicRookTables(IEnumerable<(int, ulong)> hardcodedMagics)
    {
        var entries = new List<MagicBitBoardEntry>();
        var i = 0;
        foreach (var (indexBits, magic) in hardcodedMagics)
        {
            var indexShift = 64 - indexBits;
            var square = Coord.FromOrdinal(i++);
            var mask = BitBoard.RooksMoveMask(square) & ~(BitBoard)square & GetBoxMask(square);
            if (!TryMakeMagicTable(Piece.Rook, square, mask, magic, indexBits, out var tables))
            {
                throw new Exception("WTF");
            }

            entries.Add(new MagicBitBoardEntry(mask, magic, indexShift, tables));
        }

        return entries.ToArray();
    }

    private static readonly (int, ulong)[] RookMagics =
    {
        (12, 0x00800080c0009020),
        (11, 0x0140002000100044),
        (11, 0x82001009a20080c0),
        (11, 0x0100090024203000),
        (11, 0x06802a0400804800),
        (11, 0x0080040080290200),
        (11, 0x0400100a00c80314),
        (12, 0x8180004080002100),
        (11, 0x2101800840009022),
        (10, 0x0082002302014080),
        (10, 0x0000805000802000),
        (10, 0x0820802800805002),
        (10, 0x0022808008000400),
        (10, 0x0086000a00143128),
        (10, 0x0906000200480401),
        (11, 0x0011000082522900),
        (11, 0x080084800040002a),
        (10, 0xc004c04010022000),
        (10, 0x0005090020004010),
        (10, 0x0808808018001004),
        (10, 0xa008008004008008),
        (10, 0x0016c80104201040),
        (10, 0x9200808002002100),
        (11, 0x80020200040088e1),
        (11, 0x0012209080084000),
        (10, 0x0002010200208040),
        (10, 0x2804430100352000),
        (10, 0x0000080080100080),
        (10, 0x80000800800c0080),
        (10, 0x1002200801044030),
        (10, 0x8202000200084104),
        (11, 0x00410001000c8242),
        (11, 0x8004c00180800120),
        (10, 0x1002400081002100),
        (10, 0x0a00809006802000),
        (10, 0xa020100080800800),
        (10, 0x0200810400800800),
        (10, 0x5040902028014004),
        (10, 0x080021b024000208),
        (11, 0xc04000410200008c),
        (11, 0x0030400020808000),
        (10, 0x1011482010014001),
        (10, 0x4009100020008080),
        (10, 0x0402011220420008),
        (10, 0x9000080004008080),
        (10, 0x8006001810220044),
        (10, 0x0400309822040001),
        (11, 0x40a4040481460001),
        (11, 0x0000800020400080),
        (10, 0x0040401000200040),
        (10, 0x4003002000144100),
        (10, 0x0122004110204a00),
        (10, 0x0406000520100a00),
        (10, 0x8100040080020080),
        (10, 0x011818100a030400),
        (11, 0x2031104400810200),
        (12, 0x0880088103204011),
        (11, 0x0020a81100814003),
        (11, 0x0100c3004850a001),
        (11, 0x0104200509001001),
        (11, 0x1062001020883482),
        (11, 0x0061000608240001),
        (11, 0x0200020810250084),
        (12, 0x010001004400208e),
    };

    private static readonly (int, ulong)[] BishopMagics =
    {
        (6, 0xa020410208210220),
        (5, 0x0220040400822000),
        (5, 0x8184811202030000),
        (5, 0x8110990202010902),
        (5, 0x002c04e018001122),
        (5, 0x006504a004044001),
        (5, 0x0002061096081014),
        (6, 0x0420440605032000),
        (5, 0x01080c980a141c00),
        (5, 0x0400200a02204300),
        (5, 0x4200101084810310),
        (5, 0x0200590401002100),
        (5, 0x84020110c042010d),
        (5, 0x00031c2420880088),
        (5, 0x10002104110440a0),
        (5, 0x0000010582104240),
        (5, 0x00080d501010009c),
        (5, 0x4092000408080100),
        (7, 0x0001001828010010),
        (7, 0x40220220208030a0),
        (7, 0x8201008090400000),
        (7, 0x0000202202012008),
        (5, 0x0008400404020810),
        (5, 0x0042004082088202),
        (5, 0x007a080110101000),
        (5, 0x6094101002028800),
        (7, 0x0018080004004410),
        (9, 0x688200828800810a),
        (9, 0x0881004409004004),
        (7, 0x1051020009004144),
        (5, 0x0202008102080100),
        (5, 0x0401010000484800),
        (5, 0x4001300800302100),
        (5, 0x50240c0420204926),
        (7, 0x0008640100102102),
        (9, 0x4800100821040400),
        (9, 0x00200240400400b0),
        (7, 0x0008030100027004),
        (5, 0x2001080200a48242),
        (5, 0x000400aa02002100),
        (5, 0x0a82501004000820),
        (5, 0x0002480211282840),
        (7, 0x0081001802001400),
        (7, 0x4008014010400203),
        (7, 0x0000080900410400),
        (7, 0x0220210301080200),
        (5, 0x00200b0401010080),
        (5, 0x0301012408890100),
        (5, 0x2202015016101444),
        (5, 0x0801008084210000),
        (5, 0x0a20051480900032),
        (5, 0x0000400042120880),
        (5, 0x000006100e020000),
        (5, 0x0600083004082800),
        (5, 0x2c88501312140010),
        (5, 0x0804080200420000),
        (6, 0x0040802090042000),
        (5, 0x4020006486107088),
        (5, 0x0008801052080400),
        (5, 0x631000244420a802),
        (5, 0x0080400820204100),
        (5, 0x101000100c100420),
        (5, 0x011040044840c100),
        (6, 0x0040080104012242),
    };
}