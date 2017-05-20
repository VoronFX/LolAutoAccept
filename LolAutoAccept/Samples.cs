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

		public static Bitmap AcceptMatchButton => GetSample("AcceptMatchButton.png");
		public static Bitmap AcceptMatchButtonHover => GetSample("AcceptMatchButtonHover.png");
		public static Bitmap ChampionSelect => GetSample("ChampionSelect.png");
		public static Bitmap ChampionSelectBanButton => GetSample("ChampionSelectBanButton.png");
		public static Bitmap ChampionSelectBanButtonHover => GetSample("ChampionSelectBanButtonHover.png");
		public static Bitmap ChampionSelectBanLockButtonDisabled => GetSample("ChampionSelectBanLockButtonDisabled.png");
		public static Bitmap ChampionSelectLockButton => GetSample("ChampionSelectLockButton.png");
		public static Bitmap ChampionSelectLockButtonHover => GetSample("ChampionSelectLockButtonHover.png");
		public static Bitmap ChampionSelectBanStub => GetSample("ChampionSelectBanStub.png");
		public static Bitmap ChampionSelectPickStub => throw new NotImplementedException();

		public static IEnumerable<(string Name, Bitmap Sample)> Champions
			=> Assembly
				.GetExecutingAssembly()
				.GetManifestResourceNames()
				.Select(x => Regex.Match(x,
					$@"(?si){ChampionSamplesFolderPath.Replace(".", @"\.")}(?<fullname>(?<name>\w+)_Square_0\.png)"))
				.Where(m => m.Success)
				.Select(m => (m.Groups["name"].Value, GetChampionSample(m.Groups["fullname"].Value)));

		private static Bitmap GetSample(string name)
			=> LoadSample(SamplesFolderPath + name);

		private static Bitmap GetChampionSample(string name)
			=> LoadSample(ChampionSamplesFolderPath + name);

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