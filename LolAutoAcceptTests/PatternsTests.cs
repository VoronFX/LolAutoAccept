using Microsoft.VisualStudio.TestTools.UnitTesting;
using LolAutoAccept;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternsTests
	{
		private static readonly string[] AllTestSamples = new[]
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
			"PlayScreenTest",
			"CreateCustomScreenTest"
		};


		[TestMethod()]
		public void HasBanLockButtonDisabledTest()
		{
			var trueTestSamples = new[]
			{
				"ChampionSelectBanLockButtonDisabledTest",
				"ChampionSelectBanLockButtonDisabledTest2"
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
				"PlayScreenTest",
				"CreateCustomScreenTest",
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest"
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
			foreach (var res in new[] { (1024, 576), (1280, 720), (1600, 900) })
			{
				var patternsClass = new Patterns(res.Item1, res.Item2);

				void Check(IEnumerable<string> patterns, bool expected)
				{
					foreach (var pattern in patterns)
					{
						var name = $"{pattern}_{res.Item1}x{res.Item2}.png";
						Console.WriteLine($"Testing {name}");
						var resName = string.Join(".",
							nameof(LolAutoAccept) + nameof(Tests),
							"TestSamples", name);
						var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
						if (stream == null)
							throw new Exception($"Resource {resName} not found");

						Assert.AreEqual(expected, testMethod(patternsClass,
							new LockBitmap.LockBitmap(new Bitmap(stream))), name);
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
			Console.WriteLine($"Result diff: {result.Item1 - result.Item2:P} worstFalse: {result.Item1:P} worstTrue: {result.Item2:P}");
			Assert.IsTrue(result.Item1 - result.Item2 > 0);
		}

		private static (double, double) TestAlgInterpolation(Patterns.CompareAlgorithm alg, InterpolationMode imode, List<string> summary)
		{
			var testMethods = new List<(Func<Patterns, LockBitmap.LockBitmap, double> testMethod,
				string[] truePatterns,
				string[] falsePatterns)>();

			var samplesArray = new[]
			{
				"ChampionSelectBanLockButtonDisabledTest",
				"ChampionSelectBanLockButtonDisabledTest2"
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

			return new[] { (1024, 576), (1280, 720), (1600, 900) }.Select(res =>
			  {
				  Console.WriteLine();
				  Console.WriteLine($"imode: {imode}, alg: {alg} res: {res.Item1}x{res.Item2}");
				  var patternsClass = new Patterns(res.Item1, res.Item2, imode);

				  IEnumerable<double> Calc(IEnumerable<string> patterns, Func<Patterns, LockBitmap.LockBitmap, double> method)
					  => patterns.Select(pattern =>
					  {
						  var name = $"{pattern}_{res.Item1}x{res.Item2}.png";
						//Console.WriteLine($"Testing {name}");
						var resName = string.Join(".",
							  nameof(LolAutoAccept) + nameof(Tests),
							  "TestSamples", name);
						  var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
						  if (stream == null)
							  throw new Exception($"Resource {resName} not found");

						  return method(patternsClass,
							  new LockBitmap.LockBitmap(new Bitmap(stream)));
					  });

				  var results = testMethods.Select(tm =>
				  {
					  var worstFalse = Calc(tm.falsePatterns, tm.testMethod)
						  .Aggregate(double.MaxValue, Math.Min);

					  var worstTrue = Calc(tm.truePatterns, tm.testMethod)
						  .Aggregate(double.MinValue, Math.Max);

					  Console.WriteLine($"diff: {worstFalse - worstTrue:P} worstFalse: {worstFalse:P} worstTrue: {worstTrue:P}");

					  return (worstFalse, worstTrue);
				  }).ToArray();

				  var worstFalseSummary = results
					  .Select(tm => tm.Item1)
					  .Aggregate(double.MaxValue, Math.Min);

				  var worstTrueSummary = results
					  .Select(tm => tm.Item2)
					  .Aggregate(double.MinValue, Math.Max);

				  var worstFalseAvgSummary = results.Select(tm => tm.Item1)
												 .Aggregate(0d, (d, d1) => d + d1) / results.Length;

				  var worstTrueAvgSummary = results.Select(tm => tm.Item2)
												.Aggregate(0d, (d, d1) => d + d1) / results.Length;
				  summary.Add(
					  $"diff: {worstFalseSummary - worstTrueSummary:P} worstFalse: {worstFalseSummary:P} worstTrue: {worstTrueSummary:P}"
					  + $" diffAvg: {worstFalseAvgSummary - worstTrueAvgSummary:P} worstFalseAvg: {worstFalseAvgSummary:P} worstTrueAvg: {worstTrueAvgSummary:P}"
					  + $" imode: {imode}, alg: {alg} res: {res.Item1}x{res.Item2}");

				  return (worstFalseSummary, worstTrueSummary);
			  }).Aggregate((double.MaxValue, double.MinValue), 
			  (x,x2)=> (Math.Min(x.Item1, x2.Item1), Math.Max(x.Item2, x2.Item2)));
		}
	}
}