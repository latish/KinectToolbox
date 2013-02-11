using System.IO;
using Microsoft.Kinect;

namespace Kinect.Toolbox.Record
{
    public class ReplayColorImageFrame : ReplayFrame
    {
        readonly ColorImageFrame internalFrame;
        long streamPosition;
        Stream stream;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int BytesPerPixel { get; private set; }
        public ColorImageFormat Format { get; private set; }
        public int PixelDataLength { get; set; }

        public ReplayColorImageFrame(ColorImageFrame frame)
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

        public ReplayColorImageFrame()
        {

        }

        internal override void CreateFromReader(BinaryReader reader)
        {
            TimeStamp = reader.ReadInt64();
            BytesPerPixel = reader.ReadInt32();
            Format = (ColorImageFormat)reader.ReadInt32();
            Width = reader.ReadInt32();
            Height = reader.ReadInt32();
            FrameNumber = reader.ReadInt32();

            PixelDataLength = reader.ReadInt32();

            stream = reader.BaseStream;
            streamPosition = stream.Position;

            stream.Position += PixelDataLength;
        }

        public void CopyPixelDataTo(byte[] pixelData)
        {
            if (internalFrame != null)
            {
                internalFrame.CopyPixelDataTo(pixelData);
                return;
            }

            long savedPosition = stream.Position;
            stream.Position = streamPosition;

            stream.Read(pixelData, 0, PixelDataLength);

            stream.Position = savedPosition;
        }

        public static implicit operator ReplayColorImageFrame(ColorImageFrame frame)
        {
            return new ReplayColorImageFrame(frame);
        }
    }
}
