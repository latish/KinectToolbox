using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace Kinect.Toolbox.Voice
{
	public class VoiceCommander
	{
		Thread workingThread;
		readonly Choices choices;
		bool isRunning;
		SpeechRecognitionEngine speechRecognitionEngine;
		private KinectSensor kinectSensor;
		private DateTime recordingStartTime;

		public event Action<string> OrderDetected;
		public bool RecordAudioToFile { get; set; }
		public string AudioFilePath { get; set; }

		public VoiceCommander(params string[] orders)
		{
			choices = new Choices();
			choices.Add(orders);
		}

		public void Start(KinectSensor sensor)
		{
			if (isRunning)
				throw new Exception("VoiceCommander is already running");

			isRunning = true;
			kinectSensor = sensor;
			recordingStartTime = DateTime.Now;
			workingThread = new Thread(Record) { IsBackground = true };
			workingThread.Start();
		}

		void Record(object o)
		{
			KinectAudioSource source = kinectSensor.AudioSource;

			Func<RecognizerInfo, bool> matchingFunc = r =>
			{
				string value;
				r.AdditionalInfo.TryGetValue("Kinect", out value);
				return
					 "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) &&
					 "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
			};

			var recognizerInfo = SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();

			if (recognizerInfo == null)
				return;

			speechRecognitionEngine = new SpeechRecognitionEngine(recognizerInfo.Id);

			var gb = new GrammarBuilder { Culture = recognizerInfo.Culture };
			gb.Append(choices);

			var grammar = new Grammar(gb);

			speechRecognitionEngine.LoadGrammar(grammar);

			source.AutomaticGainControlEnabled = false;
			source.BeamAngleMode = BeamAngleMode.Adaptive;

			MemoryStream contentMemoryStream = null;
			var buffer = new byte[1024];

			using (var sourceStream = source.Start())
			{
				//speechRecognitionEngine.SetInputToAudioStream(sourceStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
				speechRecognitionEngine.SetInputToDefaultAudioDevice();

				while (isRunning)
				{
					var result = speechRecognitionEngine.Recognize();
					if (result != null && OrderDetected != null && result.Confidence > 0.7)
						OrderDetected(result.Text);

					if (contentMemoryStream==null && RecordAudioToFile && !string.IsNullOrWhiteSpace(AudioFilePath))
						contentMemoryStream = new MemoryStream();

					if (contentMemoryStream == null)
						continue;
					int count;
					while ((count = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
						contentMemoryStream.Write(buffer, 0, count);
                    if(!RecordAudioToFile)
                        SaveAudioToFile(contentMemoryStream);
				}
			}

		}

		private void SaveAudioToFile(MemoryStream contentMemoryStream)
		{
			if (contentMemoryStream == null) return;

			var recordingDuration = DateTime.Now.Subtract(recordingStartTime).TotalSeconds;
			var recordingLength = (int) recordingDuration*2*16000;
			using (var headerMemoryStream = new MemoryStream())
			{
				WriteWavHeader(headerMemoryStream, recordingLength);
				using (var fileStream = new FileStream(AudioFilePath, FileMode.Create))
				{
					headerMemoryStream.WriteTo(fileStream);
					contentMemoryStream.WriteTo(fileStream);
					contentMemoryStream.Dispose();
				}
			}
		}

		public void Stop()
		{
			isRunning = false;

			if (speechRecognitionEngine != null)
			{
				speechRecognitionEngine.Dispose();
			}
		}

		/// <summary>
		/// A bare bones WAV file header writer
		/// </summary>        
		static void WriteWavHeader(Stream stream, int dataLength)
		{
			//We need to use a memory stream because the BinaryWriter will close the underlying stream when it is closed
			using (var memStream = new MemoryStream(64))
			{
				const int cbFormat = 18; //sizeof(WAVEFORMATEX)
				var format = new WaveFormatEx
									 {
										 wFormatTag = 1,
										 nChannels = 1,
										 nSamplesPerSec = 16000,
										 nAvgBytesPerSec = 32000,
										 nBlockAlign = 2,
										 wBitsPerSample = 16,
										 cbSize = 0
									 };

				using (var bw = new BinaryWriter(memStream))
				{
					//RIFF header
					WriteString(memStream, "RIFF");
					bw.Write(dataLength + cbFormat + 4); //File size - 8
					WriteString(memStream, "WAVE");
					WriteString(memStream, "fmt ");
					bw.Write(cbFormat);

					//WAVEFORMATEX
					bw.Write(format.wFormatTag);
					bw.Write(format.nChannels);
					bw.Write(format.nSamplesPerSec);
					bw.Write(format.nAvgBytesPerSec);
					bw.Write(format.nBlockAlign);
					bw.Write(format.wBitsPerSample);
					bw.Write(format.cbSize);

					//data header
					WriteString(memStream, "data");
					bw.Write(dataLength);
					memStream.WriteTo(stream);
				}
			}
		}

		static void WriteString(Stream stream, string s)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			stream.Write(bytes, 0, bytes.Length);
		}

		struct WaveFormatEx
		{
			public ushort wFormatTag;
			public ushort nChannels;
			public uint nSamplesPerSec;
			public uint nAvgBytesPerSec;
			public ushort nBlockAlign;
			public ushort wBitsPerSample;
			public ushort cbSize;
		}
	}
}
