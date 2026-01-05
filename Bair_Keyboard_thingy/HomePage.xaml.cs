using System.Drawing;
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

using System.Drawing;
using System.Windows.Forms;

namespace Bair_Keyboard_thingy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ContextMenuStrip _trayMenu = new ContextMenuStrip();
        private NotifyIcon _notifyIcon = new NotifyIcon
            {
                BalloonTipText = @"Hello, NotifyIcon!",
                Text = @"Hello, NotifyIcon!",
                Icon = new Icon("NotifyIcon.ico"),
                Visible = true,
            };

        public MainWindow()
        {
            InitializeComponent();

            this.Closing += MainWindow_Closing!; // event for when we close the window

            // Optional: double-click on tray icon to restore window
            _notifyIcon.DoubleClick += (s, e) =>
            {
                ShowWindow();
            };
            _notifyIcon.BalloonTipTitle = "Bair Keyboard thingy";
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());


        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cancel the close and hide the window instead
            e.Cancel = true;
            this.Hide();

            // Optional: show balloon tip when minimized to tray
            _notifyIcon.ShowBalloonTip(1000, "Bair Keyboard thingy", "Application minimized to tray.", ToolTipIcon.Info);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //if (HelloButton.IsChecked == true)
            //{
            //    MessageBox.Show("Hello.");
            //}
            //else if (GoodbyeButton.IsChecked == true)
            //{
            //    MessageBox.Show("Goodbye.");
            //}
            _notifyIcon.ShowBalloonTip(1000);
        }

        private void ExitApplication()
        {
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }
    }
}