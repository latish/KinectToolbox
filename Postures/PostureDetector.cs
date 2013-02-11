using System;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public abstract class PostureDetector
    {
        public event Action<string> PostureDetected;

        readonly int accumulatorTarget;
        string previousPosture = "";
        int accumulator;
        string accumulatedPosture = "";

        public string CurrentPosture
        {
            get { return previousPosture; }
            protected set { previousPosture = value; }
        }

        protected PostureDetector(int accumulators)
        {
            accumulatorTarget = accumulators;
        }

        public abstract void TrackPostures(Skeleton skeleton);
        
        protected void RaisePostureDetected(string posture)
        {
            if (accumulator < accumulatorTarget)
            {
                if (accumulatedPosture != posture)
                {
                    accumulator = 0;
                    accumulatedPosture = posture;
                }
                accumulator++;
                return;
            }

            if (previousPosture == posture)
                return;

            previousPosture = posture;
            if (PostureDetected != null)
                PostureDetected(posture);

            accumulator = 0;
        }

        protected void Reset()
        {
            previousPosture = "";
            accumulator = 0;
        }
    }
}
