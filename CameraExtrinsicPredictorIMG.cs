using Csh.Vision.DataModel;
using Csh.Vision.DataModel.Enumeration;
using Csh.Vision.DataModel.Integration;
using Csh.Vision.Interface;
using Csh.Vision.Prediction.Predictor;
using Csh.Vision.Calibration.Models;
using Csh.Vision.Calibration.Enums;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using Csh.Vision.Utility;
using OpenCvSharp;
using Csh.Vision.Prediction;
using System.IO;
using Moq;
using System.Linq;
using System.Reflection;
using Csh.Vision.Detection;
using Csh.Vision.Tools.CalibrationTool.Utility;
using Csh.Vision.Tools.CalibrationTool.ViewModel;
using Csh.Vision.Bucky;
using OpenCvSharp.Aruco;
using System.Text;

namespace Csh.Vision.Test.Prediction
{
	public sealed class CameraExtrinsicPredictorIMG : PredictorTestBase
	{

		private IPredictor predictor;
		private IMarkerLessPredictor predictor_markLess;
		private MarkerDetector markd;

		[SetUp]
		public override void SetUp()
		{

			base.SetUp();
			predictor = new CameraExtrinsicPredictor(integrationContent.Object, ConfigurationRepository, worldCoordinateRepository, calibrationRepository);
			predictor_markLess = new MarkerLessPredictor(ConfigurationRepository, worldCoordinateRepository);
			markd = new MarkerDetector(ConfigurationRepository, worldCoordinateRepository);
			var movingDisplayCameraConfig = ConfigurationRepository.Config.CameraConfigs["MovingDisplay"];
			movingDisplayCameraConfig.InstalledAtRight = false;
			movingDisplayCameraData.Setup(pcd => pcd.CameraConfig).Returns(movingDisplayCameraConfig);
			var movingCameraConfig = ConfigurationRepository.Config.CameraConfigs["Moving"];
			movingCameraConfig.InstalledAtRight = false;
			movingCameraData.Setup(pcd => pcd.CameraConfig).Returns(movingCameraConfig);

		}


		public void Test2(CalibrationResult calibrationResult)
		{

			if (calibrationResult != null)
			{
				//using (StreamWriter sw = File.AppendText(@"C:\temp\CameraExtrinsicResult.txt"))
				//{
				foreach (CalibrationStepName stepName in Enum.GetValues(typeof(CalibrationStepName)))
				{

					var frame = calibrationResult.FramesDictionary2D[stepName];

					for (var i = 0; i < frame.Count; i++)
					{

						if (frame[i].CameraExtrinsic != null)
						{
							var positioner = integrationParameters.Hardware.Positioner;
							positioner.WallZPosition = (int)(frame[i].Position.WallZ);
							positioner.TubeXPosition = (int)(frame[i].Position.OtcX);
							positioner.TubeYPosition = (int)(frame[i].Position.OtcY);
							positioner.TubeZPosition = (int)(frame[i].Position.OtcZ);
							positioner.RawTubeAngle = (int)(frame[i].Position.OtcA);
							positioner.Sid = (int)(frame[i].Position.Sid / 10);
							integrationParameters.Hardware.ActiveBucky.Location = BuckyLocation.Wall;
							var results = predictor.Predict(ProcessedCameraDataMock.Object);
							Assert.IsTrue(results[0].Status == PredictionStatus.Acceptable);
							var cameraExtrinsic = (CameraExtrinsic)results[0].ExtendedData;


							//sw.WriteLine("---------{0} {1}---------", stepName, i+1);
							//TestContext.WriteLine(stepName);
							var CameraConfig2D = ConfigurationRepository.Config.CameraConfigs["MovingDisplay"];

							for (int k = 0; k < 3; k++)
							{
								Assert.AreEqual(frame[i].CameraExtrinsic.RotationVector[k], cameraExtrinsic.RotationVector[k], 0.1);

								//TestContext.WriteLine(frame[i].CameraExtrinsic.RotationVector[k] - cameraExtrinsic.RotationVector[k]);
								//sw.WriteLine("RotationVector{0}: {1}", k, frame[i].CameraExtrinsic.RotationVector[k] - cameraExtrinsic.RotationVector[k]);

								Assert.AreEqual(frame[i].CameraExtrinsic.TransVector[k], cameraExtrinsic.TransVector[k], 20);

								//TestContext.WriteLine(Math.Abs(frame[i].CameraExtrinsic.TransVector[k] - cameraExtrinsic.TransVector[k]));
								//sw.WriteLine("TransVector{0}:    {1}", k, frame[i].CameraExtrinsic.TransVector[k] - cameraExtrinsic.TransVector[k]);
							}
							//TestContext.WriteLine(predictMarkers.Invoke("PredictMarkers", rvec, tvec));


						}

					}
					//}
					//sw.WriteLine("Done");
				}
			}
			if (calibrationResult == null)
			{
				throw new Exception("Error");
			}
		}

		public void test_marker(CalibrationResult calibrationResult, CameraLocation cameraLocation)
		{

			//MarkerLessPredictor markerLessPredictor = new MarkerLessPredictor(ConfigurationRepository, worldCoordinateRepository);
			//Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject predictMarkers = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(markerLessPredictor);
			if (calibrationResult != null)
			{
				using (StreamWriter sw = File.AppendText(@"C:\temp\CameraEx_5.txt"))
				{
					foreach (CalibrationStepName stepName in Enum.GetValues(typeof(CalibrationStepName)))
					{

						var frame = calibrationResult.FramesDictionary2D[stepName];

						for (var i = 0; i < frame.Count; i++)
						{

							if (frame[i].CameraExtrinsic != null)
							{
								var positioner = integrationParameters.Hardware.Positioner;
								positioner.WallZPosition = (int)(frame[i].Position.WallZ);
								positioner.TubeXPosition = (int)(frame[i].Position.OtcX);
								positioner.TubeYPosition = (int)(frame[i].Position.OtcY);
								positioner.TubeZPosition = (int)(frame[i].Position.OtcZ);
								positioner.RawTubeAngle = (int)(frame[i].Position.OtcA);
								positioner.Sid = (int)(frame[i].Position.Sid / 10);

								/// Extrinsic predictor
								integrationParameters.Hardware.ActiveBucky.Location = BuckyLocation.Wall;
								var results = predictor.Predict(ProcessedCameraDataMock.Object);
								Assert.IsTrue(results[0].Status == PredictionStatus.Acceptable);
								var cameraExtrinsic = (CameraExtrinsic)results[0].ExtendedData;


								/// MarkerLess predictor


								var predictedCameraExtrinsic = new CameraExtrinsic { RotationVector = new float[3], TransVector = new float[3] };
								var expectedCameraExtrinsic = new CameraExtrinsic { RotationVector = new float[3], TransVector = new float[3] };
								//Array.Copy(cameraExtrinsic, 0, predictedCameraExtrinsic.TransVector, 0,1);
								sw.WriteLine("---------{0} {1}---------", stepName, i + 1);

								for (int k = 0; k < 3; k++)
								{
									//Assert.AreEqual(frame[i].CameraExtrinsic.RotationVector[k], cameraExtrinsic.RotationVector[k], 0.1);
									///rvec[k] = cameraExtrinsic.RotationVector[k];
									///

									if (k == 2)
									{
										predictedCameraExtrinsic.RotationVector[k] = frame[i].CameraExtrinsic.RotationVector[k];
									}
									else
									{

										predictedCameraExtrinsic.RotationVector[k] = cameraExtrinsic.RotationVector[k];

									}
									expectedCameraExtrinsic.RotationVector[k] = frame[i].CameraExtrinsic.RotationVector[k];

									//TestContext.WriteLine(cameraExtrinsic.RotationVector[k]);
									sw.WriteLine("RotationVector{0}: Expected: {1} Predicted: {2}", k, expectedCameraExtrinsic.RotationVector[k], predictedCameraExtrinsic.RotationVector[k]);

									//Assert.AreEqual(frame[i].CameraExtrinsic.TransVector[k], cameraExtrinsic.TransVector[k], 50);
									///tvec[k] = cameraExtrinsic.TransVector[k];

									//sw.WriteLine("TransVector{0}:    {1}", k, frame[i].CameraExtrinsic.TransVector[k] - cameraExtrinsic.TransVector[k]);

								}

								for (int k = 0; k < 3; k++)
								{

									expectedCameraExtrinsic.TransVector[k] = frame[i].CameraExtrinsic.TransVector[k];
									predictedCameraExtrinsic.TransVector[k] = cameraExtrinsic.TransVector[k];
									//TestContext.WriteLine(Math.Abs(cameraExtrinsic.TransVector[k]));
									sw.WriteLine("TransVector{0}:    Expected: {1} Predicted: {2}", k, expectedCameraExtrinsic.TransVector[k], predictedCameraExtrinsic.TransVector[k]);
								}
								//TestContext.WriteLine(predictMarkers.Invoke("PredictMarkers", rvec, tvec));
								ProcessedCameraDataMock.Setup(pcd => pcd.CameraExtrinsic).Returns(predictedCameraExtrinsic);
								var results_markless = predictor_markLess.Predict(ProcessedCameraDataMock.Object);

								ProcessedCameraDataMock.Setup(pcd => pcd.CameraExtrinsic).Returns(expectedCameraExtrinsic);
								var expected_res = predictor_markLess.Predict(ProcessedCameraDataMock.Object);

								//TestContext.WriteLine(results_markless[0].ExtendedData);
								string childPath = $"\\TestFiles\\Calibration\\MovingDisplay\\{stepName}\\childStep_{i}.jpg";
								string imagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + childPath;
								var image = Cv2.ImRead(imagePath);



								var marker_info = (IList<IMarker>)results_markless[0].ExtendedData;
								var exp_info = (IList<IMarker>)expected_res[0].ExtendedData;

								ImageExtensions.DrawMarkers(image, marker_info, Scalar.Red);
								ImageExtensions.DrawMarkers(image, exp_info, Scalar.Green);
								Cv2.ImWrite($"C:/Users/Admin/Desktop/DrawMarker/{stepName}_childStep_{i}.jpg", image);
								/***
								markd.Detect(image, out var markers);
								foreach (var marker in markers)
								{
									if (marker.Key == 11 || marker.Key == 13 || marker.Key == 14 || marker.Key == 15)
									{
										CvAruco.DrawDetectedMarkers(image, markers.Values.ToArray(), markers.Keys, Scalar.Blue);
									}

								}
								***/
								//CvAruco.DrawDetectedMarkers(image,, markers.Keys);
								/**
								//sw.WriteLine("--- Detected Marker ---");
								foreach (var marker in markers)
								{
									if (marker.Key == 11 || marker.Key == 13 || marker.Key == 14 || marker.Key == 15)
									{
										sw.WriteLine("ID: {0}", marker.Key);
										sw.WriteLine("Left Top    : {0} {1}", marker.Value[0].X, marker.Value[0].Y);
										sw.WriteLine("Right Top   : {0} {1}", marker.Value[1].X, marker.Value[1].Y);
										sw.WriteLine("Right Bottom: {0} {1}", marker.Value[2].X, marker.Value[2].Y);
										sw.WriteLine("Left Bottom : {0} {1}", marker.Value[3].X, marker.Value[3].Y);
									}

								}
								**/
								/***
								sw.WriteLine("--- Predicted Marker ---");
								foreach (var marker in marker_info)
								{
									sw.WriteLine("ID: {0}", marker.ExpectedId);
									sw.WriteLine("Left Top    : {0} {1}", marker.ImgLeftTop.X, marker.ImgLeftTop.Y);
									sw.WriteLine("Right Top   : {0} {1}", marker.ImgRightTop.X, marker.ImgRightTop.Y);
									sw.WriteLine("Right Bottom: {0} {1}", marker.ImgRightBottom.X, marker.ImgRightBottom.Y);
									sw.WriteLine("Left Bottom : {0} {1}", marker.ImgLeftBottom.X, marker.ImgLeftBottom.Y);


								}***/

							}

						}


					}
					sw.WriteLine("Done");
				}
			}
			if (calibrationResult == null)
			{
				throw new Exception("Error");
			}

		}


		public void test_marker2(CalibrationResult calibrationResult, CameraLocation cameraLocation)
		{
			if (File.Exists(@"C:\temp\CameraEx.csv"))
			{
				File.Delete(@"C:\temp\CameraEx.csv");
			}
			
			string header = $"\"StepName\",\"StepNumber\",\"Expected_rvec0\",\"Expected_rvec1\"," +
			$"\"Expected_rvec2\",\"Actual_rvec0\",\"Actual_rvec1\"," +
			$"\"Actual_rvec2\",\"Diff_rvec0\",\"Diff_rvec1\",\"Diff_rvec2\",\"Expected_tvec0\"," +
			$"\"Expected_tvec1\",\"Expected_tvec2\",\"Actual_tvec0\",\"Actual_tvec1\"," +
			$"\"Actual_tvec2\",\"Diff_tvec0\",\"Diff_tvec1\",\"Diff_tvec2\"{Environment.NewLine}";
			File.AppendAllText(@"C:\temp\CameraEx.csv", header);
				
			
			var csv = new StringBuilder();
			if (calibrationResult != null)
			{
				
				foreach (CalibrationStepName stepName in Enum.GetValues(typeof(CalibrationStepName)))
				{

					var frame = calibrationResult.FramesDictionary2D[stepName];

					for (var i = 0; i < frame.Count; i++)
					{

						if (frame[i].CameraExtrinsic != null)
						{
							var positioner = integrationParameters.Hardware.Positioner;
							positioner.WallZPosition = (int)(frame[i].Position.WallZ);
							positioner.TubeXPosition = (int)(frame[i].Position.OtcX);
							positioner.TubeYPosition = (int)(frame[i].Position.OtcY);
							positioner.TubeZPosition = (int)(frame[i].Position.OtcZ);
							positioner.RawTubeAngle = (int)(frame[i].Position.OtcA);
							positioner.Sid = (int)(frame[i].Position.Sid / 10);

							/// Extrinsic predictor
							integrationParameters.Hardware.ActiveBucky.Location = BuckyLocation.Wall;
							var results = predictor.Predict(ProcessedCameraDataMock.Object);
							Assert.IsTrue(results[0].Status == PredictionStatus.Acceptable);
							var cameraExtrinsic = (CameraExtrinsic)results[0].ExtendedData;


							var predictedCameraExtrinsic = new CameraExtrinsic { RotationVector = new float[3], TransVector = new float[3] };
							var expectedCameraExtrinsic = new CameraExtrinsic { RotationVector = new float[3], TransVector = new float[3] };
							var diff1 = new float[3];
							var diff2 = new float[3];
							
							for (int k = 0; k < 3; k++)
							{
								
								predictedCameraExtrinsic.RotationVector[k] = cameraExtrinsic.RotationVector[k];
								expectedCameraExtrinsic.RotationVector[k] = frame[i].CameraExtrinsic.RotationVector[k];
								diff1[k] = expectedCameraExtrinsic.RotationVector[k] - predictedCameraExtrinsic.RotationVector[k];
								
							}

							for (int k = 0; k < 3; k++)
							{
								expectedCameraExtrinsic.TransVector[k] = frame[i].CameraExtrinsic.TransVector[k];
								predictedCameraExtrinsic.TransVector[k] = cameraExtrinsic.TransVector[k];
								diff2[k] = expectedCameraExtrinsic.TransVector[k] - predictedCameraExtrinsic.TransVector[k];
							}	

							var newLine = $"{stepName},{i},{expectedCameraExtrinsic.RotationVector[0]},{expectedCameraExtrinsic.RotationVector[1]}," +
							$"{expectedCameraExtrinsic.RotationVector[2]},{predictedCameraExtrinsic.RotationVector[0]},{predictedCameraExtrinsic.RotationVector[1]}," +
							$"{predictedCameraExtrinsic.RotationVector[2]},{diff1[0]},{diff1[1]},{diff1[2]},{expectedCameraExtrinsic.TransVector[0]}," +
							$"{expectedCameraExtrinsic.TransVector[1]},{expectedCameraExtrinsic.TransVector[2]},{predictedCameraExtrinsic.TransVector[0]}," +
							$"{predictedCameraExtrinsic.TransVector[1]},{predictedCameraExtrinsic.TransVector[2]},{diff2[0]},{diff2[1]},{diff2[2]}";
							csv.AppendLine(newLine);

							//TestContext.WriteLine(predictMarkers.Invoke("PredictMarkers", rvec, tvec));
							ProcessedCameraDataMock.Setup(pcd => pcd.CameraExtrinsic).Returns(predictedCameraExtrinsic);
							var results_markless = predictor_markLess.Predict(ProcessedCameraDataMock.Object);

							ProcessedCameraDataMock.Setup(pcd => pcd.CameraExtrinsic).Returns(expectedCameraExtrinsic);
							var expected_res = predictor_markLess.Predict(ProcessedCameraDataMock.Object);

							//TestContext.WriteLine(results_markless[0].ExtendedData);
							//string childPath = $"\\TestFiles\\Calibration\\MovingDisplay\\{stepName}\\childStep_{i}.jpg";
							//string imagePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + childPath;
							//var image = Cv2.ImRead(imagePath);
							var marker_info = (IList<IMarker>)results_markless[0].ExtendedData;
							var exp_info = (IList<IMarker>)expected_res[0].ExtendedData;
						}
					}
				}
				File.AppendAllText(@"C:\temp\CameraEx.csv", csv.ToString());

			}
			if (calibrationResult == null)
			{
				throw new Exception("Error");
			}

		}

		[Test]
		public void Test_2D()
		{

			var jsonPath = string.Format(TestFilePathFormatter, @"Calibration\CalibrationResults3.json");
			var calibrationResults = Utilities.LoadJson<List<CalibrationResult>>(jsonPath);
			//calibrationResults
			if (calibrationResults != null)
			{
				//Test2(calibrationResults[0]);
				test_marker2(calibrationResults[0], CameraLocation.MovingDisplay);
			}
			else
			{
				throw new Exception("Error2d");
			}
		}
	}
}