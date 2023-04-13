namespace Chessica.Core.Test;

[TestFixture]
public class BenchmarkingTests
{
    private const string Position1 = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string Position2 = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    private const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    private const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    private const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";
    private const string Position6 = "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10";
    private const string Position7 = "rn1qk1nr/pbppp1bp/1p4p1/4Pp2/3K4/8/PPPP1PPP/RNBQ1BNR w kq f6 0 1";
    private const string Position8 = "rnb1k1nr/pppp1ppp/8/4p3/1b1P3q/2Q5/PPP1PPPP/RNB1KBNR w KQkq - 0 4";

    [TestCase(Position1, 1, 20ul)]
    [TestCase(Position1, 2, 400ul)]
    [TestCase(Position1, 3, 8_902ul)]
    [TestCase(Position1, 4, 197_281ul)]
    [TestCase(Position2, 1, 48ul)]
    [TestCase(Position2, 2, 2_039ul)]
    [TestCase(Position2, 3, 97_862ul)]
    [TestCase(Position2, 4, 4_085_603ul)]
    [TestCase(Position3, 1, 14ul)]
    [TestCase(Position3, 2, 191ul)]
    [TestCase(Position3, 3, 2_812ul)]
    [TestCase(Position3, 4, 43_238ul)]
    [TestCase(Position3, 5, 674_624ul)]
    [TestCase(Position4, 1, 6ul)]
    [TestCase(Position4, 2, 264ul)]
    [TestCase(Position4, 3, 9_467ul)]
    [TestCase(Position4, 4, 422_333ul)]
    [TestCase(Position5, 1, 44ul)]
    [TestCase(Position5, 2, 1_486ul)]
    [TestCase(Position5, 3, 62_379ul)]
    [TestCase(Position5, 4, 2_103_487ul)]
    [TestCase(Position6, 1, 46ul)]
    [TestCase(Position6, 2, 2_079ul)]
    [TestCase(Position6, 3, 89_890ul)]
    [TestCase(Position6, 4, 3_894_594ul)]
    [TestCase(Position7, 1, 33ul)]
    [TestCase(Position7, 2, 983ul)]
    [TestCase(Position7, 3, 28_964ul)]
    [TestCase(Position7, 4, 844_341ul)]
    [TestCase(Position8, 1, 23ul)]
    [TestCase(Position8, 2, 1_009ul)]
    [TestCase(Position8, 3, 26_125ul)]
    [TestCase(Position8, 4, 1_072_898ul)]
    public void TestPerft(string initialFen, int depth, ulong expectedResult)
    {
        var board = BoardState.ParseFen(initialFen);
        var result = Benchmarking.Perft(board, depth);
        Assert.That(result, Is.EqualTo(expectedResult));
    }
}