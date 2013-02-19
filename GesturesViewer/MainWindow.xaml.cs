using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Kinect.Toolbox;
using Kinect.Toolbox.Record;
using System.IO;
using Microsoft.Kinect;
using Microsoft.Win32;
using Kinect.Toolbox.Voice;

namespace GesturesViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		KinectSensor kinectSensor;

		SwipeGestureDetector swipeGestureRecognizer;
		TemplatedGestureDetector circleGestureRecognizer;
		readonly ColorStreamManager colorManager = new ColorStreamManager();
		readonly DepthStreamManager depthManager = new DepthStreamManager();
		AudioStreamManager audioManager;
		SkeletonDisplayManager skeletonDisplayManager;
		readonly ContextTracker contextTracker = new ContextTracker();
		EyeTracker eyeTracker;
		ParallelCombinedGestureDetector parallelCombinedGestureDetector;
		readonly AlgorithmicPostureDetector algorithmicPostureRecognizer = new AlgorithmicPostureDetector();
		TemplatedPostureDetector templatePostureDetector;
		private bool recordNextFrameForPosture;
		bool displayDepth;

		string circleKBPath;
		string letterT_KBPath;

		KinectRecorder recorder;
		KinectReplay replay;

		BindableNUICamera nuiCamera;

		private Skeleton[] skeletons;

		VoiceCommander voiceCommander;

		public MainWindow()
		{
			InitializeComponent();
		}

		void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
		{
			switch (e.Status)
			{
				case KinectStatus.Connected:
					if (kinectSensor == null)
					{
						kinectSensor = e.Sensor;
						Initialize();
					}
					break;
				case KinectStatus.Disconnected:
					if (kinectSensor == e.Sensor)
					{
						Clean();
						MessageBox.Show("Kinect was disconnected");
					}
					break;
				case KinectStatus.NotReady:
					break;
				case KinectStatus.NotPowered:
					if (kinectSensor == e.Sensor)
					{
						Clean();
						MessageBox.Show("Kinect is no more powered");
					}
					break;
				default:
					MessageBox.Show("Unhandled Status: " + e.Status);
					break;
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			circleKBPath = Path.Combine(Environment.CurrentDirectory, @"data\circleKB.save");
			letterT_KBPath = Path.Combine(Environment.CurrentDirectory, @"data\t_KB.save");

			try
			{
				//listen to any status change for Kinects
				KinectSensor.KinectSensors.StatusChanged += Kinects_StatusChanged;

				//loop through all the Kinects attached to this PC, and start the first that is connected without an error.
				foreach (var kinect in KinectSensor.KinectSensors)
				{
					if (kinect.Status == KinectStatus.Connected)
					{
						kinectSensor = kinect;
						break;
					}
				}

				if (KinectSensor.KinectSensors.Count == 0)
					MessageBox.Show("No Kinect found");
				else
					Initialize();

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void Initialize()
		{
			if (kinectSensor == null)
				return;

			audioManager = new AudioStreamManager(kinectSensor.AudioSource);
			audioBeamAngle.DataContext = audioManager;

			kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

			kinectSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

			kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters
																{
																	Smoothing = 0.5f,
																	Correction = 0.5f,
																	Prediction = 0.5f,
																	JitterRadius = 0.05f,
																	MaxDeviationRadius = 0.04f
																});
			kinectSensor.AllFramesReady += AllFramesReady;

			swipeGestureRecognizer = new SwipeGestureDetector();
			swipeGestureRecognizer.OnGestureDetected += OnGestureDetected;

			skeletonDisplayManager = new SkeletonDisplayManager(kinectSensor, kinectCanvas);

			kinectSensor.Start();

			LoadCircleGestureDetector();
			LoadLetterTPostureDetector();

			nuiCamera = new BindableNUICamera(kinectSensor);

			elevationSlider.DataContext = nuiCamera;

			voiceCommander = new VoiceCommander("record", "stop");
			voiceCommander.OrderDetected += voiceCommander_OrderDetected;

			StartVoiceCommander();

			kinectDisplay.DataContext = colorManager;

			parallelCombinedGestureDetector = new ParallelCombinedGestureDetector();
			parallelCombinedGestureDetector.OnGestureDetected += OnGestureDetected;
			parallelCombinedGestureDetector.Add(swipeGestureRecognizer);
			parallelCombinedGestureDetector.Add(circleGestureRecognizer);
		}

		void AllFramesReady(object sender, AllFramesReadyEventArgs e)
		{
			if (replay != null && !replay.IsFinished)
				return;

			using (var frame = e.OpenColorImageFrame())
			{
				if (frame == null)
					return;

				if (recorder != null && ((recorder.Options & KinectRecordOptions.Color) != 0))
					recorder.Record(frame);

				colorManager.Update(frame);
			}

			using (var frame = e.OpenDepthImageFrame())
			{
				if (frame == null)
					return;

				if (recorder != null && ((recorder.Options & KinectRecordOptions.Depth) != 0))
					recorder.Record(frame);

				depthManager.Update(frame);
			}

			using (var frame = e.OpenSkeletonFrame())
			{
				if (frame == null)
					return;

				if (recorder != null && ((recorder.Options & KinectRecordOptions.Skeletons) != 0))
					recorder.Record(frame);

				frame.GetSkeletons(ref skeletons);

				if (skeletons.All(s => s.TrackingState == SkeletonTrackingState.NotTracked))
					return;

				ProcessFrame(frame);
			}
		}

		void ProcessFrame(ReplaySkeletonFrame frame)
		{
			var stabilities = new Dictionary<int, string>();
			foreach (var skeleton in frame.Skeletons)
			{
				if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
					continue;

				contextTracker.Add(skeleton.Position.ToVector3(), skeleton.TrackingId);
				stabilities.Add(skeleton.TrackingId, contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId) ? "Stable" : "Non stable");
				if (!contextTracker.IsStableRelativeToCurrentSpeed(skeleton.TrackingId))
					continue;

				foreach (Joint joint in skeleton.Joints)
				{
					if (joint.TrackingState != JointTrackingState.Tracked)
						continue;

					if (joint.JointType == JointType.HandRight)
						circleGestureRecognizer.Add(joint.Position, kinectSensor);
					else if (joint.JointType == JointType.HandLeft)
					{
						swipeGestureRecognizer.Add(joint.Position, kinectSensor);
						if (controlMouse.IsChecked == true)
							MouseController.Current.SetHandPosition(kinectSensor, joint, skeleton);
					}
				}

				algorithmicPostureRecognizer.TrackPostures(skeleton);
				templatePostureDetector.TrackPostures(skeleton);

				if (recordNextFrameForPosture)
				{
					templatePostureDetector.AddTemplate(skeleton);
					recordNextFrameForPosture = false;
				}
			}

			skeletonDisplayManager.Draw(frame.Skeletons, seatedMode.IsChecked == true);

			stabilitiesList.ItemsSource = stabilities;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Clean();
		}

		private void Clean()
		{
			if (swipeGestureRecognizer != null)
				swipeGestureRecognizer.OnGestureDetected -= OnGestureDetected;

			if (audioManager != null)
			{
				audioManager.Dispose();
				audioManager = null;
			}

			if (parallelCombinedGestureDetector != null)
			{
				parallelCombinedGestureDetector.Remove(swipeGestureRecognizer);
				parallelCombinedGestureDetector.Remove(circleGestureRecognizer);
				parallelCombinedGestureDetector = null;
			}

			CloseGestureDetector();

			ClosePostureDetector();

			if (voiceCommander != null)
			{
				voiceCommander.OrderDetected -= voiceCommander_OrderDetected;
				voiceCommander.Stop();
				voiceCommander = null;
			}

			if (recorder != null)
			{
				recorder.Stop();
				recorder = null;
			}

			if (eyeTracker != null)
			{
				eyeTracker.Dispose();
				eyeTracker = null;
			}

			if (kinectSensor != null)
			{
				kinectSensor.AllFramesReady -= AllFramesReady;
				kinectSensor.Stop();
				kinectSensor = null;
			}
		}

		private void replayButton_Click(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog { Title = "Select filename", Filter = "Replay files|*.replay" };

			if (openFileDialog.ShowDialog() == true)
			{
				if (replay != null)
				{
					replay.AllFramesReady -= replay_AllFramesReady;
					replay.Stop();
				}
				Stream recordStream = File.OpenRead(openFileDialog.FileName);

				replay = new KinectReplay(recordStream);

				replay.AllFramesReady += replay_AllFramesReady;

				replay.Start();
			}
		}

		void replay_AllFramesReady(object sender, ReplayAllFramesReadyEventArgs e)
		{
			var colorImageFrame = e.AllFrames.ColorImageFrame;
			if (colorImageFrame != null)
				colorManager.Update(colorImageFrame);

			var depthImageFrame = e.AllFrames.DepthImageFrame;
			if (depthImageFrame != null)
				depthManager.Update(depthImageFrame);

			var skeletonFrame = e.AllFrames.SkeletonFrame;
			if (skeletonFrame != null)
				ProcessFrame(skeletonFrame);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			displayDepth = !displayDepth;

			if (displayDepth)
			{
				viewButton.Content = "View Color";
				kinectDisplay.DataContext = depthManager;
			}
			else
			{
				viewButton.Content = "View Depth";
				kinectDisplay.DataContext = colorManager;
			}
		}

		private void nearMode_Checked_1(object sender, RoutedEventArgs e)
		{
			if (kinectSensor == null)
				return;

			kinectSensor.DepthStream.Range = DepthRange.Near;
			kinectSensor.SkeletonStream.EnableTrackingInNearRange = true;
		}

		private void nearMode_Unchecked_1(object sender, RoutedEventArgs e)
		{
			if (kinectSensor == null)
				return;

			kinectSensor.DepthStream.Range = DepthRange.Default;
			kinectSensor.SkeletonStream.EnableTrackingInNearRange = false;
		}

		private void seatedMode_Checked_1(object sender, RoutedEventArgs e)
		{
			if (kinectSensor == null)
				return;

			kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
		}

		private void seatedMode_Unchecked_1(object sender, RoutedEventArgs e)
		{
			if (kinectSensor == null)
				return;

			kinectSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
		}
	}
}
