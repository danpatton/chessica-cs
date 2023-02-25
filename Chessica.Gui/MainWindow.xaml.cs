using System.Windows;
using System.Windows.Input;
using Chessica.Core;

namespace Chessica.Gui
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = BoardView.BoardViewModel;
        }
        
        private void ExitCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = BoardView.BoardViewModel.CanUndo;
        }

        private void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            BoardView.BoardViewModel.UndoLastFullMove();
        }

        private void MenuNewGame_White_OnClick(object sender, RoutedEventArgs e)
        {
            BoardView.BoardViewModel.NewGame(Side.White);
        }

        private void MenuNewGame_Black_OnClick(object sender, RoutedEventArgs e)
        {
            BoardView.BoardViewModel.NewGame(Side.Black);
        }

        private void MenuCopyPgn_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BoardView.BoardViewModel.PgnMoveHistory);
        }

        private void MenuCopyFen_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(BoardView.BoardViewModel.FenBoardState);
        }
    }
}