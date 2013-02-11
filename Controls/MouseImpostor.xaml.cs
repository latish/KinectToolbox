using System;
using System.Windows.Controls;

namespace Kinect.Toolbox
{
    /// <summary>
    /// Interaction logic for MouseImpostor.xaml
    /// </summary>
    public partial class MouseImpostor : UserControl
    {
        public event Action OnProgressionCompleted;

        public MouseImpostor()
        {
            InitializeComponent();
        }

        public int Progression
        {
            set
            {
                if (value == 0 || value > 100)
                {
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    if (value > 100 && OnProgressionCompleted != null)
                        OnProgressionCompleted();
                }
                else
                {
                    progressBar.Visibility = System.Windows.Visibility.Visible;
                    progressBar.Value = value;

                }
            }
        }
    }
}
