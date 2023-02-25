using System.Windows;
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

        private void MenuNewGame_White_OnClick(object sender, RoutedEventArgs e)
        {
            BoardView.BoardViewModel.NewGame(Side.White);
        }

        private void MenuNewGame_Black_OnClick(object sender, RoutedEventArgs e)
        {
            BoardView.BoardViewModel.NewGame(Side.Black);
        }

        private void MenuExit_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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