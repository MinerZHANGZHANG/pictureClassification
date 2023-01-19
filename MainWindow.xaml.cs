using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;

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
        //多线程
        private BackgroundWorker backgroundWorker;

        /// <summary>
        /// 窗口初始化
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            backgroundWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
        }

        #region ——控件事件——
        //图片分类
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                System.Windows.MessageBox.Show("当前已经存在分类任务了");
                return;
            }
            switch (OutputMode.SelectedIndex)
            {
                case 0:
                    if (String.IsNullOrEmpty(ImageInputDirPath) || String.IsNullOrEmpty(ClassificatModelPath))
                    {
                        System.Windows.MessageBox.Show("需要先选择模型/图片输入目录");
                    }
                    else
                    {                       
                        ClassifyAndShowBox();
                    }
                    break;

                case 1:
                    if (String.IsNullOrEmpty(ImageInputDirPath) || String.IsNullOrEmpty(ClassificatedOutputDirPath) || String.IsNullOrEmpty(ClassificatModelPath))
                    {
                        System.Windows.MessageBox.Show("需要先选择模型/图片输入/输出目录");
                    }
                    else
                    {
                        ClassifyArgument classifyArgument = new ClassifyArgument(
                            ClassificatModelPath,
                            ImageInputDirPath,
                            ClassificatedOutputDirPath,
                            OutOtherFile.IsChecked.Value);
                        backgroundWorker.RunWorkerAsync(classifyArgument);
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
               
                System.Windows.MessageBox.Show($"已载入目录下{Directory.GetFiles(SelectPathDialog.SelectedPath).Length}个文件");
                RunProgressBar.Value = 0;
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


        private void ClassifyAndShowBox()
        {
            ImageSolution.Classificated(ClassificatModelPath, ImageInputDirPath, ImageShow, ref RunProgressBar);           
        }

        public struct ClassifyArgument
        {
            public string ClassificatModelPath;
            public string ImageInputDirPath;
            public string ClassificatedOutputDirPath;
            public bool isOutOtherFile;

            public ClassifyArgument(string classificatModelPath, string imageInputDirPath, string classificatedOutputDirPath, bool isOutOtherFile)
            {
                ClassificatModelPath = classificatModelPath;
                ImageInputDirPath = imageInputDirPath;
                ClassificatedOutputDirPath = classificatedOutputDirPath;
                this.isOutOtherFile = isOutOtherFile;
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            ClassifyArgument classifyArgument = (ClassifyArgument)e.Argument;

            ImageSolution.Classificated(
                classifyArgument.ClassificatModelPath, 
                classifyArgument.ImageInputDirPath, 
                classifyArgument.ClassificatedOutputDirPath,
                worker, 
                e,
                classifyArgument.isOutOtherFile);
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {  
           RunProgressBar.Value= e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error!= null)
            {
                System.Windows.MessageBox.Show($"分类过程中发生了{e.Error}异常");
            }
        }

        private void OverClassifity_Click(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.WorkerSupportsCancellation&&backgroundWorker.IsBusy)
            {
                backgroundWorker.CancelAsync();
            }
            else
            {
                System.Windows.MessageBox.Show("没有可以终止的分类任务");
            }
        }
    }
}
