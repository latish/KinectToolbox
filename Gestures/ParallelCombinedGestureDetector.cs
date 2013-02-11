using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kinect.Toolbox
{
    public class ParallelCombinedGestureDetector : CombinedGestureDetector
    {
        DateTime? firstDetectedGestureTime;
        List<string> detectedGesturesName = new List<string>();

        public ParallelCombinedGestureDetector(double epsilon = 1000)
            : base(epsilon)
        {
        }

        protected override void CheckGestures(string gesture)
        {
            if (!firstDetectedGestureTime.HasValue || detectedGesturesName.Contains(gesture) || DateTime.Now.Subtract(firstDetectedGestureTime.Value).TotalMilliseconds > Epsilon)
            {
                firstDetectedGestureTime = DateTime.Now;
                detectedGesturesName.Clear();
            }

            detectedGesturesName.Add(gesture);

            if (detectedGesturesName.Count == GestureDetectorsCount)
            {
                RaiseGestureDetected(string.Join("&", detectedGesturesName));
                firstDetectedGestureTime = null;
            }
        }
    }
}
