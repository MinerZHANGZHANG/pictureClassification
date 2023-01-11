using System;
using System.Windows;
using System.Windows.Forms;

namespace ImageClassification
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //图片输入路径
        protected string? ImageInputDirPath;
        //分类结果输出路径
        protected string? ClassificatedOutputDirPath;
        //分类模型路径
        protected string? ClassificatModelPath;

        /// <summary>
        /// 窗口初始化
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        #region ——控件事件——
        //图片分类
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (OutputMode.SelectedIndex)
            {
                case 0:
                    if (String.IsNullOrEmpty(ImageInputDirPath) || String.IsNullOrEmpty(ClassificatModelPath))
                    {
                        System.Windows.MessageBox.Show("需要先选择模型/图片输入目录");
                    }
                    else
                    {
                        ImageSolution.Classificated(ClassificatModelPath, ImageInputDirPath,ImageShow,ref RunProgressBar);
                    }
                    break;

                case 1:
                    if (String.IsNullOrEmpty(ImageInputDirPath) || String.IsNullOrEmpty(ClassificatedOutputDirPath) || String.IsNullOrEmpty(ClassificatModelPath))
                    {
                        System.Windows.MessageBox.Show("需要先选择模型/图片输入/输出目录");
                    }
                    else
                    {
                        ImageSolution.Classificated(ClassificatModelPath, ImageInputDirPath, ClassificatedOutputDirPath,ref RunProgressBar);
                    }
                    break;
            }
            
            
        }

        //图片输入路径选择
        private void ImageInputPath_Click(object sender, RoutedEventArgs e)
        {
            var SelectPathDialog = new FolderBrowserDialog();
            SelectPathDialog.Description = "选择导入图片的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            DialogResult result =SelectPathDialog.ShowDialog();
           
            if (result==System.Windows.Forms.DialogResult.OK)
            {
                InputPath.Content = SelectPathDialog.SelectedPath;
                ImageInputDirPath= SelectPathDialog.SelectedPath;
                System.Windows.MessageBox.Show("已经载入图片");
            }
        }

        //图片输出路径选择
        private void ImageOutputPath_Click(object sender, RoutedEventArgs e)
        {
            var SelectPathDialog = new FolderBrowserDialog();
            SelectPathDialog.Description = "选择输出结果的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            DialogResult result = SelectPathDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
               OutputPath.Content = SelectPathDialog.SelectedPath;
               ClassificatedOutputDirPath= SelectPathDialog.SelectedPath; 
            }
        }

        //模型选择
        private void ModelSelect_Click(object sender, RoutedEventArgs e)
        {
            var SelectDialog = new OpenFileDialog();
            SelectDialog.Title = "模型选择";
            SelectDialog.Filter = "模型文件|*.zip";
            var result=SelectDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                String path = SelectDialog.FileName.ToString();
                ModelPath.Content = path.Split("\\")[path.Split("\\").Length-1];
                ClassificatModelPath = path;
            }
        }


        //打开训练窗口
        private void TrainModel_Click(object sender, RoutedEventArgs e)
        {
            var newWindow=new TrainWindow();
            var result=  newWindow.ShowDialog();

            if (result == true)
            {
                System.Windows.MessageBox.Show("训练了不错的模型!");
            }
        }

        #endregion
    }
}
