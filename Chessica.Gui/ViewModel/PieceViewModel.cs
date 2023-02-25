using Chessica.Core;

namespace Chessica.Gui.ViewModel;

public class PieceViewModel
{
    public Side Side { get; }

    public Piece Piece { get; }

    public Coord Coord { get; }

    private readonly bool _boardInverted;

    public PieceViewModel(Side side, Piece piece, Coord coord, bool boardInverted)
    {
        Side = side;
        Piece = piece;
        Coord = coord;
        _boardInverted = boardInverted;
    }

    public int Row => _boardInverted ? Coord.Rank : 7 - Coord.Rank;

    public int Column => _boardInverted ? 7 - Coord.File : Coord.File;

    public int Margin => Piece == Piece.Knight && Side == Side.Black ? 0 : 10;

    public string ImageResourceKey => $"{Side}{Piece}DrawingImage";
}