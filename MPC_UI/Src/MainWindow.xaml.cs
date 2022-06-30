using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Net;
using System.IO;

using MPC_UI.ViewModel;
using MPCDataClient;
using MPCTools;
using MPCRandomnessClient;

namespace MPC_UI
{
    public partial class MainWindow : Window
    {
        private const int MAX_PORTS = 65536; //+1

        MainDataContext mainDataContext;
        ManagerDataClient managerDataClient;

        private bool isFirstClient = false;
        private bool isDebugMode = false;

        public MainWindow()
        {
            InitializeComponent();

            mainDataContext = new MainDataContext();
            DataContext = mainDataContext;

            managerDataClient = new ManagerDataClient();
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
            if (!isFirstClient && ValidateInputFirstInit()
                && !ManagerDataClient.InitConnectionExistingSession(mainDataContext.IP1, mainDataContext.Port1, mainDataContext.SessionId))
            {
                MessageBox.Show("Could not send data. Check servers' addresses.");
                return;
            }
            if (ValidateInput())
            {
                uint[] data = managerDataClient.ReadInput(inFile.Text);
                if (data != null)
                {
                    string res = ManagerDataClient.Run(mainDataContext.IP2, mainDataContext.Port2, mainDataContext.SessionId, data);
                    MessageBox.Show(res);
                    ClearScreen();
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
            GenerateSession();
            isFirstClient = true;
        }

        private void GenerateSession()
        {
            if (ValidateInputFirstInit())
            {
                mainDataContext.SessionId = ManagerDataClient.InitConnectionNewSession(mainDataContext.IP1, mainDataContext.Port1,
                    mainDataContext.IP2, mainDataContext.Port2, Operations.operations[Operation.SelectedIndex], 
                    mainDataContext.ParticipantsNum, isDebugMode);
                if (mainDataContext.SessionId != string.Empty)
                {
                    sessionId.Text = mainDataContext.SessionId; //assume valid session id received from server.
                }
                else
                {
                    string msg = "Could not create session.";
                    if (ManagerDataClient.GetServerResponse() != string.Empty)
                    {
                        msg += " " + ManagerDataClient.GetServerResponse();
                    }
                    else
                    {
                        msg += " Check servers' addresses.";
                    }
                    MessageBox.Show(msg);
                }
            }
            else
            {
                MessageBox.Show("Please make sure information is valid.");
            }
        }


        private void NewSession_Checked(object sender, RoutedEventArgs e)
        {
            ParticipantsNum.IsEnabled = true;
            sessionId.IsReadOnly = true;
            generateSession.IsEnabled = true;
            DebugMode.IsEnabled = true;
            //isFirstClient = true;
        }
        
        private void ExistingSession_Checked(object sender, RoutedEventArgs e)
        {
            ParticipantsNum.IsEnabled = false;
            sessionId.IsReadOnly = false;
            generateSession.IsEnabled = false;
            DebugMode.IsEnabled = false;
            isFirstClient = false;
        }

        private void DebugMode_Checked(object sender, RoutedEventArgs e)
        {
            isDebugMode = true;
        }

        private void DebugMode_Unchecked(object sender, RoutedEventArgs e)
        {
            isDebugMode = false;
        }

        private void ClearScreen()
        {
            mainDataContext.IP1 = "";
            mainDataContext.IP2 = "";
            mainDataContext.Port1 = 0;
            mainDataContext.Port2 = 0;
            mainDataContext.SessionId = "";
            RB_ExistingSession.IsChecked = true;
            inFile.Text = "";
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateInputFirstInit()
        {
            return ValidateIP(mainDataContext.IP1) && ValidatePort(mainDataContext.Port1)
                && ValidateIP(mainDataContext.IP2) && ValidatePort(mainDataContext.Port2)
               && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 1000;
        }

        private bool ValidateInput()
        {
            return ValidateCommInfo()
                && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 1000
                && inFile.Text != "";
        }

        private bool ValidateCommInfo()
        {
            return ValidateIP(mainDataContext.IP1) && ValidateIP(mainDataContext.IP2)
                && ValidatePort(mainDataContext.Port1) && ValidatePort(mainDataContext.Port2);
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
