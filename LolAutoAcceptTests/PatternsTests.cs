﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using LolAutoAccept;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageEdgeDetection;
using BPType = LolAutoAccept.Patterns.BanPickType;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternsTests
	{
		private static readonly string[] AcceptMatchButtonSamples =
		{
			"AcceptMatchButtonTest",
			"AcceptMatchButtonHoverTest"
		};

		private static readonly string[] ChampionSelectBanLockButtonDisabledSamples =
		{
			"ChampionSelectBanLockButtonDisabledTest",
			"ChampionSelectBanLockButtonDisabledTest2",
			"ChampionSelectBlindPickTest"
		};

		private static readonly string[] ChampionSelectBanButtonSamples =
		{
			"ChampionSelectBanButtonTest",
			"ChampionSelectBanButtonHoverTest"
		};

		private static readonly string[] ChampionSelectLockButtonSamples =
		{
			"ChampionSelectLockButtonTest",
			"ChampionSelectLockButtonHoverTest"
		};

		private static readonly string[] OtherScreensSamples = new[]
			{
				"MainScreenTest",
				"MainScreenTest2",
				"PlayScreenTest",
				"CreateCustomScreenTest"
			}.Concat(AcceptMatchButtonSamples)
			.ToArray();


		private static readonly string[] ChampionSelectSamples = new[]
				{"ChampionSelectNoButtonTest"}
			.Concat(ChampionSelectBanLockButtonDisabledSamples)
			.Concat(ChampionSelectBanButtonSamples)
			.Concat(ChampionSelectLockButtonSamples)
			.ToArray();

		private static readonly string[] AllTestSamples =
			ChampionSelectSamples
				.Concat(OtherScreensSamples)
				.ToArray();

		[TestMethod()]
		public void HasBanLockButtonDisabledTest()
		{
			TestMatch((patterns, bitmap) => patterns.HasBanLockButtonDisabled(bitmap),
				ChampionSelectBanLockButtonDisabledSamples,
				AllTestSamples.Except(ChampionSelectBanLockButtonDisabledSamples).ToArray());
		}

		[TestMethod()]
		public void IsBanButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsBanButton(bitmap),
				ChampionSelectBanButtonSamples,
				AllTestSamples.Except(ChampionSelectBanButtonSamples).ToArray());
		}

		[TestMethod()]
		public void IsLockButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsLockButton(bitmap),
				ChampionSelectLockButtonSamples,
				AllTestSamples.Except(ChampionSelectLockButtonSamples).ToArray());
		}

		[TestMethod()]
		public void IsChampionSelectTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsChampionSelect(bitmap),
				ChampionSelectSamples,
				AllTestSamples.Except(ChampionSelectSamples).ToArray());
		}

		[TestMethod()]
		public void IsAcceptMatchButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsAcceptMatchButton(bitmap),
				AcceptMatchButtonSamples,
				AllTestSamples.Except(AcceptMatchButtonSamples).ToArray());
		}

		private void TestMatch(
			Func<Patterns, LockBitmap.LockBitmap, bool> testMethod,
			string[] truePatterns, string[] falsePatterns)
		{
			foreach (var res in Patterns.SupportedResolutions)
			{
				var patternsClass = new Patterns(res);

				void Check(IEnumerable<string> patterns, bool expected)
				{
					foreach (var pattern in patterns)
					{
						var name = $"{pattern}_{res.Width}x{res.Height}.png";
						Console.WriteLine($"Testing {name}");

						Assert.AreEqual(expected, testMethod(patternsClass, GetSample(name)), name);
					}
				}

				Check(truePatterns, true);
				Check(falsePatterns, false);
			}
		}

		//[TestMethod()]
		public void CompareAlgorithms()
		{
			var summary = new List<string>();
			foreach (var imode in new[]
			{
				InterpolationMode.Bicubic, InterpolationMode.Bilinear,
				InterpolationMode.HighQualityBicubic, InterpolationMode.HighQualityBilinear, InterpolationMode.NearestNeighbor
			})
			{
				foreach (var alg in new[] { Patterns.CompareAlgorithm.Plain, Patterns.CompareAlgorithm.ColorPriority })
				{
					TestAlgInterpolation(alg, imode, summary);
					summary.Add(string.Empty);
				}
			}
			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
		}

		[TestMethod()]
		public void TestAllSamles()
		{
			var summary = new List<string>();

			var result = TestAlgInterpolation(Patterns.CompareAlgorithm.Plain, InterpolationMode.NearestNeighbor, summary);

			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
			Console.WriteLine();
			Console.WriteLine(
				$"Result diff: {result.Item1 - result.Item2:P} worstFalse: {result.Item1:P} worstTrue: {result.Item2:P}");
			Assert.IsTrue(result.Item1 - result.Item2 > 0);
		}

		private static (double, double) TestAlgInterpolation(Patterns.CompareAlgorithm alg, InterpolationMode imode,
			List<string> summary)
		{
			var testMethods = new List<(Func<Patterns, Patterns.MatchPoint[]> testMethod,
				string[] truePatterns,
				string[] falsePatterns)>
			{
				(patterns => patterns.ChampionSelectBanLockButtonDisabledSample.Value,
				ChampionSelectBanLockButtonDisabledSamples,
				AllTestSamples.Except(ChampionSelectBanLockButtonDisabledSamples).ToArray()),

				(patterns => patterns.ChampionSelectBanButtonSample.Value,
				ChampionSelectBanButtonSamples.Take(1).ToArray(),
				AllTestSamples.Except(ChampionSelectBanButtonSamples).ToArray()),

				(patterns => patterns.ChampionSelectBanButtonHoverSample.Value,
				ChampionSelectBanButtonSamples.Skip(1).Take(1).ToArray(),
				AllTestSamples.Except(ChampionSelectBanButtonSamples).ToArray()),

				(patterns => patterns.ChampionSelectLockButtonSample.Value,
				ChampionSelectLockButtonSamples.Take(1).ToArray(),
				AllTestSamples.Except(ChampionSelectLockButtonSamples).ToArray()),

				(patterns => patterns.ChampionSelectLockButtonHoverSample.Value,
				ChampionSelectLockButtonSamples.Skip(1).Take(1).ToArray(),
				AllTestSamples.Except(ChampionSelectLockButtonSamples).ToArray()),

				(patterns => patterns.AcceptMatchButtonSample.Value,
				AcceptMatchButtonSamples.Take(1).ToArray(),
				AllTestSamples.Except(AcceptMatchButtonSamples).ToArray()),

				(patterns => patterns.AcceptMatchButtonHoverSample.Value,
				AcceptMatchButtonSamples.Skip(1).Take(1).ToArray(),
				AllTestSamples.Except(AcceptMatchButtonSamples).ToArray()),

				(patterns => patterns.ChampionSelectSample.Value,
				ChampionSelectSamples,
				AllTestSamples.Except(ChampionSelectSamples).ToArray())
			};


			return Patterns.SupportedResolutions.Select(res =>
			{
				Console.WriteLine();
				Console.WriteLine($"imode: {imode}, alg: {alg} res: {res.Width}x{res.Height}");
				var patternsClass = new Patterns(Patterns.NativeResolution, imode);

				IEnumerable<double> Calc(IEnumerable<string> patterns, Func<Patterns, Patterns.MatchPoint[]> sampleSelector)
					=> patterns.Select(pattern =>
					{
						var name = $"{pattern}_{res.Width}x{res.Height}.png";
						//Console.WriteLine($"Testing {name}");
						return Patterns.IsMatchTest(GetSample(name), sampleSelector(patternsClass), alg);
					});

				var results = testMethods.Select(tm =>
				{
					var worstFalse = Calc(tm.falsePatterns, tm.testMethod).Min();
					var worstTrue = Calc(tm.truePatterns, tm.testMethod).Max();

					Console.WriteLine($"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}");

					return (worstFalse, worstTrue);
				}).ToArray();

				var worstFalseSummary = results.Select(tm => tm.Item1).Min();
				var worstTrueSummary = results.Select(tm => tm.Item2).Max();
				var worstFalseAvgSummary = results.Select(tm => tm.Item1).Sum() / results.Length;
				var worstTrueAvgSummary = results.Select(tm => tm.Item2).Sum() / results.Length;

				summary.Add(
					$"diff: {worstFalseSummary - worstTrueSummary:P} worstFalse: {worstFalseSummary:P} worstTrue: {worstTrueSummary:P}"
					+ $" diffAvg: {worstFalseAvgSummary - worstTrueAvgSummary:P} worstFalseAvg: {worstFalseAvgSummary:P} worstTrueAvg: {worstTrueAvgSummary:P}"
					+ $" imode: {imode}, alg: {alg} res: {res.Width}x{res.Height}");

				return (worstFalseSummary, worstTrueSummary);
			}).Aggregate((double.MaxValue, double.MinValue),
				(x, x2) => (Math.Min(x.Item1, x2.Item1), Math.Max(x.Item2, x2.Item2)));
		}

		[TestMethod()]
		public void DetectOurPickPosition()
		{
			Patterns patternsClass = null;

			foreach (var test in new(string sample, int position)[]
			{
				("ChampionSelectBanButtonHoverTest_1024x576", 0),
				("ChampionSelectBanButtonHoverTest_1280x720", 2),
				("ChampionSelectBanButtonHoverTest_1600x900", 1),
				("ChampionSelectBanButtonTest_1024x576", 0),
				("ChampionSelectBanButtonTest_1280x720", 2),
				("ChampionSelectBanButtonTest_1600x900", 1),
				("ChampionSelectBanLockButtonDisabledTest_1024x576", 0),
				("ChampionSelectBanLockButtonDisabledTest_1280x720", 2),
				("ChampionSelectBanLockButtonDisabledTest_1600x900", 1),
				("ChampionSelectBanLockButtonDisabledTest2_1024x576", 0),
				("ChampionSelectBanLockButtonDisabledTest2_1280x720", 0),
				("ChampionSelectBanLockButtonDisabledTest2_1600x900", 1),
				("ChampionSelectLockButtonHoverTest_1024x576", 0),
				("ChampionSelectLockButtonHoverTest_1280x720", 2),
				("ChampionSelectLockButtonHoverTest_1600x900", 1),
				("ChampionSelectLockButtonTest_1024x576", 0),
				("ChampionSelectLockButtonTest_1280x720", 2),
				("ChampionSelectLockButtonTest_1600x900", 1),
				("ChampionSelectNoButtonTest_1024x576", 0),
				("ChampionSelectNoButtonTest_1280x720", 0),
				("ChampionSelectNoButtonTest_1600x900", 0),
				("ChampionSelectBlindPickTest_1024x576", 2),
				("ChampionSelectBlindPickTest_1280x720", 2),
				("ChampionSelectBlindPickTest_1600x900", 2)
			}.OrderBy(test => Regex.Match(test.Item1, @"\d+x\d+").Value))
			{
				var name = $"{test.sample}.png";
				Console.WriteLine($"Testing {name}");
				var sample = GetSample(name);

				if (patternsClass == null || patternsClass.Resolution.Width != sample.Width
					|| patternsClass.Resolution.Height != sample.Height)
					patternsClass = new Patterns(new Size(sample.Width, sample.Height));

				Assert.AreEqual(test.position, patternsClass.DetectOurPickPosition(sample), name);
			}
		}

		private static LockBitmap.LockBitmap GetSample(string name)
		{
			var resName = string.Join(".", nameof(LolAutoAccept) + nameof(Tests), "TestSamples", name);
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
			if (stream == null)
				throw new Exception($"Resource {resName} not found");
			var lockBitmap = new LockBitmap.LockBitmap(new Bitmap(stream));//.Scale(Patterns.NativeResolution, InterpolationMode.Bicubic));//.Laplacian5x5Filter(false));
			lockBitmap.UnlockBits();
			return lockBitmap;
		}

		private class BanTestSample
		{
			public readonly string SampleName;
			public readonly LockBitmap.LockBitmap Sample;
			public readonly int Position;
			public readonly BPType Type;
			public readonly string Champion;

			public BanTestSample(string sampleName, LockBitmap.LockBitmap sample,
				int position, BPType type, string champion)
			{
				SampleName = sampleName;
				Sample = sample;
				Position = position;
				Type = type;
				Champion = champion;
			}
		}

		private IEnumerable<BanTestSample> GenBanSamples()
		{
			(int position, BPType type, string champion)[] Fill(BPType type, params
				(int position, BPType type, string champion)[] values)
				=> Enumerable.Range(0, 5).Select(x =>
						(values == null || values.All(v => v.position != x))
							? (x, type, null)
							: values.FirstOrDefault(v => v.position == x))
					.ToArray();

			var set1 = Fill(BPType.Stub,
				(0, BPType.Champion, "Brand"),
				(1, BPType.Champion, "Chogath"),
				(2, BPType.Champion, "caitlyn"));

			var set2 = Fill(BPType.Stub,
				(0, BPType.Champion, "drmundo"),
				(1, BPType.Champion, "Fiddlesticks"),
				(2, BPType.Champion, "Azir"));

			var set3 = Fill(BPType.Stub,
				(0, BPType.Champion, "Camille"),
				(1, BPType.Champion, "drmundo"),
				(2, BPType.Champion, "Diana"));

			return new(string sample, (int position, BPType type, string champion)[])[]
				{
					("ChampionSelectBanButtonHoverTest_1024x576", Fill(BPType.Stub,
						(2, BPType.Champion, "drmundo")
					)),
					("ChampionSelectBanButtonHoverTest_1280x720", Fill(BPType.Stub, null)),
					("ChampionSelectBanButtonHoverTest_1600x900", Fill(BPType.Stub,
						(2, BPType.Champion, "Diana")
					)),
					("ChampionSelectBanButtonTest_1024x576", Fill(BPType.Stub,
						(2, BPType.Champion, "drmundo")
					)),
					("ChampionSelectBanButtonTest_1280x720", Fill(BPType.Stub, null)),
					("ChampionSelectBanButtonTest_1600x900", Fill(BPType.Stub,
						(1, BPType.Champion, "Camille"),
						(2, BPType.Champion, "Braum")
					)),
					("ChampionSelectBanLockButtonDisabledTest_1024x576", Fill(BPType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest_1280x720", Fill(BPType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest_1600x900", Fill(BPType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest2_1024x576",
					Fill(BPType.Stub,
						(0, BPType.Champion, "Camille"),
						(1, BPType.Champion, "Cassiopeia"),
						(2, BPType.Champion, "drmundo")
					)),
					("ChampionSelectBanLockButtonDisabledTest2_1280x720", Fill(BPType.Stub,
						(0, BPType.Champion, "Cassiopeia"),
						(1, BPType.Champion, "Camille"),
						(2, BPType.Champion, "Braum")
					)),
					("ChampionSelectBanLockButtonDisabledTest2_1600x900", Fill(BPType.Stub,
						(0, BPType.Champion, "Draven"),
						(1, BPType.Champion, "Camille"),
						(2, BPType.Champion, "Braum")
					)),
					("ChampionSelectLockButtonHoverTest_1024x576", set1),
					("ChampionSelectLockButtonHoverTest_1280x720", set2),
					("ChampionSelectLockButtonHoverTest_1600x900", set3),
					("ChampionSelectLockButtonTest_1024x576", Fill(BPType.Stub,
						(0, BPType.Champion, "Cassiopeia"),
						(1, BPType.Champion, "Camille"),
						(2, BPType.Champion, "Braum")
					)),
					("ChampionSelectLockButtonTest_1280x720", set2),
					("ChampionSelectLockButtonTest_1600x900", set3),
					("ChampionSelectNoButtonTest_1024x576", set1),
					("ChampionSelectNoButtonTest_1280x720", Fill(BPType.Stub,
						(0, BPType.Champion, "Fiddlesticks"),
						(1, BPType.Champion, "Ezreal"),
						(2, BPType.Champion, "Cassiopeia")
					)),
					("ChampionSelectNoButtonTest_1600x900", set1)
				}.Concat(new[]
					{
						"AcceptMatchButtonHoverTest",
						"AcceptMatchButtonTest",
						"CreateCustomScreenTest",
						"MainScreenTest",
						"MainScreenTest2",
						"PlayScreenTest",
						"ChampionSelectBlindPickTest"
					}.SelectMany(x => Patterns.SupportedResolutions
						.Select(res => $"{x}_{res.Width}x{res.Height}"))
					.Select(x => (x, Fill(BPType.Unknown, null)))
					.ToArray())
				.SelectMany(x =>
				{
					var sample = GetSample($"{x.Item1}.png");
					return x.Item2.Select(x2 =>
						new BanTestSample(x.Item1, sample, x2.position, x2.type, x2.champion));
				})
				.OrderBy(x => Regex.Match(x.SampleName, @"\d+x\d+").Value);
		}

		[TestMethod()]
		public void DetermineBan()
		{
			Patterns patternsClass = null;

			foreach (var test in GenBanSamples())
			{
				if (patternsClass == null || patternsClass.Resolution.Width != test.Sample.Width
					|| patternsClass.Resolution.Height != test.Sample.Height)
					patternsClass = new Patterns(new Size(test.Sample.Width, test.Sample.Height));

				var result = patternsClass.DetermineBanTest(test.Sample, test.Position);
				Assert.AreEqual(result.type, test.Type, $"Position {test.Position}");
				if (test.Type == BPType.Champion)
					Assert.AreEqual(result.champion, test.Champion.ToLowerInvariant(), $"Position {test.Position}");
			}
		}

		[TestMethod()]
		public void DetermineBan2()
		{
			var summary = new List<string>();
			foreach (var imode in new[]
			{
				InterpolationMode.Bicubic, InterpolationMode.Bilinear,
				InterpolationMode.HighQualityBicubic, InterpolationMode.HighQualityBilinear, InterpolationMode.NearestNeighbor
			})
			{
				foreach (var alg in new[]
				{
					//Patterns.CompareAlgorithm.Plain, Patterns.CompareAlgorithm.ColorPriority,
					//	Patterns.CompareAlgorithm.JacobKrarup
					Patterns.CompareAlgorithm.ColorTons
				})
				{
					summary.Add(DetBanAlg(imode, alg) + $" {imode} {alg}");
					//summary.Add(string.Empty);
				}
			}
			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
		}

		private string DetBanAlg(InterpolationMode imode, Patterns.CompareAlgorithm alg)
		{
			Patterns patternsClass = null;
			double worstTrue = double.MinValue;
			double worstFalse = double.MaxValue;
			var badOrder = new List<(double, string)>();
			var goodOrder = new List<(double, string)>();
			foreach (var test in GenBanSamples())
			{
				Console.WriteLine();
				Console.WriteLine($"{test.Type} {test.Champion} {test.Position} {test.SampleName}");
				if (patternsClass == null || patternsClass.Resolution.Width != test.Sample.Width
					|| patternsClass.Resolution.Height != test.Sample.Height)
					patternsClass = new Patterns(new Size(test.Sample.Width, test.Sample.Height), imode);

				var res = patternsClass.DetermineBanTest2(test.Sample, test.Position, alg);
				var right = test.Type == BPType.Unknown
					? default((BPType type, string champion, double percent))
					: res.First(x => x.type == test.Type && (test.Champion == null || x.champion == test.Champion.ToLowerInvariant()));
				if (test.Type != BPType.Unknown)
				{
					Console.WriteLine($"RIGHT {right.percent:P} {right.champion} {right.type}");
					goodOrder.Add(
						(right.percent,
						$"{right.percent:P} {right.champion} {right.type} __ {test.Champion} {test.Type} {test.Position} {test.SampleName}"
						));
				}
				worstTrue = Math.Max(worstTrue, right.percent);
				worstFalse = Math.Min(worstFalse, res.Except(new[] { right }).Min(x => x.percent));

				foreach (var bad in res.Except(new[] { right }).OrderBy(x => x.percent).Take(3))
				{
					Console.WriteLine($"{bad.percent:P} {bad.champion} {bad.type}");
				}
				badOrder.AddRange(res.Except(new[] { right })
					.Select(
						x => (x.percent,
							$"{x.percent:P} {x.champion} {x.type} __ {test.Champion} {test.Type} {test.Position} {test.SampleName}")));
			}
			Console.WriteLine();
			Console.WriteLine("GOOD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, goodOrder.OrderByDescending(x => x.Item1).Select(x => x.Item2)));
			Console.WriteLine("BAD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, badOrder.OrderBy(x => x.Item1).Select(x => x.Item2)));
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}");
			return $"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}";
		}

		[TestMethod()]
		public void BanStubTest()
		{
			var summary = new List<string>();
			foreach (var imode in new[]
			{
				InterpolationMode.Bicubic,
				//InterpolationMode.Bilinear,
				//InterpolationMode.HighQualityBicubic, InterpolationMode.HighQualityBilinear, InterpolationMode.NearestNeighbor
			})
			{
				foreach (var alg in new[]
				{
					//Patterns.CompareAlgorithm.Plain, Patterns.CompareAlgorithm.ColorPriority,
					//	Patterns.CompareAlgorithm.JacobKrarup
					//Patterns.CompareAlgorithm.ColorTons,
					Patterns.CompareAlgorithm.StubMatch2,
				})
				{
					var par = new[] { 15 }; //{ 15, 20, 25, 30, 40, 50 };
					foreach (var StubMatchAvg in par)
					{
						foreach (var StubWhiteGrayLow in par)
						{
							foreach (var StubWhiteGrayHigh in par)
							{
								foreach (var StubBlackGrayHigh in par)
								{
									//Patterns.StubMatchAvg = StubMatchAvg;
									//Patterns.StubWhiteGrayLow = StubWhiteGrayLow;
									//Patterns.StubWhiteGrayHigh = StubWhiteGrayHigh;
									//Patterns.StubBlackGrayHigh = StubBlackGrayHigh;
									summary.Add(DoBanTest(imode, alg) +
												$" {imode} {alg} _ {StubMatchAvg} {StubWhiteGrayLow} {StubWhiteGrayHigh} {StubBlackGrayHigh}");
									//summary.Add(string.Empty);
								}
							}
						}
					}
				}
			}
			Console.WriteLine();
			foreach (string s in summary)
			{
				Console.WriteLine(s);
			}
		}

		private string DoBanTest(InterpolationMode imode, Patterns.CompareAlgorithm alg)
		{
			Patterns patternsClass = null;
			var results = GenBanSamples()
					//.Select(x =>
					//new BanTestSample(x.SampleName, new LockBitmap.LockBitmap(x.Sample.RecreateBitmap().Laplacian3x3Filter(false))
					//	, x.Position, x.Type, x.Champion))
					.Select(test =>
			{
				if (patternsClass == null || patternsClass.Resolution.Width != test.Sample.Width
					|| patternsClass.Resolution.Height != test.Sample.Height)
					patternsClass = new Patterns(new Size(test.Sample.Width, test.Sample.Height), imode);
				return (patternsClass.BanStubTest(test.Sample, test.Position, alg), test);
			}).ToArray();

			double worstTrue = results.Where(x => x.Item2.Type == BPType.Stub).Max(x => x.Item1);
			double worstFalse = results.Where(x => x.Item2.Type != BPType.Stub).Min(x => x.Item1);

			Console.WriteLine();
			Console.WriteLine("GOOD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, results
				.Where(x => x.Item2.Type == BPType.Stub)
				.OrderByDescending(x => x.Item1)
				.Select(x => $"{x.Item1:P} {x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}"))
			);
			Console.WriteLine("BAD ORDERED");
			Console.WriteLine(string.Join(Environment.NewLine, results
				.Where(x => x.Item2.Type != BPType.Stub)
				.OrderBy(x => x.Item1)
				.Select(x => $"{x.Item1:P} {x.Item2.Champion ?? x.Item2.Type.ToString()} {x.Item2.SampleName}"))
			);
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine($"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}");
			return $"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}";
		}

		[TestMethod()]
		public void CachedBitmapPixelsSpeedTest()
		{
			var stopWatch = new Stopwatch();
			const int mulTimes = 100;
			int dummySum = 0;

			var bitmap = new Bitmap(500, 500);
			stopWatch.Start();
			for (int mul = 0; mul < mulTimes; mul++)
				for (int x = 0; x < bitmap.Width; x++)
					for (int y = 0; y < bitmap.Height; y++)
					{
						var pixel = bitmap.GetPixel(x, y);
						dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
					}
			stopWatch.Stop();

			var bitmapTime = stopWatch.Elapsed;
			using (var lockBitmap = new LockBitmap.LockBitmap(bitmap))
			{
				stopWatch.Restart();
				for (int mul = 0; mul < mulTimes; mul++)
					for (int x = 0; x < lockBitmap.Width; x++)
						for (int y = 0; y < lockBitmap.Height; y++)
						{
							var pixel = lockBitmap.GetPixel(x, y);
							dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
						}
				stopWatch.Stop();
			}
			var lockBitmapTime = stopWatch.Elapsed;

			var cachedBitmap = new CachedBitmapPixels(bitmap);
			stopWatch.Restart();
			for (int mul = 0; mul < mulTimes; mul++)
				for (int x = 0; x < cachedBitmap.Width; x++)
					for (int y = 0; y < cachedBitmap.Height; y++)
					{
						var pixel = cachedBitmap[x, y];
						dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
					}
			stopWatch.Stop();
			var cachedBitmapTime = stopWatch.Elapsed;

			var cachedBitmapArray = new CachedBitmapPixels(bitmap);
			stopWatch.Restart();
			for (int mul = 0; mul < mulTimes; mul++)
			{
				var pixels = cachedBitmapArray.CacheAll();
				for (int x = 0; x < cachedBitmapArray.Width; x++)
					for (int y = 0; y < cachedBitmapArray.Height; y++)
					{
						var pixel = pixels[x + y * cachedBitmapArray.Width];
						dummySum += pixel.A + pixel.R + pixel.G + pixel.B;
					}
			}
			stopWatch.Stop();
			var cachedBitmapArrayTime = stopWatch.Elapsed;
			Console.WriteLine($"Bitmap time {bitmapTime}");
			Console.WriteLine($"LockBitmap time {lockBitmapTime}");
			Console.WriteLine($"CachedBitmapTime time {cachedBitmapTime}");
			Console.WriteLine($"CachedBitmapArrayTime time {cachedBitmapArrayTime}");

			Assert.IsTrue(cachedBitmapTime < bitmapTime && cachedBitmapTime < lockBitmapTime);
			Assert.IsTrue(cachedBitmapArrayTime < bitmapTime && cachedBitmapArrayTime < lockBitmapTime);
		}
	}

}