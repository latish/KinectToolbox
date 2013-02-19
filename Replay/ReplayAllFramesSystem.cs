using System.IO;
using System.Linq;

namespace Kinect.Toolbox.Record
{
	class ReplayAllFramesSystem : ReplayBase<ReplayAllFrames>
	{
		internal override void AddFrame(BinaryReader reader)
		{
			var header = (KinectRecordOptions)reader.ReadInt32();
			switch (header)
			{
				case KinectRecordOptions.Color:
					var colorFrame = new ReplayColorImageFrame();
					colorFrame.CreateFromReader(reader);
					frames.Add(new ReplayAllFrames { ColorImageFrame = colorFrame });
					break;
				case KinectRecordOptions.Depth:
					if (frames.Any())
					{
						var depthFrame = new ReplayDepthImageFrame();
						depthFrame.CreateFromReader(reader);
						frames.Last().DepthImageFrame = depthFrame;
					}
					break;
				case KinectRecordOptions.Skeletons:
					if (frames.Any())
					{
						var skeletonFrame = new ReplaySkeletonFrame();
						skeletonFrame.CreateFromReader(reader);
						frames.Last().SkeletonFrame = skeletonFrame;
					}
					break;
			}
		}
	}
}