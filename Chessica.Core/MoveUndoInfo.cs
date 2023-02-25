using Optional;

namespace Chessica.Core;

public record MoveUndoInfo(
    bool OwnSideCanCastleLong,
    bool OwnSideCanCastleShort,
    bool EnemySideCanCastleLong,
    bool EnemySideCanCastleShort,
    Option<Coord> OwnSideEnPassantSquare,
    Option<Coord> EnemySideEnPassantSquare,
    Option<Piece> CapturedPiece,
    int HalfMoveClock);
