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
        private const int MAX_PORTS = 65536; //+1

        MainDataContext mainDataContext;
        MPCDataClient.ManagerDataClient managerDataClient;
        //private bool isFirstClient = false;

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
                uint[] data = managerDataClient.ReadInput(inFile.Text);
                if (data != null)
                {
                    managerDataClient.Run(mainDataContext.IP2, mainDataContext.Port2, mainDataContext.SessionId, data);
                    MessageBox.Show("Computation is done.");
                }
                else
                {
                    MessageBox.Show("Could not parse input file, validate file's content & format.");
                }
            }
            else
            {
                MessageBox.Show("Please make sure all information is valid.");
            }
        }

        private void StartNewSession_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputFirstInit())
            {
                mainDataContext.SessionId = managerDataClient.InitConnectionNewSession(mainDataContext.IP1, mainDataContext.Port1, 
                    Operation.SelectedIndex, mainDataContext.ParticipantsNum);
                sessionId.Text = mainDataContext.SessionId; //assume valid session id
                //isFirstClient = true;
            }
            else
            {
                MessageBox.Show("Please make sure information is valid.");
            }
        }


        private void NewSession_Checked(object sender, RoutedEventArgs e)
        {
            ParticipantsNum.IsReadOnly = false;
            sessionId.IsReadOnly = true;
        }
        
        private void ExistingSession_Checked(object sender, RoutedEventArgs e)
        {
            //ParticipantsNum.IsReadOnly = true;
            //sessionId.IsReadOnly = false;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateInputFirstInit()
        {
            return ValidateIP(mainDataContext.IP1)
               && ValidatePort(mainDataContext.Port1)
               && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 100; //TODO how many??
        }

        private bool ValidateInput()
        {
            return ValidateIP(mainDataContext.IP1) && ValidateIP(mainDataContext.IP2)
                && ValidatePort(mainDataContext.Port1) && ValidatePort(mainDataContext.Port2)
                && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 100 //TODO how many??
                && mainDataContext.SessionId != "" && inFile.Text != "";
        }

        private bool ValidateIP(string ip)
        {
            IPAddress ipAddress;
            return IPAddress.TryParse(ip, out ipAddress);
        }

        private bool ValidatePort(int port)
        {
            return port > -1 && port < MAX_PORTS;
        }
    }
}
