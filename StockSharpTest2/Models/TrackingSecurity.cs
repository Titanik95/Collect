using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collect.Models
{
    [Serializable]
    public class TrackingSecurity : INotifyPropertyChanged
    {
        Security security;
        int minimumVolume;
        [NonSerialized]
        int volumeRecieved;
        [NonSerialized]
        int volumeSent;
        bool tracking;

        public Security Security
        {
            get
            {
                return security;
            }
        }

        public int MinimumVolume
        {
            get { return minimumVolume; }
            set
            {
                if (minimumVolume == value) return;
                minimumVolume = value;
                OnPropertyChanged("MinumumVolume");
            }
        }

        public int VolumeRecieved
        {
            get { return volumeRecieved; }
            set
            {
                if (volumeRecieved == value) return;
                volumeRecieved = value;
                OnPropertyChanged("VolumeRecieved");
            }
        }

        public int VolumeSent
        {
            get { return volumeRecieved; }
            set
            {
                if (volumeSent == value) return;
                volumeSent = value;
                OnPropertyChanged("VolumeSent");
            }
        }

        public bool Tracking
        {
            get { return tracking; }
            set
            {
                if (tracking == value) return;
                tracking = value;
                OnPropertyChanged("Tracking");
                OnPropertyChanged("TrackingNot");
            }
        }

        public bool TrackingNot
        {
            get { return !tracking; }
        }

        public TrackingSecurity(Security s, int volRec, int volSen, bool track, int minVol)
        {
            security = s;
            volumeRecieved = volRec;
            volumeSent = volSen;
            tracking = track;
            minimumVolume = minVol;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return security.ToString();
        }
    }
}
