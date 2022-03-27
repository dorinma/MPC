using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Net;
using System.IO;

using MPC_UI.ViewModel;

namespace MPC_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainDataContext mainDataContext;
        MPCDataClient.ManagerDataClient managerDataClient;

        public MainWindow()
        {
            InitializeComponent();

            mainDataContext = new MainDataContext();
            DataContext = mainDataContext;

            managerDataClient = new MPCDataClient.ManagerDataClient();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Csv files (*.csv)|*.csv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                inFile.Text = openFileDialog.FileName;
            if (Path.GetExtension(openFileDialog.FileName) != ".csv")
            {
                MessageBox.Show("Please choose .csv file.");
                inFile.Text = "";
            }
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {

            }
            else
            {
                MessageBox.Show("Please make sure all information is valid.");
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateInput()
        {
            return ValidateIP(mainDataContext.IP1) && ValidateIP(mainDataContext.IP2)
                && ValidatePort(mainDataContext.Port1) && ValidatePort(mainDataContext.Port2)
                && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 100 //TODO how many??
                && inFile.Text != "";
        }

        private bool ValidateIP(string ip)
        {
            IPAddress ipAddress;
            return IPAddress.TryParse(ip, out ipAddress);
        }

        private bool ValidatePort(int port)
        {
            return port > -1 && port < 65536;
        }
    }
}
