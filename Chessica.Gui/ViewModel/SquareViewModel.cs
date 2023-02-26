using System.Windows;
using System.Windows.Media;
using Chessica.Core;

namespace Chessica.Gui.ViewModel;

public class SquareViewModel
{
    private readonly bool _boardInverted;
    private readonly bool _isSelected;
    private readonly bool _isPotentialMove;
    private readonly bool _isPotentialCapture;

    public SquareViewModel(Coord coord, bool boardInverted, bool isSelected, bool isPotentialMove, bool isPotentialCapture)
    {
        _boardInverted = boardInverted;
        _isSelected = isSelected;
        _isPotentialMove = isPotentialMove;
        _isPotentialCapture = isPotentialCapture;
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

    public double Opacity => _isSelected ? 0.5 : 1.0;

    public Visibility PotentialMoveHighlighting => _isPotentialMove ? Visibility.Visible : Visibility.Hidden;

    public Visibility PotentialCaptureHighlighting => _isPotentialCapture ? Visibility.Visible : Visibility.Hidden;
}
