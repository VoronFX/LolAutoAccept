using Microsoft.VisualStudio.TestTools.UnitTesting;
using LolAutoAccept;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BPType = LolAutoAccept.Patterns.BanPickType;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternsTests
	{
		private static readonly string[] AllTestSamples =
		{
			"AcceptMatchButtonTest",
			"AcceptMatchButtonHoverTest",
			"ChampionSelectBanLockButtonDisabledTest",
			"ChampionSelectBanLockButtonDisabledTest2",
			"ChampionSelectBanButtonTest",
			"ChampionSelectBanButtonHoverTest",
			"ChampionSelectLockButtonTest",
			"ChampionSelectLockButtonHoverTest",
			"ChampionSelectNoButtonTest",
			"MainScreenTest",
			"MainScreenTest2",
			"PlayScreenTest",
			"CreateCustomScreenTest",
			"BlindPickTest"
		};


		[TestMethod()]
		public void HasBanLockButtonDisabledTest()
		{
			var trueTestSamples = new[]
			{
				"ChampionSelectBanLockButtonDisabledTest",
				"ChampionSelectBanLockButtonDisabledTest2",
				"BlindPickTest"
			};
			TestMatch((patterns, bitmap) => patterns.HasBanLockButtonDisabled(bitmap),
				trueTestSamples,
				AllTestSamples.Except(trueTestSamples).ToArray());
		}

		[TestMethod()]
		public void IsBanButtonTest()
		{
			var trueTestSamples = new[]
			{
				"ChampionSelectBanButtonTest",
				"ChampionSelectBanButtonHoverTest"
			};
			TestMatch((patterns, bitmap) => patterns.IsBanButton(bitmap),
				trueTestSamples,
				AllTestSamples.Except(trueTestSamples).ToArray());
		}

		[TestMethod()]
		public void IsLockButtonTest()
		{
			var trueTestSamples = new[]
			{
				"ChampionSelectLockButtonTest",
				"ChampionSelectLockButtonHoverTest"
			};
			TestMatch((patterns, bitmap) => patterns.IsLockButton(bitmap),
				trueTestSamples,
				AllTestSamples.Except(trueTestSamples).ToArray());
		}

		[TestMethod()]
		public void IsChampionSelectTest()
		{
			var falseTestSamples = new[]
			{
				"MainScreenTest",
				"MainScreenTest2",
				"PlayScreenTest",
				"CreateCustomScreenTest",
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest",
			};
			TestMatch((patterns, bitmap) => patterns.IsChampionSelect(bitmap),
				AllTestSamples.Except(falseTestSamples).ToArray(),
				falseTestSamples);
		}

		[TestMethod()]
		public void IsAcceptMatchButtonTest()
		{
			var trueTestSamples = new[]
			{
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest"
			};
			TestMatch((patterns, bitmap) => patterns.IsAcceptMatchButton(bitmap),
				trueTestSamples,
				AllTestSamples.Except(trueTestSamples).ToArray());
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
			var testMethods = new List<(Func<Patterns, LockBitmap.LockBitmap, double> testMethod,
				string[] truePatterns,
				string[] falsePatterns)>();

			var samplesArray = new[]
			{
				"ChampionSelectBanLockButtonDisabledTest",
				"ChampionSelectBanLockButtonDisabledTest2",
				"BlindPickTest"
			};
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectBanLockButtonDisabledSample.Value, alg),
				samplesArray, AllTestSamples.Except(samplesArray).ToArray()));

			samplesArray = new[]
			{
				"ChampionSelectBanButtonTest",
				"ChampionSelectBanButtonHoverTest"
			};
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectBanButtonSample.Value, alg),
				new[] { "ChampionSelectBanButtonTest" }, AllTestSamples.Except(samplesArray).ToArray()));
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectBanButtonHoverSample.Value, alg),
				new[] { "ChampionSelectBanButtonHoverTest" }, AllTestSamples.Except(samplesArray).ToArray()));

			samplesArray = new[]
			{
				"ChampionSelectLockButtonTest",
				"ChampionSelectLockButtonHoverTest"
			};
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectLockButtonSample.Value, alg),
				new[] { "ChampionSelectLockButtonTest" }, AllTestSamples.Except(samplesArray).ToArray()));
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectLockButtonHoverSample.Value, alg),
				new[] { "ChampionSelectLockButtonHoverTest" }, AllTestSamples.Except(samplesArray).ToArray()));

			samplesArray = new[]
			{
				"MainScreenTest",
				"MainScreenTest2",
				"PlayScreenTest",
				"CreateCustomScreenTest",
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest"
			};
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.ChampionSelectSample.Value, alg),
				AllTestSamples.Except(samplesArray).ToArray(), samplesArray));

			samplesArray = new[]
			{
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest"
			};
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.AcceptMatchButtonSample.Value, alg),
				new[] { "AcceptMatchButtonTest" }, AllTestSamples.Except(samplesArray).ToArray()));
			testMethods.Add(((patterns, bitmap) =>
					Patterns.IsMatchTest(bitmap, patterns.AcceptMatchButtonHoverSample.Value, alg),
				new[] { "AcceptMatchButtonHoverTest" }, AllTestSamples.Except(samplesArray).ToArray()));

			return Patterns.SupportedResolutions.Select(res =>
			{
				Console.WriteLine();
				Console.WriteLine($"imode: {imode}, alg: {alg} res: {res.Width}x{res.Height}");
				var patternsClass = new Patterns(res, imode);

				IEnumerable<double> Calc(IEnumerable<string> patterns, Func<Patterns, LockBitmap.LockBitmap, double> method)
					=> patterns.Select(pattern =>
					{
						var name = $"{pattern}_{res.Width}x{res.Height}.png";
						//Console.WriteLine($"Testing {name}");
						return method(patternsClass, GetSample(name));
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
				("BlindPickTest_1024x576", 2),
				("BlindPickTest_1280x720", 2),
				("BlindPickTest_1600x900", 2)
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
			var lockBitmap = new LockBitmap.LockBitmap(new Bitmap(stream));
			lockBitmap.UnlockBits();
			return lockBitmap;
		}

		private class BanTestSample
		{
			public readonly string SampleName;
			public readonly Patterns.MatchPoint[] Sample;
			public readonly int Position;
			public readonly BPType Type;
			public readonly string Champion;

			public BanTestSample(string sampleName, Patterns.MatchPoint[] sample, 
				int position, BPType type, string champion)
			{
				SampleName = sampleName;
				Sample = sample;
				Position = position;
				Type = type;
				Champion = champion;
			}
		}

		//private BanTestSample[] GenBanSamples()
		//{
		//	(int position, BPType type, string champion)[] Fill(BPType type, params
		//		(int position, BPType type, string champion)[] values)
		//		=> Enumerable.Range(0, 5).Select(x =>
		//				(values == null || values.All(v => v.position != x))
		//					? (x, type, null)
		//					: values.FirstOrDefault(v => v.position == x))
		//			.ToArray();

		//	var set1 = Fill(BPType.Stub,
		//		(0, BPType.Champion, "Brand"),
		//		(1, BPType.Champion, "Chogath"),
		//		(2, BPType.Champion, "caitlyn"));

		//	var set2 = Fill(BPType.Stub,
		//		(0, BPType.Champion, "drmundo"),
		//		(1, BPType.Champion, "Fiddlesticks"),
		//		(2, BPType.Champion, "Azir"));

		//	var set3 = Fill(BPType.Stub,
		//		(0, BPType.Champion, "Camille"),
		//		(1, BPType.Champion, "drmundo"),
		//		(2, BPType.Champion, "Diana"));

		//	var samples = new (string sample, (int position, BPType type, string champion)[])[]
		//	{
		//		("ChampionSelectBanButtonHoverTest_1024x576", Fill(BPType.Stub,
		//			(2, BPType.Champion, "drmundo")
		//		)),
		//		("ChampionSelectBanButtonHoverTest_1280x720", Fill(BPType.Stub, null)),
		//		("ChampionSelectBanButtonHoverTest_1600x900", Fill(BPType.Stub,
		//			(2, BPType.Champion, "Diana")
		//		)),
		//		("ChampionSelectBanButtonTest_1024x576", Fill(BPType.Stub,
		//			(2, BPType.Champion, "drmundo")
		//		)),
		//		("ChampionSelectBanButtonTest_1280x720", Fill(BPType.Stub, null)),
		//		("ChampionSelectBanButtonTest_1600x900", Fill(BPType.Stub,
		//			(1, BPType.Champion, "Camille"),
		//			(2, BPType.Champion, "Braum")
		//		)),
		//		("ChampionSelectBanLockButtonDisabledTest_1024x576", Fill(BPType.Stub, null)),
		//		("ChampionSelectBanLockButtonDisabledTest_1280x720", Fill(BPType.Stub, null)),
		//		("ChampionSelectBanLockButtonDisabledTest_1600x900", Fill(BPType.Stub, null)),
		//		("ChampionSelectBanLockButtonDisabledTest2_1024x576",
		//			Fill(BPType.Stub,
		//				(0, BPType.Champion, "Camille"),
		//				(1, BPType.Champion, "Cassiopeia"),
		//				(2, BPType.Champion, "Braum")
		//			)),
		//		("ChampionSelectBanLockButtonDisabledTest2_1280x720", Fill(BPType.Stub,
		//			(0, BPType.Champion, "Cassiopeia"),
		//			(1, BPType.Champion, "Camille"),
		//			(2, BPType.Champion, "Braum")
		//		)),
		//		("ChampionSelectBanLockButtonDisabledTest2_1600x900", Fill(BPType.Stub,
		//			(0, BPType.Champion, "Draven"),
		//			(1, BPType.Champion, "Camille"),
		//			(2, BPType.Champion, "Braum")
		//		)),
		//		("ChampionSelectLockButtonHoverTest_1024x576", set1),
		//		("ChampionSelectLockButtonHoverTest_1280x720", set2),
		//		("ChampionSelectLockButtonHoverTest_1600x900", set3),
		//		("ChampionSelectLockButtonTest_1024x576", Fill(BPType.Stub,
		//			(0, BPType.Champion, "Cassiopeia"),
		//			(1, BPType.Champion, "Camille"),
		//			(2, BPType.Champion, "Braum")
		//		)),
		//		("ChampionSelectLockButtonTest_1280x720", set2),
		//		("ChampionSelectLockButtonTest_1600x900", set3),
		//		("ChampionSelectNoButtonTest_1024x576", set1),
		//		("ChampionSelectNoButtonTest_1280x720", Fill(BPType.Stub,
		//			(0, BPType.Champion, "Fiddlesticks"),
		//			(1, BPType.Champion, "Ezreal"),
		//			(2, BPType.Champion, "Cassiopeia")
		//		)),
		//		("ChampionSelectNoButtonTest_1600x900", set1)
		//	}.Concat(new[]
		//		{
		//			"AcceptMatchButtonHoverTest",
		//			"AcceptMatchButtonTest",
		//			"CreateCustomScreenTest",
		//			"MainScreenTest",
		//			"MainScreenTest2",
		//			"PlayScreenTest",
		//			"BlindPickTest"
		//		}.SelectMany(x => Patterns.SupportedResolutions
		//			.Select(res => $"{x}_{res.Width}x{res.Height}"))
		//		.Select(x => (x, Fill(BPType.Unknown, null)))
		//		.ToArray())
		//		.SelectMany(x =>
		//		{
		//			var 
		//			x.Item2.Select()
					
		//		})
		//}

		[TestMethod()]
		public void DetermineBan()
		{
			Patterns patternsClass = null;

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

			foreach (var test in new(string sample, (int position, BPType type, string champion)[])[]
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
						(2, BPType.Champion, "Braum")
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
						"BlindPickTest"
					}.SelectMany(x => Patterns.SupportedResolutions
						.Select(res => $"{x}_{res.Width}x{res.Height}"))
					.Select(x => (x, Fill(BPType.Unknown, null)))
					.ToArray())
				.OrderBy(test2 => Regex.Match(test2.Item1, @"\d+x\d+").Value))
			{
				var name = $"{test.Item1}.png";
				Console.WriteLine($"Testing {name}");
				var sample = GetSample(name);

				if (patternsClass == null || patternsClass.Resolution.Width != sample.Width
					|| patternsClass.Resolution.Height != sample.Height)
					patternsClass = new Patterns(new Size(sample.Width, sample.Height));

				foreach (var ban in test.Item2)
				{
					var result = patternsClass.DetermineBanTest(sample, ban.position);
					Assert.AreEqual(result.type, ban.type, $"Position {ban.position}");
					if (ban.type == BPType.Champion)
						Assert.AreEqual(result.champion, ban.champion.ToLowerInvariant(), $"Position {ban.position}");
				}
			}
		}
	}
}