using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Kinect.Toolbox.Gestures.Learning_Machine;

namespace Kinect.Toolbox
{
    public class LearningMachine
    {
        readonly List<RecordedPath> paths;

        public LearningMachine(Stream kbStream)
        {
            if (kbStream == null || kbStream.Length == 0)
            {
                paths = new List<RecordedPath>();
                return;
            }

            BinaryFormatter formatter = new BinaryFormatter {Binder = new CustomBinder()};


            paths = (List<RecordedPath>)formatter.Deserialize(kbStream);
        }

        public List<RecordedPath> Paths
        {
            get { return paths; }
        }

        public bool Match(List<Vector2> entries, float threshold, float minimalScore, float minSize)
        {
            return Paths.Any(path => path.Match(entries, threshold, minimalScore, minSize));
        }

        public void Persist(Stream kbStream)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(kbStream, Paths);
        }

        public void AddPath(RecordedPath path)
        {
            path.CloseAndPrepare();
            Paths.Add(path);
        }
    }
}
