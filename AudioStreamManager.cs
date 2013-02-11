using Microsoft.Kinect;
using System;

namespace Kinect.Toolbox
{
    public class AudioStreamManager : Notifier, IDisposable
    {
        readonly KinectAudioSource audioSource;

        public AudioStreamManager(KinectAudioSource source)
        {
            audioSource = source;
            audioSource.BeamAngleChanged += audioSource_BeamAngleChanged;
        }

        void audioSource_BeamAngleChanged(object sender, BeamAngleChangedEventArgs e)
        {
            RaisePropertyChanged(()=>BeamAngle);
        }

        public double BeamAngle
        {
            get
            {
                return (audioSource.BeamAngle - KinectAudioSource.MinBeamAngle) / (KinectAudioSource.MaxBeamAngle - KinectAudioSource.MinBeamAngle);
            }
        }

        public void Dispose()
        {
            audioSource.BeamAngleChanged -= audioSource_BeamAngleChanged;
        }
    }
}
