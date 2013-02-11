using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox
{
    public class TemplatedPostureDetector : PostureDetector
    {
        public float Epsilon { get; set; }
        public float MinimalScore { get; set; }
        public float MinimalSize { get; set; }
        readonly LearningMachine learningMachine;
        readonly string postureName;

        public LearningMachine LearningMachine
        {
            get { return learningMachine; }
        }

        public TemplatedPostureDetector(string postureName, Stream kbStream) : base(4)
        {
            this.postureName = postureName;
            learningMachine = new LearningMachine(kbStream);

            MinimalScore = 0.95f;
            MinimalSize = 0.1f;
            Epsilon = 0.02f;
        }

        public override void TrackPostures(Skeleton skeleton)
        {
            if (LearningMachine.Match(skeleton.Joints.ToListOfVector2(), Epsilon, MinimalScore, MinimalSize))
                RaisePostureDetected(postureName);
        }

        public void AddTemplate(Skeleton skeleton)
        {
            RecordedPath recordedPath = new RecordedPath(skeleton.Joints.Count);

            recordedPath.Points.AddRange(skeleton.Joints.ToListOfVector2());

            LearningMachine.AddPath(recordedPath);
        }

        public void SaveState(Stream kbStream)
        {
            LearningMachine.Persist(kbStream);
        }
    }
}
