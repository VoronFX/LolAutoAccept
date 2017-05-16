using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternSamplesTests
	{
		[TestMethod()]
		public void FindParamScreenSamples()
		{
			bool anySuccess = false;
			var summary = new List<string>();
			foreach (var imode in new[]
			{
				InterpolationMode.Bicubic,
				InterpolationMode.Bilinear,
				InterpolationMode.HighQualityBicubic,
				InterpolationMode.HighQualityBilinear,
				InterpolationMode.NearestNeighbor
			})
			{
				var result = TestScreenSamplesInterpolation(imode, summary);
				anySuccess = anySuccess || result.WorstTrue - result.WorstFalse > 0;
				summary.Add(string.Empty);
			}
			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
			Assert.IsTrue(anySuccess);
		}

		[TestMethod()]
		public void TestScreenSamples()
		{
			var summary = new List<string>();

			var result = TestScreenSamplesInterpolation(InterpolationMode.NearestNeighbor, summary);

			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
			Console.WriteLine();
			Console.WriteLine($"Result {FormatWorstTrueFalse(result.WorstFalse, result.WorstTrue)}");
			Assert.IsTrue(result.WorstTrue - result.WorstFalse > 0);
		}

		private static (double WorstFalse, double WorstTrue) TestScreenSamplesInterpolation(InterpolationMode imode,
			List<string> summary)
		{
			return Patterns.SupportedResolutions.Select(res =>
			{
				Console.WriteLine();
				Console.WriteLine($"imode: {imode} res: {res.Width}x{res.Height}");

				IEnumerable<double> Calc(IEnumerable<string> samples, Pattern pattern)
					=> samples.Select(sample =>
					{
						var name = $"{sample}_{res.Width}x{res.Height}.png";
						//Console.WriteLine($"Testing {name}");
						return pattern.Match(Samples.LoadSample(name), Point.Empty);
					});

				var results = Samples.ScreenSamples.Select(tm =>
				{
					var pattern = new DifferencePattern(tm.patternSample.Scaled(res, imode));
					var worstFalse = Calc(tm.falsePatterns, pattern).Max();
					var worstTrue = Calc(tm.truePatterns, pattern).Min();

					Console.WriteLine(FormatWorstTrueFalse(worstFalse, worstTrue));

					return new { worstFalse, worstTrue };
				}).ToArray();

				var worstFalseSummary = results.Select(tm => tm.worstFalse).Max();
				var worstTrueSummary = results.Select(tm => tm.worstTrue).Min();
				var worstFalseAvgSummary = results.Select(tm => tm.worstFalse).Sum() / results.Length;
				var worstTrueAvgSummary = results.Select(tm => tm.worstTrue).Sum() / results.Length;

				summary.Add(
					$"Absolute {FormatWorstTrueFalse(worstFalseSummary, worstTrueSummary)}"
					+ $" Average {FormatWorstTrueFalse(worstFalseAvgSummary, worstTrueAvgSummary)}"
					+ $" imode: {imode} res: {res.Width}x{res.Height}");

				return (worstFalseSummary, worstTrueSummary);
			}).Aggregate((double.MinValue, double.MaxValue),
				(x, x2) => (Math.Max(x.Item1, x2.Item1), Math.Min(x.Item2, x2.Item2)));
		}

		[TestMethod()]
		public void FindParamStubTest()
		{
			bool anySuccess = false;
			var summary = new List<string>();


			foreach (var lightHomogeneityThreshold in new byte[] { 10, 15, 25, 30, 45 })
			{
				foreach (var darkHomogeneityThreshold in new byte[] { 5, 10, 20, 30 })
				{
					foreach (var targetContrast in new[] { 0.1f, 0.2f, 0.3f })
					{
						//Patterns.StubMatchAvg = StubMatchAvg;
						//Patterns.StubWhiteGrayLow = StubWhiteGrayLow;
						//Patterns.StubWhiteGrayHigh = StubWhiteGrayHigh;
						//Patterns.StubBlackGrayHigh = StubBlackGrayHigh;
						var result = TestStubSampleParams(lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast);
						anySuccess = anySuccess || result.WorstTrue - result.WorstFalse > 0;
						summary.Add(FormatWorstTrueFalse(result.WorstFalse, result.WorstTrue) +
									$" _ {lightHomogeneityThreshold} {darkHomogeneityThreshold} {targetContrast}");
						//summary.Add(string.Empty);
					}
				}
			}

			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
			Assert.IsTrue(anySuccess);
		}

		private (double WorstFalse, double WorstTrue) TestStubSampleParams(
			byte lightHomogeneityThreshold, byte darkHomogeneityThreshold, float targetContrast)
		{
			Size currentResolution = Size.Empty;
			Rectangle[] banRects = null;
			Pattern pattern = null;

			void EnsurePatternResolution(Size resolution)
			{
				Rectangle Scale(Rectangle rectangle)
					=> resolution == Patterns.NativeResolution
						? rectangle
						: rectangle.Scale(resolution.Width / (double)Patterns.NativeResolution.Width,
							resolution.Height / (double)Patterns.NativeResolution.Height);

				if (resolution != currentResolution || banRects == null || pattern == null)
				{
					banRects = Enumerable.Range(0, 6)
						.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30))).ToArray();

					pattern = new ContrastMaskPattern(
						LolAutoAccept.Samples.ChampionSelectBanStub.Scaled(banRects.First().Size, InterpolationMode.NearestNeighbor),
						lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast);
				}
			}

			var results = Samples.GenBanSamples()
				//.Select(x =>
				//new BanTestSample(x.SampleName, new LockBitmap.LockBitmap(x.Sample.RecreateBitmap().Laplacian3x3Filter(false))
				//	, x.Position, x.Type, x.Champion))
				.Select(test =>
				{
					EnsurePatternResolution(new Size(test.Sample.Width, test.Sample.Height));
					return (pattern.Match(test.Sample, banRects[test.Position].Location), test);
				}).ToArray();

			double worstTrue = results.Where(x => x.Item2.Type == Samples.BanTestSample.BanPickType.Stub).Min(x => x.Item1);
			double worstFalse = results.Where(x => x.Item2.Type != Samples.BanTestSample.BanPickType.Stub).Max(x => x.Item1);

			Console.WriteLine();
			Console.WriteLine("GOOD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, results
				.Where(x => x.Item2.Type == Samples.BanTestSample.BanPickType.Stub)
				.OrderBy(x => x.Item1)
				.Select(x => $"{x.Item1:P} {x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}"))
			);
			Console.WriteLine("BAD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, results
				.Where(x => x.Item2.Type != Samples.BanTestSample.BanPickType.Stub)
				.OrderByDescending(x => x.Item1)
				.Select(x => $"{x.Item1:P} {x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}"))
			);
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine(FormatWorstTrueFalse(worstFalse, worstTrue));
			return (worstFalse, worstTrue);
		}

		private static string FormatWorstTrueFalse(double worstFalse, double worstTrue)
			=> $"diff: {worstTrue - worstFalse:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}";
	}
}