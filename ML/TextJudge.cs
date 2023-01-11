using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageClassification
{

    public class JiebaLambdaInput
    {
        public string Comment { get; set; }
    }


    public class JiebaLambdaOutput 
    {
        public string JiebaText { get; set; }
    }

    public class JiebLambda
    {
        public static void MyAction(JiebaLambdaInput input,JiebaLambdaOutput output)
        {
            JiebaNet.Segmenter.JiebaSegmenter jiebaSegmenter = new JiebaNet.Segmenter.JiebaSegmenter();
            output.JiebaText = string.Join(" ", jiebaSegmenter.Cut(input.Comment));
        }
    }




    public class CommentInfo
    {
        [LoadColumn(0)]
        public bool Label { get; set; }
        [LoadColumn(1)]
        public string Comment { get; set; }
    }

    public class JudgeResult : CommentInfo
    {
        public string JiebaText { get; set; }
        public float[] Features { get; set; }
        public bool JudgeLabel;
        public float Score;
        public float Probability;
    }

    internal class TextJudge
    {
        static readonly string dataPath = Path.Combine(Environment.CurrentDirectory, "DataSet", "weiboData.csv");

        public static void Judge()
        {
            MLContext mLContext = new MLContext();
            var fulldata = mLContext.Data.LoadFromTextFile<CommentInfo>(dataPath, separatorChar: ',', hasHeader: true);
            var trainTestData = mLContext.Data.TrainTestSplit(fulldata, testFraction: 0.15);
            var trainData = trainTestData.TrainSet;
            var testData = trainTestData.TestSet;

            var trainingPipeline = mLContext.Transforms.CustomMapping<JiebaLambdaInput, JiebaLambdaOutput>(mapAction: JiebLambda.MyAction, contractName: "JiebaLambda")
                .Append(mLContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: "JiebaText"))
                .Append(mLContext.BinaryClassification.Trainers.FieldAwareFactorizationMachine(labelColumnName: "Label", featureColumnName: "Features"));
            ITransformer trainModel = trainingPipeline.Fit(trainData);

            var predictions = trainModel.Transform(testData);

            mLContext.Model.Save(trainModel, trainData.Schema, "TextModel.zip");

            var metrics = mLContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label");
            MessageBox.Show($"Evalution Accuracy:{metrics.Accuracy:P2}");

            var predEngine = mLContext.Model.CreatePredictionEngine<CommentInfo, JudgeResult>(trainModel);

            CommentInfo sample1 = new CommentInfo { Comment = "咋又开始封校了，什么时候阴性能出校。" };
            var predictionResult1 = predEngine.Predict(sample1);
            MessageBox.Show($"对于第一句和封校相关的话\n判断为 {predictionResult1.JudgeLabel}");

            CommentInfo sample2 = new CommentInfo { Comment = "春暖花开~岁月静好。。。" };
            var predictionResult2 = predEngine.Predict(sample2);
            MessageBox.Show($"对于第二句正常的话\n判断为 {predictionResult2.JudgeLabel}");
        }
    }
}
