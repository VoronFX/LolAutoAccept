using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public partial class PatternSamplesTests
	{
		[TestMethod()]
		public void TestScreenSamples()
		{
			var results = TestScreenSamplesInterpolation(InterpolationMode.NearestNeighbor);

			Console.WriteLine(results);
			Assert.IsTrue(results.AnySuccess);
		}

		[TestMethod()]
		public void FindParamScreenSamples()
		{
			var results = new ResultList();

			foreach (var imode in new[]
			{
				InterpolationMode.Bicubic,
				InterpolationMode.Bilinear,
				InterpolationMode.HighQualityBicubic,
				InterpolationMode.HighQualityBilinear,
				InterpolationMode.NearestNeighbor
			})
			{
				results.AddRange(TestScreenSamplesInterpolation(imode));
			}

			Console.WriteLine(results);
			Assert.IsTrue(results.AnySuccess);
		}

		[TestMethod()]
		public void FindParamStubTest()
		{
			var results = new ResultList();

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
						results.Add(TestStubSampleParams(lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast));
					}
				}
			}

			Console.WriteLine(results);
			Assert.IsTrue(results.AnySuccess);
		}

		[TestMethod()]
		public void FindParamDetectBanChampion()
		{
			var results = new ResultList();
			var bestDiff = double.MinValue;

			Parallel.ForEach(new[]
			{
				//InterpolationMode.Bicubic,
				//InterpolationMode.Bilinear,
				//InterpolationMode.HighQualityBicubic,
				InterpolationMode.HighQualityBilinear,
				//InterpolationMode.NearestNeighbor
			}, imode =>
			{
				Parallel.ForEach(new[]
				{
					1, 2, 3
				}, shift =>
				{
					Parallel.ForEach(new[]
					{
						new Point(0, 0),
						new Point(0, 0+shift),
						new Point(0+shift, 0),
						new Point(shift, shift)
					}, point =>
					{
						Parallel.ForEach(new[]
						{
							new Size(30, 30),
							new Size(30-shift, 30),
							new Size(30, 30-shift),
							new Size(30-shift, 30-shift)
						}, size =>
						{
							Parallel.ForEach(new[] { 0, 1, 2, 3 },
								crop =>
								{
									var result = TestDetectBanChampionParams(imode, new Rectangle(point, size), crop);
									result.Name = $"{imode} {new Rectangle(point, size)} crop:{crop}";
									lock (results)
									{
										if (result.Difference < bestDiff)
											bestDiff = result.Difference;
										else
											result.Output.Dispose();
										results.Add(result);
									}
								});
						});
					});
				});
			});

			results = new ResultList(results.OrderByDescending(x => x.Difference));
			Console.WriteLine(results);
			Console.WriteLine(results.First().Output);
			Assert.IsTrue(results.AnySuccess);
		}
	}

	public partial class PatternSamplesTests
	{
		private static ResultList TestScreenSamplesInterpolation(InterpolationMode imode)
		{
			return new ResultList(Patterns.SupportedResolutions.Select(res =>
			{

				IEnumerable<double> Calc(IEnumerable<string> samples, Pattern pattern)
					=> samples.Select(sample =>
					{
						var name = $"{sample}_{res.Width}x{res.Height}.png";
						//Console.WriteLine($"Testing {name}");
						return pattern.Match(Samples.LoadSample(name), Point.Empty);
					});

				var results = new ResultList(Samples.ScreenSamples.Select(tm =>
				{
					var pattern = new DifferencePattern(tm.patternSample.Scaled(res, imode));
					var result = new WorstTrueFalseResult
					{
						WorstFalse = Calc(tm.falsePatterns, pattern).Max(),
						WorstTrue = Calc(tm.truePatterns, pattern).Min()
					};

					return result;
				}));

				var aggregated = results.Aggregate().WithOutput(results.ToString());
				aggregated.Name = $"imode: {imode} res: {res.Width}x{res.Height}";
				return aggregated;
			}));
		}

		private WorstTrueFalseResult TestStubSampleParams(
			byte lightHomogeneityThreshold, byte darkHomogeneityThreshold, float targetContrast)
		{
			Size currentResolution = Size.Empty;
			Rectangle[] banRects = null;
			Pattern pattern = null;

			void EnsurePatternResolution(Size resolution)
			{
				if (resolution == currentResolution && banRects != null && pattern != null) return;

				banRects = Enumerable.Range(0, 6)
					.Select(i => ScalePatternsRectangle(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30), resolution)).ToArray();

				pattern = new ContrastMaskPattern(
					LolAutoAccept.Samples.ChampionSelectBanStub.Scaled(banRects.First().Size, InterpolationMode.NearestNeighbor),
					lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast);
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

			(double, string) Format((double, Samples.BanTestSample) x) =>
				(x.Item1, $"{x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}");

			return WorstTrueFalseResult.FromBadGood(
				results.Where(x => x.Item2.Type != Samples.BanTestSample.BanPickType.Stub).Select(Format).ToArray(),
				results.Where(x => x.Item2.Type == Samples.BanTestSample.BanPickType.Stub).Select(Format).ToArray());
		}

		private static readonly Lazy<Samples.BanTestSample[]> DetectBanChampionSamples =
			new Lazy<Samples.BanTestSample[]>(() => Samples
					.GenBanSamples()
					.Where(x => x.Type != Samples.BanTestSample.BanPickType.Stub)
					.ToArray(),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private static readonly WeakCache<(string, int, Size, InterpolationMode), DifferencePattern>
			BanChampionPatternsCache = new WeakCache<(string, int, Size, InterpolationMode), DifferencePattern>();

		private WorstTrueFalseResult TestDetectBanChampionParams(InterpolationMode imode, Rectangle shift, int crop)
		{
			LolAutoAccept.Samples.UseCache = true;
			Size currentResolution = Size.Empty;
			Rectangle[] banRects = null;
			(string Name, DifferencePattern Pattern)[] patterns = null;

			void EnsurePatternResolution(Size resolution)
			{
				if (resolution == currentResolution && banRects != null && patterns != null) return;

				banRects = Enumerable.Range(0, 6)
					.Select(i => ScalePatternsRectangle(
						new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661) + shift.X, 10 + shift.Y, shift.Width, shift.Height), resolution))
					.ToArray();

				patterns = LolAutoAccept.Samples.Champions.Select(x => 
				(x.Name, BanChampionPatternsCache.GetOrAdd((x.Name, crop, banRects.First().Size, imode), key =>
				{
					lock (x.Sample)
						return new DifferencePattern(x.Sample.Croped(crop).Scaled(key.Item3, imode));
				}))).ToArray();
			}

			var badResults = new List<(double Result, string Name)>();
			var goodResults = new List<(double Result, string Name)>();

			foreach (var test in DetectBanChampionSamples.Value)
			{
				//if (test.Sample.Width != 1600)
				//	continue;
				EnsurePatternResolution(new Size(test.Sample.Width, test.Sample.Height));
				foreach (var pattern in patterns)
				{
					var list = string.Equals(test.Champion, pattern.Name, StringComparison.OrdinalIgnoreCase)
						? goodResults
						: badResults;
					list.Add((pattern.Pattern.Match(test.Sample, banRects[test.Position].Location),
												$" {pattern.Name} - {test.Champion} _ {test.SampleName}"));
				}
			}

			return WorstTrueFalseResult.FromBadGood(
				badResults.ToArray(),
				goodResults.ToArray());
		}

		private Rectangle ScalePatternsRectangle(Rectangle rectangle, Size resolution)
				=> resolution == Patterns.NativeResolution
					? rectangle
					: rectangle.Scale(resolution.Width / (double)Patterns.NativeResolution.Width,
						resolution.Height / (double)Patterns.NativeResolution.Height);

		private class WorstTrueFalseResult
		{
			public double WorstTrue { get; set; } = double.MinValue;
			public double WorstFalse { get; set; } = double.MinValue;
			public string Name { get; set; }
			public TextWriter Output { get; set; } = new StringWriter();
			public double Difference => WorstTrue - WorstFalse;
			public bool IsSuccess => Difference > 0;

			public override string ToString()
				=> $"diff: {Difference:P} worstFalse: {WorstFalse:P} worstTrue: {WorstTrue:P} _ {Name}";

			public WorstTrueFalseResult WithOutput(string output)
			{
				Output = new StringWriter(new StringBuilder(ToString()));
				return this;
			}

			public static WorstTrueFalseResult FromBadGood(
				(double Result, string Name)[] bad,
				(double Result, string Name)[] good)

			{
				var result = new WorstTrueFalseResult()
				{
					WorstFalse = bad.Max(x => x.Result),
					WorstTrue = good.Min(x => x.Result)
				};
				result.Output.WriteLine();
				result.Output.WriteLine("GOOD ORDERED");
				result.Output.WriteLine(string.Join(Environment.NewLine, good
					.OrderBy(x => x.Result)
					.Select(x => $"{x.Result:P} _ {x.Name}"))
				);
				result.Output.WriteLine("BAD ORDERED");
				result.Output.WriteLine(string.Join(Environment.NewLine, bad
					.OrderByDescending(x => x.Result)
					.Select(x => $"{x.Result:P} _ {x.Name}"))
				);
				result.Output.WriteLine();
				return result;
			}
		}

		private class ResultList : List<WorstTrueFalseResult>
		{
			public ResultList(IEnumerable<WorstTrueFalseResult> collection)
				: base(collection) { }

			public ResultList() { }

			public bool AnySuccess => this.Any(x => x.IsSuccess);

			public override string ToString()
			{
				var output = new StringWriter();
				output.WriteLine();
				ForEach(x => output.WriteLine(x));
				output.WriteLine();
				output.WriteLine("Aggregate " + Aggregate());
				output.WriteLine("AggregateAverage " + AggregateAverage());
				return output.ToString();
			}

			public WorstTrueFalseResult Aggregate()
				=> new WorstTrueFalseResult
				{
					WorstFalse = this.Max(x => x.WorstFalse),
					WorstTrue = this.Min(x => x.WorstTrue)
				};

			public WorstTrueFalseResult AggregateAverage()
				=> new WorstTrueFalseResult
				{
					WorstFalse = this.Sum(x => x.WorstFalse) / Count,
					WorstTrue = this.Sum(x => x.WorstTrue) / Count
				};
		}
	}
}