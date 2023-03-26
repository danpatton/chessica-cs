using System.Diagnostics;
using Optional;

namespace Chessica.Core;

public class SideState
{
    public Side Side { get; }

    public BitBoard King;
    public BitBoard Queens;
    public BitBoard Rooks;
    public BitBoard Knights;
    public BitBoard Bishops;
    public BitBoard Pawns;

    private bool _canCastleLong;
    public bool CanCastleLong
    {
        get => _canCastleLong;
        private set
        {
            if (value != _canCastleLong)
            {
                _hash.FlipLongCastling(Side);
            }

            _canCastleLong = value;
        }
    }

    private bool _canCastleShort;

    public bool CanCastleShort
    {
        get => _canCastleShort;
        private set
        {
            if (value != _canCastleShort)
            {
                _hash.FlipShortCastling(Side);
            }

            _canCastleShort = value;
        }
    }

    private Option<Coord> _enPassantSquare = Option.None<Coord>();

    private readonly ZobristHash _hash = new();

    public SideState(Side side, bool canCastleLong = true, bool canCastleShort = true)
    {
        Side = side;
        CanCastleLong = canCastleLong;
        CanCastleShort = canCastleShort;
    }

    public SideState Clone()
    {
        var clone = new SideState(Side, CanCastleLong, CanCastleShort)
        {
            King = King,
            Queens = Queens,
            Rooks = Rooks,
            Knights = Knights,
            Bishops = Bishops,
            Pawns = Pawns
        };
        _enPassantSquare.MatchSome(clone.SetEnPassantSquare);
        clone._hash.SetFrom(_hash);
        return clone;
    }

    public long HashValue => _hash.Value;

    public bool CanCastle => CanCastleLong || CanCastleShort;

    public bool IsEnPassantSquare(Coord coord) => _enPassantSquare.Match(c => c.Equals(coord), () => false);

    public void SetEnPassantSquare(Coord coord)
    {
        _enPassantSquare.MatchSome(oldCoord => _hash.FlipEnPassantFile(oldCoord.File));
        _enPassantSquare = Option.Some(coord);
        _hash.FlipEnPassantFile(coord.File);
    }

    public Option<Coord> EnPassantSquare => _enPassantSquare;

    public void ClearEnPassantSquare()
    {
        _enPassantSquare.MatchSome(c =>
        {
            _hash.FlipEnPassantFile(c.File);
            _enPassantSquare = Option.None<Coord>();
        });
    }

    public void SetCastlingRights(bool canCastleLong, bool canCastleShort)
    {
        CanCastleLong = canCastleLong;
        CanCastleShort = canCastleShort;
    }

    public double GetSimpleMaterialScore()
    {
        return King.Count * Piece.King.Value() +
               Queens.Count * Piece.Queen.Value()
               + Rooks.Count * Piece.Rook.Value()
               + Knights.Count * Piece.Knight.Value()
               + Bishops.Count * Piece.Bishop.Value()
               + Pawns.Count * Piece.Pawn.Value();
    }

    public void ApplyMove(Piece pieceType, Coord from, Coord to)
    {
        switch (pieceType)
        {
            case Piece.King:
                King.Move(from, to);
                _hash.MovePiece(Side, Piece.King, from, to);
                CanCastleShort = false;
                CanCastleLong = false;
                break;
            case Piece.Queen:
                Queens.Move(from, to);
                _hash.MovePiece(Side, Piece.Queen, from, to);
                break;
            case Piece.Rook:
                Rooks.Move(from, to);
                _hash.MovePiece(Side, Piece.Rook, from, to);
                var startingRank = Side == Side.White ? 0 : 7;
                if (from.Rank == startingRank)
                {
                    switch (from.File)
                    {
                        case 7:
                            CanCastleShort = false;
                            break;
                        case 0:
                            CanCastleLong = false;
                            break;
                    }
                }
                break;
            case Piece.Knight:
                Knights.Move(from, to);
                _hash.MovePiece(Side, Piece.Knight, from, to);
                break;
            case Piece.Bishop:
                Bishops.Move(from, to);
                _hash.MovePiece(Side, Piece.Bishop, from, to);
                break;
            case Piece.Pawn:
                Pawns.Move(from, to);
                _hash.MovePiece(Side, Piece.Pawn, from, to);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void UndoMove(Piece pieceType, Coord from, Coord to)
    {
        switch (pieceType)
        {
            case Piece.King:
                King.Move(to, from);
                _hash.MovePiece(Side, Piece.King, to, from);
                break;
            case Piece.Queen:
                Queens.Move(to, from);
                _hash.MovePiece(Side, Piece.Queen, to, from);
                break;
            case Piece.Rook:
                Rooks.Move(to, from);
                _hash.MovePiece(Side, Piece.Rook, to, from);
                break;
            case Piece.Knight:
                Knights.Move(to, from);
                _hash.MovePiece(Side, Piece.Knight, to, from);
                break;
            case Piece.Bishop:
                Bishops.Move(to, from);
                _hash.MovePiece(Side, Piece.Bishop, to, from);
                break;
            case Piece.Pawn:
                Pawns.Move(to, from);
                _hash.MovePiece(Side, Piece.Pawn, to, from);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void ApplyPromotion(Coord from, Coord to, Piece promotion)
    {
        RemovePiece(Piece.Pawn, from);
        AddPiece(promotion, to);
    }

    public void UndoPromotion(Coord from, Coord to, Piece promotion)
    {
        RemovePiece(promotion, to);
        AddPiece(Piece.Pawn, from);
    }

    public void ApplyCapture(Coord coord)
    {
        var startingRank = Side == Side.White ? 0 : 7;
        if (Rooks.IsOccupied(coord) && coord.Rank == startingRank)
        {
            switch (coord.File)
            {
                case 7:
                    CanCastleShort = false;
                    break;
                case 0:
                    CanCastleLong = false;
                    break;
            }
        }

        if (Pawns.IsOccupied(coord))
        {
            RemovePiece(Piece.Pawn, coord);
        }
        else if (Bishops.IsOccupied(coord))
        {
            RemovePiece(Piece.Bishop, coord);
        }
        else if (Knights.IsOccupied(coord))
        {
            RemovePiece(Piece.Knight, coord);
        }
        else if (Rooks.IsOccupied(coord))
        {
            RemovePiece(Piece.Rook, coord);
        }
        else if (Queens.IsOccupied(coord))
        {
            RemovePiece(Piece.Queen, coord);
        }
        else if (King.IsOccupied(coord))
        {
            // allow king to be "captured" during search (this should never happen as part of game play)
            RemovePiece(Piece.King, coord);
        }
    }

    public void UndoCapture(Piece piece, Coord coord)
    {
        AddPiece(piece, coord);
    }

    public void ApplyEnPassantCapture(Coord coord)
    {
        var targetPawnRank = Side == Side.White ? 3 : 4;
        var targetPawn = coord with { Rank = targetPawnRank };
        RemovePiece(Piece.Pawn, targetPawn);
    }

    public BitBoard PiecesOfType(Piece pieceType)
    {
        switch (pieceType)
        {
            case Piece.King: return King;
            case Piece.Queen: return Queens;
            case Piece.Rook: return Rooks;
            case Piece.Knight: return Knights;
            case Piece.Bishop: return Bishops;
            case Piece.Pawn: return Pawns;
            default: throw new Exception("WTF");
        }
    }

    public IEnumerable<(Piece Piece, Coord Coord)> GetPieces()
    {
        foreach (var kingCoord in King)
        {
            yield return (Piece.King, kingCoord);
        }
        foreach (var queenCoord in Queens)
        {
            yield return (Piece.Queen, queenCoord);
        }
        foreach (var rookCoord in Rooks)
        {
            yield return (Piece.Rook, rookCoord);
        }
        foreach (var knightCoord in Knights)
        {
            yield return (Piece.Knight, knightCoord);
        }
        foreach (var bishopCoord in Bishops)
        {
            yield return (Piece.Bishop, bishopCoord);
        }
        foreach (var pawnCoord in Pawns)
        {
            yield return (Piece.Pawn, pawnCoord);
        }
    }

    public Option<Piece> GetPiece(Coord coord)
    {
        if (Pawns.IsOccupied(coord))
        {
            var others = Bishops | Knights | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Pawn);
        }
        if (Bishops.IsOccupied(coord))
        {
            var others = Pawns | Knights | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Bishop);
        }
        if (Knights.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Rooks | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Knight);
        }
        if (Rooks.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Queens | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Rook);
        }
        if (Queens.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Rooks | King;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.Queen);
        }
        if (King.IsOccupied(coord))
        {
            var others = Pawns | Bishops | Knights | Rooks | Queens;
            Debug.Assert(!others.IsOccupied(coord), $"Multiple {Side} pieces occupying {coord} !?");
            return Option.Some(Piece.King);
        }
        return Option.None<Piece>();
    }

    public BitBoard AllPieces => King | Queens | Rooks | Knights | Bishops | Pawns;

    public static SideState StartingPosition(Side side)
    {
        switch (side)
        {
            case Side.White:
                return new SideState(side)
                {
                    King = 0x10,
                    Queens = 0x08,
                    Rooks = 0x81,
                    Knights = 0x42,
                    Bishops = 0x24,
                    Pawns = 0xff00
                };
            case Side.Black:
                return new SideState(side)
                {
                    King = 0x1000000000000000,
                    Queens = 0x0800000000000000,
                    Rooks = 0x8100000000000000,
                    Knights = 0x4200000000000000,
                    Bishops = 0x2400000000000000,
                    Pawns = 0xff000000000000
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void AddPiece(Piece piece, Coord coord)
    {
        _hash.FlipPiece(Side, piece, coord);
        switch (piece)
        {
            case Piece.Pawn:
                Pawns |= coord;
                break;
            case Piece.Bishop:
                Bishops |= coord;
                break;
            case Piece.Knight:
                Knights |= coord;
                break;
            case Piece.Rook:
                Rooks |= coord;
                break;
            case Piece.Queen:
                Queens |= coord;
                break;
            case Piece.King:
                King |= coord;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void RemovePiece(Piece piece, Coord coord)
    {
        _hash.FlipPiece(Side, piece, coord);
        switch (piece)
        {
            case Piece.Pawn:
                Pawns &= ~(BitBoard)coord;
                break;
            case Piece.Bishop:
                Bishops &= ~(BitBoard)coord;
                break;
            case Piece.Knight:
                Knights &= ~(BitBoard)coord;
                break;
            case Piece.Rook:
                Rooks &= ~(BitBoard)coord;
                break;
            case Piece.Queen:
                Queens &= ~(BitBoard)coord;
                break;
            case Piece.King:
                King &= ~(BitBoard)coord;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SerialiseTo(BinaryWriter writer)
    {
        writer.Write(King);
        writer.Write(Queens);
        writer.Write(Rooks);
        writer.Write(Knights);
        writer.Write(Bishops);
        writer.Write(Pawns);
        _enPassantSquare.Match(
            coord => writer.Write(coord),
            () => writer.Write(0));
        writer.Write((int)Side);
        writer.Write(CanCastleLong);
        writer.Write(CanCastleShort);
    }
}