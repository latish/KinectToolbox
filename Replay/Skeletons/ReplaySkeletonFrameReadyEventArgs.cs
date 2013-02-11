using System;

namespace Kinect.Toolbox.Record
{
    public class ReplaySkeletonFrameReadyEventArgs : EventArgs
    {
        public ReplaySkeletonFrame SkeletonFrame { get; set; }
    }
}
