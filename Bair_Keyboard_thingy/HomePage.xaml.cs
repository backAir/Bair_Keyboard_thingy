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
        private Dictionary<string, QMK_API.QMK_HID> keyboards = new();

        private ContextMenuStrip _trayMenu = new ContextMenuStrip();
        private NotifyIcon _notifyIcon = new NotifyIcon
            {
                BalloonTipText = @"Hello, NotifyIcon!",
                Text = @"Hello, NotifyIcon!",
                Icon = new Icon("NotifyIcon.ico"),
                Visible = true,
            };


        public void Add_Keyboard(int vendorID, int productID, string name, int layer_count)
        {
            QMK_API.QMK_HID keyboard = new(vendorID, productID, name, layer_count);
            keyboard.MessageReceived += OnMessageReceived;
            keyboards.Add(name, keyboard);
        }

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing!; // event for when we close the window
            //this.Hide();

            //Add_Keyboard(0x7C92, 0x0001, "pedal", 4);
            //Add_Keyboard(0x7C92, 0x0002, "trippel_pedal", 4);
            //Add_Keyboard(0x7C92, 0x0003, "numpad", 4);
            LoadConfig();

            // Optional: double-click on tray icon to restore window
            _notifyIcon.DoubleClick += (s, e) =>
            {
                ShowWindow();
            };
            
            _notifyIcon.BalloonTipTitle = "Bair's Keyboard thingy";
            _notifyIcon.ContextMenuStrip = _trayMenu;
            _trayMenu.Items.Add("Show", null, (s, e) => ShowWindow());

            Add_Settings_Menus(_trayMenu);
            // Add the submenu to the tray context menu
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            Setup_Dropdown();

            //SaveConfig();
        }

        private void LoadConfig()
        {
            var data = Config_File.Config.LoadSave();
            foreach (var keyboard in data.Keyboards)
            {
                Add_Keyboard(keyboard.VendorID, keyboard.ProductID, keyboard.Name, keyboard.LayerCount);
            }
        }
        private void SaveConfig()
        {
            List <Config_File.KeyboardInfo> keyboardInfoList = new();
            foreach (var keyboard in keyboards.Values)
            {
                var keyboard_enabled = true;
                keyboardInfoList.Add(new Config_File.KeyboardInfo(keyboard._vendorId, keyboard._productId, keyboard.name, keyboard.layer_count, keyboard_enabled));
            }

            Config_File.ConfigSave config = new(keyboardInfoList);
            Config_File.Config.MakeSave(config);
        }

        private void Add_Settings_Menus(ContextMenuStrip trayMenu)
        {
            foreach (var keyboard in keyboards)
            {
                var settingsMenu = new ToolStripMenuItem(keyboard.Key);
                for (global::System.Int32 i = 0; i < keyboard.Value.layer_count; i++)
                {
                    int j = i;
                    settingsMenu.DropDownItems.Add($"Layer {i}", null, (s, e) => ChangeLayer(keyboard.Value, j));
                }
                trayMenu.Items.Add(settingsMenu);
            }
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
            //message[2] = (byte)(HelloButton.IsChecked == true? 1 : 2);


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
            Debug.WriteLine("layer: "+layer);
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

        private void LaunchProgram(byte programID)
        {
            switch (programID)
            {
                case 1:
                    Process.Start("C:\\Users\\tonyl\\Documents\\programs\\Gaming\\PrismLauncher\\prismlauncher.exe");
                    break;
                case 2:
                    ProcessStartInfo startInfo = new ProcessStartInfo()
                    {
                        FileName = @"C:\Users\tonyl\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Discord Inc\Discord",
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    break;
            }
        }


        private void Setup_Dropdown()
        {
            KeyboardDropdown.Items.Clear();
            KeyboardDropdown.Items.Add("Pedal");
            KeyboardDropdown.Items.Add("TrippelPedal");
        }
        private void KeyboardDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }





        // Drag + double-click maximize
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }
}