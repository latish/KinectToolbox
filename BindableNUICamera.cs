using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public class BindableNUICamera : Notifier
    {
        readonly KinectSensor nuiCamera;
        public int ElevationAngle
        {
            get { return nuiCamera.ElevationAngle; }
            set
            {
                if (nuiCamera.ElevationAngle == value)
                    return;

                if (value > ElevationMaximum)
                    value = ElevationMaximum;

                if (value < ElevationMinimum)
                    value = ElevationMinimum;

                nuiCamera.TrySetElevationAngle(value);

                RaisePropertyChanged(() => ElevationAngle);                
            }
        }

        public int ElevationMaximum
        {
            get { return nuiCamera.MaxElevationAngle; }
        }


        public int ElevationMinimum
        {
            get { return nuiCamera.MinElevationAngle; }
        }

        public BindableNUICamera(KinectSensor nuiCamera)
        {
            this.nuiCamera = nuiCamera;
        }
    }
}
