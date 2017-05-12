using Microsoft.VisualStudio.TestTools.UnitTesting;
using LolAutoAccept;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternsTests
	{
		//[TestMethod()]
		//public void IsMatchTestTest()
		//{
		//	foreach (var res in new[] { (1280, 720) })
		//	{
		//		var testSamples = new[]
		//		{
		//			""
		//		}.Select(ts => new { path = ts, sample = new LockBitmap.LockBitmap(new Bitmap(ts)) });

		//		var patternsClass = new Patterns(res.Item1, res.Item2);


		//		var patterns = new[]
		//		{
		//		patternsClass.ChampionSelectSample.Value,
		//		patternsClass.AcceptMatchButtonSample.Value,
		//		patternsClass.AcceptMatchButtonHoverSample.Value,
		//		patternsClass.ChampionSelectBanButtonSample.Value,
		//		patternsClass.ChampionSelectBanButtonHoverSample.Value,
		//		patternsClass.ChampionSelectLockButtonSample.Value,
		//		patternsClass.ChampionSelectLockButtonHoverSample.Value
		//		};

		//		var calcedMatch =
		//			patterns.SelectMany(p => testSamples.Select(ts => new
		//			{
		//				pattern = p,
		//				samplePath = ts.path,
		//				result = Patterns.IsMatchTest(ts.sample, p)
		//			}));

		//		foreach (var pattern in patterns)
		//		{
		//			var min = calcedMatch.Where(m => m.pattern == pattern && m.samplePath != "")
		//				.Aggregate(0d, (d, tuples) => Math.Min(d, tuples.result));

		//			var right = calcedMatch.First(m => m.pattern == pattern && m.samplePath == "");

		//			if (min - right.result < 0.2)
		//				Assert.Fail();
		//		}
		//	}
		//}


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
	}
}