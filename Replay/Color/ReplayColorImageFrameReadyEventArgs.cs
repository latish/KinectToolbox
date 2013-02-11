using System;

namespace Kinect.Toolbox.Record
{
    public class ReplayColorImageFrameReadyEventArgs : EventArgs
    {
        public ReplayColorImageFrame ColorImageFrame { get; set; }
    }
}
