using System;
using System.Collections.Generic;
using System.IO;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace ImageClassification.Dialog
{
    /// <summary>
    /// TagSelectDialog.xaml 的交互逻辑
    /// </summary>
    public partial class TagSelectDialog : Window
    {
        //训练窗口
        private TrainWindow trainWindow;
        //文件夹名称
        private string childDirName;
        //文件夹的图片文件列表
        private List<FileInfo> imageFileList=new();
        //所选标签
        private string selectTag;
        //展示图片的列表
        private List<Image> imageShowList=new();


        public TagSelectDialog(TrainWindow parentWindow)
        {
            InitializeComponent();
            //设置父窗体类
            this.trainWindow = parentWindow;

            //添加图片展示控件到列表
            imageShowList.Add(ImageShow0);
            imageShowList.Add(ImageShow1);
            imageShowList.Add(ImageShow2);

            //将父窗体的图片标签添加到下拉菜单中
            foreach(var tag in parentWindow.TagListBox.Items)
            {
                TagComboBox.Items.Add(tag);
            }
            //如果菜单有子物体，则把当前所选tag设置为第一个子物体
            if (TagComboBox.HasItems)
            {
                TagComboBox.SelectedIndex = 0;
                TagTextBox.Text=TagComboBox.Items[0].ToString();
                selectTag= TagComboBox.Items[0].ToString();
            }                                
        }

        ///// <summary>
        ///// 窗体关闭时的事件
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{

        //}

        /// <summary>
        /// 选择图片文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectImageDir_Click(object sender, RoutedEventArgs e)
        {
            //创建文件夹选择对话框
            var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "选择该目录下，要批量添加标签的文件夹";
            folderDialog.InitialDirectory = trainWindow.ImageInputDirPath;

            DialogResult result = folderDialog.ShowDialog();
            //如果选择了文件夹
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(folderDialog.SelectedPath);
                //判断所选的文件夹在不在trainWindow所选的图片文件夹下
                if (directoryInfo.Parent.FullName==trainWindow.ImageInputDirPath)
                {
                    //清空图片列表
                    imageFileList.Clear();
                    //清空图片展示控件展示的图片
                    for (int i = 0; i < imageShowList.Count; i++)
                    {
                        imageShowList[i].Source = null;
                    }

                    //记录所选文件夹名称
                    childDirName = directoryInfo.Name;
                    //添加图片
                    imageFileList.AddRange(directoryInfo.GetFiles("*.jpg"));
                    imageFileList.AddRange(directoryInfo.GetFiles("*.jpeg"));
                    imageFileList.AddRange(directoryInfo.GetFiles("*.png"));
                    imageFileList.AddRange(directoryInfo.GetFiles("*.bmp"));

                    //在控件中显示路径
                    ImageDirLabel.Content = directoryInfo.FullName;
                    //添加文件到图片展示控件显示
                    for (int i = 0; i < Math.Min(imageFileList.Count, imageShowList.Count); i++)
                    {
                        BitmapImage bitmapImage= new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(imageFileList[i].FullName);
                        bitmapImage.EndInit();
                        imageShowList[i].Source= bitmapImage; 
                    }
                    //输出信息
                    System.Windows.MessageBox.Show($"在文件夹下找到{imageFileList.Count}幅图片");
                }
                else
                {
                    System.Windows.MessageBox.Show($"请选择训练窗口中所选图片导入文件夹\n({trainWindow.ImageInputDirPath})下的文件夹");
                }
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {          
            DialogResult = false;
        }

        /// <summary>
        /// 确认添加标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            //排除图片文件列表为空或未选择文件夹的情况
            if (imageFileList.Count == 0||string.IsNullOrEmpty(childDirName))
            {
                System.Windows.MessageBox.Show("未选择图片文件夹\n或文件夹下未获取到图片");
            }
            //排除选择的的标签为空的情况
            else if(string.IsNullOrEmpty(selectTag))
            {
                System.Windows.MessageBox.Show("未选择或输入图片的标签");
            }
            else
            {
                //暂时修改替换模式，避免大量循环判断导致的卡顿
                bool temp = trainWindow.isReplaceTag;
                trainWindow.isReplaceTag = false;
                //遍历所选的文件夹下的图片，添加标记信息到文件
                for(int i=0;i<imageFileList.Count;i++) 
                {
                    ImageTagData imageTagData = new ImageTagData
                    {
                        ImagePath = Path.Combine(childDirName, imageFileList[i].Name),
                        Label = selectTag                       
                    };
                    trainWindow.AddTagToFile(imageTagData);
                }
                //改回来
                trainWindow.isReplaceTag = temp;
                //二次确认
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show(
                    $"是否将{childDirName}文件夹\n下的{imageFileList.Count}张图片\n设置为{selectTag}标签", "批量标记", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    System.Windows.MessageBox.Show($"已将{childDirName}\n(完整路径:" +
                        $"{Path.Combine(trainWindow.ImageInputDirPath,childDirName)})下的{imageFileList.Count}幅图片标记为{selectTag}");
                    DialogResult = true;
                }
              
            }         
        }

        /// <summary>
        /// 输入文本改变时,设置标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TagTextBox.Text.Length > 0)
            {
                selectTag= TagTextBox.Text;
            }        
        }

        /// <summary>
        /// 标签下拉菜单改变时，设置标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TagComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(TagComboBox.Text.Length > 0)
            {
                //System.Windows.MessageBox.Show(TagComboBox.SelectedItem.ToString());
                selectTag = TagComboBox.SelectedItem.ToString();
                TagTextBox.Text = selectTag;
            }
          
        }
    }
}
