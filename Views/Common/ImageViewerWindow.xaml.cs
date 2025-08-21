using System.Windows;
using System.Windows.Media.Imaging;

namespace DrJaw.Views.Common
{
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(BitmapSource image, string? title = null)
        {
            InitializeComponent();
            if (!string.IsNullOrWhiteSpace(title))
                Title = title;
            Preview.Source = image;
        }
    }
}
