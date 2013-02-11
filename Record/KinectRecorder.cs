using System;
using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox.Record
{
    public class KinectRecorder
    {
        Stream recordStream;
        readonly BinaryWriter writer;

        DateTime previousFlushDate;

        // Recorders
        readonly ColorRecorder colorRecoder;
        readonly DepthRecorder depthRecorder;
        readonly SkeletonRecorder skeletonRecorder;

        public KinectRecordOptions Options { get; set; }

        // Ctr
        public KinectRecorder(KinectRecordOptions options, Stream stream)
        {
            Options = options;

            recordStream = stream;
            writer = new BinaryWriter(recordStream);

            writer.Write((int)Options);

            if ((Options & KinectRecordOptions.Color) != 0)
            {
                colorRecoder = new ColorRecorder(writer);
            }
            if ((Options & KinectRecordOptions.Depth) != 0)
            {
                depthRecorder = new DepthRecorder(writer);
            }
            if ((Options & KinectRecordOptions.Skeletons) != 0)
            {
                skeletonRecorder = new SkeletonRecorder(writer);
            }

            previousFlushDate = DateTime.Now;
        }

        public void Record(SkeletonFrame frame)
        {
            if (writer == null)
                throw new Exception("This recorder is stopped");

            if (skeletonRecorder == null)
                throw new Exception("Skeleton recording is not actived on this KinectRecorder");

            skeletonRecorder.Record(frame);
            Flush();
        }

        public void Record(ColorImageFrame frame)
        {
            if (writer == null)
                throw new Exception("This recorder is stopped");

            if (colorRecoder == null)
                throw new Exception("Color recording is not actived on this KinectRecorder");

            colorRecoder.Record(frame);
            Flush();
        }

        public void Record(DepthImageFrame frame)
        {
            if (writer == null)
                throw new Exception("This recorder is stopped");

            if (depthRecorder == null)
                throw new Exception("Depth recording is not actived on this KinectRecorder");

            depthRecorder.Record(frame);
            Flush();
        }

        void Flush()
        {
            var now = DateTime.Now;

            if (now.Subtract(previousFlushDate).TotalSeconds > 60)
            {
                previousFlushDate = now;
                writer.Flush();
            }
        }

        public void Stop()
        {
            if (writer == null)
                throw new Exception("This recorder is already stopped");

            writer.Close();
            writer.Dispose();

            recordStream.Dispose();
            recordStream = null;
        }
    }
}
