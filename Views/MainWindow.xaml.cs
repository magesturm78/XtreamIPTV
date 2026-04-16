using System.Windows;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Get mouse position relative to the window (this)
            Point mousePos = e.GetPosition(this);

            // Check if mouse is on the left side (X < half of width)
            if (mousePos.X < 25 && mousePos.Y < 200)
            {
                MainViewModel.Instance.ShowSideNav();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space)
            {
                if (MainViewModel.Instance.CurrentView is PlayerView pv)
                {
                    pv.PlayButton_Click(null, null);
                }
                
            }
        }
    }
}