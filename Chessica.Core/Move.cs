using Optional;

namespace Chessica.Core;

public class Move
{
    public Piece Piece { get; }
    public Coord From { get; }
    public Coord To { get; }
    public bool IsCapture { get; }

    public Move(Piece piece, Coord from, Coord to, bool isCapture = false)
    {
        Piece = piece;
        From = from;
        To = to;
        IsCapture = isCapture;
    }

    public virtual MoveUndoInfo Apply(SideState ownSide, SideState enemySide, int halfMoveClock)
    {
        var isEnPassantCapture = IsCapture && Piece == Piece.Pawn && enemySide.IsEnPassantSquare(To);

        var moveUndoInfo = new MoveUndoInfo(
            ownSide.CanCastleLong,
            ownSide.CanCastleShort,
            enemySide.CanCastleLong,
            enemySide.CanCastleShort,
            ownSide.EnPassantSquare,
            enemySide.EnPassantSquare,
            isEnPassantCapture ? Option.Some(Piece.Pawn) : IsCapture ? enemySide.GetPiece(To) : Option.None<Piece>(),
            halfMoveClock);

        ownSide.ClearEnPassantSquare();
        ownSide.ApplyMove(Piece, From, To);

        if (isEnPassantCapture)
        {
            enemySide.ApplyEnPassantCapture(To);
        }
        else if (IsCapture)
        {
            enemySide.ApplyCapture(To);
        }

        if (Piece == Piece.Pawn && !IsCapture)
        {
            if (ownSide.Side == Side.White && From.Rank == 1 && To.Rank == 3)
            {
                ownSide.SetEnPassantSquare(From with { Rank = 2 });
            }
            else if (ownSide.Side == Side.Black && From.Rank == 6 && To.Rank == 4)
            {
                ownSide.SetEnPassantSquare(From with { Rank = 5 });
            }
        }

        return moveUndoInfo;
    }

    public virtual void Undo(SideState ownSide, SideState enemySide, MoveUndoInfo moveUndoInfo)
    {
        ownSide.UndoMove(Piece, From, To);
        moveUndoInfo.CapturedPiece.MatchSome(capturedPiece =>
        {
            var squareOfCapturedPiece = Piece == Piece.Pawn
                ? moveUndoInfo.EnemySideEnPassantSquare.Match(
                    x => x == To ? To with { Rank = (byte)(To.Rank == 5 ? 4 : 3) } : To,
                    () => To)
                : To;
            enemySide.UndoCapture(capturedPiece, squareOfCapturedPiece);
        });
        moveUndoInfo.OwnSideEnPassantSquare.Match(ownSide.SetEnPassantSquare, ownSide.ClearEnPassantSquare);
        ownSide.SetCastlingRights(moveUndoInfo.OwnSideCanCastleLong, moveUndoInfo.OwnSideCanCastleShort);
        enemySide.SetCastlingRights(moveUndoInfo.EnemySideCanCastleLong, moveUndoInfo.EnemySideCanCastleShort);
    }

    public virtual double PositionalNudge(BoardState board)
    {
        var backRank = board.SideToMove == Side.White ? 0 : 7;
        if (board.FullMoveNumber < 10)
        {
            switch (Piece)
            {
                case Piece.Pawn:
                    var startingRank = board.SideToMove == Side.White ? 1 : 6;
                    var pushTwoRank = board.SideToMove == Side.White ? 3 : 4;
                    // "control the centre"
                    if (From.Rank == startingRank && To.Rank == pushTwoRank && (From.File == 3 || From.File == 4))
                    {
                        return 0.2;
                    }
                    break;
                case Piece.Knight:
                    return To.File == 0 || To.File == 7
                        // "a knight on the rim is dim"
                        ? -0.1
                        // "develop minor pieces"; "knights before bishops"
                        : From.Rank == backRank
                            ? 0.15
                            : 0d;
                case Piece.Bishop:
                    // "develop minor pieces"
                    return From.Rank == backRank ? 0.1 : 0d;
                case Piece.Queen:
                case Piece.Rook:
                    // avoid needlessly moving queen or rooks early
                    return -0.1;
                case Piece.King:
                    // avoid needlessly moving king early (if not castling)
                    return -0.2;
            }
        }
        else if (board.FullMoveNumber < 25)
        {
            switch (Piece)
            {
                case Piece.King:
                    // avoid pointless king moves in the middlegame
                    return -0.1;
                case Piece.Rook:
                    // "rooks on the seventh"
                    var seventh = board.SideToMove == Side.White ? 6 : 1;
                    return To.Rank == seventh ? 0.3 : 0d;
            }
        }
        
        // TODO: push passed pawns / get a rook behind a passed pawn

        return 0d;
    }

    public override string ToString()
    {
        return Piece.Ascii() + From + (IsCapture ? "x" : "") + To;
    }

    public virtual string ToPgnSpec(BoardState board)
    {
        var pieceSpec = Piece.Ascii();
        var fromSpec = Piece == Piece.King
            ? ""
            : Piece == Piece.Pawn && IsCapture
                ? From.FileChar.ToString()
                : board.GetPgnCoordDisambiguation(Piece, From, To);
        var captureSpec = IsCapture ? "x" : "";
        var toSpec = To.ToString();
        return string.Concat(pieceSpec, fromSpec, captureSpec, toSpec);
    }

    public virtual bool Equals(Move? other)
    {
        return other != null &&
               other.From == From &&
               other.To == To &&
               other.IsCapture == IsCapture;
    }

    public override bool Equals(object? obj)
    {
        return Equals((Move?)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Piece, From, To, IsCapture);
    }
}

public class CastlingMove : Move
{
    public CastlingMove(Coord from, Coord to)
        : base(Piece.King, from, to)
    {
    }

    public Move RookMove
    {
        get
        {
            var rookFrom = To.File == 6 ? From with { File = 7 } : From with { File = 0 };
            var rookTo = To.File == 6 ? From with { File = 5 } : From with { File = 3 };
            return new Move(Piece.Rook, rookFrom, rookTo);
        }
    }

    public override MoveUndoInfo Apply(SideState ownSide, SideState enemySide, int halfMoveClock)
    {
        var moveUndoInfo = base.Apply(ownSide, enemySide, halfMoveClock);
        ownSide.ApplyMove(Piece.Rook, RookMove.From, RookMove.To);
        return moveUndoInfo;
    }

    public override void Undo(SideState ownSide, SideState enemySide, MoveUndoInfo moveUndoInfo)
    {
        ownSide.UndoMove(Piece.Rook, RookMove.From, RookMove.To);
        base.Undo(ownSide, enemySide, moveUndoInfo);
    }

    public override double PositionalNudge(BoardState board)
    {
        // "castle within the first 10 moves"
        return 0.05 * Math.Min(10, board.FullMoveNumber);
    }

    public override string ToPgnSpec(BoardState board)
    {
        return To.File == 6 ? "O-O" : "O-O-O";
    }

    public override bool Equals(Move? other)
    {
        return other is CastlingMove castlingMove &&
               castlingMove.From == From &&
               castlingMove.To == To &&
               castlingMove.RookMove.Equals(RookMove);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Piece, From, To, IsCapture, RookMove);
    }
}

public class PromotionMove : Move
{
    public Piece Promotion { get; }

    public PromotionMove(Coord from, Coord to, bool isCapture, Piece promotion)
        : base(Piece.Pawn, from, to, isCapture)
    {
        Promotion = promotion;
    }

    public override MoveUndoInfo Apply(SideState ownSide, SideState enemySide, int halfMoveClock)
    {
        var moveUndoInfo = new MoveUndoInfo(
            ownSide.CanCastleLong,
            ownSide.CanCastleShort,
            enemySide.CanCastleLong,
            enemySide.CanCastleShort,
            ownSide.EnPassantSquare,
            enemySide.EnPassantSquare,
            IsCapture ? enemySide.GetPiece(To) : Option.None<Piece>(),
            halfMoveClock);

        ownSide.ClearEnPassantSquare();
        ownSide.Pawns &= ~(BitBoard)From;
        ownSide.ApplyPromotion(From, To, Promotion);
        if (IsCapture)
        {
            enemySide.ApplyCapture(To);
        }

        return moveUndoInfo;
    }

    public override void Undo(SideState ownSide, SideState enemySide, MoveUndoInfo moveUndoInfo)
    {
        ownSide.UndoPromotion(From, To, Promotion);
        moveUndoInfo.CapturedPiece.MatchSome(capturedPiece =>
        {
            enemySide.UndoCapture(capturedPiece, To);
        });
        moveUndoInfo.OwnSideEnPassantSquare.Match(ownSide.SetEnPassantSquare, ownSide.ClearEnPassantSquare);
        ownSide.SetCastlingRights(moveUndoInfo.OwnSideCanCastleLong, moveUndoInfo.OwnSideCanCastleShort);
        enemySide.SetCastlingRights(moveUndoInfo.EnemySideCanCastleLong, moveUndoInfo.EnemySideCanCastleShort);
    }

    public override string ToPgnSpec(BoardState board)
    {
        var fromSpec = board.GetPgnCoordDisambiguation(Piece, From, To);
        var captureSpec = IsCapture ? "x" : "";
        var toSpec = To.ToString();
        var promotionSpec = Promotion.Ascii();
        return string.Concat(fromSpec, captureSpec, toSpec, "=", promotionSpec);
    }

    public override bool Equals(Move? other)
    {
        return other is PromotionMove promotionMove &&
               promotionMove.From == From &&
               promotionMove.To == To &&
               promotionMove.Promotion == Promotion;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Piece, From, To, IsCapture, (int)Promotion);
    }
}