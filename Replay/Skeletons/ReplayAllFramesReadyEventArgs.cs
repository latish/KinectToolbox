using System;

namespace Kinect.Toolbox.Record
{
	public class ReplayAllFramesReadyEventArgs:EventArgs
	{
		public ReplayAllFrames AllFrames { get; set; } 
	}
}