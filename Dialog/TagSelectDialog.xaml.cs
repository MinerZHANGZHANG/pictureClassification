using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        private string ImageInputDirPath;
        private TrainWindow trainWindow;

        private string childDirName;
        private List<FileInfo> imageFileList=new();
        private string selectTag;
        private List<Image> imageShowList=new();
        public TagSelectDialog(TrainWindow parentWindow,string imageInputDirPath)
        {
            InitializeComponent();

            this.ImageInputDirPath = imageInputDirPath;
            this.trainWindow = parentWindow;

            imageShowList.Add(ImageShow0);
            imageShowList.Add(ImageShow1);
            imageShowList.Add(ImageShow2);

            foreach(var tag in parentWindow.TagListBox.Items)
            {
                TagComboBox.Items.Add(tag);
            }
            if (TagComboBox.HasItems)
            {
                TagComboBox.SelectedIndex = 0;
            }
            
           
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void SelectImageDir_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "选择该目录下，要批量添加标签的文件夹";
            folderDialog.InitialDirectory = ImageInputDirPath;
            DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FileInfo fileInfo = new FileInfo(folderDialog.SelectedPath);
                if (Directory.Exists(Path.Combine(ImageInputDirPath, fileInfo.Name)))
                {
                    imageFileList.Clear();
                    DirectoryInfo directoryInfo = new DirectoryInfo(folderDialog.SelectedPath);
                    childDirName = directoryInfo.Name;

                    imageFileList.AddRange(directoryInfo.GetFiles("*.jpg"));
                    imageFileList.AddRange(directoryInfo.GetFiles("*.png"));
                    imageFileList.AddRange(directoryInfo.GetFiles("*.bmp"));

                    for(int i = 0; i < Math.Min(imageFileList.Count, imageShowList.Count); i++)
                    {
                        BitmapImage bitmapImage= new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(imageFileList[i].FullName);
                        bitmapImage.EndInit();
                        imageShowList[i].Source= bitmapImage; ;
                    }

                    System.Windows.MessageBox.Show($"在文件夹下找到{imageFileList.Count}幅图片");
                }
                else
                {
                    System.Windows.MessageBox.Show($"请选择{ImageInputDirPath}下的文件夹");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {          
            DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (imageFileList.Count == 0||string.IsNullOrEmpty(childDirName))
            {
                System.Windows.MessageBox.Show("未选择图片文件夹\n或文件夹下未获取到图片");
            }
            else if(string.IsNullOrEmpty(selectTag))
            {
                System.Windows.MessageBox.Show("未选择或输入图片的标签");
            }
            else
            {
                for(int i=0;i<imageFileList.Count;i++) 
                {
                    ImageTagData imageTagData = new ImageTagData
                    {
                        ImagePath = Path.Combine(childDirName, imageFileList[i].Name),
                        Label = selectTag                       
                    };
                    trainWindow.AddToCSV(imageTagData);
                }
                System.Windows.MessageBox.Show($"已将{Path.Combine(ImageInputDirPath, childDirName)}\n" +
                    $"下的{imageFileList.Count}幅图片标记为{selectTag}");
                DialogResult = true;
            }
           
        }

        private void TagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TagTextBox.Text.Length > 0)
            {
                selectTag= TagTextBox.Text;
            }        
        }

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
