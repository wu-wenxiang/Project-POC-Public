using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UFileClient.Display;

namespace UFileClient.Display
{
    /// <summary>
    /// SingleImageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SingleImageWindow : Window
    {
        public SingleImageWindow()
        {
            InitializeComponent();
            singleImageWindow.SetAllButtonEnable(true);
        }

        private ExplorerImage container;
        public ExplorerImage Container
        {
            set
            {
                container = value;
                singleImageWindow.Thumbnail = value.thumbs;
            }
            get
            {
                return container;
            }
        }
        public UFileClient.Display.SingleImage SingeImage
        {
            get
            {
                return FindName("singleImageWindow") as UFileClient.Display.SingleImage;
            }
        }
    }
}
