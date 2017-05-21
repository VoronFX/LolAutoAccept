using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace LolAutoAccept
{
	public partial class Patterns
	{
		private class ChampionSelectPatterns
		{
			public Rectangle[] BanRects { get; }
			public Rectangle[] BanStubRects { get; }
			public Rectangle[] SummonerNameRects { get; }
			public Rectangle[] AllieSummonerPickRects { get; }
			public Rectangle[] EnemySummonerPickRects { get; }

			public Rectangle ChampionSearchRect { get; }
			public Rectangle FirstSelectRect { get; }
			public Rectangle FirstSelectChampionRect { get; }

			public Lazy<Pattern> ScreenSample { get; }
			public Lazy<Pattern> BanButtonSample { get; }
			public Lazy<Pattern> BanButtonHoverSample { get; }
			public Lazy<Pattern> BanLockButtonDisabledSample { get; }
			public Lazy<Pattern> LockButtonSample { get; }
			public Lazy<Pattern> LockButtonHoverSample { get; }

			public Lazy<Pattern> ChampionSearchSample { get; }
			public Lazy<Pattern> FirstSelectBanSample { get; }

			public Lazy<Pattern> BanStubSample { get; }
			public Lazy<Pattern> PickStubSample { get; }

			public Lazy<Pattern> DetectPickPositionSample { get; }

			public ChampionSelectPatterns(Size resolution)
			{
				Lazy<Pattern> PrepareSample(Bitmap sample)
					=> new Lazy<Pattern>(() =>
						new DifferencePattern(sample
							.Scaled(resolution, InterpolationMode.NearestNeighbor)), false);

				ScreenSample = PrepareSample(Samples.ChampionSelect.Screen);
				BanButtonSample = PrepareSample(Samples.ChampionSelect.BanButton);
				BanButtonHoverSample = PrepareSample(Samples.ChampionSelect.BanButtonHover);
				BanLockButtonDisabledSample = PrepareSample(Samples.ChampionSelect.BanLockButtonDisabled);
				LockButtonSample = PrepareSample(Samples.ChampionSelect.LockButton);
				LockButtonHoverSample = PrepareSample(Samples.ChampionSelect.LockButtonHover);

				Rectangle Scale(Rectangle rectangle)
					=> resolution == NativeResolution
						? rectangle
						: rectangle.Scale(resolution.Width / (double)NativeResolution.Width,
							resolution.Height / (double)NativeResolution.Height);

				ChampionSearchRect = Scale(new Rectangle(744, 58, 20, 28));
				FirstSelectRect = Scale(new Rectangle(343, 109, 69, 69));
				FirstSelectChampionRect = Scale(new Rectangle(343 + 16, 109 + 16, 36, 36));

				BanRects = Enumerable.Range(0, 6)
					.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 12, 30, 28))).ToArray();
				BanStubRects = Enumerable.Range(0, 6)
					.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30))).ToArray();
				SummonerNameRects = Enumerable.Range(0, 4)
					.Select(i => Scale(new Rectangle(125, 115 + i * 80, 6, 22))).ToArray();
				AllieSummonerPickRects = Enumerable.Range(0, 4)
					.Select(i => Scale(new Rectangle(45, 104 + i * 80, 62, 62))).ToArray();
				EnemySummonerPickRects = Enumerable.Range(0, 4)
					.Select(i => Scale(new Rectangle(1172, 104 + i * 80, 62, 62))).ToArray();

				BanStubSample = new Lazy<Pattern>(() =>
					new ContrastMaskPattern(Samples.ChampionSelect.BanStub
							.Scaled(BanStubRects.First().Size, InterpolationMode.NearestNeighbor),
						15, 10, 0.1f), false);

				PickStubSample = new Lazy<Pattern>(() =>
					new ContrastMaskPattern(Samples.ChampionSelect.PickStub
							.Scaled(BanRects.First().Size, InterpolationMode.NearestNeighbor),
						70, 45, 0.3f), false);

				DetectPickPositionSample = new Lazy<Pattern>(() =>
					new HueSaturationPattern(SummonerNameRects.First().Size, 45f, 5f), false);

				ChampionSearchSample = new Lazy<Pattern>(() =>
					new ContrastMaskPattern(Samples.ChampionSelect.ChampionSearch
							.Scaled(ChampionSearchRect.Size, InterpolationMode.NearestNeighbor),
						45, 20, 0.1f), false);
			}
		}

	}
}