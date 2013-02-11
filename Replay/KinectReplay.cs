using System;
using System.IO;
using System.Threading;

namespace Kinect.Toolbox.Record
{
    public class KinectReplay : IDisposable
    {
        BinaryReader reader;
        Stream stream;
        readonly SynchronizationContext synchronizationContext;

        // Events
        public event EventHandler<ReplayColorImageFrameReadyEventArgs> ColorImageFrameReady;
        public event EventHandler<ReplayDepthImageFrameReadyEventArgs> DepthImageFrameReady;
        public event EventHandler<ReplaySkeletonFrameReadyEventArgs> SkeletonFrameReady;        


        // Replay
        ReplaySystem<ReplayColorImageFrame> colorReplay;
        ReplaySystem<ReplayDepthImageFrame> depthReplay;
        ReplaySystem<ReplaySkeletonFrame> skeletonReplay;

        public bool Started { get; internal set; }

        public bool IsFinished
        {
            get
            {
                if (colorReplay != null && !colorReplay.IsFinished)
                    return false;

                if (depthReplay != null && !depthReplay.IsFinished)
                    return false;

                if (skeletonReplay != null && !skeletonReplay.IsFinished)
                    return false;

                return true;
            }
        }

        public KinectReplay(Stream stream)
        {
            this.stream = stream;
            reader = new BinaryReader(stream);

            synchronizationContext = SynchronizationContext.Current;

            KinectRecordOptions options = (KinectRecordOptions) reader.ReadInt32();

            if ((options & KinectRecordOptions.Color) != 0)
            {
                colorReplay = new ReplaySystem<ReplayColorImageFrame>();
            }
            if ((options & KinectRecordOptions.Depth) != 0)
            {
                depthReplay = new ReplaySystem<ReplayDepthImageFrame>();
            }
            if ((options & KinectRecordOptions.Skeletons) != 0)
            {
                skeletonReplay = new ReplaySystem<ReplaySkeletonFrame>();
            }

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                KinectRecordOptions header = (KinectRecordOptions)reader.ReadInt32();
                switch (header)
                {
                    case KinectRecordOptions.Color:
                        colorReplay.AddFrame(reader);
                        break;
                    case KinectRecordOptions.Depth:
                        depthReplay.AddFrame(reader);
                        break;
                    case KinectRecordOptions.Skeletons:
                        skeletonReplay.AddFrame(reader);
                        break;
                }
            }
        }

        public void Start()
        {
            if (Started)
                throw new Exception("KinectReplay already started");

            Started = true;

            if (colorReplay != null)
            {
                colorReplay.Start();
                colorReplay.FrameReady += frame => synchronizationContext.Send(state =>
                {
                    if (ColorImageFrameReady != null)
                        ColorImageFrameReady(this, new ReplayColorImageFrameReadyEventArgs { ColorImageFrame = frame });
                }, null);
            }

            if (depthReplay != null)
            {
                depthReplay.Start();
                depthReplay.FrameReady += frame => synchronizationContext.Send(state =>
                {
                    if (DepthImageFrameReady != null)
                        DepthImageFrameReady(this, new ReplayDepthImageFrameReadyEventArgs { DepthImageFrame = frame });
                }, null);
            }

            if (skeletonReplay != null)
            {
                skeletonReplay.Start();
                skeletonReplay.FrameReady += frame => synchronizationContext.Send(state =>
                {
                    if (SkeletonFrameReady != null)
                        SkeletonFrameReady(this, new ReplaySkeletonFrameReadyEventArgs { SkeletonFrame = frame });
                }, null);
            }
        }

        public void Stop()
        {
            if (colorReplay != null)
            {
                colorReplay.Stop();
            }

            if (depthReplay != null)
            {
                depthReplay.Stop();
            }

            if (skeletonReplay != null)
            {
                skeletonReplay.Stop();
            }

            Started = false;
        }

        public void Dispose()
        {
            Stop();

            colorReplay = null;
            depthReplay = null;
            skeletonReplay = null;

            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }

            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
}
