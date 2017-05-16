using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using BanPickType = LolAutoAccept.Tests.Samples.BanTestSample.BanPickType;

namespace LolAutoAccept.Tests
{
	public static class Samples
	{
		public static class OtherScreens
		{
			public static string[] AcceptMatchButton { get; } =
			{
				"AcceptMatchButtonTest",
				"AcceptMatchButtonHoverTest"
			};

			public static string[] All { get; } = new[]
				{
					"MainScreenTest",
					"MainScreenTest2",
					"PlayScreenTest",
					"CreateCustomScreenTest"
				}.Concat(AcceptMatchButton)
				.ToArray();
		}

		public static class ChampionSelect
		{
			public static string[] BanLockButtonDisabled { get; } =
			{
				"ChampionSelectBanLockButtonDisabledTest",
				"ChampionSelectBanLockButtonDisabledTest2",
				"ChampionSelectBlindPickTest"
			};

			public static string[] BanButton { get; } =
			{
				"ChampionSelectBanButtonTest",
				"ChampionSelectBanButtonHoverTest"
			};

			public static string[] LockButton { get; } =
			{
				"ChampionSelectLockButtonTest",
				"ChampionSelectLockButtonHoverTest"
			};

			public static string[] All { get; } = new[]
					{"ChampionSelectNoButtonTest"}
				.Concat(BanLockButtonDisabled)
				.Concat(BanButton)
				.Concat(LockButton)
				.ToArray();
		}

		public static string[] All { get; } =
			ChampionSelect.All
				.Concat(OtherScreens.All)
				.ToArray();

		public static (Bitmap patternSample, string[] truePatterns, string[] falsePatterns)[] 
			ScreenSamples { get; } =
		{
			(LolAutoAccept.Samples.ChampionSelectBanLockButtonDisabled,
				ChampionSelect.BanLockButtonDisabled,
				All.Except(ChampionSelect.BanLockButtonDisabled).ToArray()),

			(LolAutoAccept.Samples.ChampionSelectBanButton,
				ChampionSelect.BanButton.Take(1).ToArray(),
				All.Except(ChampionSelect.BanButton).ToArray()),

			(LolAutoAccept.Samples.ChampionSelectBanButtonHover,
				ChampionSelect.BanButton.Skip(1).Take(1).ToArray(),
				All.Except(ChampionSelect.BanButton).ToArray()),

			(LolAutoAccept.Samples.ChampionSelectLockButton,
				ChampionSelect.LockButton.Take(1).ToArray(),
				All.Except(ChampionSelect.LockButton).ToArray()),

			(LolAutoAccept.Samples.ChampionSelectLockButtonHover,
				ChampionSelect.LockButton.Skip(1).Take(1).ToArray(),
				All.Except(ChampionSelect.LockButton).ToArray()),

			(LolAutoAccept.Samples.AcceptMatchButton,
				OtherScreens.AcceptMatchButton.Take(1).ToArray(),
				All.Except(OtherScreens.AcceptMatchButton).ToArray()),

			(LolAutoAccept.Samples.AcceptMatchButtonHover,
				OtherScreens.AcceptMatchButton.Skip(1).Take(1).ToArray(),
				All.Except(OtherScreens.AcceptMatchButton).ToArray()),

			(LolAutoAccept.Samples.ChampionSelect,
				ChampionSelect.All,
				All.Except(ChampionSelect.All).ToArray())
		};

		public static (string sample, int position)[] PickPosition { get; } =
				new(string sample, int position)[]
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
				}.OrderBy(test => Regex.Match(test.Item1, @"\d+x\d+").Value)
				.ToArray();

		public class BanTestSample
		{
			public readonly string SampleName;
			public readonly CachedBitmapPixels Sample;
			public readonly int Position;
			public readonly BanPickType Type;
			public readonly string Champion;

			public BanTestSample(string sampleName, CachedBitmapPixels sample,
				int position, BanPickType type, string champion)
			{
				SampleName = sampleName;
				Sample = sample;
				Position = position;
				Type = type;
				Champion = champion;
			}

			public enum BanPickType
			{
				Unknown,
				Stub,
				Champion,
			}
		}

		public static IEnumerable<BanTestSample> GenBanSamples()
		{
			(int position, BanPickType type, string champion)[] Fill(BanPickType type, params
				(int position, BanPickType type, string champion)[] values)
				=> Enumerable.Range(0, 5).Select(x =>
						(values == null || values.All(v => v.position != x))
							? (x, type, null)
							: values.FirstOrDefault(v => v.position == x))
					.ToArray();

			var set1 = Fill(BanPickType.Stub,
				(0, BanPickType.Champion, "Brand"),
				(1, BanPickType.Champion, "Chogath"),
				(2, BanPickType.Champion, "caitlyn"));

			var set2 = Fill(BanPickType.Stub,
				(0, BanPickType.Champion, "drmundo"),
				(1, BanPickType.Champion, "Fiddlesticks"),
				(2, BanPickType.Champion, "Azir"));

			var set3 = Fill(BanPickType.Stub,
				(0, BanPickType.Champion, "Camille"),
				(1, BanPickType.Champion, "drmundo"),
				(2, BanPickType.Champion, "Diana"));

			return new(string sample, (int position, BanPickType type, string champion)[])[]
				{
					("ChampionSelectBanButtonHoverTest_1024x576", Fill(BanPickType.Stub,
						(2, BanPickType.Champion, "drmundo")
					)),
					("ChampionSelectBanButtonHoverTest_1280x720", Fill(BanPickType.Stub, null)),
					("ChampionSelectBanButtonHoverTest_1600x900", Fill(BanPickType.Stub,
						(2, BanPickType.Champion, "Diana")
					)),
					("ChampionSelectBanButtonTest_1024x576", Fill(BanPickType.Stub,
						(2, BanPickType.Champion, "drmundo")
					)),
					("ChampionSelectBanButtonTest_1280x720", Fill(BanPickType.Stub, null)),
					("ChampionSelectBanButtonTest_1600x900", Fill(BanPickType.Stub,
						(1, BanPickType.Champion, "Camille"),
						(2, BanPickType.Champion, "Braum")
					)),
					("ChampionSelectBanLockButtonDisabledTest_1024x576", Fill(BanPickType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest_1280x720", Fill(BanPickType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest_1600x900", Fill(BanPickType.Stub, null)),
					("ChampionSelectBanLockButtonDisabledTest2_1024x576",
					Fill(BanPickType.Stub,
						(0, BanPickType.Champion, "Camille"),
						(1, BanPickType.Champion, "Cassiopeia"),
						(2, BanPickType.Champion, "drmundo")
					)),
					("ChampionSelectBanLockButtonDisabledTest2_1280x720", Fill(BanPickType.Stub,
						(0, BanPickType.Champion, "Cassiopeia"),
						(1, BanPickType.Champion, "Camille"),
						(2, BanPickType.Champion, "Braum")
					)),
					("ChampionSelectBanLockButtonDisabledTest2_1600x900", Fill(BanPickType.Stub,
						(0, BanPickType.Champion, "Draven"),
						(1, BanPickType.Champion, "Camille"),
						(2, BanPickType.Champion, "Braum")
					)),
					("ChampionSelectLockButtonHoverTest_1024x576", set1),
					("ChampionSelectLockButtonHoverTest_1280x720", set2),
					("ChampionSelectLockButtonHoverTest_1600x900", set3),
					("ChampionSelectLockButtonTest_1024x576", Fill(BanPickType.Stub,
						(0, BanPickType.Champion, "Cassiopeia"),
						(1, BanPickType.Champion, "Camille"),
						(2, BanPickType.Champion, "Braum")
					)),
					("ChampionSelectLockButtonTest_1280x720", set2),
					("ChampionSelectLockButtonTest_1600x900", set3),
					("ChampionSelectNoButtonTest_1024x576", set1),
					("ChampionSelectNoButtonTest_1280x720", Fill(BanPickType.Stub,
						(0, BanPickType.Champion, "Fiddlesticks"),
						(1, BanPickType.Champion, "Ezreal"),
						(2, BanPickType.Champion, "Cassiopeia")
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
					.Select(x => (x, Fill(BanPickType.Unknown, null)))
					.ToArray())
				.SelectMany(x =>
				{
					var sample = LoadSample($"{x.Item1}.png");
					return x.Item2.Select(x2 =>
						new BanTestSample(x.Item1, sample, x2.position, x2.type, x2.champion));
				})
				.OrderBy(x => Regex.Match(x.SampleName, @"\d+x\d+").Value);
		}

		private static readonly ConcurrentDictionary<string, WeakReference<CachedBitmapPixels>> SamplesCache
			= new ConcurrentDictionary<string, WeakReference<CachedBitmapPixels>>();

		public static CachedBitmapPixels LoadSample(string name)
		{
			var resName = string.Join(".", nameof(LolAutoAccept) + nameof(Tests), "TestSamples", name);

			if (SamplesCache.TryGetValue(resName, out WeakReference<CachedBitmapPixels> weakRef)
				&& weakRef.TryGetTarget(out CachedBitmapPixels cached))
				return cached;

			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
			if (stream == null)
				throw new Exception($"Resource {resName} not found");
			var sample = new CachedBitmapPixels(new Bitmap(stream));
			SamplesCache.AddOrUpdate(resName, new WeakReference<CachedBitmapPixels>(sample), 
				(s, reference) =>
			{
				reference.SetTarget(sample);
				return reference;
			});
			return sample;
		}
	}
}