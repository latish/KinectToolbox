using System;

namespace Kinect.Toolbox.Record
{
    public class ReplayDepthImageFrameReadyEventArgs : EventArgs
    {
        public ReplayDepthImageFrame DepthImageFrame { get; set; }
    }
}
