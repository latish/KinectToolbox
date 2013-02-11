using System;
using System.IO;
using System.Linq;
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

        public event Action<string> OrderDetected;

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
            workingThread = new Thread(Record) {IsBackground = true};
            workingThread.Start();
        }

        void Record()
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

            using (Stream sourceStream = source.Start())
            {
                speechRecognitionEngine.SetInputToAudioStream(sourceStream, 
                    new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));

                while (isRunning)
                {
                    RecognitionResult result = speechRecognitionEngine.Recognize();
                    if (result != null && OrderDetected != null && result.Confidence > 0.7)
                        OrderDetected(result.Text);
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
    }
}
