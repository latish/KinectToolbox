using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Kinect;
using System;

namespace Kinect.Toolbox
{
    public class ContextTracker
    {
        readonly Dictionary<int, List<ContextPoint>> points = new Dictionary<int, List<ContextPoint>>();
        readonly int windowSize;

        public float Threshold { get; set; }

        public ContextTracker(int windowSize = 40, float threshold = 0.05f)
        {
            this.windowSize = windowSize;
            Threshold = threshold;
        }

        public void Add(Vector3 position, int trackingID)
        {
            if (!points.ContainsKey(trackingID))
            {
                points.Add(trackingID, new List<ContextPoint>());
            }

            points[trackingID].Add(new ContextPoint() { Position = position, Time = DateTime.Now });

            if (points[trackingID].Count > windowSize)
            {
                points[trackingID].RemoveAt(0);
            }
        }

        public void Add(Skeleton skeleton, JointType jointType)
        {
            var trackingID = skeleton.TrackingId;
            var position = skeleton.Joints.Where(j => j.JointType == jointType).First().Position.ToVector3();

            Add(position, trackingID);
        }

        public bool IsStable(int trackingID)
        {
            List<ContextPoint> currentPoints = points[trackingID];
            if (currentPoints.Count != windowSize)
                return false;

            Vector3 current = currentPoints[currentPoints.Count - 1].Position;

            Vector3 average = Vector3.Zero;

            for (int index = 0; index < currentPoints.Count - 2; index++)
            {
                average += currentPoints[index].Position;
            }

            average /= currentPoints.Count - 1;

            if ((average - current).Length > Threshold)
                return false;


            return true;
        }

        public bool IsStableRelativeToCurrentSpeed(int trackingID)
        {
            List<ContextPoint> currentPoints = points[trackingID];
            if (currentPoints.Count < 2)
                return false;

            Vector3 previousPosition = currentPoints[currentPoints.Count - 2].Position;
            Vector3 currentPosition = currentPoints[currentPoints.Count - 1].Position;

            DateTime previousTime = currentPoints[currentPoints.Count - 2].Time;
            DateTime currentTime = currentPoints[currentPoints.Count - 1].Time;

            var currentSpeed = (currentPosition - previousPosition).Length / ((currentTime - previousTime).TotalMilliseconds);

            if (currentSpeed > Threshold)
                return false;

            return true;
        }

        public bool IsStableRelativeToAverageSpeed(int trackingID)
        {
            List<ContextPoint> currentPoints = points[trackingID];
            if (currentPoints.Count != windowSize)
                return false;

            Vector3 startPosition = currentPoints[0].Position;
            Vector3 currentPosition = currentPoints[currentPoints.Count - 1].Position;

            DateTime startTime = currentPoints[0].Time;
            DateTime currentTime = currentPoints[currentPoints.Count - 1].Time;

            var currentSpeed = (currentPosition - startPosition).Length / ((currentTime - startTime).TotalMilliseconds);

            if (currentSpeed > Threshold)
                return false;

            return true;
        }

        public bool IsShouldersTowardsSensor(Skeleton skeleton)
        {
            var leftShoulderPosition = skeleton.Joints.Where(j => j.JointType == JointType.ShoulderLeft).First().Position.ToVector3();
            var rightShoulderPosition = skeleton.Joints.Where(j => j.JointType == JointType.ShoulderRight).First().Position.ToVector3();

            var leftDistance = leftShoulderPosition.Z;
            var rightDistance = rightShoulderPosition.Z;

            if (Math.Abs(leftDistance - rightDistance) > Threshold)
                return false;

            return true;
        }
    }
}
