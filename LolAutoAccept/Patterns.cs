using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using ImageEdgeDetection;
using XnaFan.ImageComparison;

namespace LolAutoAccept
{
	public class Patterns
	{
		public static Size[] SupportedResolutions { get; }
			= {new Size(1024, 576), new Size(1280, 720), new Size(1600, 900)};

		public static Size NativeResolution { get; }
			= new Size(1280, 720);

		public class BanPickSample
		{
			public readonly string Name;
			public readonly Pattern BanSample;
			public readonly Pattern PickSample;

			public BanPickSample(string name, Pattern banSample, Pattern pickSample)
			{
				Name = name;
				BanSample = banSample;
				PickSample = pickSample;
			}
		}

		private Rectangle[] BanRects { get; }
		private Rectangle[] SummonerNameRects { get; }
		private Rectangle[] AllieSummonerPickRects { get; }
		private Rectangle[] EnemySummonerPickRects { get; }

		private Lazy<Pattern> AcceptMatchButtonSample { get; }
		private Lazy<Pattern> AcceptMatchButtonHoverSample { get; }
		private Lazy<Pattern> ChampionSelectSample { get; }
		private Lazy<Pattern> ChampionSelectBanButtonSample { get; }
		private Lazy<Pattern> ChampionSelectBanButtonHoverSample { get; }
		private Lazy<Pattern> ChampionSelectBanLockButtonDisabledSample { get; }
		private Lazy<Pattern> ChampionSelectLockButtonSample { get; }
		private Lazy<Pattern> ChampionSelectLockButtonHoverSample { get; }

		private BanPickSample[] ChampionSamples { get; }

		private Lazy<Pattern> ChampionSelectBanStubSample { get; }
		private Lazy<Pattern> ChampionSelectPickStubSample { get; }

		private Lazy<Pattern> ChampionSelectDetectPickPositionSample { get; }

		public Size Resolution { get; }

		public Patterns(Size resolution)
		{
			if (!SupportedResolutions.Contains(resolution))
				throw new ArgumentException($"Resolution {resolution} is not supported");

			Resolution = resolution;

			Lazy<Pattern> PrepareSample(Bitmap sample)
				=> new Lazy<Pattern>(() =>
					new DifferencePattern(sample
						.Scaled(Resolution, InterpolationMode.NearestNeighbor)), false);

			AcceptMatchButtonSample = PrepareSample(Samples.AcceptMatchButton);
			AcceptMatchButtonHoverSample = PrepareSample(Samples.AcceptMatchButtonHover);
			ChampionSelectSample = PrepareSample(Samples.ChampionSelect);
			ChampionSelectBanButtonSample = PrepareSample(Samples.ChampionSelectBanButton);
			ChampionSelectBanButtonHoverSample = PrepareSample(Samples.ChampionSelectBanButtonHover);
			ChampionSelectBanLockButtonDisabledSample = PrepareSample(Samples.ChampionSelectBanLockButtonDisabled);
			ChampionSelectLockButtonSample = PrepareSample(Samples.ChampionSelectLockButton);
			ChampionSelectLockButtonHoverSample = PrepareSample(Samples.ChampionSelectLockButtonHover);

			Rectangle Scale(Rectangle rectangle)
				=> Resolution == NativeResolution
					? rectangle
					: rectangle.Scale(Resolution.Width / (double) NativeResolution.Width,
						Resolution.Height / (double) NativeResolution.Height);

			BanRects = Enumerable.Range(0, 6)
				.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30))).ToArray();
			SummonerNameRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(125, 115 + i * 80, 6, 22))).ToArray();
			AllieSummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(45, 104 + i * 80, 62, 62))).ToArray();
			EnemySummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(1172, 104 + i * 80, 62, 62))).ToArray();

			ChampionSamples = Samples.Champions
				.Select(cs => new BanPickSample(cs.Name,
					new DifferencePattern(
						cs.Sample.Scaled(BanRects.First().Size, InterpolationMode.NearestNeighbor)),
					new DifferencePattern(
						cs.Sample.Scaled(AllieSummonerPickRects.First().Size, InterpolationMode.NearestNeighbor))))
				.ToArray();

			ChampionSelectBanStubSample = new Lazy<Pattern>(() =>
				new ContrastMaskPattern(Samples.ChampionSelectBanStub
						.Scaled(BanRects.First().Size, InterpolationMode.NearestNeighbor), 
						15, 10, 0.1f), false);

			ChampionSelectPickStubSample = new Lazy<Pattern>(() =>
				new ContrastMaskPattern(Samples.ChampionSelectPickStub
						.Scaled(BanRects.First().Size, InterpolationMode.NearestNeighbor),
					70, 45, 0.3f), false);

			ChampionSelectDetectPickPositionSample = new Lazy<Pattern>(() =>
				new HueSaturationPattern(SummonerNameRects.First().Size, 45f, 5f), false);
		}

		private const double BaseTreshold = 0.93165;
		private const double BanTreshold = 0.13;
		private const double BanStubTreshold = 0.0774;

		public bool IsAcceptMatchButton(CachedBitmapPixels screenshot)
			=> AcceptMatchButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || AcceptMatchButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsChampionSelect(CachedBitmapPixels screenshot)
			=> ChampionSelectSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool HasBanLockButtonDisabled(CachedBitmapPixels screenshot)
			=> ChampionSelectBanLockButtonDisabledSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsBanButton(CachedBitmapPixels screenshot)
			=> ChampionSelectBanButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || ChampionSelectBanButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public bool IsLockButton(CachedBitmapPixels screenshot)
			=> ChampionSelectLockButtonSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold)
			   || ChampionSelectLockButtonHoverSample.Value.IsMatch(screenshot, Point.Empty, BaseTreshold);

		public int DetectOurPickPosition(CachedBitmapPixels screenshot) =>
			SummonerNameRects.Select(rectangle =>
					ChampionSelectDetectPickPositionSample.Value.Match(screenshot, rectangle.Location))
				.Select((x, i) => (x, i))
				.Aggregate((double.MinValue, 0), (seed, x) => x.Item1 > seed.Item1 ? x : seed).Item2;

		public bool IsBanStub(CachedBitmapPixels screenshot, int position)
			=> ChampionSelectBanStubSample.Value.IsMatch(screenshot, BanRects[position].Location, BanStubTreshold);

		public string DetectBanChampion(CachedBitmapPixels screenshot, int position)
			=> ChampionSamples.FirstOrDefault(x =>
				x.BanSample.IsMatch(screenshot, BanRects[position].Location, 0.5))?.Name;
	}
}