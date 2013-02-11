using System.Windows.Media.Imaging;

namespace Kinect.Toolbox
{
    public interface IStreamManager
    {
        WriteableBitmap Bitmap { get; }
    }
}