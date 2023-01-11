using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.ML;
using Microsoft.ML.Data;


namespace ImageClassification
{
    internal class LinerPrediction
    {

        public class HouseData
        {
            public float Size { get; set; }
            public float Price { get; set; }
        }

        public class Prediction
        {
            [ColumnName("Score")]
            public float Price { get; set; }
        }

        public static void Predicted()
        {
            MLContext mlContext = new MLContext();

            HouseData[] houseDatas =
            {
               new HouseData(){Size=1.1f,Price=1.2F},
               new HouseData(){Size=1.4f,Price=2.3F},
               new HouseData(){Size=1.8f,Price=3.0F},
               new HouseData(){Size=2.1f,Price=3.7F},
               new HouseData(){Size=1.1f,Price=1.2F},
               new HouseData(){Size=1.4f,Price=2.3F},
               new HouseData(){Size=1.8f,Price=3.0F},
               new HouseData(){Size=2.1f,Price=3.7F},
               new HouseData(){Size=1.1f,Price=1.2F},
               new HouseData(){Size=1.4f,Price=2.2F},
               new HouseData(){Size=1.8f,Price=3.3F},
               new HouseData(){Size=2.1f,Price=3.5F},
               new HouseData(){Size=1.1f,Price=1.1F},
               new HouseData(){Size=1.4f,Price=2.6F},
               new HouseData(){Size=1.8f,Price=3.2F},
               new HouseData(){Size=2.1f,Price=3.3F},
            };
            var trainTestData = mlContext.Data.TrainTestSplit(mlContext.Data.LoadFromEnumerable(houseDatas),testFraction:0.2);
            IDataView testData = trainTestData.TestSet;
            IDataView trainingData = trainTestData.TrainSet;

            var pipeline = mlContext.Transforms.Concatenate("Features", new[] { "Size" })
                .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "Price", maximumNumberOfIterations: 100));

            var model = pipeline.Fit(trainingData);

            var size = new HouseData() { Size = 2.5f };
            var price = mlContext.Model.CreatePredictionEngine<HouseData, Prediction>(model).Predict(size);

            MessageBox.Show($"Predicted price for size:{size.Size} is value {price.Price}");

            var testPriceDataView = model.Transform(testData);

            var metrics = mlContext.Regression.Evaluate(testPriceDataView, labelColumnName: "Price");

            MessageBox.Show($"Model r^2:{metrics.RSquared:0.##}\nRMS error:{metrics.RootMeanSquaredError:0.##}");

            mlContext.Model.Save(model, trainingData.Schema, "testModel.zip");
        }
    }
}
