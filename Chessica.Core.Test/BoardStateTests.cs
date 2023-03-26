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

    [TestCase("e4 e5 Bc4 Nc6 Nf3", "Nf3 Nc6 e4 e5 Bc4")]
    [TestCase("e4 d5 exd5 Qxd5 Nf3 e6", "e4 d5 Nf3 e6 exd5 Qxd5")]
    [TestCase("e4 e5 Bc4 Nc6 Nf3 Nf6 O-O", "Nf3 Nc6 e4 e5 Bc4 Nf6 O-O")]
    public void TestTranspositionHashValue(string board1Moves, string board2Moves)
    {
        var board1 = BoardState.StartingPosition;
        foreach (var move in board1Moves.Split(" "))
        {
            board1.Push(move);
        }

        var board2 = BoardState.StartingPosition;
        foreach (var move in board2Moves.Split(" "))
        {
            board2.Push(move);
        }

        Assert.That(board1.HashValue, Is.EqualTo(board2.HashValue));

        while (board1.TryPop() && board2.TryPop())
        {
        }

        Assert.Multiple(() =>
        {
            Assert.That(board1.HashValue, Is.EqualTo(BoardState.StartingPosition.HashValue));
            Assert.That(board2.HashValue, Is.EqualTo(BoardState.StartingPosition.HashValue));
        });
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

    [TestCase("r3r1k1/pQp2ppp/2n2n2/4p3/2P5/B4q1b/P1PP1P1P/R3RBK1 b - - 4 15", "Qg4+ Kh1 Qf3+ Kg1 Qg4+ Kh1 Qf3+ Kg1")]
    [TestCase("8/p4ppp/2b5/6RK/1n2r3/8/5r1P/3k4 b - - 0 51", "R4e2 Ra5 Rf4 Rg5 R2e4 Ra5 Rf2 Rg5 Nc2 Ra5 Nb4 Rg5")]
    public void TestThreefoldRepetition(string inputFen, string moves)
    {
        var board = BoardState.ParseFen(inputFen);
        foreach (var moveSpec in moves.Split(" "))
        {
            Assert.That(board.IsDrawByThreefoldRepetition(), Is.False);
            board.Push(moveSpec);
        }
        Assert.That(board.IsDrawByThreefoldRepetition(), Is.True);
    }
}