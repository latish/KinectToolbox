using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kinect.Toolbox
{
    public class SerialCombinedGestureDetector : CombinedGestureDetector
    {
        DateTime? previousGestureTime;
        List<string> detectedGesturesName = new List<string>();

        public SerialCombinedGestureDetector(double epsilon = 1000)
            : base(epsilon)
        {
        }

        protected override void CheckGestures(string gesture)
        {            
            var currentTime = DateTime.Now;

            if (!previousGestureTime.HasValue || detectedGesturesName.Contains(gesture) || currentTime.Subtract(previousGestureTime.Value).TotalMilliseconds > Epsilon)
            {
                detectedGesturesName.Clear();
            }

            previousGestureTime = currentTime;

            detectedGesturesName.Add(gesture);

            if (detectedGesturesName.Count == GestureDetectorsCount)
            {
                RaiseGestureDetected(string.Join(">", detectedGesturesName));
                previousGestureTime = null;
            }
        }
    }
}
