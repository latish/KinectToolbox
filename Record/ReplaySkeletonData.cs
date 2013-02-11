using System.Collections.Generic;
using System.IO;
using Microsoft.Research.Kinect.Nui;
using System.Linq;

namespace Kinect.Toolbox.Record
{
    public class ReplaySkeletonData
    {
        public List<Joint> Joints { get; private set; }
        public Vector Position { get; private set; }
        public SkeletonQuality Quality { get; private set; }
        public int TrackingID { get; private set; }
        public SkeletonTrackingState TrackingState { get; private set; }
        public int UserIndex { get; private set; }

        public ReplaySkeletonData(SkeletonData data)
        {
            Position = data.Position;
            Quality = data.Quality;
            TrackingID = data.TrackingID;
            TrackingState = data.TrackingState;
            UserIndex = data.UserIndex;

            Joints = data.Joints.Cast<Joint>().ToList();
        }

        internal ReplaySkeletonData(BinaryReader reader)
        {
            TrackingState = (SkeletonTrackingState)reader.ReadInt32();
            Position = reader.ReadVector();
            TrackingID = reader.ReadInt32();
            UserIndex = reader.ReadInt32();
            Quality = (SkeletonQuality)reader.ReadInt32();

            int jointsCount = reader.ReadInt32();
            Joints = new List<Joint>();

            for (int index = 0; index < jointsCount; index++)
            {
                Joint joint = new Joint
                                  {
                                      ID = (JointID)reader.ReadInt32(),
                                      TrackingState = (JointTrackingState)reader.ReadInt32(),
                                      Position = reader.ReadVector()
                                  };

                Joints.Add(joint);
            }
        }

        public static implicit operator ReplaySkeletonData(SkeletonData data)
        {
            return new ReplaySkeletonData(data);
        }
    }
}
