using System.IO;
using System.Windows;
using Kinect.Toolbox;

namespace GesturesViewer
{
    partial class MainWindow
    {
        void LoadLetterTPostureDetector()
        {
            using (Stream recordStream = File.Open(letterT_KBPath, FileMode.OpenOrCreate))
            {
                templatePostureDetector = new TemplatedPostureDetector("T", recordStream);
                templatePostureDetector.PostureDetected += templatePostureDetector_PostureDetected;
            }
        }

        void ClosePostureDetector()
        {
            if (templatePostureDetector == null)
                return;

            using (Stream recordStream = File.Create(letterT_KBPath))
            {
                templatePostureDetector.SaveState(recordStream);
            }
            templatePostureDetector.PostureDetected -= templatePostureDetector_PostureDetected;
        }

        void templatePostureDetector_PostureDetected(string posture)
        {
            MessageBox.Show("Give me a......." + posture);
        }

        private void recordT_Click(object sender, RoutedEventArgs e)
        {
           recordNextFrameForPosture = true;
        }
    }
}
