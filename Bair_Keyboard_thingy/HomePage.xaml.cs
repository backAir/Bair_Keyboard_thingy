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
using System.Diagnostics;

namespace Bair_Keyboard_thingy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private QMK_API.QMK_HID trippel_pedal;
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
            trippel_pedal = new(0x7C92, 0x0002);
            trippel_pedal.MessageReceived += OnMessageReceived;


            this.Closing += MainWindow_Closing!; // event for when we close the window

            // Optional: double-click on tray icon to restore window
            _notifyIcon.DoubleClick += (s, e) =>
            {
                ShowWindow();
            };
            _notifyIcon.BalloonTipTitle = "Bair's Keyboard thingy";
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            

            var settingsMenu = new ToolStripMenuItem("TrippelPedal");

            // Add "dropdown items" to it
            settingsMenu.DropDownItems.Add("Layer 0", null, (s, e) => ChangeLayer(trippel_pedal,0));
            settingsMenu.DropDownItems.Add("Layer 1", null, (s, e) => ChangeLayer(trippel_pedal, 1));
            settingsMenu.DropDownItems.Add("Layer 2", null, (s, e) => ChangeLayer(trippel_pedal, 2));
            settingsMenu.DropDownItems.Add("Layer 3", null, (s, e) => ChangeLayer(trippel_pedal, 3));

            // Add the submenu to the tray context menu
            _trayMenu.Items.Add(settingsMenu);
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


        private enum PC_TO_HID_IDs: byte
        {
            LAYER_CHANGE = 20
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var message = new byte[33];

            message[0] = 1;
            //if (HelloButton.IsChecked == true)
            //{
            //    message[1] = 0xAA;
            //}
            //else
            //{
            //    message[1] = 0xAB;
            //}

            //! fucked up id's: 01, 02, 04, 0C, 0D, 0E, 11, 12
            message[1] = ((byte)PC_TO_HID_IDs.LAYER_CHANGE);
            message[2] = (byte)(HelloButton.IsChecked == true? 1 : 2);

            trippel_pedal.SendAsync(message);

            //if (HelloButton.IsChecked == true)
            //{
            //    MessageBox.Show("Hello.");
            //}
            //else if (GoodbyeButton.IsChecked == true)
            //{
            //    MessageBox.Show("Goodbye.");
            //}
            //_notifyIcon.ShowBalloonTip(1000);
        }


        private void ChangeLayer(QMK_API.QMK_HID keyboard, int layer)
        {
            var message = new byte[33];

            message[0] = 1;
            //! fucked up id's: 01, 02, 04, 0C, 0D, 0E, 11, 12
            message[1] = ((byte)PC_TO_HID_IDs.LAYER_CHANGE);
            message[2] = (byte)(layer);
            keyboard.SendAsync(message);
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

        void OnMessageReceived(object? sender, byte[] message)
        {
            // Handle the message
            Debug.WriteLine(
                $"Received {message.Length} bytes: {string.Join(" ", message)}"
            );
            if (message[1] == 21)
            {
                LaunchProgram(message[2]);
            }
        }

        private void LaunchProgram(int programID)
        {
            Debug.Print("launching prism launcher");
            Process.Start("C:\\PATH\\TO\\PrismLauncher\\prismlauncher.exe");
        }
    }
}