using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;

namespace Kinect.Toolbox
{
    public class EyeTracker : IDisposable
    {
        FaceTracker faceTracker;
        KinectSensor sensor;
        byte[] colors;
        short[] depths;
        float epsilon;

        public bool? IsLookingToSensor
        {
            get;
            private set;
        }

        public EyeTracker(KinectSensor sensor, float epsilon = 0.02f)
        {
            faceTracker = new FaceTracker(sensor);
            this.sensor = sensor;
            this.epsilon = epsilon;
        }

        public void Track(Skeleton skeleton)
        {
            // Colors
            if (colors == null)
            {
                colors = new byte[sensor.ColorStream.FramePixelDataLength];
            }

            var colorFrame = sensor.ColorStream.OpenNextFrame(0);
            if (colorFrame == null)
            {
                IsLookingToSensor = null;
                return;
            }

            colorFrame.CopyPixelDataTo(colors);

            // Depths
            if (depths == null)
            {
                depths = new short[sensor.DepthStream.FramePixelDataLength];
            }

            var depthFrame = sensor.DepthStream.OpenNextFrame(0);
            if (depthFrame == null)
            {
                IsLookingToSensor = null;
                return;
            }
            depthFrame.CopyPixelDataTo(depths);

            // Track
            var frame = faceTracker.Track(sensor.ColorStream.Format, colors, sensor.DepthStream.Format, depths, skeleton);

            if (frame == null)
            {
                IsLookingToSensor = null;
                return;
            }
            var shape = frame.Get3DShape();

            var leftEyeZ = shape[FeaturePoint.AboveMidUpperLeftEyelid].Z;
            var rightEyeZ = shape[FeaturePoint.AboveMidUpperRightEyelid].Z;

            IsLookingToSensor = Math.Abs(leftEyeZ - rightEyeZ) <= epsilon;
        }

        public void Dispose()
        {
            faceTracker.Dispose();
        }
    }
}
