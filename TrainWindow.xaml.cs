using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using ImageClassification.Dialog;
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
        #region ——字段——
        //图片预处理设置
        public ImageNetSetting imageNetSetting;

        //图片文件夹输入路径
        public string ImageInputDirPath;
        //模型输出路径
        protected string ModelOutputDirPath;
        //标记文件保存路径
        protected string TagFilePath;
        //标记文件夹名称
        protected const string tagDirName = "ImageTag";
        //标记文件的后缀(分隔符定义在ImageSolution类里了)
        private const string tagFileSuffix = "tsv";

        //目录下图片文件列表
        protected List<FileInfo> imageInfos=new();
        //目录下未识别的文件夹列表
        private List<DirectoryInfo> directoryInfos = new(); 
        //目录下的非图片文件列表
        private List<FileInfo> otherInfos= new();

        //图片计数
        protected int imageCount = 0;
        
        //添加标签时是否覆盖之前的标签
        private bool isReplaceTag;

        #endregion

        /// <summary>
        /// 窗体初始化函数
        /// </summary>
        public TrainWindow()
        {
            InitializeComponent();
            //添加自定义鼠标点击事件到放置图片标签的ListBox
            TagListBox.AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(TagListBox_PreviewMouseRightButtonUp), true);
     
            imageNetSetting = DefaultImageSetting;
            isReplaceTag=IsUpdateTag.IsChecked.Value;

            //如果标签文件夹不存在就新建一个
            if (!Directory.Exists(tagDirName))
            {
                Directory.CreateDirectory(tagDirName);
            }
        }

        
        /// <summary>
        /// 添加图片数据到文件，格式为[图片名称][分隔符][图片标签]
        /// </summary>
        /// <param name="imageTagData">图片数据(标签和图片路径)</param>
        public void AddTagToFile(ImageTagData imageTagData)
        {
            //判断图片输入路径是否存在
            if (string.IsNullOrEmpty(ImageInputDirPath))
            {
                System.Windows.MessageBox.Show("未选择图片输入路径");
                return;
            }
            //是否采用替换方式添加标签 且 标签文件存在
            if (isReplaceTag && File.Exists(TagFilePath))
            {
                //读取标签文件，把其中对应的标签信息进行替换
                string[] lines = File.ReadAllLines(TagFilePath);
                string[] imageInfo;
                for (int i = 0; i < lines.Length; i++)
                {
                    imageInfo = lines[i].Split(fileSplit);
                    if (imageInfo[0] == imageInfos[imageCount].Name)
                    {
                        lines[i] = $"{imageInfo[0]}{fileSplit}{imageTagData.Label}";
                        File.Delete(TagFilePath);
                        File.WriteAllLines(TagFilePath, lines);

                        return;
                    }
                }
                //如果找不到就添加到最后
                using (FileStream fileStream = new FileStream(TagFilePath, FileMode.Append))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8")))
                    {
                        streamWriter.WriteLine($"{imageTagData.ImagePath}{fileSplit}{imageTagData.Label}");
                        streamWriter.Flush();
                    }
                }

            }
            else
            {
                using (FileStream fileStream = new FileStream(TagFilePath, FileMode.Append))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8")))
                    {
                        streamWriter.WriteLine($"{imageTagData.ImagePath}{fileSplit}{imageTagData.Label}");
                        streamWriter.Flush();
                    }
                }
            }
                     
        }

        /// <summary>
        /// 显示图片（根据图片计数ImageCount和图片列表imageInfos）
        /// </summary>
        protected void ShowImage()
        {           
            //判断图片计数是否溢出
            if (imageCount >= imageInfos.Count||imageCount<0)
            {
                return;
            }
            //修改计数文本控件
            Count.Content = $"{imageCount + 1}/{imageInfos.Count}";
            //转换为位图显示到图片控件
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(imageInfos[imageCount].FullName);
            bitmapImage.EndInit();
            ImageShow.Source = bitmapImage;
            //设置图片标签文本
            TryGetImageTag(out string imageTag);
            ImageTagTextBlock.Text = $"标签:{imageTag}";
            //设置图片路径文本
            ImagePathTextBlock.Text = $"路径:{imageInfos[imageCount].FullName}";
        }


 
        /// <summary>
        /// 获取图片的标签
        /// </summary>
        /// <param name="imageTag">图片标签</param>
        /// <returns>是否有标签</returns>
        protected bool TryGetImageTag(out string imageTag)
        {
            //判断是否选择了图片文件夹
            if (string.IsNullOrEmpty(ImageInputDirPath))
            {
                System.Windows.MessageBox.Show("未选择导入图片的文件夹路径");
                imageTag = "";
                return false;
            }

            //判断标签文件是否存在
            if (!File.Exists(TagFilePath))
            {
                imageTag = "未添加标记";
                return false;
            }

            //图片文件夹信息
            DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
            //获取图片名称
            var curImageName = imageInfos[imageCount].Name;
           
            //判断当前图片在所选目录下是否存在
            if (File.Exists(Path.Combine(directoryInfo.FullName,curImageName)))
            {
                //挨个比较标签记录信息，如果找到对应的图片就返回true和输出标签名称
                string[] lines = File.ReadAllLines(TagFilePath, Encoding.GetEncoding("utf-8"));
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Split(fileSplit)[0] == curImageName)
                    {
                        imageTag = lines[i].Split(fileSplit)[1];
                        return true;
                    }
                }
                imageTag = "未添加标记";
                return false;
            }
            else
            {
                imageTag = "当前目录下未找到图片";
                return false;
            }
        
        }


        #region ——窗体事件——
        /// <summary>
        /// 按钮事件，选择输入的图片路径
        /// </summary>
        private void ImageInputPath_Click(object sender, RoutedEventArgs e)
        {
            //创建文件选择对话框
            var SelectPathDialog = new FolderBrowserDialog();
            //设置对话框属性
            SelectPathDialog.Description = "选择导入图片的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            //根据对话框选择的结果进行操作
            DialogResult result = SelectPathDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //清空已有的图片信息
                imageInfos.Clear();
                //清空已有的子文件夹信息和其它文件信息
                otherInfos.Clear();
                directoryInfos.Clear();

                //图片计数归零
                imageCount = 0;
                //设置图片导入路径
                InputPath.Content = SelectPathDialog.SelectedPath;
                ImageInputDirPath = SelectPathDialog.SelectedPath;

                //获取所选文件夹信息
                DirectoryInfo ImagesDir = new DirectoryInfo(ImageInputDirPath);
                //添加图片文件
                imageInfos.AddRange(ImagesDir.GetFiles("*.jpg"));
                imageInfos.AddRange(ImagesDir.GetFiles("*.jpeg"));
                imageInfos.AddRange(ImagesDir.GetFiles("*.png"));
                imageInfos.AddRange(ImagesDir.GetFiles("*.bmp"));
                //添加文件夹文件
                directoryInfos.AddRange(ImagesDir.GetDirectories());
                //添加其它未识别的文件(没写)

                //输出添加的图片数量
                System.Windows.MessageBox.Show($"已经添加了{imageInfos.Count}幅图片");

                //弹窗选择是否乱序导入图片
                MessageBoxResult messageBoxResult= System.Windows.MessageBox.Show("是否使用乱序导入图片\n", "导入方式", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    int index;
                    FileInfo temp; 
                    //遍历并随机交换文件在列表中的位置
                    for (int i = 0; i < imageInfos.Count; i++)
                    {
                        index = new Random().Next(i, imageInfos.Count);                       
                        temp = imageInfos[i];
                        imageInfos[i] = imageInfos[index];
                        imageInfos[index] = temp;
                    }
                }


                //设置该路径对应的标签文件路径
                TagFilePath = Path.Combine(tagDirName, $"ImagesTag-{ImagesDir.Parent.Name}-{ImagesDir.Name}.{tagFileSuffix}");

                //判断是否已经存在对应文件夹的标签文件
                if (File.Exists(TagFilePath))
                {
                    //统计文件中的记录数量
                    string[]? lines = File.ReadAllLines(TagFilePath);
                    int count = lines.Length;

                    //将标签文件中的标签添加到ListBox控件
                    TagListBox.Items.Clear();
                    for(int i = 0; i < count; i++)
                    {
                        if (!TagListBox.Items.Contains(lines[i].Split(fileSplit)[1]))
                        {
                            TagListBox.Items.Add(lines[i].Split(fileSplit)[1]);
                        }
                    }
                    

                    //弹窗选择是否重置标签文件
                    MessageBoxResult boxResult= System.Windows.MessageBox.Show($"检测到该文件夹已经添加标签文件，是否重置该文件",caption:"是否重置文件",MessageBoxButton.YesNo);
                    if (boxResult == MessageBoxResult.Yes)
                    {
                        //二次确认
                        MessageBoxResult secondResult = System.Windows.MessageBox.Show($"确定要删除记录吗?\n其中包含{count}条图片标签记录", caption: "是否重置文件", MessageBoxButton.YesNo);
                        if(secondResult == MessageBoxResult.Yes)
                        {
                            File.Delete(TagFilePath);
                            System.Windows.MessageBox.Show($"已删除记录:\n{TagFilePath}");
                        }                      
                    }
                }
                //显示图片
                ShowImage();
            }

            
        }

        /// <summary>
        /// 按钮事件，选择分类后的图片输出路径
        /// </summary>
        private void ImageOutputPath_Click(object sender, RoutedEventArgs e)
        {
            //创建文件夹选择窗口
            var SelectPathDialog = new FolderBrowserDialog();
            SelectPathDialog.Description = "选择输出结果的文件夹";
            SelectPathDialog.InitialDirectory = Environment.CurrentDirectory;
            DialogResult result = SelectPathDialog.ShowDialog();
            //设置输出路径字段
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
            //判断输入的Tag是否符合要求
            if (string.IsNullOrEmpty(TagInput.Text))
            {
                System.Windows.MessageBox.Show($"输入tag不能为空");
            }
            else if (TagListBox.Items.Contains(TagInput.Text))
            {
                //System.Windows.MessageBox.Show(TagListBox.Items[0].GetType().ToString());
                System.Windows.MessageBox.Show($"已经添加过tag {TagInput.Text} 了");
            }
            else
            {
                //添加到Listbox控件
                TagListBox.Items.Add(TagInput.Text);
            }
        }

        /// <summary>
        /// 按钮事件，保存模型到特定路径
        /// </summary>
        private void ModelSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ImageInputDirPath) || string.IsNullOrEmpty(ModelOutputDirPath))
            {
                System.Windows.MessageBox.Show($"未选择图片导入路径或模型输出路径");
            }
            else
            {
                TrainState.Text = "正在训练模型...";
                MessageBoxResult secondResult = System.Windows.MessageBox.Show($"确定要开始训练模型吗?\n共标记了{File.ReadAllLines(TagFilePath).Length}幅图片", caption: "是否开始训练文件", MessageBoxButton.YesNo);
                if (secondResult == MessageBoxResult.Yes)
                {                    
                    ImageSolution.TrainAndSaveModel(TagFilePath, ImageInputDirPath, ModelOutputDirPath, imageNetSetting, ModelName.Text);                    
                }
                TrainState.Text = "";
            }
        }

        /// <summary>
        /// ListBox点击事件，根据被点击的标签来设置图片的类型，并输出到文件
        /// </summary>
        private void TagListBox_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //如果选择的标签不为空
            if (TagListBox.SelectedItem != null)
            {
                //当图片计数大于总数时弹窗提示
                if (imageCount >= imageInfos.Count)
                {
                    System.Windows.MessageBox.Show($"文件夹中所有图片都被分配了标签");
                }
                else
                {
                    //根据点击的标签和当前图片序号，写入标记信息到文件中
                    ImageTagData imageTagData = new ImageTagData { ImagePath = imageInfos[imageCount].Name, Label = TagListBox.SelectedItem.ToString() };
                    AddTagToFile(imageTagData);                   
                    //计数达到图片总数时弹窗提示，否则显示下一张图片
                    if (imageCount+1 == imageInfos.Count)
                    {
                        System.Windows.MessageBox.Show("已经完成了这个文件下图片的标记");
                        ShowImage();
                    }
                    else
                    {
                        //当采用叠加的方式添加标签信息时，不跳到下一张图片
                        if (isReplaceTag)
                        {
                            imageCount += 1;
                        }
                        ShowImage();
                    }
                    
                }
            }
        }

        /// <summary>
        /// 按钮事件，开始自动训练(未完成)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoTrain_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("该功能尚未实现");
            //if (string.IsNullOrEmpty(ImageInputDirPath))
            //{
            //    System.Windows.MessageBox.Show("未选择导入图片的文件夹路径");
            //    return;
            //}
            //DirectoryInfo directoryInfo = new DirectoryInfo(ImageInputDirPath);
            //string csvTagPath = Path.Combine(directoryInfo.Parent.FullName, $"{directoryInfo.Name}-ImagesTag.csv");
            //if (csvTagPath == null)
            //{

            //}
            //else if (ModelName.Text == null)
            //{
            //}
            //else if(uint.TryParse(TrainTime.Text,out uint result))
            //{
            //    System.Windows.MessageBox.Show("自动多次训练还存在问题");              
            //    //ImageSolution.AutoTrainAndSave(csvTagPath, ImageInputDirPath, ModelOutputDirPath, result, ImageSolution.DefaultImageSetting);
            //}
            //else
            //{
                
            //}
        }

        /// <summary>
        /// 打开图片预处理窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageSetting_Click(object sender, RoutedEventArgs e)
        {
            ImageSettingDialog imageSettingDialog = new ImageSettingDialog(this);
            bool? result = imageSettingDialog.ShowDialog();
            if (result == true)
            {
                System.Windows.MessageBox.Show($"已重新设置图片处理属性为:\n高度:{imageNetSetting.imageHeight}" +
                $"\n宽度:{imageNetSetting.imageWidth}\n颜色值偏移量:{imageNetSetting.mean}\n颜色值缩放量:{imageNetSetting.scale}");
            }
        }

        /// <summary>
        /// 打开批量导入图片设置标签窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportImages_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ImageInputDirPath))
            {
                System.Windows.MessageBox.Show("需要先选择导入训练图片的文件夹");
                return;
            }

            TagSelectDialog tagSelectDialog = new TagSelectDialog(this);
            bool? result = tagSelectDialog.ShowDialog();
        }

        /// <summary>
        /// 撤销最后一次添加的标签
        /// </summary>
        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            //if(TryGetImageTag(out string imageTag))
            //{
            //    //找到文件中对应的行进行删除
            //    var lines = File.ReadAllLines(TagFilePath);
            //    var imagePath = imageInfos[imageCount].Name;
            //    List<int> delLineList = new();
            //    for(int i = 0; i < lines.Length; i++)
            //    {
            //        if (imagePath == lines[i].Split(fileSplit)[0])
            //        {
            //            delLineList.Add(i);
            //            //可能一个标记文件中有多条对这个图片的标记，所以未使用Break
            //        }
            //    }

            //   // System.Windows.MessageBox.Show($"{lines.ToArray().}");
            //    foreach (int count in delLineList)
            //    {
            //        lines[count] = null;
            //    }
            //    File.Delete(TagFilePath);
            //    File.WriteAllLines(TagFilePath, lines.ToArray());
            //    System.Windows.MessageBox.Show($"已清除对当前图片的{delLineList.Count}条标记");
            //    ShowImage();
            //    return;

            //}
            //else
            //{
            //    System.Windows.MessageBox.Show("当前图片未添加标签");
            //}

            //判断是否有标签文件
            if (imageCount == 0 || string.IsNullOrEmpty(TagFilePath))
            {
                System.Windows.MessageBox.Show("没有可以撤销的操作");
                return;
            }

            //通过读取所有文件，删除最后一行的方式撤回
            if (File.Exists(TagFilePath))
            {
                var lines = File.ReadAllLines(TagFilePath);
                if (lines.Length > 0)
                {
                    File.WriteAllLines(TagFilePath, lines.Take(lines.Length - 1).ToArray());
                    System.Windows.MessageBox.Show($"已撤回最后一次对图片的的标记");
                    ShowImage();
                    return;
                }
            }
            System.Windows.MessageBox.Show("没有可以撤销的操作");


        }

        /// <summary>
        /// 根据输入切换图片
        /// </summary>
        private void SwitchByIndex_Click(object sender, RoutedEventArgs e)
        {
            //判断一下是否有图片
            if(imageInfos.Count==0)
            {
                System.Windows.MessageBox.Show("未导入图片");
                return;
            }
            //转换字符串为整数,并限制范围
            if(uint.TryParse(IndexInput.Text,out uint index))
            {
                //if(index== 0 || index>imageInfos.Count)
                //{                  
                //    System.Windows.MessageBox.Show($"输入的序号不在1到{imageInfos.Count}的范围内");
                //}
                //else
                //{
                //    imageCount = (int)index-1;
                //    ShowImage();
                //}
                imageCount = (int)(Math.Clamp(index, 1, imageInfos.Count) - 1);
                ShowImage();
            }
            else
            {
                IndexInput.Text = string.Empty;
                System.Windows.MessageBox.Show("请输入正确的图片数字序号");
            }
        }


        /// <summary>
        /// 上一张图片按钮
        /// </summary>
        private void PastImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageCount - 1 >= 0)
            {
                imageCount -= 1;
                ShowImage();
            }

        }

        /// <summary>
        /// 下一张图片按钮
        /// </summary>
        private void NextImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageCount + 1 < imageInfos.Count)
            {
                imageCount += 1;
                ShowImage();
            }
        }

        /// <summary>
        /// 是否开启覆盖选项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void isUpdateTag_Checked(object sender, RoutedEventArgs e)
        {
            isReplaceTag = IsUpdateTag.IsChecked.Value;
            //System.Windows.MessageBox.Show(isReplaceTag.ToString());
        }

        /// <summary>
        /// 清空tag标签栏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            TagListBox.Items.Clear();
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



    }
}
