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
        double minimumVolume;
		[NonSerialized]
		double volumeReceived;
		[NonSerialized]
        double volumeSent;

        public Security Security
        {
            get
            {
                return security;
            }
        }

        public double MinimumVolume
        {
            get { return minimumVolume; }
            set
            {
                if (minimumVolume == value) return;
                minimumVolume = value;
                OnPropertyChanged("MinumumVolume");
            }
        }

		public double VolumeReceived
		{
			get { return volumeReceived; }
			set
			{
				if (volumeReceived == value) return;
				volumeReceived = value;
				OnPropertyChanged("VolumeReceived");
			}
		}

		public double VolumeSent
        {
            get { return volumeSent; }
            set
            {
                if (volumeSent == value) return;
                volumeSent = value;
                OnPropertyChanged("VolumeSent");
            }
        }

        public TrackingSecurity(Security s, double volRec, double volSen, double minVol)
        {
            security = s;
			volumeReceived = volRec;
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
