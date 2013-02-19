using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kinect.Toolbox.Record
{
	internal abstract class ReplayBase<T> where T : ReplayFrame, new()
	{
		protected readonly List<T> frames = new List<T>();
		private CancellationTokenSource cancellationTokenSource;
		internal virtual event Action<T> FrameReady;

		internal abstract void AddFrame(BinaryReader reader);

		public bool IsFinished { get; private set; }

		public void Start()
		{
			Stop();

			IsFinished = false;

			cancellationTokenSource = new CancellationTokenSource();

			CancellationToken token = cancellationTokenSource.Token;

			Task.Factory.StartNew(() =>
											 {
												 foreach (T frame in frames)
												 {
													 Thread.Sleep(TimeSpan.FromMilliseconds(frame.TimeStamp));

													 if (token.IsCancellationRequested)
														 break;

													 if (FrameReady != null)
														 FrameReady(frame);
												 }

												 IsFinished = true;
											 }, token);
		}

		public void Stop()
		{
			if (cancellationTokenSource == null)
				return;

			cancellationTokenSource.Cancel();
		}
	}

	class ReplaySystem<T> : ReplayBase<T> where T : ReplayFrame, new()
	{
		internal override void AddFrame(BinaryReader reader)
		{
			T frame = new T();

			frame.CreateFromReader(reader);

			frames.Add(frame);
		}
	}
}