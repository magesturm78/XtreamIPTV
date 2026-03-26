using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using XtreamIPTV.ViewModels;

namespace XtreamIPTV.Views
{
    /// <summary>
    /// Interaction logic for HTMLPlayerView.xaml
    /// </summary>
    public partial class HTMLPlayerView : UserControl
    {
        private readonly DispatcherTimer _timer = new();
        private PlayerViewModel? VM => DataContext as PlayerViewModel;

        public HTMLPlayerView()
        {
            InitializeComponent();
            InitializeWebView();
            Unloaded += (_, _) =>
            {
                webView?.CoreWebView2?.NavigateToString("<HTML></HTML>");
            };
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async(null);

            webView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested1; ;
        }

        private void CoreWebView2_NewWindowRequested1(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
        }

    }
}
