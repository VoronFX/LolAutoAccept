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
		public void FindParamScreenSamples()
		{
			FindParams(TestScreenSamplesInterpolation, new[]
			{
				InterpolationMode.Bicubic,
				InterpolationMode.Bilinear,
				InterpolationMode.HighQualityBicubic,
				InterpolationMode.HighQualityBilinear,
				InterpolationMode.NearestNeighbor
			});
		}

		private struct ContrastMaskMatchParams
		{
			public readonly byte LightHomogeneityThreshold;
			public readonly byte DarkHomogeneityThreshold;
			public readonly float TargetContrast;

			public ContrastMaskMatchParams(byte lightHomogeneityThreshold, byte darkHomogeneityThreshold, float targetContrast)
			{
				this.LightHomogeneityThreshold = lightHomogeneityThreshold;
				this.DarkHomogeneityThreshold = darkHomogeneityThreshold;
				this.TargetContrast = targetContrast;
			}

			public override string ToString()
				=> $@"{{ HomogeneityThreshold Light:{LightHomogeneityThreshold} Dark:{DarkHomogeneityThreshold} {nameof(TargetContrast)}:{TargetContrast} }}";
		}

		[TestMethod()]
		public void FindParamStubTest()
		{
			var paramsSet = new List<ContrastMaskMatchParams>();
			foreach (var lightHomogeneityThreshold in new byte[] { 10, 15, 25, 30, 45 })
			{
				foreach (var darkHomogeneityThreshold in new byte[] { 5, 10, 20, 30 })
				{
					foreach (var targetContrast in new[] { 0.1f, 0.2f, 0.3f })
					{
						paramsSet.Add(new ContrastMaskMatchParams(
							lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast));
					}
				}
			}

			FindParams(TestStubSampleParams, paramsSet);
		}

		[TestMethod()]
		public void FindParamChampionSearchTest()
		{
			var paramsSet = new List<ContrastMaskMatchParams>();
			foreach (var lightHomogeneityThreshold in new byte[] { 10, 15, 25, 30, 45 })
			{
				foreach (var darkHomogeneityThreshold in new byte[] { 5, 10, 20, 30 })
				{
					foreach (var targetContrast in new[] { 0.1f, 0.2f, 0.3f })
					{
						paramsSet.Add(new ContrastMaskMatchParams(
							lightHomogeneityThreshold, darkHomogeneityThreshold, targetContrast));
					}
				}
			}

			FindParams(TestChampionSearchSampleParams, paramsSet);
		}

		private struct DiffMatchParams
		{
			public readonly int Crop;
			public readonly Rectangle Rectangle;
			public readonly InterpolationMode InterpolationMode;

			public DiffMatchParams(InterpolationMode interpolationMode, Rectangle rectangle, int crop)
			{
				this.Crop = crop;
				this.Rectangle = rectangle;
				this.InterpolationMode = interpolationMode;
			}

			public override string ToString()
				=> $"{{ {InterpolationMode} {Rectangle} {nameof(Crop)}:{Crop} }}";
		}

		[TestMethod()]
		public void FindParamDetectFirstSelectChampion()
		{
			FindParams(TestDetectFirstSelectChampionParams, new[]
				{
					InterpolationMode.Bicubic,
					InterpolationMode.Bilinear,
					InterpolationMode.HighQualityBicubic,
					InterpolationMode.HighQualityBilinear,
					InterpolationMode.NearestNeighbor
				}.SelectMany(imode =>
					new[]
					{
						0
						//-3,-2,-1,1,
						//2, 3, 4, 5, 6
					}.SelectMany(shift =>
						new[]
						{
							new Point(0, 0),
							//new Point(0, 0 + shift),
							//new Point(0 + shift,0),
							//new Point(0 + shift,0+ shift)
						}.SelectMany(point =>
							new[]
							{
								new Size(36, 36),
								//new Size(35, 35),
								//new Size(34, 34)
							}.SelectMany(size =>
								new[] { 30 }.Select(crop =>
									  new DiffMatchParams(imode, new Rectangle(point, size), crop)
								)
							)
						)
					)
				)
			);
		}

		[TestMethod()]
		public void FindParamDetectBanChampion()
		{
			FindParams(TestDetectBanChampionParams, new[]
				{
					//InterpolationMode.Bicubic,
					//InterpolationMode.Bilinear,
					//InterpolationMode.HighQualityBicubic,
					InterpolationMode.HighQualityBilinear,
					//InterpolationMode.NearestNeighbor
				}.SelectMany(imode =>
					new[]
					{
						1, 2, 3
					}.SelectMany(shift =>
						new[]
						{
							new Point(0, 0),
							new Point(0, 0 + shift),
							new Point(0 + shift, 0),
							new Point(shift, shift)
						}.SelectMany(point =>
							new[]
							{
								new Size(30, 30),
								new Size(30 - shift, 30),
								new Size(30, 30 - shift),
								new Size(30 - shift, 30 - shift)
							}.SelectMany(size =>
								new[] { 0, 1, 2, 3 }.Select(crop =>
									  new DiffMatchParams(imode, new Rectangle(point, size), crop)
								)
							)
						)
					)
				)
			);
		}
	}

	public partial class PatternSamplesTests
	{
		private static WorstTrueFalseResult TestScreenSamplesInterpolation(InterpolationMode imode)
		{
			LolAutoAccept.Samples.UseCache = true;

			var badResults = new List<(double Result, string Name)>();
			var goodResults = new List<(double Result, string Name)>();

			foreach (var resolution in Patterns.SupportedResolutions)
			{
				foreach (var testSample in Samples.ScreenSamples)
				{
					IEnumerable<(double, string)> Calc(IEnumerable<string> samples, Pattern pattern)
						=> samples.Select(sampleName =>
						{
							var sample = Samples.LoadSample(sampleName, resolution);
							return (pattern.Match(sample, Point.Empty), $"{sampleName} {resolution}");
						});

					var diffPattern = new DifferencePattern(testSample.patternSample.Scaled(resolution, imode));

					badResults.AddRange(Calc(testSample.falsePatterns, diffPattern));
					goodResults.AddRange(Calc(testSample.truePatterns, diffPattern));
				}
			}

			return WorstTrueFalseResult.FromBadGood(
				badResults.ToArray(),
				goodResults.ToArray());
		}

		private static readonly Lazy<Samples.BanPickTestSample[]> BanSamples =
			new Lazy<Samples.BanPickTestSample[]>(() => Samples
					.GenBanSamples()
					.ToArray(),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private static readonly Lazy<Samples.BanPickTestSample[]> DetectBanChampionSamples =
			new Lazy<Samples.BanPickTestSample[]>(() => BanSamples.Value
					.Where(x => x.Type != Samples.BanPickTestSample.BanPickType.Stub)
					.ToArray(),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private WorstTrueFalseResult TestStubSampleParams(
			ContrastMaskMatchParams contrastMaskMatchParams)
		{
			LolAutoAccept.Samples.UseCache = true;
			Size currentResolution = Size.Empty;
			Rectangle[] banRects = null;
			Pattern pattern = null;

			void EnsurePatternResolution(Size resolution)
			{
				if (resolution == currentResolution && banRects != null && pattern != null) return;

				banRects = Enumerable.Range(0, 6)
					.Select(i => ScalePatternsRectangle(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30), resolution))
					.ToArray();

				var banStubSample = LolAutoAccept.Samples.ChampionSelect.BanStub;
				lock (banStubSample)
				{
					pattern = new ContrastMaskPattern(
						banStubSample.Scaled(banRects.First().Size, InterpolationMode.NearestNeighbor),
						contrastMaskMatchParams.LightHomogeneityThreshold,
						contrastMaskMatchParams.DarkHomogeneityThreshold,
						contrastMaskMatchParams.TargetContrast);
				}
			}

			var results = BanSamples.Value
				.Select(test =>
				{
					EnsurePatternResolution(new Size(test.Sample.Width, test.Sample.Height));
					return (pattern.Match(test.Sample, banRects[test.Position].Location), test);
				}).ToArray();

			(double, string) Format((double, Samples.BanPickTestSample) x) =>
				(x.Item1, $"{x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}");

			return WorstTrueFalseResult.FromBadGood(
				results.Where(x => x.Item2.Type != Samples.BanPickTestSample.BanPickType.Stub).Select(Format).ToArray(),
				results.Where(x => x.Item2.Type == Samples.BanPickTestSample.BanPickType.Stub).Select(Format).ToArray());
		}

		private WorstTrueFalseResult TestChampionSearchSampleParams(
			ContrastMaskMatchParams contrastMaskMatchParams)
		{
			LolAutoAccept.Samples.UseCache = true;
			Size currentResolution = Size.Empty;
			Rectangle rect = default(Rectangle);
			Pattern pattern = null;

			void EnsurePatternResolution(Size resolution)
			{
				if (resolution == currentResolution && pattern != null) return;

				rect = ScalePatternsRectangle(new Rectangle(744, 58, 20, 28), resolution);

				var championSearchSample = LolAutoAccept.Samples.ChampionSelect.ChampionSearch;
				lock (championSearchSample)
				{
					pattern = new ContrastMaskPattern(
						championSearchSample.Scaled(rect.Size, InterpolationMode.NearestNeighbor),
						contrastMaskMatchParams.LightHomogeneityThreshold,
						contrastMaskMatchParams.DarkHomogeneityThreshold,
						contrastMaskMatchParams.TargetContrast);
				}
			}

			IEnumerable<(double Result, string Name)> Calc(IEnumerable<string> samples)
				=> Patterns.SupportedResolutions.SelectMany(resolution =>
				samples.Select(sampleName =>
					{
						var sample = Samples.LoadSample(sampleName, resolution);

						EnsurePatternResolution(new Size(sample.Width, sample.Height));

						return (pattern.Match(sample, rect.Location), $"{sampleName} {resolution}");
					})
				);

			var badResults = Calc(Samples.All.Except(Samples.ChampionSelect.ChampionSearch));
			var goodResults = Calc(Samples.ChampionSelect.ChampionSearch);

			return WorstTrueFalseResult.FromBadGood(
				badResults.ToArray(),
				goodResults.ToArray());
		}

		private static readonly WeakCache<(string, int, Size, InterpolationMode), DifferencePattern>
			ChampionPatternsCache = new WeakCache<(string, int, Size, InterpolationMode), DifferencePattern>();

		private WorstTrueFalseResult TestDetectBanChampionParams(DiffMatchParams diffMatchParams)
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
						new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661) + diffMatchParams.Rectangle.X, 10 + diffMatchParams.Rectangle.Y,
							diffMatchParams.Rectangle.Width, diffMatchParams.Rectangle.Height), resolution))
					.ToArray();

				patterns = LolAutoAccept.Samples.Champions.Select(x =>
					(x.Name, ChampionPatternsCache.GetOrAdd(
						(x.Name, diffMatchParams.Crop, banRects.First().Size, diffMatchParams.InterpolationMode), key =>
						{
							lock (x.Sample)
								return new DifferencePattern(x.Sample.Croped(diffMatchParams.Crop)
									.Scaled(key.Item3, diffMatchParams.InterpolationMode));
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

		private static readonly Lazy<Samples.SelectTestSample[]> DetectFirstSelectChampionSamples =
			new Lazy<Samples.SelectTestSample[]>(() => Samples
					.GenSelectSamples()
					.ToArray(),
				LazyThreadSafetyMode.ExecutionAndPublication);

		private WorstTrueFalseResult TestDetectFirstSelectChampionParams(DiffMatchParams diffMatchParams)
		{
			LolAutoAccept.Samples.UseCache = true;
			Size currentResolution = Size.Empty;
			Rectangle selectRect = default(Rectangle);
			(string Name, DifferencePattern Pattern)[] patterns = null;

			void EnsurePatternResolution(Size resolution)
			{
				if (resolution == currentResolution && patterns != null) return;

				selectRect = ScalePatternsRectangle(
					new Rectangle(343 + 16 + diffMatchParams.Rectangle.X, 109 + 16 + diffMatchParams.Rectangle.Y,
						diffMatchParams.Rectangle.Width, diffMatchParams.Rectangle.Height), resolution);

				patterns = LolAutoAccept.Samples.Champions.Select(x =>
					(x.Name, ChampionPatternsCache.GetOrAdd(
						(x.Name, diffMatchParams.Crop, selectRect.Size, diffMatchParams.InterpolationMode), key =>
						{
							lock (x.Sample)
								return new DifferencePattern(x.Sample.Croped(diffMatchParams.Crop)
									.Scaled(key.Item3, diffMatchParams.InterpolationMode));
						}))).ToArray();
			}

			var badResults = new List<(double Result, string Name)>();
			var goodResults = new List<(double Result, string Name)>();

			foreach (var test in DetectFirstSelectChampionSamples.Value)
			{
				EnsurePatternResolution(new Size(test.Sample.Width, test.Sample.Height));
				foreach (var pattern in patterns)
				{
					var list = string.Equals(test.Champion, pattern.Name, StringComparison.OrdinalIgnoreCase)
						? goodResults
						: badResults;
					list.Add((pattern.Pattern.Match(test.Sample, selectRect.Location),
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
	}

	public partial class PatternSamplesTests
	{
		private static void FindParams<TParams>(Func<TParams, WorstTrueFalseResult> matchFunc,
			IEnumerable<TParams> paramsSet)
		{
			var results = new ResultList();
			var bestDiff = double.MinValue;

			Parallel.ForEach(paramsSet, currParams =>
			{
				var result = matchFunc(currParams);
				result.Name = currParams.ToString();
				lock (results)
				{
					if (result.Difference < bestDiff)
					{
						bestDiff = result.Difference;
						results.ForEach(r => r.Output.Dispose());
					}
					else
						result.Output.Dispose();
					results.Add(result);
				}
			});

			results = new ResultList(results.OrderByDescending(x => x.Difference));
			Console.WriteLine(results);
			Console.WriteLine(results.First().Output);
			Assert.IsTrue(results.AnySuccess);
		}

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
				: base(collection)
			{
			}

			public ResultList()
			{
			}

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