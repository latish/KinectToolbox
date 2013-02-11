using System.IO;

namespace Kinect.Toolbox.Record
{
    public abstract class ReplayFrame
    {
        public int FrameNumber { get; protected set; }
        public long TimeStamp { get; protected set; }

        internal abstract void CreateFromReader(BinaryReader reader);
    }
}
