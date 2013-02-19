using System.IO;

namespace Kinect.Toolbox.Record
{
    public abstract class ReplayFrame
    {
        public virtual int FrameNumber { get; protected set; }
        public virtual long TimeStamp { get; protected set; }

        internal abstract void CreateFromReader(BinaryReader reader);
    }
}
