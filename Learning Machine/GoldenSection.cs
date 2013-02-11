using System;
using System.Collections.Generic;

namespace Kinect.Toolbox.Gestures.Learning_Machine
{
    public static class GoldenSection
    {
        static readonly float ReductionFactor = 0.5f * (-1 + (float)Math.Sqrt(5));
        static readonly float Diagonal = (float)Math.Sqrt(2);

        public static float Search(List<Vector2> current, List<Vector2> target, float a, float b, float epsilon)
        {
            float x1 = ReductionFactor * a + (1 - ReductionFactor) * b;
            List<Vector2> rotatedList = current.Rotate(x1);
            float fx1 = rotatedList.DistanceTo(target);

            float x2 = (1 - ReductionFactor) * a + ReductionFactor * b;
            rotatedList = current.Rotate(x2);
            float fx2 = rotatedList.DistanceTo(target);

            do
            {
                if (fx1 < fx2)
                {
                    b = x2;
                    x2 = x1;
                    fx2 = fx1;
                    x1 = ReductionFactor * a + (1 - ReductionFactor) * b;
                    rotatedList = current.Rotate(x1);
                    fx1 = rotatedList.DistanceTo(target);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    fx1 = fx2;
                    x2 = (1 - ReductionFactor) * a + ReductionFactor * b;
                    rotatedList = current.Rotate(x2);
                    fx2 = rotatedList.DistanceTo(target);
                }
            }
            while (Math.Abs(b - a) > epsilon);

            float min = Math.Min(fx1, fx2);

            return 1.0f - 2.0f * min / Diagonal;
        }

        static List<Vector2> ProjectListToDefinedCount(List<Vector2> positions, int n)
        {
            List<Vector2> source = new List<Vector2>(positions);
            List<Vector2> destination = new List<Vector2>
                                            {
                                                source[0]
                                            };

            // define the average length of each segment
            float averageLength = positions.Length() / (n - 1);
            float currentDistance = 0;


            for (int index = 1; index < source.Count; index++)
            {
                Vector2 pt1 = source[index - 1];
                Vector2 pt2 = source[index];

                float distance = (pt1 - pt2).Length;
                // If the distance between the 2 points is greater than average length, we introduce a new point
                if ((currentDistance + distance) >= averageLength)
                {
                    Vector2 newPoint = pt1 + ((averageLength - currentDistance) / distance) * (pt2 - pt1);

                    destination.Add(newPoint);
                    source.Insert(index, newPoint);
                    currentDistance = 0;
                }
                else
                {
                    // merging points by ignoring it
                    currentDistance += distance;
                }
            }

            if (destination.Count < n)
            {
                destination.Add(source[source.Count - 1]);
            }

            return destination;
        }

        // A bit of trigonometry
        public static float GetAngleBetween(Vector2 start, Vector2 end)
        {
            if (start.X != end.X)
            {
                return (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
            }

            if (end.Y > start.Y)
                return MathHelper.PiOver2;

            return -MathHelper.PiOver2;
        }

        // Resample to required length then rotate to get first point at 0 radians, scale to 1x1 and finally center the path to (0,0)
        public static List<Vector2> Pack(List<Vector2> positions, int samplesCount)
        {
            List<Vector2> locals = ProjectListToDefinedCount(positions, samplesCount);

            float angle = GetAngleBetween(locals.Center(), positions[0]);
            locals = locals.Rotate(-angle);

            locals.ScaleToReferenceWorld();
            locals.CenterToOrigin();

            return locals;
        }
    }
}

