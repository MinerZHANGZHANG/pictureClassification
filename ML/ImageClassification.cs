using Microsoft.ML;
using Microsoft.ML.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;

namespace ImageClassification
{
    public class ImageSolution
    {
        #region ——字段——
        //默认的资源文件夹路径
        static readonly string AssetsFolder = @"D:\Data\程序\ImageClassification\DataSet\ImageClassification";
        //训练数据/测试数据/训练标签文件/模型文件/图片特征提取模型文件的路径
        static readonly string TrainDataFolder = Path.Combine(AssetsFolder, "train");
        static readonly string TestDataFolder = Path.Combine(AssetsFolder, "test");
        static readonly string TrainTagsPath = Path.Combine(AssetsFolder, "train_tags.tsv");
        static readonly string imageClassifierZip = Path.Combine(AssetsFolder, "MLModel", "imageClassifier.zip");
        static readonly string inceptionPb = Path.Combine(AssetsFolder, "TensorFlow", "tensorflow_inception_graph.pb");

        //标记文件使用的分隔符
        public const char fileSplit = '\t';
        //默认的图片预处理设置
        public static readonly ImageNetSetting DefaultImageSetting = new ImageNetSetting(224, 224, 117, 1, true);

        //识别的图片格式
        private static readonly List<string> imageTypes = new() { ".jpg", ".jpeg",".png",".bmp" };

        #endregion


        #region ——数据结构——
        /// <summary>
        /// 图片数据
        /// </summary>
        public struct ImageNetSetting
        {
            //宽高
            public int imageHeight = 224;
            public int imageWidth = 224;
            //颜色值偏移量
            public float mean = 117;
            //颜色值缩放量
            public float scale = 1;
            //是否交错像素颜色
            public bool channelsLast = true;

            public ImageNetSetting(int imageHeight, int imageWidth, float mean, float scale, bool channelsLast)
            {
                this.imageHeight = imageHeight;
                this.imageWidth = imageWidth;
                this.mean = mean;
                this.scale = scale;
                this.channelsLast = channelsLast;
            }
        }

        /// <summary>
        /// 图片训练数据类
        /// </summary>
        public class ImageNetData
        {
            //图片的在文件夹下的相对路径
            [LoadColumn(0)]
            public string ImagePath;
            
            //图片的标签
            [LoadColumn(1)]
            public string Label;

        }

        /// <summary>
        /// 图片预测结果数据类
        /// </summary>
        public class ImageNetPrediction
        {
            //判断结果标签
            public string PredictedLabelValue;
        }

        #endregion


        #region ——图片分类基本流程函数——
        /// <summary>
        /// 默认路径训练和保存结果
        /// </summary>
        public static void TrainAndSaveModel()
        {
            MLContext mLContext = new MLContext(seed: 1);

            var fullData = mLContext.Data.LoadFromTextFile<ImageNetData>(path: TrainTagsPath, separatorChar: '\t', hasHeader: false);

            var trainTestData = mLContext.Data.TrainTestSplit(fullData, testFraction: 0.1);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            MessageBox.Show("开始训练模型");

            var pipeline = mLContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelTokey", inputColumnName: "Label")
                .Append(mLContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: TrainDataFolder, inputColumnName: nameof(ImageNetData.ImagePath)))
                .Append(mLContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: DefaultImageSetting.imageWidth, imageHeight: DefaultImageSetting.imageHeight, inputColumnName: "input"))
                .Append(mLContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: DefaultImageSetting.channelsLast, offsetImage: DefaultImageSetting.mean))
                .Append(mLContext.Model.LoadTensorFlowModel(inceptionPb).
                    ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                .Append(mLContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelTokey", featureColumnName: "softmax2_pre_activation"))
                .Append(mLContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                .AppendCacheCheckpoint(mLContext);

            ITransformer model = pipeline.Fit(trainData);

            var evaData = model.Transform(testData);
            var metrics = mLContext.MulticlassClassification.Evaluate(evaData, labelColumnName: "LabelTokey", predictedLabelColumnName: "PredictedLabel");
            MessageBox.Show($"最高K预测计数{metrics.TopKPredictionCount}\n混淆矩阵：{metrics.ConfusionMatrix}\n损失降低日志{metrics.LogLossReduction}");

            mLContext.Model.Save(model, trainData.Schema, imageClassifierZip);
            MessageBox.Show("成功训练并保存模型");          
        }

        /// <summary>
        /// 加载和显示预测结果
        /// </summary>
        static void LoadAndPrediction()
        {
            MLContext mLContext = new MLContext(seed: 1);

            ITransformer loadedModel = mLContext.Model.Load(imageClassifierZip, out var modelInputSchema);

            var predictor = mLContext.Model.CreatePredictionEngine<ImageNetData, ImageNetPrediction>(loadedModel);

            DirectoryInfo testdir = new DirectoryInfo(TestDataFolder);

            foreach(var jpgfile in testdir.GetFiles("*.png"))
            {
                ImageNetData image = new ImageNetData();
                image.ImagePath = jpgfile.FullName;
                var pred = predictor.Predict(image);

                MessageBox.Show($"对于图片{jpgfile.Name}\n判断为:{pred.PredictedLabelValue}");

            }
        }

        /// <summary>
        /// 图片分类
        /// </summary>
        public static void Classificated()
        {
            TrainAndSaveModel();
            LoadAndPrediction();
        }

        #endregion


        #region ——对接窗体的图片分类函数——

        /// <summary>
        /// 图片分类(弹窗显示结果)
        /// </summary>
        /// <param name="ModelPath">模型路径</param>
        /// <param name="ImageDirPath">图片文件夹路径</param>
        /// <param name="imageControl">图片显示控件</param>
        /// <param name="progressBar">进度条</param>
        public static void Classificated(string ModelPath,string ImageDirPath,Image imageControl,ref ProgressBar progressBar)
        {
            //创建机器学习上下文
            MLContext mLContext = new MLContext(seed: 1);
          
            try
            {
                //加载模型
                ITransformer loadedModel = mLContext.Model.Load(ModelPath, out var modelInputSchema);
                //创建预测器
                var predictor = mLContext.Model.CreatePredictionEngine<ImageNetData, ImageNetPrediction>(loadedModel);

                //添加路径中的图片文件到列表
                DirectoryInfo ImagesDir = new DirectoryInfo(ImageDirPath);
                List<FileInfo> fileList = new List<FileInfo>();

               foreach(string imageType in imageTypes)
                {
                    fileList.AddRange(ImagesDir.GetFiles($"*{imageType}"));
                }

                //遍历文件，使用模型判断图片类型
                for (int i = 0; i < fileList.Count; i++)
                {
                    FileInfo? imageFile = fileList[i];
                    ImageNetData image = new ImageNetData();
                    image.ImagePath = imageFile.FullName;
                    var pred = predictor.Predict(image);
                    
                    //转换图片格式，赋值给图片显示控件
                    BitmapImage bitmapImage=new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource=new Uri(imageFile.FullName);  
                    bitmapImage.EndInit();
                    imageControl.Source = bitmapImage;

                    //弹窗显示结果
                    MessageBox.Show($"对于图片{imageFile.Name}\n判断为:{pred.PredictedLabelValue}");

                    
                    progressBar.Value = ((float)i + 1 / (float)fileList.Count) * 100;

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("分类发生错误:\n"+ex.Message);
            }
        }

        /// <summary>
        /// 图片分类（输出结果到不同文件夹）
        /// </summary>
        /// <param name="ModelPath">模型路径</param>
        /// <param name="ImageDirPath">图片文件夹路径</param>
        /// <param name="OutputPath">输出文件夹路径</param>
        /// <param name="progressBar">进度条控件</param>
        public static void Classificated(string ModelPath, string ImageDirPath,string OutputPath,BackgroundWorker worker,DoWorkEventArgs doWorkEvent, bool copyOtherFile=true)
        {
            //创建机器学习上下文
            MLContext mLContext = new MLContext(seed: 1);

            try
            {
                //加载模型
                ITransformer loadedModel = mLContext.Model.Load(ModelPath, out var modelInputSchema);
                //创建预测器
                var predictor = mLContext.Model.CreatePredictionEngine<ImageNetData, ImageNetPrediction>(loadedModel);

                //添加路径中的图片文件到列表
                DirectoryInfo ImagesDir = new DirectoryInfo(ImageDirPath);
                List<FileInfo> imageFileList = new List<FileInfo>();
                //为被识别的文件夹和文件
                List<DirectoryInfo> otherDirList = new List<DirectoryInfo>();
                List<FileInfo> otherFileList=new List<FileInfo>();

                foreach (FileInfo file in ImagesDir.GetFiles())
                {
                    if (imageTypes.Contains(file.Extension))
                    {
                        imageFileList.Add(file);
                    }
                    else
                    {
                        otherFileList.Add(file);
                    }
                }
                otherDirList.AddRange(ImagesDir.GetDirectories());


                //不同类别图片统计字典
                Dictionary<string, uint> ImageTypeCount = new Dictionary<string, uint>();

                //遍历文件，使用模型判断图片类型
                for (int i = 0; i < imageFileList.Count; i++)
                {
                    FileInfo? imageFile = imageFileList[i];

                    ImageNetData image = new ImageNetData();
                    image.ImagePath = imageFile.FullName;
                    var pred = predictor.Predict(image);

                    //根据情况添加图片到分类名称下的文件夹
                    string imageType = pred.PredictedLabelValue;
                    if (!Directory.Exists(Path.Combine(OutputPath, imageType)))
                    {
                        Directory.CreateDirectory(Path.Combine(OutputPath, imageType));
                    }
  
                    File.Copy(imageFile.FullName, Path.Combine(OutputPath, imageType, imageFile.Name), false);
                    

                    //在对应的字典键添加计数
                    if (ImageTypeCount.ContainsKey(imageType))
                    {
                        ImageTypeCount[imageType] += 1;
                    }
                    else
                    {
                        ImageTypeCount.Add(imageType, 1);
                    }
                    
                    //判断是否终止分类
                    if (worker.CancellationPending)
                    {
                        doWorkEvent.Cancel= true;
                        break;
                    }
                    else
                    {
                        //反馈当前分类进度
                        worker.ReportProgress((int)(((float)(i + 1)/ imageFileList.Count) * 100));
                    }

                }

                //输出分类结果
                StringBuilder  OverMeassage= new StringBuilder($"共计{imageFileList.Count}张图片\n已输出到{OutputPath}目录下，其中:");
                foreach(string type in ImageTypeCount.Keys)
                {
                    OverMeassage.Append($"\n{type}:{ImageTypeCount[type]}条");
                }

                //如果目录下有其他文件也输出一下
                if(otherDirList.Count > 0)
                {
                    OverMeassage.Append($"\n目录下子文件夹:{otherDirList.Count}个\n(只识别所选文件夹下的图片,不包括子文件夹)");
                    if (copyOtherFile)
                    {
                        if (!Directory.Exists(Path.Combine(OutputPath, "未识别文件")))
                        {
                            Directory.CreateDirectory(Path.Combine(OutputPath, "未识别文件"));
                        }
                        foreach (DirectoryInfo dir in otherDirList)
                        {
                            CopyFolder(dir.FullName, Path.Combine(OutputPath, "未识别文件", dir.Name));
                        }
                    }                                     
                }
                if(otherFileList.Count > 0)
                {
                    if (copyOtherFile)
                    {
                        if (!Directory.Exists(Path.Combine(OutputPath, "未识别文件")))
                        {
                            Directory.CreateDirectory(Path.Combine(OutputPath, "未识别文件"));
                        }
                        foreach (FileInfo file in otherFileList)
                        {
                            File.Copy(file.FullName, Path.Combine(OutputPath, "未识别文件", file.Name));
                        }
                    }                   
                    OverMeassage.Append($"\n未识别文件:{otherFileList.Count}个\n(部分格式的图片无法识别,例如gif和webp)");
                }             
                
                //完成后弹窗提示
                MessageBox.Show(OverMeassage.ToString());
            }
            catch (Exception ex)
            {               
                if(ex is IOException)
                {
                    MessageBox.Show("分类发生错误:\n" + ex.Message+"\n可能是分类结果输出目录下已经有同名图片");
                }
                else
                {
                    MessageBox.Show("分类发生错误:\n" + ex.Message);
                }
                
            }
            
        }

        /// <summary>
        /// 训练和保存模型
        /// </summary>
        /// <param name="trainTagPath">训练集标签文件路径</param>
        /// <param name="trainImageFolderPath">训练集图片文件夹路径</param>
        /// <param name="trainModelSavePath">模型保存路径</param>
        /// <param name="imageNetSetting">图像预处理设置</param>
        /// <param name="trainModelSaveName">模型名称</param>
        /// <param name="seed">种子</param>
        /// <param name="inceptionPbModel">特征提取的TensorFlow模型路径</param>
        public static void TrainAndSaveModel(string trainTagPath, string trainImageFolderPath, string trainModelSavePath, ImageNetSetting imageNetSetting,string trainModelSaveName = "图像分类.zip", int seed=1,string inceptionPbModel = "tensorflow_inception_graph.pb")
        {
            //创建机器学习上下文
            MLContext mLContext = new MLContext(seed: seed);
            //获取csv数据集(图片tag和路径),因为用,分隔，所以文件名不能有半角逗号
            var fullData = mLContext.Data.LoadFromTextFile<ImageNetData>(path: trainTagPath, separatorChar: fileSplit, hasHeader: false);            
            //随机划分训练集和测试集
            var trainTestData = mLContext.Data.TrainTestSplit(fullData, testFraction: 0.1);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;           

            //模型训练管道
            //转换值为键https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.ml.imageestimatorscatalog.extractpixels?view=ml-dotnet
            var pipeline = mLContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelTokey", inputColumnName: "Label")
                //加载图片
                .Append(mLContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: trainImageFolderPath, inputColumnName: nameof(ImageNetData.ImagePath)))
                //重置图片大小
                .Append(mLContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: imageNetSetting.imageWidth, imageHeight: imageNetSetting.imageHeight, inputColumnName: "input"))
                //将像素提取为数字向量
                .Append(mLContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: imageNetSetting.channelsLast, offsetImage: imageNetSetting.mean,scaleImage:imageNetSetting.scale))
                //使用tensorFlow的模型分析输出图片特征
                .Append(mLContext.Model.LoadTensorFlowModel(inceptionPbModel).
                    ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                //使用特定算法进行分类
                .Append(mLContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelTokey", featureColumnName: "softmax2_pre_activation"))
                //将键转换回值
                .Append(mLContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                .AppendCacheCheckpoint(mLContext);

            //使用该管道训练模型
            try
            {
                ITransformer model = pipeline.Fit(trainData);

                //将测试数据转换为模型对应的格式
                var evaData = model.Transform(testData);
                //测试模型拟合度
                var metrics = mLContext.MulticlassClassification.Evaluate(evaData, labelColumnName: "LabelTokey", predictedLabelColumnName: "PredictedLabel");
                //MessageBox.Show($"最高K预测计数{metrics.TopKPredictionCount}\n混淆矩阵维数：{metrics.ConfusionMatrix.Counts}\n损失降低日志{metrics.LogLossReduction}");
                MessageBox.Show($"测试宏观准确率;{metrics.MacroAccuracy}\n测试微观准确率;{metrics.MicroAccuracy}","训练完成");
                //保存模型
                string path = Path.Combine(trainModelSavePath, $"{trainModelSaveName}MI{metrics.MicroAccuracy:P0}-MA{metrics.MacroAccuracy:P0}.zip");
                mLContext.Model.Save(model, trainData.Schema, path);
                MessageBox.Show($"成功训练并保存模型为\n{path}");
            }
            catch(Exception ex)
            {
                MessageBox.Show($"模型训练过程中发生{ex.Message}错误","Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            
        }

        #endregion


        #region ——自动模型训练函数——
        ///// <summary>
        ///// 自动训练模型(未完成)
        ///// </summary>
        ///// <param name="trainTagPath"></param>
        ///// <param name="trainImageFolderPath"></param>
        ///// <param name="trainModelSavePath"></param>
        ///// <param name="ExperimentTime"></param>
        ///// <param name="trainModelSaveName"></param>
        ///// <param name="seed"></param>
        ///// <param name="inceptionPbModel"></param>
        //public static void AutoTrainAndSave(string trainTagPath, string trainImageFolderPath, string trainModelSavePath,uint ExperimentTime, ImageNetSetting imageNetSetting, string trainModelSaveName = "图像分类.zip",int seed=1,string inceptionPbModel = "tensorflow_inception_graph.pb")
        //{
        //    MLContext mLContext = new MLContext(seed: seed);
        //    //获取csv数据集(图片tag和路径),因为用,分隔，所以文件名不能有半角逗号
        //    var fullData = mLContext.Data.LoadFromTextFile<ImageNetData>(path: trainTagPath, separatorChar: fileSplit, hasHeader: false);
        //    //随机划分训练集和测试集
        //    var trainTestData = mLContext.Data.TrainTestSplit(fullData, testFraction: 0.1);
        //    var trainData = trainTestData.TrainSet;
        //    var testData = trainTestData.TestSet;


        //    //模型训练管道
        //    //转换值为键
        //    var pipeline = mLContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelTokey", inputColumnName: "Label")
        //        //加载图片
        //        .Append(mLContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: trainImageFolderPath, inputColumnName: nameof(ImageNetData.ImagePath)))
        //        //重置图片大小
        //        .Append(mLContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: imageNetSetting.imageWidth, imageHeight: imageNetSetting.imageHeight, inputColumnName: "input"))
        //        //提取像素信息
        //        .Append(mLContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: imageNetSetting.channelsLast, offsetImage: imageNetSetting.mean))
        //        //使用tensorFlow的模型分析输出图片特征
        //        .Append(mLContext.Model.LoadTensorFlowModel(inceptionPbModel).
        //            ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
        //        .Append(mLContext.Auto().MultiClassification(labelColumnName: "LabelTokey", featureColumnName: "softmax2_pre_activation"))
        //        .Append(mLContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"));





        //    AutoMLExperiment experiment = mLContext.Auto().CreateExperiment();
        //    experiment
        //        .SetPipeline(pipeline)
        //        .SetRegressionMetric(RegressionMetric.RSquared)
        //        .SetTrainingTimeInSeconds(ExperimentTime)
        //        .SetDataset(trainTestData);

        //    mLContext.Log += MLContext_Log;

        //    //使用该管道训练模型
        //    MessageBox.Show("开始训练模型");

        //    TrialResult experimentResults = experiment.Run();    
        //    var model = experimentResults.Model;
        //    //将测试数据转换为模型对应的格式
        //    var evaData = model.Transform(testData);
        //    //测试模型拟合度

        //    MessageBox.Show($"测试准确率;{experimentResults.Metric}");
        //    //保存模型
        //    string path = Path.Combine(trainModelSavePath, $"{trainModelSaveName}METRICS{experimentResults.Metric:P0}.zip");
        //    mLContext.Model.Save(model, trainData.Schema, path);
        //    MessageBox.Show($"成功训练并保存模型为\n{path}");
        //}

        //private static void MLContext_Log(object? sender, LoggingEventArgs e)
        //{
        //    if(e.Source.Equals("AutoMLExperiment"))
        //    {
        //        Console.WriteLine(e.RawMessage);
        //    }
        //}

        #endregion


        #region ——辅助方法——
        /// <summary>
        /// 复制文件夹
        /// </summary>
        /// <param name="srcPath">原文件夹路径</param>
        /// <param name="dstPath">目标文件夹路径</param>
        /// <returns></returns>
        public static void CopyFolder(string srcPath, string dstPath)
        {
            try
            {
                if(!Directory.Exists(dstPath))
                {
                    Directory.CreateDirectory(dstPath);
                }
                //获取文件路径
                string[] files = Directory.GetFiles(srcPath);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(dstPath, name);
                    File.Copy(file, dest);//复制文件
                }
                //获取文件夹路径
                string[] folders =Directory.GetDirectories(srcPath);
                //递归复制文件夹中的文件
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(dstPath, name);
                    CopyFolder(folder, dest);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"复制文件夹过程中发生{e.Message}异常");
            }
        }
        #endregion
    }

}
