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
        int volumeSent;

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

        public int VolumeSent
        {
            get { return volumeSent; }
            set
            {
                if (volumeSent == value) return;
                volumeSent = value;
                OnPropertyChanged("VolumeSent");
            }
        }

        public TrackingSecurity(Security s, int volSen, int minVol)
        {
            security = s;
            volumeSent = volSen;
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
