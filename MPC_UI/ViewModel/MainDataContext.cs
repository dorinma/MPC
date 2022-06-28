using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MPC_UI.ViewModel
{
    class MainDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        string ip1, ip2, sessionId;
        int port1, port2, participants;

        public MainDataContext()
        {
            ip1 = "";
            ip2 = "";
            port1 = 0;
            port2 = 0;
            participants = 1;
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

    }
}
