using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public partial class PresenceControl : UserControl
    {
        KinectSensor kinectSensor;
        byte[] depthFrame32;
        short[] pixelData;
        WriteableBitmap bitmap;

        public PresenceControl()
        {
            InitializeComponent();
        }

        public void SetKinectSensor(KinectSensor sensor)
        {
            kinectSensor = sensor;

            kinectSensor.DepthFrameReady += kinectSensor_DepthFrameReady;
            kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            kinectSensor.SkeletonFrameReady += kinectSensor_SkeletonFrameReady;
        }

        void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = null;

            leftEllipse.Visibility = System.Windows.Visibility.Collapsed;
            rightEllipse.Visibility = System.Windows.Visibility.Collapsed;

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                    return;

                Tools.GetSkeletons(frame, ref skeletons);

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
                            rightEllipse.Visibility = System.Windows.Visibility.Visible;
                            var handRightDepthPosition = kinectSensor.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution640x480Fps30);

                            rightTransform.X = (handRightDepthPosition.X / 640.0f) * Width;
                            rightTransform.Y = (handRightDepthPosition.Y / 480.0f) * Height;
                        }
                        else if (joint.JointType == JointType.HandLeft)
                        {
                            leftEllipse.Visibility = System.Windows.Visibility.Visible;
                            var handLeftDepthPosition = kinectSensor.MapSkeletonPointToDepth(joint.Position, DepthImageFormat.Resolution640x480Fps30);

                            leftTransform.X = (handLeftDepthPosition.X / 640.0f) * Width;
                            leftTransform.Y = (handLeftDepthPosition.Y / 480.0f) * Height;
                        }
                    }
                }
            }
        }

        void kinectSensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (var frame = e.OpenDepthImageFrame())
            {
                if (frame == null)
                    return;
               
                if (depthFrame32 == null)
                {
                    pixelData = new short[frame.PixelDataLength];
                    depthFrame32 = new byte[frame.Width * frame.Height * sizeof(int)];
                }

                frame.CopyPixelDataTo(pixelData);

                if (bitmap == null)
                {
                    bitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32, null);
                    image.Source = bitmap;
                }

                ConvertDepthFrame(pixelData);

                int stride = bitmap.PixelWidth * sizeof(int);
                Int32Rect dirtyRect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                bitmap.WritePixels(dirtyRect, depthFrame32, stride, 0);
            }
        }

        void ConvertDepthFrame(short[] depthFrame16)
        {
            int i32 = 0;
            for (int i16 = 0; i16 < depthFrame16.Length; i16++)
            {
                int user = depthFrame16[i16] & 0x07;
                int realDepth = (depthFrame16[i16] >> DepthImageFrame.PlayerIndexBitmaskWidth);

                byte intensity = (byte)(255 - (255 * realDepth / 0x1fff));

                depthFrame32[i32] = 0;
                depthFrame32[i32 + 1] = 0;
                depthFrame32[i32 + 2] = 0;
                depthFrame32[i32 + 3] = 255;

                if (user > 0)
                {
                    depthFrame32[i32] = intensity;
                }
                else
                {
                    depthFrame32[i32] = (byte)(intensity / 2);
                    depthFrame32[i32 + 1] = (byte)(intensity / 2);
                    depthFrame32[i32 + 2] = (byte)(intensity / 2);
                }

                i32 += 4;
            }
        }

        public void Clean()
        {
            kinectSensor.DepthFrameReady -= kinectSensor_DepthFrameReady;
            kinectSensor.SkeletonFrameReady -= kinectSensor_SkeletonFrameReady;
        }
    }
}
