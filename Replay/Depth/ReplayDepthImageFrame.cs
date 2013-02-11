using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox.Record
{
    public class ReplayDepthImageFrame : ReplayFrame
    {
        readonly DepthImageFrame internalFrame;
        long streamPosition;
        Stream stream;
        BinaryReader streamReader;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BytesPerPixel { get; private set; }
        public DepthImageFormat Format { get; private set; }
        public int PixelDataLength { get; set; }

        public ReplayDepthImageFrame(DepthImageFrame frame)
        {
            Format = frame.Format;
            BytesPerPixel = frame.BytesPerPixel;
            FrameNumber = frame.FrameNumber;
            TimeStamp = frame.Timestamp;
            Width = frame.Width;
            Height = frame.Height;

            PixelDataLength = frame.PixelDataLength;
            internalFrame = frame;
        }

        public ReplayDepthImageFrame()
        {
            
        }

        internal override void CreateFromReader(BinaryReader reader)
        {
            TimeStamp = reader.ReadInt64();
            BytesPerPixel = reader.ReadInt32();
            Format = (DepthImageFormat)reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            FrameNumber = reader.ReadInt32();

            PixelDataLength = reader.ReadInt32();

            stream = reader.BaseStream;
            streamReader = reader;
            streamPosition = stream.Position;

            stream.Position += PixelDataLength * 2;
        }

        public void CopyPixelDataTo(short[] pixelData)
        {
            if (internalFrame != null)
            {
                internalFrame.CopyPixelDataTo(pixelData);
                return;
            }

            long savedPosition = stream.Position;
            stream.Position = streamPosition;

            for (int index = 0; index < PixelDataLength; index++)
            {
                pixelData[index] = streamReader.ReadInt16();
            }

            stream.Position = savedPosition;
        }

        public static implicit operator ReplayDepthImageFrame(DepthImageFrame frame)
        {
            return new ReplayDepthImageFrame(frame);
        }
    }
}
