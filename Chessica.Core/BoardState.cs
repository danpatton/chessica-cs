using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Optional;

namespace Chessica.Core;

public class BoardState
{
    private readonly Stack<Tuple<Move, MoveUndoInfo>> _moveStack = new();

    private Option<List<Move>> _cachedLegalMoves;
    private Option<BitBoard> _cachedPassedPawns;

    public SideState WhiteState { get; }
    public SideState BlackState { get; }

    public int HalfMoveClock { get; private set; }

    public int FullMoveNumber { get; private set; }

    public Side SideToMove { get; private set; }

    public BoardState(SideState whiteState, SideState blackState, Side sideToMove, int halfMoveClock = 0, int fullMoveNumber = 1)
    {
        WhiteState = whiteState;
        BlackState = blackState;
        SideToMove = sideToMove;
        HalfMoveClock = halfMoveClock;
        FullMoveNumber = fullMoveNumber;
    }

    public BoardState Clone()
    {
        return new BoardState(WhiteState.Clone(), BlackState.Clone(), SideToMove, HalfMoveClock, FullMoveNumber);
    }

    private readonly Regex _castlingMoveRegex = new("([Oo0](-[Oo0]){1,2})[+#]?");
    private readonly Regex _promotionMoveRegex = new("([a-h])?(x)?([a-h][1-8])\\=([QRBN])[+#]?");
    private readonly Regex _standardMoveRegex = new ("([KQRBN])?([a-h])?([1-8])?(x)?([a-h][1-8])[+#]?");

    public double GetScore()
    {
        // TODO: improve on this!
        return WhiteState.GetSimpleMaterialScore() - BlackState.GetSimpleMaterialScore();
    }

    public Move ToMove(string moveSpec)
    {
        var legalMoves = GetLegalMoves().ToList();

        var castlingMoveMatch = _castlingMoveRegex.Match(moveSpec);
        if (castlingMoveMatch.Success)
        {
            Coord kingFrom = SideToMove == Side.White ? "e1" : "e8";
            var n = castlingMoveMatch.Groups[1].Value.Count(c => c == '-');
            var kingToFile = n == 2 ? "c" : "g";
            Coord kingTo = kingToFile + (SideToMove == Side.White ? "1" : "8");
            var castlingMove = new CastlingMove(kingFrom, kingTo);
            if (!legalMoves.Contains(castlingMove))
            {
                throw new Exception($"{moveSpec} is not a legal move");
            }

            return castlingMove;
        }

        var promotionMoveMatch = _promotionMoveRegex.Match(moveSpec);
        if (promotionMoveMatch.Success)
        {
            Coord to = promotionMoveMatch.Groups[3].Value;
            var promotionPiece = promotionMoveMatch.Groups[4].Value.ParsePiece();
            var isCapture = promotionMoveMatch.Groups[2].Value == "x";
            var fromRank = to.Rank == 7 ? 6 : 1;
            var fromFile = string.IsNullOrEmpty(promotionMoveMatch.Groups[1].Value)
                ? to.File
                : promotionMoveMatch.Groups[1].Value[0] - 'a';
            var from = new Coord { File = fromFile, Rank = fromRank };
            return new PromotionMove(from, to, isCapture, promotionPiece);
        }

        var standardMoveMatch = _standardMoveRegex.Match(moveSpec);
        if (standardMoveMatch.Success)
        {
            var piece = standardMoveMatch.Groups[1].Value.ParsePiece();
            var isCapture = standardMoveMatch.Groups[4].Value == "x";
            Coord to = standardMoveMatch.Groups[5].Value;
            var candidateMoves = legalMoves
                .Where(m => m.Piece == piece && m.To == to && m.IsCapture == isCapture);
            var fromFileForDisambiguation = standardMoveMatch.Groups[2].Value;
            if (!string.IsNullOrEmpty(fromFileForDisambiguation))
            {
                var fromFile = fromFileForDisambiguation[0] - 'a';
                candidateMoves = candidateMoves.Where(m => m.From.File == fromFile);
            }
            var fromRankForDisambiguation = standardMoveMatch.Groups[3].Value;
            if (!string.IsNullOrEmpty(fromRankForDisambiguation))
            {
                var fromRank = fromRankForDisambiguation[0] - '1';
                candidateMoves = candidateMoves.Where(m => m.From.Rank == fromRank);
            }

            var finalCandidateMoves = candidateMoves.ToList();
            switch (finalCandidateMoves.Count)
            {
                case 0:
                    throw new Exception($"{moveSpec} is not a valid move");
                case 1:
                    return finalCandidateMoves.Single();
                default:
                    throw new Exception($"{moveSpec} is ambiguous");
            }
        }

        throw new Exception($"Malformed move: {moveSpec}");
    }

    public static BoardState StartingPosition => new(
        SideState.StartingPosition(Side.White),
        SideState.StartingPosition(Side.Black),
        Side.White);

    private static readonly Regex FenStringRegex = new (
        "([RNBQKPrnbqkp1-8/]{15,}) ([wb]) (K?Q?k?q?|-) ([a-h]?[1-8]?|-) (\\d+) (\\d+)");

    public string GetPgnCoordDisambiguation(Piece pieceType, Coord from, Coord to)
    {
        var potentiallyAmbiguousMoves = GetLegalMoves()
            .Where(m => m.Piece == pieceType && m.To == to && m.From != from)
            .ToArray();

        if (!potentiallyAmbiguousMoves.Any())
        {
            return string.Empty;
        }

        var needsExplicitFile = false;
        var needsExplicitRank = false;

        if (from.File != to.File && potentiallyAmbiguousMoves.All(move => move.From.File != from.File))
        {
            needsExplicitFile = true;
        }
        else if (from.Rank != to.Rank && potentiallyAmbiguousMoves.All(move => move.From.Rank != from.Rank))
        {
            needsExplicitRank = true;
        }
        else
        {
            needsExplicitFile = true;
            needsExplicitRank = true;
        }

        return (needsExplicitFile ? from.FileChar.ToString() : "") +
               (needsExplicitRank ? from.RankChar.ToString() : "");
    }
    
    public static BoardState ParseFen(string fen)
    {
        var match = FenStringRegex.Match(fen);
        if (!match.Success)
        {
            throw new Exception("Invalid FEN: " + fen);
        }
        var rows = match.Groups[1].Value.Split('/').Reverse().ToArray();
        if (rows.Length != 8)
        {
            throw new Exception("Invalid FEN: " + fen);
        }
        
        var sideToMove = match.Groups[2].Value == "w" ? Side.White : Side.Black;
        var castlingRights = match.Groups[3].Value;
        var whiteState = new SideState(
            Side.White, castlingRights.Contains('Q'), castlingRights.Contains('K'));
        var blackState = new SideState(
            Side.Black, castlingRights.Contains('q'), castlingRights.Contains('k'));
        
        for (var rank = 0; rank < 8; ++rank)
        {
            var row = rows[rank];
            if (row.Length > 8)
            {
                throw new Exception("Invalid FEN: " + fen);
            }
            var file = 0;
            foreach (var c in row)
            {
                var n = c - '0';
                if (n is > 0 and <= 8)
                {
                    file += n;
                }
                else
                {
                    var (piece, side) = c.ParseFenChar();
                    var coord = new Coord { File = file, Rank = rank };
                    switch (side)
                    {
                        case Side.White:
                            whiteState.AddPiece(piece, coord);
                            break;
                        case Side.Black:
                            blackState.AddPiece(piece, coord);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    ++file;
                }
            }
        }
        
        if (match.Groups[4].Value != "-")
        {
            Coord enPassantSquare = match.Groups[4].Value;
            switch (sideToMove)
            {
                case Side.White:
                    blackState.SetEnPassantSquare(enPassantSquare);
                    break;
                case Side.Black:
                    whiteState.SetEnPassantSquare(enPassantSquare);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var halfMoveClock = int.Parse(match.Groups[5].Value);
        var fullMoveNumber = int.Parse(match.Groups[6].Value);
        
        return new BoardState(whiteState, blackState, sideToMove, halfMoveClock, fullMoveNumber);
    }

    public IEnumerable<Move> GetLegalMoves()
    {
        return _cachedLegalMoves.Match(
            legalMoves => legalMoves,
            () =>
            {
                var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
                var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
                var legalMoves = MoveCalculator.CalculateMoves(ownSide, enemySide, out _).ToList();
                _cachedLegalMoves = Option.Some(legalMoves);
                return legalMoves;
            });
    }

    public IEnumerable<Move> GetPseudoLegalMoves()
    {
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
        return MoveCalculator.CalculatePseudoLegalMoves(ownSide, enemySide);
    }

    public (bool InCheck, int NumLegalMoves) GetGameState()
    {
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
        var legalMoves = MoveCalculator.CalculateMoves(ownSide, enemySide, out var inCheck);
        return (inCheck, legalMoves.Count());
    }

    public bool HasKing()
    {
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        return ownSide.King != 0;
    }

    public bool IsCheckmate()
    {
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
        var legalMoves = MoveCalculator.CalculateMoves(ownSide, enemySide, out var inCheck);
        return inCheck && !legalMoves.Any();
    }

    public bool IsStalemate()
    {
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
        var legalMoves = MoveCalculator.CalculateMoves(ownSide, enemySide, out var inCheck);
        return !inCheck && !legalMoves.Any();
    }

    private class MovePopper : IDisposable
    {
        private readonly BoardState _board;
        private readonly long _initialBoardHash;
        private readonly Move _move;
        private readonly MoveUndoInfo _moveUndoInfo;

        public MovePopper(BoardState board, long initialBoardHash)
        {
            _board = board;
            _initialBoardHash = initialBoardHash;
            var (move, moveUndoInfo) = board._moveStack.Peek();
            _move = move;
            _moveUndoInfo = moveUndoInfo;
        }

        public void Dispose()
        {
            if (!_board._moveStack.TryPeek(out var t))
            {
                throw new Exception("WTF");
            }

            var (move, moveUndoInfo) = t;
            if (!move.Equals(_move) || !moveUndoInfo.Equals(_moveUndoInfo))
            {
                throw new Exception("WTF");
            }
            _board.TryPop();
            if (_board.HashValue != _initialBoardHash)
            {
                throw new Exception("WTF");
            }
        }
    }

    public IDisposable Push(string moveSpec)
    {
        return Push(ToMove(moveSpec));
    }

    public IDisposable Push(Move move)
    {
        var initialBoardHash = HashValue;
        var ownSide = SideToMove == Side.White ? WhiteState : BlackState;
        var enemySide = SideToMove == Side.White ? BlackState : WhiteState;
        var moveUndoInfo = move.Apply(ownSide, enemySide, HalfMoveClock);
        enemySide.ClearEnPassantSquare();
        SideToMove = SideToMove == Side.White ? Side.Black : Side.White;
        if (move.Piece == Piece.Pawn || move.IsCapture)
        {
            HalfMoveClock = 0;
        }
        else
        {
            ++HalfMoveClock;
        }

        if (SideToMove == Side.White)
        {
            ++FullMoveNumber;
        }

        _moveStack.Push(Tuple.Create(move, moveUndoInfo));

        // mutated board state --> invalidate caches
        InvalidateCaches();

        return new MovePopper(this, initialBoardHash);
    }

    public bool TryPop()
    {
        if (_moveStack.TryPop(out var t))
        {
            var (move, moveUndoInfo) = t;
            var ownSide = SideToMove == Side.White ? BlackState : WhiteState;
            var enemySide = SideToMove == Side.White ? WhiteState : BlackState;
            move.Undo(ownSide, enemySide, moveUndoInfo);
            SideToMove = SideToMove == Side.White ? Side.Black : Side.White;
            HalfMoveClock = moveUndoInfo.HalfMoveClock;
            if (SideToMove == Side.Black)
            {
                --FullMoveNumber;
            }

            // mutated board state --> invalidate caches
            InvalidateCaches();

            return true;
        }

        return false;
    }

    private void InvalidateCaches()
    {
        _cachedLegalMoves = Option.None<List<Move>>();
        _cachedPassedPawns = Option.None<BitBoard>();
    }

    public string ToFenString(bool includeMoveClocks = true)
    {
        var sb = new StringBuilder();
        for (var rank = 7; rank >= 0; --rank)
        {
            var blankSquareCount = 0;
            for (var file = 0; file < 8; ++file)
            {
                var coord = new Coord { File = file, Rank = rank };
                var whitePiece = WhiteState.GetPiece(coord);
                var blackPiece = BlackState.GetPiece(coord);
                Debug.Assert(!whitePiece.HasValue || !blackPiece.HasValue, $"Both sides occupying {coord} !?");
                if (!whitePiece.HasValue && !blackPiece.HasValue)
                {
                    ++blankSquareCount;
                    continue;
                }
                if (blankSquareCount > 0)
                {
                    sb.Append(blankSquareCount);
                    blankSquareCount = 0;
                }
                blackPiece.MatchSome(p => sb.Append(p.ToFenChar(Side.Black)));
                whitePiece.MatchSome(p => sb.Append(p.ToFenChar(Side.White)));
            }

            if (blankSquareCount > 0)
            {
                sb.Append(blankSquareCount);
            }

            if (rank > 0)
            {
                sb.Append('/');
            }
        }

        sb.Append(SideToMove == Side.White ? " w " : " b ");
        if (!WhiteState.CanCastle && !BlackState.CanCastle)
        {
            sb.Append("-");
        }
        else
        {
            if (WhiteState.CanCastleShort)
            {
                sb.Append('K');
            }
            if (WhiteState.CanCastleLong)
            {
                sb.Append('Q');
            }
            if (BlackState.CanCastleShort)
            {
                sb.Append('k');
            }
            if (BlackState.CanCastleLong)
            {
                sb.Append('q');
            }
        }

        var enPassantSquare = SideToMove == Side.White
            ? BlackState.EnPassantSquare
            : WhiteState.EnPassantSquare;
        sb.Append(enPassantSquare.Match(s => $" {s}", () => " -"));

        if (includeMoveClocks)
        {
            sb.Append($" {HalfMoveClock} {FullMoveNumber}");
        }

        return sb.ToString();
    }

    public IEnumerable<(Side Side, Piece Piece, Coord Coord)> GetAllPieces()
    {
        foreach (var (whitePiece, whitePieceCoord) in WhiteState.GetPieces())
        {
            yield return (Side.White, whitePiece, whitePieceCoord);
        }
        foreach (var (blackPiece, blackPieceCoord) in BlackState.GetPieces())
        {
            yield return (Side.Black, blackPiece, blackPieceCoord);
        }
    }

    public Option<Piece> GetPiece(Side side, Coord coord)
    {
        return side switch
        {
            Side.White => WhiteState.GetPiece(coord),
            Side.Black => BlackState.GetPiece(coord),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public bool IsOccupied(Coord coord) => WhiteState.AllPieces.IsOccupied(coord) ||
                                           BlackState.AllPieces.IsOccupied(coord);

    public bool IsPassedPawn(Coord coord) => GetPassedPawns().IsOccupied(coord);

    public BitBoard GetPassedPawns()
    {
        return _cachedPassedPawns.Match(
            passedPawns => passedPawns,
            () =>
            {
                var passedPawns = CalculatePassedPawns(Side.White) | CalculatePassedPawns(Side.Black);
                _cachedPassedPawns = Option.Some(passedPawns);
                return passedPawns;
            });
    }

    private BitBoard CalculatePassedPawns(Side side)
    {
        var ownSide = side == Side.White ? WhiteState : BlackState;
        var enemySide = side == Side.White ? BlackState : WhiteState;
        var ownPawns = ownSide.Pawns;
        var enemyPawns = enemySide.Pawns;
        BitBoard ownPassedPawns = 0;
        foreach (var ownPawn in ownPawns)
        {
            var fileMask = BitBoard.File(ownPawn.File);
            if (ownPawn.File > 0) fileMask |= BitBoard.File(ownPawn.File - 1);
            if (ownPawn.File < 7) fileMask |= BitBoard.File(ownPawn.File + 1);
            var aheadMask = BitBoard.AheadOfRank(side, ownPawn.Rank);
            var mask = fileMask & aheadMask & enemyPawns;
            if (mask == 0)
            {
                ownPassedPawns |= ownPawn;
            }
        }
        return ownPassedPawns;
    }

    public long HashValue => ZobristHash.Of(this);
}
