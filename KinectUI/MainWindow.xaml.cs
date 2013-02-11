using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Kinect.Toolbox;

namespace KinectUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinectSensor; 
        Skeleton[] skeletons;

        public MainWindow()
        {
            InitializeComponent();
        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (kinectSensor == null)
                    {
                        kinectSensor = e.Sensor;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (kinectSensor == e.Sensor)
                    {
                        Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        private void MainWindow_Loaded_1(object sender, RoutedEventArgs e)
        {
            try
            {
                //listen to any status change for Kinects
                KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (KinectSensor kinect in KinectSensor.KinectSensors)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        kinectSensor = kinect;
                        break;
                    }
                }

                if (KinectSensor.KinectSensors.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Initialize()
        {
            if (kinectSensor == null)
                return;

            kinectSensor.DepthStream.Range = DepthRange.Near;
            kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;

            kinectSensor.SkeletonFrameReady += kinectSensor_SkeletonFrameReady;

            presenceControl.SetKinectSensor(kinectSensor);

            kinectSensor.SkeletonStream.Enable();
            kinectSensor.Start();

            MouseController.Current.DisableGestureClick = true;
            MouseController.Current.ImpostorCanvas = mouseCanvas;

            MouseController.Current.DataSmoothingFactor = 0.6f;
            MouseController.Current.PredictionFactor = 0.1f;
        }

        private void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                frame.GetSkeletons(ref skeletons);

                if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
                    return;

                foreach (var skeleton in skeletons)
                {
                    if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
                        continue;

                    foreach (Joint joint in skeleton.Joints)
                    {
                        if (joint.TrackingState != JointTrackingState.Tracked)
                            continue;

                        if (joint.JointType == JointType.HandRight)
                        {
                            MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
                        }
                    }
                }
            }
        }

        private void Clean()
        {
            if (kinectSensor != null)
            {
                presenceControl.Clean();
                kinectSensor.Dispose();
                kinectSensor = null;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
