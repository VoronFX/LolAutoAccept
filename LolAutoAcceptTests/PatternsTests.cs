using Microsoft.VisualStudio.TestTools.UnitTesting;
using LolAutoAccept;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageEdgeDetection;
using BPType = LolAutoAccept.Tests.Samples.BanPickTestSample.BanPickType;

namespace LolAutoAccept.Tests
{
	[TestClass()]
	public class PatternsTests
	{

		[TestMethod()]
		public void HasBanLockButtonDisabledTest()
		{
			TestMatch((patterns, bitmap) => patterns.HasBanLockButtonDisabled(bitmap),
				Samples.ChampionSelect.BanLockButtonDisabled,
				Samples.All.Except(Samples.ChampionSelect.BanLockButtonDisabled).ToArray());
		}

		[TestMethod()]
		public void IsBanButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsBanButton(bitmap),
				Samples.ChampionSelect.BanButton,
				Samples.All.Except(Samples.ChampionSelect.BanButton).ToArray());
		}

		[TestMethod()]
		public void IsLockButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsLockButton(bitmap),
				Samples.ChampionSelect.LockButton,
				Samples.All.Except(Samples.ChampionSelect.LockButton).ToArray());
		}

		[TestMethod()]
		public void IsChampionSelectTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsChampionSelect(bitmap),
				Samples.ChampionSelect.All,
				Samples.All.Except(Samples.ChampionSelect.All).ToArray());
		}

		[TestMethod()]
		public void IsAcceptMatchButtonTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsAcceptMatchButton(bitmap),
				Samples.OtherScreens.AcceptMatchButton,
				Samples.All.Except(Samples.OtherScreens.AcceptMatchButton).ToArray());
		}

		[TestMethod()]
		public void IsChampionSearchTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsChampionSearch(bitmap),
				Samples.ChampionSelect.ChampionSearch,
				Samples.All.Except(Samples.ChampionSelect.ChampionSearch).ToArray());
		}

		[TestMethod()]
		public void IsFirstSelectBanTest()
		{
			TestMatch((patterns, bitmap) => patterns.IsFirstSelectBan(bitmap),
				Samples.ChampionSelect.FirstSelectBan,
				Samples.All.Except(Samples.ChampionSelect.FirstSelectBan).ToArray());
		}

		private void TestMatch(
			Func<Patterns, CachedBitmapPixels, bool> testMethod,
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

						Assert.AreEqual(expected, testMethod(patternsClass, Samples.LoadSample(name)), name);
					}
				}

				Check(truePatterns, true);
				Check(falsePatterns, false);
			}
		}

		[TestMethod()]
		public void DetectOurPickPositionTest()
		{
			Patterns patternsClass = null;

			foreach (var test in Samples.PickPosition)
			{
				var name = $"{test.sample}.png";
				Console.WriteLine($"Testing {name}");
				var sample = Samples.LoadSample(name);

				patternsClass = EnsureRightResolution(patternsClass, sample.Width, sample.Height);

				Assert.AreEqual(test.position, patternsClass.DetectOurPickPosition(sample), name);
			}
		}

		[TestMethod()]
		public void IsBanStubTest()
		{
			Patterns patternsClass = null;

			foreach (var test in Samples.GenBanSamples())
			{
				patternsClass = EnsureRightResolution(patternsClass, test.Sample.Width, test.Sample.Height);

				Assert.AreEqual(patternsClass.IsBanStub(test.Sample, test.Position),
					test.Type == BPType.Stub, $"Position {test.Position}");
			}
		}

		[TestMethod()]
		public void DetectBanChampionTest()
		{
			Patterns patternsClass = null;

			foreach (var test in Samples.GenBanSamples())
			{
				patternsClass = EnsureRightResolution(patternsClass, test.Sample.Width, test.Sample.Height);

				Assert.AreEqual(test.Champion?.ToLowerInvariant(),
					patternsClass.DetectBanChampion(test.Sample, test.Position), $"Position {test.Position}");
			}
		}

		private Patterns EnsureRightResolution(Patterns patterns, int width, int height)
			=> EnsureRightResolution(patterns, new Size(width, height));

		private Patterns EnsureRightResolution(Patterns patterns, Size resolution)
		{
			if (patterns == null || patterns.Resolution.Width != resolution.Width
			    || patterns.Resolution.Height != resolution.Height)
				return new Patterns(resolution);
			return patterns;
		}
	}
}