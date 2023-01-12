using ImageClassification.Dialog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;
using static ImageClassification.ImageSolution;

namespace ImageClassification
{
    /// <summary>
    /// 图片数据类
    /// </summary>
    public class ImageTagData
    {
        //图片路径
        public string ImagePath;
        //标签
        public string Label;
    }

    /// <summary>
    /// TrainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TrainWindow : Window
    {
        public ImageNetSetting imageNetSetting;

        //图片文件夹输入路径
        protected string ImageInputDirPath;
        //模型输出路径
        protected string ModelOutputDirPath;

        //图片文件列表
        protected List<FileInfo> imageInfos=new();
        //图片计数
        protected int imageCount = 0;
        //标签列表
        private List<string> tagsList=new();


        /// <summary>
        /// 窗体初始化函数
        /// </summary>
        public TrainWindow()
        {
            InitializeComponent();
            //添加自定义鼠标点击事件到放置图片标签的ListBox
            TagListBox.AddHandler(System.Windows.Controls.ListBox.MouseLeftButtonUpEvent, new MouseButtonEventHandler(TagListBox_PreviewMouseRightButtonUp), true);

            imageNetSetting = DefaultImageSetting;
        }

        
        /// <summary>
        /// 添加图片数据到csv文件
        /// </summary>
        /// <param name="imageTagData">图片数据(标签和图片路径)</param>
        public void AddToCSV(ImageTagData imageTagData)
        {
            if (string.IsNullOrEmpty(ImageInputDirPath))
            {
                System.Windows.MessageBox.Show("未选择图片输入路径");
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
            if(directoryInfo.Parent.Exists)
            {
                //写入数据到csv文件
                string csvPath = Path.Combine(directoryInfo.Parent.FullName, $"{directoryInfo.Name}-ImagesTag.csv");
                FileStream fileStream = new FileStream(csvPath, FileMode.Append);
                StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8"));
                streamWriter.WriteLine($"{imageTagData.ImagePath},{imageTagData.Label}");
                //关闭文件流
                streamWriter.Flush();
                streamWriter.Close();
                fileStream.Close();
            }
            else
            {
                System.Windows.MessageBox.Show($"{directoryInfo.FullName}的父目录不存在，请选择别的目录");
            }      
        }

        /// <summary>
        /// 显示图片（根据图片计数ImageCount和图片列表imageInfos）
        /// </summary>
        protected void ShowImage()
        {           
            //判断图片计数是否溢出
            if (imageCount >= imageInfos.Count)
            {
                return;
            }
            //修改计数文本
            Count.Content = $"当前{imageCount + 1}/{imageInfos.Count}";
            //设置图片到控件
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(imageInfos[imageCount].FullName);
            bitmapImage.EndInit();
            ImageShow.Source = bitmapImage;     
        }

        #region ——窗体事件——
        /// <summary>
        /// 按钮事件，选择输入的图片路径
        /// </summary>
        private void ImageInputPath_Click(object sender, RoutedEventArgs e)
        {
            var SelectPathDialog = new FolderBrowserDialog();
            SelectPathDialog.Description = "选择导入图片的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            DialogResult result = SelectPathDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                imageInfos.Clear();
                imageCount = 0;
                InputPath.Content = SelectPathDialog.SelectedPath;
                ImageInputDirPath = SelectPathDialog.SelectedPath;

                DirectoryInfo ImagesDir = new DirectoryInfo(ImageInputDirPath);
                imageInfos.AddRange(ImagesDir.GetFiles("*.jpg"));
                imageInfos.AddRange(ImagesDir.GetFiles("*.png"));
                imageInfos.AddRange(ImagesDir.GetFiles("*.bmp"));

                System.Windows.MessageBox.Show($"已经添加了{imageInfos.Count}幅图片");

                string csv_path = Path.Combine(ImagesDir.Parent.FullName, $"{ImagesDir.Name}-ImagesTag.csv");
                if (File.Exists(csv_path))
                {
                    //统计文件中的记录数量
                    string[]? lines = File.ReadAllLines(csv_path);
                    int count = lines.Length;

                    MessageBoxResult boxResult= System.Windows.MessageBox.Show($"检测到已经添加标签文件，\n其中包含{count}条记录\n是否重置该文件",caption:"是否重置文件",MessageBoxButton.YesNo);
                    if (boxResult == MessageBoxResult.Yes)
                    {
                        File.Delete(csv_path);
                    }
                }

                ShowImage();
            }

            
        }

        /// <summary>
        /// 按钮事件，选择分类后的图片输出路径
        /// </summary>
        private void ImageOutputPath_Click(object sender, RoutedEventArgs e)
        {
            var SelectPathDialog = new FolderBrowserDialog();
            SelectPathDialog.Description = "选择输出结果的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            DialogResult result = SelectPathDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                OutputPath.Content = SelectPathDialog.SelectedPath;
                ModelOutputDirPath = SelectPathDialog.SelectedPath;
            }
        }

        /// <summary>
        /// 按钮事件，添加用户输入的tag到ListBox
        /// </summary>
        private void TagAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TagInput.Text))
            {
                System.Windows.MessageBox.Show($"输入tag不能为空");
            }
            else if (tagsList.Contains(TagInput.Text))
            {
                System.Windows.MessageBox.Show($"已经添加过tag {TagInput.Text} 了");
            }
            else
            {
                tagsList.Add(TagInput.Text);
                TagListBox.Items.Add(TagInput.Text);
            }
        }

        /// <summary>
        /// 按钮事件，保存模型到特定路径
        /// </summary>
        private void ModelSave_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
            string csvTagPath = Path.Combine(directoryInfo.Parent.FullName, $"{directoryInfo.Name}-ImagesTag.csv");
            if (ImageInputDirPath == null)
            {

            }
            else if (csvTagPath == null)
            {

            }
            else if (ModelName.Text == null)
            {

            }
            else
            {
                ImageSolution.TrainAndSaveModel(csvTagPath, ImageInputDirPath, ModelOutputDirPath, imageNetSetting, ModelName.Text);
            }
                
        }

        /// <summary>
        /// ListBox点击事件，根据被点击的标签来设置图片的类型，并输出到文件
        /// </summary>
        private void TagListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //System.Windows.MessageBox.Show("我被点击了");
            if (TagListBox.SelectedItem != null)
            {
                if (imageCount >= imageInfos.Count)
                {
                    System.Windows.MessageBox.Show($"文件夹中所有图片都被分配了标签");
                }
                else
                {
                    ImageTagData imageTagData = new ImageTagData { ImagePath = imageInfos[imageCount].Name, Label = TagListBox.SelectedItem.ToString() };
                    AddToCSV(imageTagData);
                    imageCount += 1;                    
                    if (imageCount == imageInfos.Count)
                    {
                        System.Windows.MessageBox.Show("已经完成了这个文件下图片的标记");
                    }
                    else
                    {
                        ShowImage();
                    }
                    
                }
            }
        }

        /// <summary>
        /// 按钮事件，开始自动训练
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoTrain_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
            string csvTagPath = Path.Combine(directoryInfo.Parent.FullName, $"{directoryInfo.Name}-ImagesTag.csv");
            if (ImageInputDirPath == null)
            {

            }
            else if (csvTagPath == null)
            {

            }
            else if (ModelName.Text == null)
            {
            }
            else if(uint.TryParse(TrainTime.Text,out uint result))
            {
                System.Windows.MessageBox.Show("自动多次训练还存在问题");              
                //ImageSolution.AutoTrainAndSave(csvTagPath, ImageInputDirPath, ModelOutputDirPath, result, ImageSolution.DefaultImageSetting);
            }
            else
            {
                
            }
        }
        
        ///// <summary>
        ///// 按钮事件，打开训练集Csv文件
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void OpenCsv_Click(object sender, RoutedEventArgs e)
        //{
        //    if (string.IsNullOrEmpty(ImageInputDirPath))
        //    {
        //        return;
        //    }
        //    DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
        //    string csvTagPath = Path.Combine(directoryInfo.Parent.FullName, $"{directoryInfo.Name}-ImagesTag.csv");
        //    if (File.Exists(csvTagPath))
        //    {
        //        System.Diagnostics.Process csvViewProcess = new System.Diagnostics.Process();
        //        csvViewProcess.StartInfo.FileName= csvTagPath;
        //        csvViewProcess.StartInfo.CreateNoWindow = true;
        //        csvViewProcess.StartInfo.UseShellExecute= false;
        //        csvViewProcess.Start();
        //        csvViewProcess.WaitForExit();
        //    }
        //}

        #endregion

        private void ImageSetting_Click(object sender, RoutedEventArgs e)
        {
            ImageSettingDialog imageSettingDialog = new ImageSettingDialog(this);
            bool? result= imageSettingDialog.ShowDialog();
            if (result == true) 
            {
                System.Windows.MessageBox.Show($"已重新设置图片处理属性为:\n高度:{imageNetSetting.imageHeight}" +
                $"\n宽度:{imageNetSetting.imageWidth}\n颜色值偏移量:{imageNetSetting.mean}\n颜色值缩放量:{imageNetSetting.scale}");
            }
        }

        private void ImportImages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ImageInputDirPath))
            {
                System.Windows.MessageBox.Show("需要先选择导入训练图片的文件夹");
                return;
            }

            TagSelectDialog tagSelectDialog = new TagSelectDialog(this,ImageInputDirPath);
            bool? result= tagSelectDialog.ShowDialog();
        }
    }
}
