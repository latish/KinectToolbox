using System;
using System.IO;

namespace Kinect.Toolbox.Record
{
	public class ReplayAllFrames : ReplayFrame
	{
		public ReplayColorImageFrame ColorImageFrame { get; set; }
		public ReplayDepthImageFrame DepthImageFrame { get; set; }
		public ReplaySkeletonFrame SkeletonFrame { get; set; }

		public override int FrameNumber { get { return ColorImageFrame.FrameNumber; } }

		public override long TimeStamp { get { return ColorImageFrame.TimeStamp; } }

		internal override void CreateFromReader(BinaryReader reader)
		{
			throw new NotImplementedException();
		}
	}
}