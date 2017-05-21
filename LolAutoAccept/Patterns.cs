using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using ImageEdgeDetection;
using XnaFan.ImageComparison;

namespace LolAutoAccept
{
	public partial class Patterns
	{
		public static Size[] SupportedResolutions { get; }
			= { new Size(1024, 576), new Size(1280, 720), new Size(1600, 900) };

		public static Size NativeResolution { get; }
			= new Size(1280, 720);

		public class BanPickSample
		{
			public readonly string Name;
			public readonly Pattern BanSample;
			public readonly Pattern PickSample;
			public readonly Pattern FirstSelectSample;

			public BanPickSample(string name, Pattern banSample, Pattern pickSample, Pattern firstSelectSample)
			{
				Name = name;
				BanSample = banSample;
				PickSample = pickSample;
				FirstSelectSample = firstSelectSample;
			}
		}

		private Lazy<Pattern> AcceptMatchButtonSample { get; }
		private Lazy<Pattern> AcceptMatchButtonHoverSample { get; }

		private ChampionSelectPatterns ChampionSelect { get; }

		private BanPickSample[] ChampionSamples { get; }

		public Size Resolution { get; }

		public Patterns(Size resolution)
		{
			if (!SupportedResolutions.Contains(resolution))
				throw new ArgumentException($"Resolution {resolution} is not supported");

			Resolution = resolution;
			ChampionSelect = new ChampionSelectPatterns(resolution);

			Lazy<Pattern> PrepareSample(Bitmap sample)
				=> new Lazy<Pattern>(() =>
					new DifferencePattern(sample
						.Scaled(Resolution, InterpolationMode.NearestNeighbor)), false);

			AcceptMatchButtonSample = PrepareSample(Samples.AcceptMatchButton);
			AcceptMatchButtonHoverSample = PrepareSample(Samples.AcceptMatchButtonHover);

			ChampionSamples = Samples.Champions
				.Select(cs => new BanPickSample(cs.Name.ToLowerInvariant(),
					new DifferencePattern(
						cs.Sample.Croped(3).Scaled(ChampionSelect.BanRects.First().Size, InterpolationMode.HighQualityBilinear)),
					new DifferencePattern(
						cs.Sample.Scaled(ChampionSelect.AllieSummonerPickRects.First().Size, InterpolationMode.NearestNeighbor)),
					new DifferencePattern(
						cs.Sample.Croped(30).Scaled(ChampionSelect.FirstSelectChampionRect.Size, InterpolationMode.Bilinear))))
				.ToArray();
		}

		private const double BaseTreshold = 0.93165;
		private const double BanStubTreshold = 0.0774;
		private const double BanTreshold = 0.89825;
		private const double ChampionSearchTreshold = 0.52195;
		private const double FirstSelectBanTreshold = 0.14855;
		private const double FirstSelectChampionTreshold = 0.89525;

		public bool IsAcceptMatchButton(CachedBitmapPixels screenshot)
			=> AcceptMatchButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || AcceptMatchButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsChampionSelect(CachedBitmapPixels screenshot)
			=> ChampionSelect.ScreenSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool HasBanLockButtonDisabled(CachedBitmapPixels screenshot)
			=> ChampionSelect.BanLockButtonDisabledSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsBanButton(CachedBitmapPixels screenshot)
			=> ChampionSelect.BanButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || ChampionSelect.BanButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsLockButton(CachedBitmapPixels screenshot)
			=> ChampionSelect.LockButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || ChampionSelect.LockButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public int DetectOurPickPosition(CachedBitmapPixels screenshot) =>
			ChampionSelect.SummonerNameRects.Select(rectangle =>
					ChampionSelect.DetectPickPositionSample.Value.Match(screenshot, rectangle.Location))
				.Select((x, i) => (x, i))
				.Aggregate((double.MinValue, 0), (seed, x) => x.Item1 > seed.Item1 ? x : seed).Item2;

		public bool IsBanStub(CachedBitmapPixels screenshot, int position)
			=> ChampionSelect.BanStubSample.Value.IsMatch(screenshot, ChampionSelect.BanStubRects[position].Location, BanStubTreshold);

		public string DetectBanChampion(CachedBitmapPixels screenshot, int position)
			=> ChampionSamples.FirstOrDefault(x =>
				x.BanSample.IsMatch(screenshot, ChampionSelect.BanRects[position].Location, BanTreshold))?.Name;

		public bool IsChampionSearch(CachedBitmapPixels screenshot)
			=> ChampionSelect.ChampionSearchSample.Value.IsMatch(screenshot, ChampionSelect.ChampionSearchRect.Location, ChampionSearchTreshold);

		public bool IsFirstSelectBan(CachedBitmapPixels screenshot)
			=> ChampionSelect.FirstSelectBanSample.Value.IsMatch(screenshot, ChampionSelect.FirstSelectRect.Location, FirstSelectBanTreshold);

		public string DetectFirstSelectChampion(CachedBitmapPixels screenshot)
			=> ChampionSamples.FirstOrDefault(x =>
				x.FirstSelectSample.IsMatch(screenshot, ChampionSelect.FirstSelectChampionRect.Location, FirstSelectChampionTreshold))?.Name;

	}
}