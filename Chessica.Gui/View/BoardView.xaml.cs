using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using Chessica.Core;
using Chessica.Gui.ViewModel;
using Image = System.Windows.Controls.Image;

namespace Chessica.Gui.View;

public partial class BoardView
{
    public BoardViewModel BoardViewModel { get; } = new();

    private Point _pieceDragStartPoint;

    public BoardView()
    {
        InitializeComponent();
        DataContext = BoardViewModel;
    }

    private const string DragDropFormatString = "Chessica::PieceDragDrop";

    private void PieceImage_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (sender is not Image { DataContext: PieceViewModel clickedPiece }) return;
        {
            if (clickedPiece.Side == BoardViewModel.UserSide)
            {
                if (clickedPiece == BoardViewModel.SelectedPiece)
                {
                    BoardViewModel.SelectedPiece = null;
                    return;
                }

                BoardViewModel.SelectedPiece = clickedPiece;
            }
            else if (BoardViewModel.SelectedPiece != null)
            {
                var possibleTargetCoords = BoardViewModel.GetLegalTargetCoords(BoardViewModel.SelectedPiece).ToArray();
                if (possibleTargetCoords.Contains(clickedPiece.Coord))
                {
                    // capture
                    BoardViewModel.MovePiece(BoardViewModel.SelectedPiece, clickedPiece.Coord);
                }
                BoardViewModel.SelectedPiece = null;
            }
        }
    }

    private void PieceImage_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _pieceDragStartPoint = e.GetPosition(null!);
    }

    private void PieceImage_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (sender is not Image { DataContext: PieceViewModel sourcePiece } image) return;
        var diff = e.GetPosition(null!) - _pieceDragStartPoint;
        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            BoardViewModel.SelectedPiece = null;
            var possibleDropCoords = BoardViewModel.GetLegalTargetCoords(sourcePiece).ToArray();
            if (possibleDropCoords.Any())
            {
                BoardViewModel.SelectedPiece = sourcePiece;
                var dragData = new DataObject(DragDropFormatString, (sourcePiece, possibleDropCoords));
                DragDrop.DoDragDrop(image, dragData, DragDropEffects.Move);
            }
        }
    }

    private void PieceImage_OnDragEnter(object sender, DragEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (sender is not Image { DataContext: PieceViewModel targetPiece }) return;
        if (!e.Data.GetDataPresent(DragDropFormatString) ||
            e.Data.GetData(DragDropFormatString) is not (PieceViewModel, Coord[] possibleDropCoords) ||
            !possibleDropCoords.Contains(targetPiece.Coord))
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void PieceImage_OnDrop(object sender, DragEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (sender is not Image { DataContext: PieceViewModel targetPiece }) return;
        if (e.Data.GetDataPresent(DragDropFormatString) &&
            e.Data.GetData(DragDropFormatString) is (PieceViewModel sourcePiece, Coord[] possibleDropCoords))
        {
            if (possibleDropCoords.Contains(targetPiece.Coord))
            {
                // capture
                BoardViewModel.MovePiece(sourcePiece, targetPiece.Coord);
            }
        }
    }

    private void SquaresGrid_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (BoardViewModel.SelectedPiece != null &&
            e.OriginalSource is Rectangle { DataContext: SquareViewModel targetSquare })
        {
            var possibleTargetCoords = BoardViewModel.GetLegalTargetCoords(BoardViewModel.SelectedPiece).ToArray();
            if (possibleTargetCoords.Contains(targetSquare.Coord))
            {
                BoardViewModel.MovePiece(BoardViewModel.SelectedPiece, targetSquare.Coord);
            }
            BoardViewModel.SelectedPiece = null;
        }
    }

    private void SquaresGrid_OnDragEnter(object sender, DragEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (e.OriginalSource is not Rectangle { DataContext: SquareViewModel targetSquare } ||
            !e.Data.GetDataPresent(DragDropFormatString) ||
            e.Data.GetData(DragDropFormatString) is not (PieceViewModel, Coord[] possibleDropCoords) ||
            !possibleDropCoords.Contains(targetSquare.Coord))
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void SquaresGrid_OnDrop(object sender, DragEventArgs e)
    {
        if (!BoardViewModel.IsUsersMove) return;
        if (e.Data.GetDataPresent(DragDropFormatString) &&
            e.Data.GetData(DragDropFormatString) is (PieceViewModel sourcePiece, Coord[] possibleDropCoords))
        {
            if (e.OriginalSource is Rectangle { DataContext: SquareViewModel targetSquare } &&
                possibleDropCoords.Contains(targetSquare.Coord))
            {
                BoardViewModel.MovePiece(sourcePiece, targetSquare.Coord);
            }
        }
    }
}