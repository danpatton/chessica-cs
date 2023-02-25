using System.Windows;
using Chessica.Core;

namespace Chessica.Gui.View;

public partial class PromotionChoiceWindow
{
    private readonly Side _userSide;

    public PromotionChoiceWindow(Side userSide)
    {
        _userSide = userSide;
        InitializeComponent();
        DataContext = this;
    }

    public string QueenImageResourceKey => $"{_userSide}QueenDrawingImage";
    public string RookImageResourceKey => $"{_userSide}RookDrawingImage";
    public string KnightImageResourceKey => $"{_userSide}KnightDrawingImage";
    public string BishopImageResourceKey => $"{_userSide}BishopDrawingImage";

    public Piece? SelectedPiece { get; set; }

    private void Queen_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedPiece = Piece.Queen;
        Close();
    }
    
    private void Rook_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedPiece = Piece.Rook;
        Close();
    }
    
    private void Knight_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedPiece = Piece.Knight;
        Close();
    }
    
    private void Bishop_OnClick(object sender, RoutedEventArgs e)
    {
        SelectedPiece = Piece.Bishop;
        Close();
    }
}