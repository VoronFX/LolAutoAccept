using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ImageEdgeDetection;
using XnaFan.ImageComparison;

namespace LolAutoAccept
{
	public class Patterns
	{
		public static Size[] SupportedResolutions { get; }
			= { new Size(1024, 576), new Size(1280, 720), new Size(1600, 900) };

		public static Size NativeResolution { get; }
			= new Size(1280, 720);

		public class MatchPoint
		{
			public readonly Color Color;
			public readonly int X;
			public readonly int Y;

			public MatchPoint(Color color, int x, int y)
			{
				this.Color = color;
				this.X = x;
				this.Y = y;
			}
		}

		public class BanPickSample
		{
			public readonly string Name;
			public readonly MatchPoint[] BanSample;
			public readonly MatchPoint[] PickSample;

			public BanPickSample(string name, MatchPoint[] banSample, MatchPoint[] pickSample)
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

		public Lazy<MatchPoint[]> AcceptMatchButtonSample { get; }
		public Lazy<MatchPoint[]> AcceptMatchButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanButtonHoverSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanLockButtonDisabledSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectLockButtonHoverSample { get; }

		private BanPickSample[] ChampionSamples { get; }

		public Lazy<MatchPoint[]> ChampionSelectBanStubSample { get; }
		public Lazy<MatchPoint[]> ChampionSelectBanStub2Sample { get; }
		public Lazy<MatchPoint[]> ChampionSelectPickStubSample { get; }

		public Size Resolution { get; }
		public InterpolationMode InterpolationMode { get; }

		public Patterns(Size resolution, InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor)
		{
			if (!SupportedResolutions.Contains(resolution))
				throw new ArgumentException($"Resolution {resolution} is not supported");

			Resolution = resolution;
			InterpolationMode = interpolationMode;

			AcceptMatchButtonSample = PrepareSample("AcceptMatchButton.png");
			AcceptMatchButtonHoverSample = PrepareSample("AcceptMatchButtonHover.png");
			ChampionSelectSample = PrepareSample("ChampionSelect.png");
			ChampionSelectBanButtonSample = PrepareSample("ChampionSelectBanButton.png");
			ChampionSelectBanButtonHoverSample = PrepareSample("ChampionSelectBanButtonHover.png");
			ChampionSelectBanLockButtonDisabledSample = PrepareSample("ChampionSelectBanLockButtonDisabled.png");
			ChampionSelectLockButtonSample = PrepareSample("ChampionSelectLockButton.png");
			ChampionSelectLockButtonHoverSample = PrepareSample("ChampionSelectLockButtonHover.png");

			BanRects = Enumerable.Range(0, 6)
				.Select(i => Scale(new Rectangle(200 + 38 * i + (i < 3 ? 0 : 661), 10, 30, 30))).ToArray();
			SummonerNameRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(128, 126 + i * 80, 6, 22))).ToArray();
			AllieSummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(45, 104 + i * 80, 62, 62))).ToArray();
			EnemySummonerPickRects = Enumerable.Range(0, 4)
				.Select(i => Scale(new Rectangle(1172, 104 + i * 80, 62, 62))).ToArray();

			var championSamplesNamespace = string.Join(@"\.", nameof(LolAutoAccept), "Samples", "Champions");

			ChampionSamples = Assembly.GetExecutingAssembly().GetManifestResourceNames()
				.Select(x => Regex.Match(x, $@"(?si){championSamplesNamespace}\.(?<fullname>(?<name>\w+)_Square_0\.png)"))
				.Where(m => m.Success)
				.Select(m =>
				{
					var bitmap = GetSample("Champions." + m.Groups["fullname"].Value);
					var banBitmap = Scale(bitmap, BanRects.First().Size);
					var pickBitmap = Scale(bitmap, AllieSummonerPickRects.First().Size);
					return new BanPickSample(m.Groups["name"].Value.ToLowerInvariant(),
						ExtractMatchPoints(banBitmap), ExtractMatchPoints(pickBitmap));
				}).ToArray();

			ChampionSelectBanStubSample =
				PrepareSample("ChampionSelectBanStub3.png", BanRects.First().Size);
			ChampionSelectBanStub2Sample =
				PrepareSample("ChampionSelectBanStub3.png", BanRects.First().Size);
			ChampionSelectPickStubSample =
				PrepareSample("ChampionSelectBanStub.png", AllieSummonerPickRects.First().Size);
		}

		private Lazy<MatchPoint[]> PrepareSample(string name)
			=> PrepareSample(name, Resolution);

		private Lazy<MatchPoint[]> PrepareSample(string name, Size scale)
		{
			var bitmap = GetSample(name);//.Laplacian5x5Filter(false);
			return new Lazy<MatchPoint[]>(() =>
			{
				using (bitmap)
				using (var scaledBitmap = Scale(bitmap, scale))
					return ExtractMatchPoints(scaledBitmap);
			}, false);
		}

		private static Bitmap GetSample(string name)
		{
			var resName = string.Join(".", nameof(LolAutoAccept), "Samples", name);
			var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
			if (stream == null)
				throw new Exception($"Resource {resName} not found");
			return new Bitmap(stream);
		}

		private MatchPoint[] ExtractMatchPoints(Bitmap bitmap)
		{
			var checkPoints = new List<MatchPoint>();
			using (var lockBitmap = new LockBitmap.LockBitmap(bitmap))
			{
				for (int x = 0; x < lockBitmap.Width; x++)
					for (int y = 0; y < lockBitmap.Height; y++)
					{
						var color = lockBitmap.GetPixel(x, y);
						if (color.A == 0xff)
						{
							checkPoints.Add(new MatchPoint(color, x, y));
						}
					}
				return checkPoints.ToArray();
			}
		}

		private Rectangle Scale(Rectangle rectangle)
		{
			if (Resolution == NativeResolution)
				return rectangle;

			var xK = Resolution.Width / (double)NativeResolution.Width;
			var yK = Resolution.Height / (double)NativeResolution.Height;
			return new Rectangle((int)(rectangle.X * xK), (int)(rectangle.Y * yK),
				(int)(rectangle.Width * xK), (int)(rectangle.Height * yK));
		}

		private Bitmap Scale(Bitmap source, Size targetSize)
			=> source.Scale(targetSize, InterpolationMode);

		private const double BaseTreshold = 0.06835;
		private const double BanTreshold = 0.13;
		private const double BanStubTreshold = 0.05;

		public bool IsAcceptMatchButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, AcceptMatchButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, AcceptMatchButtonHoverSample.Value, BaseTreshold);

		public bool IsChampionSelect(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectSample.Value, BaseTreshold);

		public bool HasBanLockButtonDisabled(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanLockButtonDisabledSample.Value, BaseTreshold);

		public bool IsBanButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectBanButtonHoverSample.Value, BaseTreshold);

		public bool IsLockButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectLockButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectLockButtonHoverSample.Value, BaseTreshold);

		private static bool IsMatch(LockBitmap.LockBitmap screenshot,
			MatchPoint[] sample, double threshold, Point offset = default(Point))
		{
			int diff = 0;
			int diffTreshold = (int)(sample.Length * 255 * 3 * threshold);
			//int colordiff = 0;
			//int colorTreshold = (int)(sample.Length * 255 * threshold);
			foreach (var pixel in sample)
			{
				var targetPixel = screenshot.GetPixel(pixel.X + offset.X, pixel.Y + offset.Y);

				diff += Math.Abs(targetPixel.R - pixel.Color.R)
						+ Math.Abs(targetPixel.G - pixel.Color.G)
						+ Math.Abs(targetPixel.B - pixel.Color.B);

				//colordiff += Math.Max(Math.Max(
				//		Math.Abs(targetPixel.R - pixel.Color.R), 
				//		Math.Abs(targetPixel.G - pixel.Color.G)),
				//		Math.Abs(targetPixel.B - pixel.Color.B));
				//#if !DEBUG
				//				if (diff > diffTreshold)
				//					return false;
				//#endif
			}
			Console.WriteLine($"diff:{(double)diff / (sample.Length * 255 * 3):P} threshold: {threshold}");
			//Console.WriteLine($"colordiff:{(double)colordiff / (sample.Length * 255)} threshold: {threshold} colorTreshold: {colorTreshold} colordiff: {colordiff}");
			if (diff > diffTreshold)
				return false;
			return true;
		}

		public enum BanPickType
		{
			Unknown,
			Stub,
			Champion,
		}

		public enum CompareAlgorithm
		{
			Plain,
			ColorPriority,
			JacobKrarup,
			ColorTons,
			StubMatch,
			StubMatch2
		}
		public static int StubMatchAvg = 30;
		public static int StubWhiteGrayLow = 20;
		public static int StubWhiteGrayHigh = 50;
		public static int StubBlackGrayHigh = 30;

		public static double IsMatchTest(LockBitmap.LockBitmap screenshot,
			MatchPoint[] sample, CompareAlgorithm algorithm, Point offset = default(Point))
		{
			int diff = 0;
			int colordiff = 0;
			int[] ggg = new int[10 * 10 * 10];
			int[] gggS = new int[10 * 10 * 10];
			int stubmatch = 0;

			if (algorithm != CompareAlgorithm.JacobKrarup)
				foreach (var pixel in sample)
				{
					var targetPixel = screenshot.GetPixel(pixel.X + offset.X, pixel.Y + offset.Y);

					switch (algorithm)
					{
						case CompareAlgorithm.Plain:
							diff += Math.Abs(targetPixel.R - pixel.Color.R)
									+ Math.Abs(targetPixel.G - pixel.Color.G)
									+ Math.Abs(targetPixel.B - pixel.Color.B);
							break;
						case CompareAlgorithm.ColorPriority:
							colordiff += Math.Max(Math.Max(
									Math.Abs(targetPixel.R - pixel.Color.R),
									Math.Abs(targetPixel.G - pixel.Color.G)),
								Math.Abs(targetPixel.B - pixel.Color.B));
							break;
						case CompareAlgorithm.JacobKrarup:
							break;
						case CompareAlgorithm.ColorTons:
							gggS[(pixel.Color.R / 26) + (pixel.Color.G / 26) * 10 + (pixel.Color.B / 26) * 10 * 10]++;
							ggg[(targetPixel.R / 26) + (targetPixel.G / 26) * 10 + (targetPixel.B / 26) * 10 * 10]++;
							break;
						case CompareAlgorithm.StubMatch:
							var targetGray = (targetPixel.R + targetPixel.G + targetPixel.B) / 3;
							if (Math.Abs(targetPixel.R - targetGray) < StubMatchAvg
								&& Math.Abs(targetPixel.G - targetGray) < StubMatchAvg
								&& Math.Abs(targetPixel.B - targetGray) < StubMatchAvg)
								stubmatch++;
							if ((pixel.Color.R + pixel.Color.G + pixel.Color.B) / 3 > 255 / 2
								&& targetGray > StubWhiteGrayLow && targetGray < StubWhiteGrayHigh)
								stubmatch++;
							else if (targetGray < StubBlackGrayHigh)
								stubmatch++;
							break;
						case CompareAlgorithm.StubMatch2:
							break;

						//var targetGray = (targetPixel.R + targetPixel.G + targetPixel.B) / 3;
						//if (Math.Abs(targetPixel.R - targetGray) < StubMatchAvg
						//    && Math.Abs(targetPixel.G - targetGray) < StubMatchAvg
						//    && Math.Abs(targetPixel.B - targetGray) < StubMatchAvg)
						//	stubmatch++;
						//if ((pixel.Color.R + pixel.Color.G + pixel.Color.B) / 3 > 255 / 2
						//    && targetGray > StubWhiteGrayLow && targetGray < StubWhiteGrayHigh)
						//	stubmatch++;
						//else if (targetGray < StubBlackGrayHigh)
						//	stubmatch++;
						//break;
						default:
							throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
					}
				}


			switch (algorithm)
			{
				case CompareAlgorithm.Plain:
					return (double)diff / (sample.Length * 255 * 3);
				case CompareAlgorithm.ColorPriority:
					return (double)colordiff / (sample.Length * 255);
				case CompareAlgorithm.JacobKrarup:
					var b1 = sample.RecreateBitmap();
					var b2 = screenshot.RecreateBitmap();
					var b3 = new Bitmap(b1.Width, b1.Height);
					using (var g = Graphics.FromImage(b3))
					{
						g.DrawImage(b2, 0, 0, new Rectangle(offset, b1.Size), GraphicsUnit.Pixel);
					}
					return 1 - b3.PercentageDifference(b1, 0);
				case CompareAlgorithm.ColorTons:
					double gggdiff = ggg.Select((t, i) => Math.Abs(t - gggS[i])).Sum();
					return gggdiff / sample.Length / 2;
				case CompareAlgorithm.StubMatch:
					return 1 - (double)stubmatch / sample.Length / 2;
				case CompareAlgorithm.StubMatch2:
					const int whiteAvgEdge = 70;
					const int blackAvgEdge = 45;

					var black = sample.Where(x => 
					x.Color == Color.FromArgb(0,0,0)).ToArray();
					var blackAvg = black.Aggregate(new int[3], (color, pixel) =>
					{
						var targetPixel = screenshot.GetPixel(pixel.X + offset.X, pixel.Y + offset.Y);
						color[0] += targetPixel.R;
						color[1] += targetPixel.G;
						color[2] += targetPixel.B;
						return color;
					}, color => Color.FromArgb(color[0] / black.Length, color[1] / black.Length, color[2] / black.Length));

					var white = sample.Where(x => x.Color == Color.FromArgb(255,255,255)).ToArray();
					var whiteAvg = white.Aggregate(new int[3], (color, pixel) =>
					{
						var targetPixel = screenshot.GetPixel(pixel.X + offset.X, pixel.Y + offset.Y);
						color[0] += targetPixel.R;
						color[1] += targetPixel.G;
						color[2] += targetPixel.B;
						return color;
					}, color => Color.FromArgb(color[0] / white.Length, color[1] / white.Length, color[2] / white.Length));

					var match = (black.Count(x => Math.Abs(x.Color.R - blackAvg.R) < blackAvgEdge
											  && Math.Abs(x.Color.G - blackAvg.G) < blackAvgEdge
											  && Math.Abs(x.Color.B - blackAvg.B) < blackAvgEdge)
						  + white.Count(x => Math.Abs(x.Color.R - whiteAvg.R) < whiteAvgEdge
											  && Math.Abs(x.Color.G - whiteAvg.G) < whiteAvgEdge
											  && Math.Abs(x.Color.B - whiteAvg.B) < whiteAvgEdge)) / (double)sample.Length;

					return 1 - match * Math.Min(1, Math.Max(0, ((whiteAvg.R + whiteAvg.G + whiteAvg.B) / 3
									- (blackAvg.R + blackAvg.G + blackAvg.B) / 3) / 30d));
				default:
					throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null);
			}
		}

		public int DetectOurPickPosition(LockBitmap.LockBitmap screenshot) =>
			SummonerNameRects.Select(rectangle =>
				{
					int match = 0;
					for (int x = rectangle.Left; x < rectangle.Right; x++)
						for (int y = rectangle.Top; y < rectangle.Bottom; y++)
						{
							var pixel = screenshot.GetPixel(x, y);
							if (pixel.R < 150 || pixel.G < 100 || pixel.B > 50)
								continue;

							match += pixel.R + pixel.G - pixel.B;
						}
					return match;
				})
				.Select((x, i) => (x, i))
				.Aggregate((int.MinValue, 0), (seed, x) => x.Item1 > seed.Item1 ? x : seed).Item2;


		public (BanPickType type, string champion) DetermineBan(LockBitmap.LockBitmap screenshot, int position)
		{
			if (IsMatch(screenshot, ChampionSelectBanStubSample.Value, BanStubTreshold, BanRects[position].Location))
				return (BanPickType.Stub, null);

			var match = ChampionSamples.FirstOrDefault(s =>
				IsMatch(screenshot, s.BanSample, BanTreshold, BanRects[position].Location));

			return match == null ? (BanPickType.Unknown, null) : (BanPickType.Champion, match.Name.ToLowerInvariant());
		}

		public (BanPickType type, string champion) DetermineBanTest(LockBitmap.LockBitmap screenshot, int position)
		{
			if (//IsMatch(screenshot, ChampionSelectBanStubSample.Value, BanStubTreshold, BanRects[position].Location)|| 
				IsMatch(screenshot, ChampionSelectBanStub2Sample.Value, BanStubTreshold, BanRects[position].Location))
				return (BanPickType.Stub, null);

			var matches = ChampionSamples.Where(s =>
				IsMatch(screenshot, s.BanSample, BanTreshold, BanRects[position].Location)).ToArray();

			foreach (var banPickSample in matches)
			{
				IsMatch(screenshot, banPickSample.BanSample, BanTreshold, BanRects[position].Location);
			}
			if (matches.Count() > 1)
				throw new Exception(
					$"Multiple matches: {Environment.NewLine}{string.Join(Environment.NewLine, matches.Select(m => m.Name))}");

			return DetermineBan(screenshot, position);
		}

		public (BanPickType type, string champion, double percent)[] DetermineBanTest2(LockBitmap.LockBitmap screenshot,
			int position, CompareAlgorithm alg)
		{
			//if (IsMatch(screenshot, ChampionSelectBanStubSample.Value, BanStubTreshold, BanRects[position].Location))
			//	return (BanPickType.Stub, null);

			return ChampionSamples.Select(x => (BanPickType.Champion, x.Name, IsMatchTest(screenshot, x.BanSample,
					alg, BanRects[position].Location)))
				.Concat(new(BanPickType type, string champion, double percent)[]
				{
					(BanPickType.Stub, null, //Math.Min(
						//IsMatchTest(screenshot, ChampionSelectBanStubSample.Value, alg, BanRects[position].Location),
						IsMatchTest(screenshot, ChampionSelectBanStub2Sample.Value, alg, BanRects[position].Location)
					//)
					)
				})
				.ToArray();


			//var matches = ChampionSamples.Where(s =>
			//	IsMatch(screenshot, s.BanSample, BanTreshold, BanRects[position].Location)).ToArray();

			//foreach (var banPickSample in matches)
			//{
			//	IsMatch(screenshot, banPickSample.BanSample, BanTreshold, BanRects[position].Location);
			//}
			//if (matches.Count() > 1)
			//	throw new Exception($"Multiple matches: {Environment.NewLine}{string.Join(Environment.NewLine, matches.Select(m => m.Name))}");

			//return DetermineBan(screenshot, position);
		}

		public double BanStubTest(LockBitmap.LockBitmap screenshot,
			int position, CompareAlgorithm alg)
		{
			return IsMatchTest(screenshot, ChampionSelectBanStubSample.Value, alg, BanRects[position].Location);
		}
	}
}