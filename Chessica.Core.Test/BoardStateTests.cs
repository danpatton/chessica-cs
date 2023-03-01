using Chessica.Pgn;

namespace Chessica.Core.Test;

public class BoardStateTests
{
    [Test]
    public void TestBlockingCheck()
    {
        var board = BoardState.StartingPosition;
        var moveSequence = new List<Move>
        {
            new (Piece.Pawn, "e2", "e4"),
            new (Piece.Pawn, "d7", "d5"),
            new (Piece.Pawn, "e4", "d5", true),
            new (Piece.Queen, "d8", "d5", true),
            new (Piece.Knight, "g1", "f3"),
            new (Piece.Queen, "d5", "e5")
        };
        TestUtils.AssertValidMoveSequence(board, moveSequence);
        var expectedLegalMoves = new List<Move>
        {
            new (Piece.Bishop, "f1", "e2"),
            new (Piece.Queen, "d1", "e2"),
            new (Piece.Knight, "f3", "e5", true)
        };
        var actualLegalMoves = board.GetLegalMoves().ToList();
        CollectionAssert.AreEquivalent(expectedLegalMoves, actualLegalMoves);
    }

    [Test]
    public void TestScholarsMate()
    {
        var board = BoardState.StartingPosition;
        var moveSequence = new List<Move>
        {
            new (Piece.Pawn, "e2", "e4"),
            new (Piece.Pawn, "e7", "e5"),
            new (Piece.Queen, "d1", "h5"),
            new (Piece.Knight, "b8", "c6"),
            new (Piece.Bishop, "f1", "c4"),
            new (Piece.Pawn, "d7", "d6"),
            new (Piece.Queen, "h5", "f7", true)
        };
        TestUtils.AssertValidMoveSequence(board, moveSequence);
        Assert.That(board.IsCheckmate);
    }

    [Test]
    public void TestGameFromPgn()
    {
        var pgn = PgnGame.Parse(TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example1.pgn"));
        var fen = TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example1.fen").Split("\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Multiple(() =>
        {
            Assert.That(pgn.Tags, Has.Count.EqualTo(21));
            Assert.That(pgn.Tags, Has.Member(new KeyValuePair<string, string>("Result", "0-1")));
            Assert.That(pgn.Moves, Has.Count.EqualTo(38));
        });
        TestUtils.SimulatePgnGame(pgn, fen, PgnGameResult.BlackWin);
    }

    [Test]
    public void TestGameFromPgn2()
    {
        var pgn = PgnGame.Parse(TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example2.pgn"));
        var fen = TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example2.fen").Split("\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Multiple(() =>
        {
            Assert.That(pgn.Tags, Has.Count.EqualTo(7));
            Assert.That(pgn.Tags, Has.Member(new KeyValuePair<string, string>("Result", "1/2-1/2")));
            Assert.That(pgn.Moves, Has.Count.EqualTo(85));
        });
        TestUtils.SimulatePgnGame(pgn, fen, PgnGameResult.Draw);
    }

    [Test]
    public void TestGameFromPgn3()
    {
        var pgn = PgnGame.Parse(TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example3.pgn"));
        var fen = TestUtils.LoadEmbeddedResource("Chessica.Core.Test.Pgn.example3.fen").Split("\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Multiple(() =>
        {
            Assert.That(pgn.Tags, Has.Count.EqualTo(13));
            Assert.That(pgn.Tags, Has.Member(new KeyValuePair<string, string>("Result", "1-0")));
            Assert.That(pgn.Moves, Has.Count.EqualTo(203));
        });
        TestUtils.SimulatePgnGame(pgn, fen, PgnGameResult.WhiteWin);
    }

    [Test]
    public void TestFenForStartingPosition()
    {
        var board = BoardState.StartingPosition;
        Assert.That(
            board.ToFenString(),
            Is.EqualTo("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"));
        board.Push(board.ToMove("e4"));
        Assert.That(
            board.ToFenString(),
            Is.EqualTo("rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"));
        board.Push(board.ToMove("c5"));
        Assert.That(
            board.ToFenString(),
            Is.EqualTo("rnbqkbnr/pp1ppppp/8/2p5/4P3/8/PPPP1PPP/RNBQKBNR w KQkq c6 0 2"));
        board.Push(board.ToMove("Nf3"));
        Assert.That(
            board.ToFenString(),
            Is.EqualTo("rnbqkbnr/pp1ppppp/8/2p5/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2"));
    }

    [TestCase("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")]
    [TestCase("4r1k1/1p3p1p/1qp2bp1/r2p4/Pp1P3P/1P2P1P1/3Q1PB1/R2R2K1 b - h3 0 24")]
    [TestCase("r2q1rk1/pb1n1ppp/1ppbpn2/3p4/2PP4/1PN1PN2/PBQ1BPPP/R3K2R w KQ - 2 10")]
    [TestCase("rnbqk2r/ppp1ppbp/3p1np1/8/2PPP3/2N2N2/PP3PPP/R1BQKB1R b KQkq e3 0 5")]
    public void TestFenRoundTrip(string inputFen)
    {
        var boardState = BoardState.ParseFen(inputFen);
        var outputFen = boardState.ToFenString();
        Assert.That(outputFen, Is.EqualTo(inputFen));
    }

    [TestCase("7k/8/8/6q1/8/4q1N1/8/1K6 b - - 0 1", Piece.Queen, "e3", "g3", "Qexg3")]
    [TestCase("7k/8/8/6q1/8/4q1N1/8/1K6 b - - 0 1", Piece.Queen, "g5", "g3", "Q5xg3")]
    [TestCase("7k/8/8/4q1q1/8/4q1N1/8/1K6 b - - 0 1", Piece.Queen, "e3", "g3", "Qe3xg3")]
    [TestCase("7k/8/8/4q1q1/8/4q1N1/8/1K6 b - - 0 1", Piece.Queen, "g5", "g3", "Qg5xg3")]
    [TestCase("7k/8/8/4q1q1/8/4q1N1/8/1K6 b - - 0 1", Piece.Queen, "e5", "g3", "Qe5xg3")]
    [TestCase("rn1qkb1r/pppBpp1p/3p1np1/8/3PP3/8/PPPN1PPP/R1BQK1NR b KQkq - 0 5", Piece.Knight, "b8", "d7", "Nbxd7")]
    [TestCase("rn1qkb1r/pppBpp1p/3p1np1/8/3PP3/8/PPPN1PPP/R1BQK1NR b KQkq - 0 5", Piece.Knight, "f6", "d7", "Nfxd7")]
    public void TestRankAndFileDisambiguation(string inputFen, Piece piece, string from, string to, string expectedPgnSpec)
    {
        var board = BoardState.ParseFen(inputFen);
        var move = new Move(piece, from, to, true);
        var pgnSpec = move.ToPgnSpec(board);
        Assert.That(pgnSpec, Is.EqualTo(expectedPgnSpec));
    }
}