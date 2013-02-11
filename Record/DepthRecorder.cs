using System;
using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox.Record
{
    class DepthRecorder
    {
        DateTime referenceTime;
        readonly BinaryWriter writer;

        internal DepthRecorder(BinaryWriter writer)
        {
            this.writer = writer;
            referenceTime = DateTime.Now;
        }

        public void Record(DepthImageFrame frame)
        {
            // Header
            writer.Write((int)KinectRecordOptions.Depth);

            // Data
            TimeSpan timeSpan = DateTime.Now.Subtract(referenceTime);
            referenceTime = DateTime.Now;
            writer.Write((long)timeSpan.TotalMilliseconds);
            writer.Write(frame.BytesPerPixel);
            writer.Write((int)frame.Format);
            writer.Write(frame.Width);
            writer.Write(frame.Height);

            writer.Write(frame.FrameNumber);

            // Bytes
            short[] shorts = new short[frame.PixelDataLength];
            frame.CopyPixelDataTo(shorts);
            writer.Write(shorts.Length);
            foreach (short s in shorts)
            {
                writer.Write(s);
            }
        }
    }
}
