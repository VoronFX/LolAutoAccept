using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ImageEdgeDetection;

namespace LolAutoAccept
{
	public static class Samples
	{
		private static readonly string SamplesFolderPath
			= string.Join(@".", nameof(LolAutoAccept), "Samples") + ".";

		private static readonly string ChampionSamplesFolderPath
			= string.Join(@".", nameof(LolAutoAccept), "Samples", "Champions") + ".";

		private static readonly string SummonerSpellsSamplesFolderPath
			= string.Join(@".", nameof(LolAutoAccept), "Samples", "SummonerSpells") + ".";

		public static Bitmap AcceptMatchButton => GetSample("AcceptMatchButton.png");
		public static Bitmap AcceptMatchButtonHover => GetSample("AcceptMatchButtonHover.png");

		public static class ChampionSelect
		{
			private static readonly string ChampionSelectSamplesFolderPath
				= string.Join(@".", nameof(LolAutoAccept), "Samples", "ChampionSelect") + ".";

			private static Bitmap GetChampionSelectSample(string name)
				=> LoadSample(ChampionSelectSamplesFolderPath + name);

			public static Bitmap Screen => GetChampionSelectSample("Screen.png");
			public static Bitmap BanButton => GetChampionSelectSample("BanButton.png");
			public static Bitmap BanButtonHover => GetChampionSelectSample("BanButtonHover.png");
			public static Bitmap BanLockButtonDisabled => GetChampionSelectSample("BanLockButtonDisabled.png");
			public static Bitmap LockButton => GetChampionSelectSample("LockButton.png");
			public static Bitmap LockButtonHover => GetChampionSelectSample("LockButtonHover.png");
			public static Bitmap BanStub => GetChampionSelectSample("BanStub.png");
			public static Bitmap PickStub => throw new NotImplementedException();
			public static Bitmap ChampionSearch => GetChampionSelectSample("ChampionSearch.png");
			public static Bitmap FirstSelectBan => GetChampionSelectSample("FirstSelectBan.png");
		}

		public static IEnumerable<(string Name, Bitmap Sample)> Champions
			=> Assembly
				.GetExecutingAssembly()
				.GetManifestResourceNames()
				.Select(x => Regex.Match(x,
					$@"(?si){ChampionSamplesFolderPath.Replace(".", @"\.")}(?<fullname>(?<name>\w+)_Square_0\.png)"))
				.Where(m => m.Success)
				.Select(m => (m.Groups["name"].Value, LoadSample(ChampionSamplesFolderPath + m.Groups["fullname"].Value)));

		public static IEnumerable<(string Name, Bitmap Sample)> SummonerSpells
			=> Assembly
				.GetExecutingAssembly()
				.GetManifestResourceNames()
				.Select(x => Regex.Match(x,
					$@"(?si){SummonerSpellsSamplesFolderPath.Replace(".", @"\.")}(?<fullname>.*?Spell_Summoner(?<name>\w+)\.png)"))
				.Where(m => m.Success)
				.Select(m => (m.Groups["name"].Value, LoadSample(SummonerSpellsSamplesFolderPath + m.Groups["fullname"].Value)));

		private static Bitmap GetSample(string name)
			=> LoadSample(SamplesFolderPath + name);

		private static Bitmap LoadSample(string resName)
		{
			Bitmap Load()
			{
				var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
				if (stream == null)
					throw new Exception($"Resource {resName} not found");
				return new Bitmap(stream);
			}
			return UseCache ? SamplesCache.GetOrAdd(resName, key => Load()) : Load();
		}

		private static readonly WeakCache<string, Bitmap> SamplesCache
			= new WeakCache<string, Bitmap>();

		public static bool UseCache { get; set; }
	}
}