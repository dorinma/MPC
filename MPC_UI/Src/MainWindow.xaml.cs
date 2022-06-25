using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Net;
using System.IO;

using MPC_UI.ViewModel;
using MPCDataClient;
using MPCTools;
using System.DirectoryServices.ActiveDirectory;

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
        MPCRandomnessClient.ManagerRandomnessClient managerRandomnessClient;
        private bool isFirstClient = false;
        private bool isDebugMode = true;

        public MainWindow()
        {
            InitializeComponent();

            mainDataContext = new MainDataContext();
            DataContext = mainDataContext;

            managerDataClient = new MPCDataClient.ManagerDataClient();
            managerRandomnessClient = new MPCRandomnessClient.ManagerRandomnessClient();
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
            if (!isFirstClient && ValidateInputFirstInit())
                ManagerDataClient.InitConnectionExistingSession(mainDataContext.IP1, mainDataContext.Port1, mainDataContext.SessionId);
            if (ValidateInput())
            {
                uint[] data = managerDataClient.ReadInput(inFile.Text);
                if (data != null)
                {
                    string res = ManagerDataClient.Run(mainDataContext.IP2, mainDataContext.Port2, mainDataContext.SessionId, data);
                    MessageBox.Show(res);
                    /*if (isDebugMode)
                    {
                        using (StreamWriter outputFile = new StreamWriter(Path.Combine(@"..\\..\\..\\Out", "ComputationOutput.txt")))
                        {
                            outputFile.WriteLine(res);
                            MessageBox.Show("Computation is done, output is saved to Out folder.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Computation is done.");
                    }*/
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
                    Operations.operations[Operation.SelectedIndex-1], mainDataContext.ParticipantsNum, isDebugMode);
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

        private void refreshRnd_Click(object sender, RoutedEventArgs e)
        {
            /*
            if (ValidateCommInfo())
            {
                if (managerRandomnessClient.Run(mainDataContext.IP1, mainDataContext.IP2, mainDataContext.Port1, mainDataContext.Port2)) 
                    MessageBox.Show("New randomness has been generated.");
                else MessageBox.Show("Could not generate new randomness.");
            }
            else MessageBox.Show("Please insert valid IPs & ports.");
            */
        }

        private void DebugMode_Checked(object sender, RoutedEventArgs e)
        {
            isDebugMode = true;
        }

        private void DebugMode_Unchecked(object sender, RoutedEventArgs e)
        {
            isDebugMode = false;
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
            return ValidateCommInfo()
                && mainDataContext.ParticipantsNum > 0 && mainDataContext.ParticipantsNum < 100 //TODO how many??
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
