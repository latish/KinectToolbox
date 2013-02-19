using System;
using System.IO;
using System.Threading;
using Microsoft.Kinect;

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
		public event EventHandler<ReplayAllFramesReadyEventArgs> AllFramesReady;

		public CoordinateMapper CoordinateMapper { get; private set; }

		// Replay
		ReplaySystem<ReplayColorImageFrame> colorReplay;
		ReplaySystem<ReplayDepthImageFrame> depthReplay;
		ReplaySystem<ReplaySkeletonFrame> skeletonReplay;
		ReplayAllFramesSystem allReplay;

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

				if (allReplay != null && !allReplay.IsFinished)
					return false;

				return true;
			}
		}

		public KinectReplay(Stream stream)
		{
			this.stream = stream;
			reader = new BinaryReader(stream);

			synchronizationContext = SynchronizationContext.Current;

			var options = (KinectRecordOptions)reader.ReadInt32();
			var paramsArrayLength = reader.ReadInt32();
			var colorToDepthRelationalParameters = reader.ReadBytes(paramsArrayLength);
			CoordinateMapper = new CoordinateMapper(colorToDepthRelationalParameters);

			if ((options & KinectRecordOptions.All) != 0)
			{
				allReplay = new ReplayAllFramesSystem();
				while (reader.BaseStream.Position != reader.BaseStream.Length)
					allReplay.AddFrame(reader);
			}
			else
			{
				if ((options & KinectRecordOptions.Color) != 0)
					colorReplay = new ReplaySystem<ReplayColorImageFrame>();
				if ((options & KinectRecordOptions.Depth) != 0)
					depthReplay = new ReplaySystem<ReplayDepthImageFrame>();
				if ((options & KinectRecordOptions.Skeletons) != 0)
					skeletonReplay = new ReplaySystem<ReplaySkeletonFrame>();

				while (reader.BaseStream.Position != reader.BaseStream.Length)
				{
					var header = (KinectRecordOptions)reader.ReadInt32();
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

			if (allReplay != null)
			{
				allReplay.Start();
				allReplay.FrameReady += frame => synchronizationContext.Send(state =>
				  {
					  if (AllFramesReady != null)
						  AllFramesReady(this, new ReplayAllFramesReadyEventArgs { AllFrames = frame });
				  }, null);
			}
		}

		public void Stop()
		{
			if (colorReplay != null)
				colorReplay.Stop();

			if (depthReplay != null)
				depthReplay.Stop();

			if (skeletonReplay != null)
				skeletonReplay.Stop();

			if (allReplay != null)
				allReplay.Stop();

			Started = false;
		}

		public void Dispose()
		{
			Stop();

			colorReplay = null;
			depthReplay = null;
			skeletonReplay = null;
			allReplay = null;

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
