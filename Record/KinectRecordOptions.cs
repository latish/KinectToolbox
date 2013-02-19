using System;

namespace Kinect.Toolbox.Record
{
    [FlagsAttribute]
    public enum KinectRecordOptions
    {
        Color = 1,
        Depth = 2,
        Skeletons = 4,
        All =7
    }
}