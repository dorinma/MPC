using System.ComponentModel;
using MPCTools;

namespace MPC_UI.ViewModel
{
    class MainDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string ip1, ip2, sessionId, selectedOper;
        int port1, port2, participants;
        string[] operations;

        public MainDataContext()
        {
            participants = 1;
            SetOperations();
            if(operations != null && operations.Length > 0)
                selectedOper = operations[0];
        }

        private void SetOperations()
        {
            operations = new string[MPCTools.Operations.operations.Length];
            for (int i = 0; i < MPCTools.Operations.operations.Length; i++)
            {
                operations[i] = MPCTools.Operations.operations[i].ToString();
            }
        }

        public string IP1
        {
            get
            {
                return ip1;
            }
            set
            {
                ip1 = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IP1"));
            }
        }

        public string IP2
        {
            get
            {
                return ip2;
            }
            set
            {
                ip2 = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("IP2"));
            }
        }
        public int Port1
        {
            get
            {
                return port1;
            }
            set
            {
                port1 = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Port1"));
            }
        }
        public int Port2
        {
            get
            {
                return port2;
            }
            set
            {
                port2 = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Port2"));
            }
        }
        public int ParticipantsNum
        {
            get
            {
                return participants;
            }
            set
            {
                participants = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ParticipantsNum"));
            }
        }
        public string SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                sessionId = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SessionId"));
            }
        }

        public string[] Operations
        {
            get
            {
                return operations;
            }
            set
            {
                operations = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Operations"));
            }
        }

        public string SelectedOper
        {
            get
            {
                return selectedOper;
            }
            set
            {
                selectedOper = value;

                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("SelectedOper"));
            }
        }

    }
}
