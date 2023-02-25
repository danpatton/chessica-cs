using System.Windows.Media;
using Chessica.Core;

namespace Chessica.Gui.ViewModel;

public class SquareViewModel
{
    private readonly bool _boardInverted;
    private readonly bool _isSelected;
    private readonly bool _isHighlighted;

    public SquareViewModel(Coord coord, bool boardInverted, bool isSelected, bool isHighlighted)
    {
        _boardInverted = boardInverted;
        _isSelected = isSelected;
        _isHighlighted = isHighlighted;
        Coord = coord;
    }

    public Coord Coord { get; }

    public int Row => _boardInverted ? Coord.Rank : 7 - Coord.Rank;

    public int Column => _boardInverted ? 7 - Coord.File : Coord.File;

    public Brush Fill => Coord.IsDarkSquare ? Brushes.DarkOliveGreen : Brushes.LightGreen;
    
    public Brush Foreground => Coord.IsDarkSquare ? Brushes.LightGreen : Brushes.DarkOliveGreen;

    public string FileIndicator => Coord.Rank == (_boardInverted ? 7 : 0)
        ? Coord.FileChar.ToString()
        : string.Empty;

    public string RankIndicator => Coord.File == (_boardInverted ? 0 : 7)
        ? Coord.RankChar.ToString()
        : string.Empty;

    public Brush? Stroke => _isHighlighted ? Brushes.Blue : null;

    public double Opacity => _isSelected ? 0.5 : _isHighlighted ? 0.75 : 1.0;
}
