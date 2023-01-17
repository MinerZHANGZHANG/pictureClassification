using System.Windows;


namespace ImageClassification.Dialog
{
    /// <summary>
    /// ImageSettingDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ImageSettingDialog : Window
    {
        private TrainWindow parentWindow;
        public ImageSettingDialog(TrainWindow parentWindow)
        {
            InitializeComponent();
            this.parentWindow = parentWindow;
            HeightTextBox.Text=parentWindow.imageNetSetting.imageHeight.ToString();
            WidthTextBox.Text=parentWindow.imageNetSetting.imageWidth.ToString();
            ColorOffsetTextBox.Text=parentWindow.imageNetSetting.mean.ToString();
            ColorScaleTextBox.Text=parentWindow.imageNetSetting.scale.ToString();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(HeightTextBox.Text,out int height)
                && int.TryParse(WidthTextBox.Text, out int width)
                && int.TryParse(ColorOffsetTextBox.Text, out int colorOffset)
                && float.TryParse(ColorScaleTextBox.Text, out float colorScale))
            {
                parentWindow.imageNetSetting = new ImageSolution.ImageNetSetting(height, width, colorOffset, colorScale, true);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("请输入正确的数值", "输入异常", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
