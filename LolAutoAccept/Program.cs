using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LolAutoAccept
{
	public sealed partial class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			Program pg = new Program();
			Application.Run();
		}

		private PatterCaptureForm patternCaptureForm;
		private AutoPickForm autoPickForm;

		private readonly NotifyIcon notifyIcon;
		private readonly ProgramContextMenu contextMenu;

		private Program()
		{
			//autoAcceptPatterns = new[]
			//{
			//	SequenceFromSample(Resources.oldClientSample),
			//	SequenceFromSample(Resources.newClientSample),
			//};
			//autoPickPatterns = new[]
			//{
			//	SequenceFromSample(Resources.oldClientSamplePick),
			//	SequenceFromSample(Resources.oldClientSamplePick2),
			//	SequenceFromSample(Resources.oldClientSamplePickOld)
			//};
			//var region = new Rect() { Left = 1000, Right = 0, Top = 1000, Bottom = 0 };
			//foreach (var point in autoPickFastPatterns.Concat(autoAcceptFastPatterns).SelectMany(p => p.MatchPoints))
			//{
			//	region.Left = Math.Min(region.Left, point.Item1.X);
			//	region.Right = Math.Max(region.Right, point.Item1.X);
			//	region.Top = Math.Min(region.Top, point.Item1.Y);
			//	region.Bottom = Math.Max(region.Bottom, point.Item1.Y);
			//}
			//captureRegion = new Rectangle(region.Left, region.Top, region.Right - region.Left, region.Bottom - region.Top);

			contextMenu = new ProgramContextMenu(this);

			notifyIcon = new NotifyIcon
			{
				Icon = SystemIcons.Application,
				ContextMenu = contextMenu,
				Text = nameof(LolAutoAccept),
				BalloonTipTitle = nameof(LolAutoAccept),
				Visible = true,
			};

			Task.Run(AutoWorker);

			// Don't inline cause it will be grabage collected and exception will be thrown
			windowChangeEvents = WinEventProc;

			SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, windowChangeEvents, 0, 0,
				WINEVENT_OUTOFCONTEXT);
			SetWinEventHook(EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, windowChangeEvents, 0, 0,
				WINEVENT_OUTOFCONTEXT);

			//bmpScreenshot = new Bitmap(200, 200, PixelFormat.Format24bppRgb);
			//using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
			//{
			//	// Take the screenshot from the upper left corner to the right bottom corner.
			//	//gfxScreenshot.CopyFromScreen(
			//	//	rect.Left + captureRegion.Left, rect.Top + captureRegion.Top,
			//	//	captureRegion.Left, captureRegion.Top,
			//	//	captureRegion.Size,// new Size(bmpScreenshot.Width, bmpScreenshot.Height),
			//	//	CopyPixelOperation.SourceCopy);
			//	gfxScreenshot.CopyFromScreen(0, 0, 0, 0,
			//		new Size(bmpScreenshot.Width, bmpScreenshot.Height),
			//		CopyPixelOperation.SourceCopy);
			//}
			//bmpScreenshot.Save("test.bmp");
			//var x = new LockBitmap.LockBitmap(bmpScreenshot);
			while (true)
			{
				var lolLuncherProcesses = Process.GetProcessesByName("LeagueClientUx")
					.Where(p => p.MainWindowHandle != IntPtr.Zero);


				var tempRect = new Rect();
				GetWindowRect(lolLuncherProcesses.First().MainWindowHandle, ref tempRect);

				var width = tempRect.Right - tempRect.Left;
				var height = tempRect.Bottom - tempRect.Top;

				if (width > 0 && height > 0)
				{
					if (bmpScreenshot == null || bmpScreenshot.Width != width || bmpScreenshot.Height != height)
					{
						//Create a new bitmap.
						bmpScreenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb);
					}

					// Create a graphics object from the bitmap.
					using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
					{
						// Take the screenshot from the upper left corner to the right bottom corner.
						gfxScreenshot.CopyFromScreen(tempRect.Left, tempRect.Top, 0, 0,
							new Size(bmpScreenshot.Width, bmpScreenshot.Height),
							CopyPixelOperation.SourceCopy);
					}


					//bmpScreenshot.Save("test.bmp");
					var p = new Patterns(bmpScreenshot.Width, bmpScreenshot.Height);
					var l = new LockBitmap.LockBitmap(bmpScreenshot);
					l.UnlockBits();
					Console.WriteLine("res");
					p.IsChampionSelect(l);
					p.HasBanLockButtonDisabled(l);
					p.IsBanButton(l);
					p.IsLockButton(l);

				}
			}
		}

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly WinEventDelegate windowChangeEvents;

		//private readonly ManagementEventWatcher startWatcher = new ManagementEventWatcher(new WqlEventQuery
		//{
		//	EventClassName = "Win32_ProcessStartTrace",
		//	Condition = "ProcessName='LoLClient.exe'"
		//});


		private static byte[] SequenceFromSample(Bitmap bitmap)
		{
			var sampleBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
			using (var g = Graphics.FromImage(sampleBitmap))
			{
				g.DrawImageUnscaled(bitmap, Point.Empty);
			}
			using (var lockBitmap = new LockBitmap.LockBitmap(sampleBitmap))
			{
				var cCount = lockBitmap.Depth / 8;

				// Get start index of the specified pixel
				var i = ((lockBitmap.Height / 2 * lockBitmap.Width) + 0) * cCount;

				byte[] pattern = new byte[lockBitmap.Width * cCount];
				for (int j = 0; j < pattern.Length; j++)
				{
					pattern[j] = lockBitmap.Pixels[j + i];
				}
				return pattern;
			}
		}

		//private readonly byte[][] autoPickPatterns;
		//private readonly byte[][] autoAcceptPatterns;

		class FastPattern
		{
			public readonly Point ClickPoint;
			public readonly (Point, Color)[] MatchPoints;
			public readonly SizeF PatternSize;

			public FastPattern(Point clickPoint, SizeF patternSize, (Point, Color)[] matchPoints)
			{
				this.ClickPoint = clickPoint;
				this.MatchPoints = matchPoints;
				PatternSize = patternSize;
			}
		}

		//This is a replacement for Cursor.Position in WinForms
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		static extern bool SetCursorPos(int x, int y);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

		public const int MOUSEEVENTF_LEFTDOWN = 0x02;
		public const int MOUSEEVENTF_LEFTUP = 0x04;

		//This simulates a left mouse click
		public static void LeftMouseClick(int xpos, int ypos)
		{
			SetCursorPos(xpos, ypos);
			mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
			mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		delegate void WinEventDelegate(
			IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread,
			uint dwmsEventTime);

		[DllImport("user32.dll")]
		static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
			WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		private const uint WINEVENT_OUTOFCONTEXT = 0;
		private const uint EVENT_SYSTEM_FOREGROUND = 3;
		private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
		private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

		TaskCompletionSource<bool> awaiter = new TaskCompletionSource<bool>();

		public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
			uint dwEventThread, uint dwmsEventTime)
		{
			Task.Run(() => awaiter.TrySetResult(true));
		}

		private static string GetActiveWindowsProcessname()
		{
			try
			{
				IntPtr handle = GetForegroundWindow();

				uint processId;
				return GetWindowThreadProcessId(handle, out processId) > 0
					? Process.GetProcessById((int) processId).MainModule.FileName
					: "";
			}
			catch (Exception exception)
			{
				return "";
			}
		}


		private Bitmap bmpScreenshot;

		private async Task AutoWorker()
		{
			while (true)
			{
				try
				{
					if (!contextMenu.AutoAccept.Checked &&
					    !contextMenu.AutoLock.Checked)
						continue;

					IntPtr foregroundWindow = GetForegroundWindow();

					var lolClientProcesses = Process.GetProcessesByName("League of Legends");
					if (lolClientProcesses.Any())
					{
						await Sleep(true);
						continue;
					}

					var lolLuncherProcesses = Process.GetProcessesByName("LeagueClientUx");
					if (!lolLuncherProcesses.Any() || lolLuncherProcesses.All(p => p.MainWindowHandle != foregroundWindow))
					{
						await Sleep(lolLuncherProcesses.Any());
						continue;
					}

					notifyIcon.Icon = SystemIcons.Shield;

					var tempRect = new Rect();
					GetWindowRect(foregroundWindow, ref tempRect);

					var width = tempRect.Right - tempRect.Left;
					var height = tempRect.Bottom - tempRect.Top;

					if (width > 0 && height > 0)
					{
						if (bmpScreenshot == null || bmpScreenshot.Width != width || bmpScreenshot.Height != height)
						{
							//Create a new bitmap.
							bmpScreenshot = new Bitmap(width, height, PixelFormat.Format32bppArgb);
						}

						// Create a graphics object from the bitmap.
						using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
						{
							// Take the screenshot from the upper left corner to the right bottom corner.
							gfxScreenshot.CopyFromScreen(tempRect.Left, tempRect.Top, 0, 0,
								new Size(bmpScreenshot.Width, bmpScreenshot.Height),
								CopyPixelOperation.SourceCopy);
						}

						patternCaptureForm?.AddBitmap(bmpScreenshot);
					}


					//if (tempRect.Right - tempRect.Left > 0 && tempRect.

					//var rect = new Rectangle(tempRect.Left, tempRect.Top, tempRect.Right - tempRect.Left,
					//	tempRect.Bottom - tempRect.Top);
					//if (rect.IsEmpty)
					//{
					//	continue;
					//}
					//if (bmpScreenshot == null || bmpScreenshot.Width != rect.Width || bmpScreenshot.Height != rect.Height)
					//{
					//	//Create a new bitmap.
					//	bmpScreenshot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
					//}

					//// Create a graphics object from the bitmap.
					//using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
					//{
					//	// Take the screenshot from the upper left corner to the right bottom corner.
					//	//gfxScreenshot.CopyFromScreen(
					//	//	rect.Left + captureRegion.Left, rect.Top + captureRegion.Top,
					//	//	captureRegion.Left, captureRegion.Top,
					//	//	captureRegion.Size,// new Size(bmpScreenshot.Width, bmpScreenshot.Height),
					//	//	CopyPixelOperation.SourceCopy);
					//	gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0,
					//		new Size(bmpScreenshot.Width, bmpScreenshot.Height),
					//		CopyPixelOperation.SourceCopy);
					//}

					//patternCaptureForm?.AddBitmap(bmpScreenshot);
					////int i = 0;
					////while (File.Exists($"lolautopicker{i}.png")) i++;
					////	bmpScreenshot.Save($"lolautopicker{i}.png");

					//notifyIcon.BalloonTipIcon = ToolTipIcon.Info;

					//if (contextMenu.AutoAccept.Checked
					//	&& FindAndClick(bmpScreenshot, rect.Location, autoAcceptFastPatterns))
					//{
					//	notifyIcon.BalloonTipText = "Auto accepted!";
					//	notifyIcon.ShowBalloonTip(1000);
					//	await Task.Delay(1000);
					//}
					//else if (contextMenu.AutoPick.Checked
					//		 && FindAndClick(bmpScreenshot, rect.Location, autoPickFastPatterns))
					//{
					//	notifyIcon.BalloonTipText = "Auto picked!";
					//	notifyIcon.ShowBalloonTip(1000);
					//	await Task.Delay(1000);
					//}

					//using (var bitmap = new LockBitmap.LockBitmap(bmpScreenshot))
					//{

					//	if (contextMenu.MenuItems[0].Checked
					//		&& FindAndClick(bitmap, rect.Location, autoAcceptPatterns))
					//	{
					//		notifyIcon.BalloonTipText = "Auto accepted!";
					//		notifyIcon.ShowBalloonTip(1000);
					//		await Task.Delay(1000);
					//	}
					//	else if (contextMenu.MenuItems[1].Checked
					//		&& FindAndClick(bitmap, rect.Location, autoPickPatterns))
					//	{
					//		notifyIcon.BalloonTipText = "Auto picked!";
					//		notifyIcon.ShowBalloonTip(1000);
					//		await Task.Delay(1000);
					//	}

					//}
				}
				catch (Exception exception)
				{
					notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
					notifyIcon.BalloonTipText = exception.Message;
					notifyIcon.ShowBalloonTip(1000);
				}
				await Task.Delay(500);
			}
		}

		private static bool FindAndClick(LockBitmap.LockBitmap bitmap, Point screenOffset, byte[][] patterns)
		{
			Point matchPosition;
			foreach (byte[] pattern in patterns)
			{
				if (FindOccurrenceFast(bitmap.Pixels, bitmap.Width, bitmap.Depth, pattern, out matchPosition))
				{
					matchPosition.Offset(screenOffset);
					LeftMouseClick(matchPosition.X, matchPosition.Y);
					LeftMouseClick(matchPosition.X, matchPosition.Y);
					return true;
				}
			}
			return false;
		}

		private static bool FindAndClick(Bitmap bitmap, Point screenOffset, FastPattern[] patterns)
		{
			foreach (var pattern in patterns)
			{
				//for (int xer = -2; xer < 2; xer++)
				//{
				//	for (int yer = -2; yer < 2; yer++)
				//	{
				if (pattern.MatchPoints.All(mp =>
				{
					var pixel = bitmap.GetPixel(
						(int) Math.Round((mp.Item1.X / pattern.PatternSize.Width) * bitmap.Width),
						(int) Math.Round((mp.Item1.Y / pattern.PatternSize.Height) * bitmap.Height));
					return
						(Math.Abs(pixel.R - mp.Item2.R)
						 + Math.Abs(pixel.G - mp.Item2.G)
						 + Math.Abs(pixel.B - mp.Item2.B))
						/ 255f / 3 <= 0.1;
				}))
				{
					Point matchPosition = new Point(
						(int) ((pattern.ClickPoint.X / pattern.PatternSize.Width) * bitmap.Width),
						(int) ((pattern.ClickPoint.Y / pattern.PatternSize.Height) * bitmap.Height));
					matchPosition.Offset(screenOffset);
					LeftMouseClick(matchPosition.X, matchPosition.Y);
					LeftMouseClick(matchPosition.X, matchPosition.Y);
					return true;
				}
				//	}
				//}
			}
			return false;
		}

		private async Task Sleep(bool deep)
		{
			if (deep)
			{
				bmpScreenshot = null;
				GC.Collect();
			}
			notifyIcon.Icon = SystemIcons.Application;

			await Task.WhenAny(awaiter.Task, Task.Delay(deep ? 10000 : 1000));

			awaiter = new TaskCompletionSource<bool>();
		}

		private static bool FindOccurrenceFast(byte[] data, int width, int depth, byte[] sequencePattern, out Point position)
		{
			position = Point.Empty;
			for (int index = 0; index < data.Length - sequencePattern.Length; index++)
			{
				bool match = true;
				for (int i2 = 0; i2 < sequencePattern.Length; i2++)
				{
					if (data[index + i2] != sequencePattern[i2])
					{
						match = false;
						break;
					}
				}
				if (match)
				{
					var cCount = depth / 8;
					var widthInBytes = width * cCount;
					position = new Point((index % widthInBytes) / cCount, index / widthInBytes);
					return true;
				}
			}
			return false;
		}


		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr FindWindow(string strClassName, string strWindowName);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

		public struct Rect
		{
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }
		}
	}

	public class Patterns
	{
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
		private Lazy<MatchPoint[]> AcceptMatchButtonSample { get; }
		private Lazy<MatchPoint[]> AcceptMatchButtonHoverSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectBanButtonSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectBanButtonHoverSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectBanLockButtonDisabledSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectLockButtonSample { get; }
		private Lazy<MatchPoint[]> ChampionSelectLockButtonHoverSample { get; }

		public int TargetWidth { get; }
		public int TargetHeight { get; }

		public Patterns(int targetWidth, int targetHeight)
		{
			TargetWidth = targetWidth;
			TargetHeight = targetHeight;

			var samplesNamespace = $"{nameof(LolAutoAccept)}.Samples.";
			AcceptMatchButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"AcceptMatchButton.png"), false);
			AcceptMatchButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"AcceptMatchButtonHover.png"), false);
			ChampionSelectSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelect.png"), false);
			ChampionSelectBanButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelectBanButton.png"), false);
			ChampionSelectBanButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelectBanButtonHover.png"), false);
			ChampionSelectBanLockButtonDisabledSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelectBanLockButtonDisabled.png"), false);
			ChampionSelectLockButtonSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelectLockButton.png"), false);
			ChampionSelectLockButtonHoverSample = new Lazy<MatchPoint[]>(() =>
				PrepareSample(samplesNamespace+"ChampionSelectLockButtonHover.png"), false);
		}

		private MatchPoint[] PrepareSample(string resourceName)
		{
			var checkPoints = new List<MatchPoint>();
			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			using (var bitmap = new Bitmap(stream))
			using (var scaledBitmap = Scale(bitmap, TargetWidth, TargetHeight))
			using (var lockBitmap = new LockBitmap.LockBitmap(scaledBitmap))
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

		private Bitmap Scale(Bitmap source, int targetWidth, int targetHeight)
		{
			if (source.Width == targetWidth && source.Height == targetHeight)
				return source;
			var bitmap = new Bitmap(targetWidth, targetHeight);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				g.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
				return bitmap;
			}
		}

		private const double BaseTreshold = 0.10;

		public bool IsAcceptMatchButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, AcceptMatchButtonSample.Value, 0.07)
			   || IsMatch(screenshot, AcceptMatchButtonHoverSample.Value, 0.07);

		public bool IsChampionSelect(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectSample.Value, BaseTreshold);

		public bool HasBanLockButtonDisabled(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanLockButtonDisabledSample.Value, 0.04);

		public bool IsBanButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectBanButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectBanButtonHoverSample.Value, BaseTreshold);

		public bool IsLockButton(LockBitmap.LockBitmap screenshot)
			=> IsMatch(screenshot, ChampionSelectLockButtonSample.Value, BaseTreshold)
			   || IsMatch(screenshot, ChampionSelectLockButtonHoverSample.Value, BaseTreshold);

		private static bool IsMatch(LockBitmap.LockBitmap screenshot, MatchPoint[] sample, double threshold)
		{
			int diff = 0;
			int diffTreshold = (int) (sample.Length * 255 * 3 * threshold);
			int colordiff = 0;
			int colorTreshold = (int)(sample.Length * 255 * threshold);
			foreach (var pixel in sample)
			{
				var targetPixel = screenshot.GetPixel(pixel.X, pixel.Y);

				diff += Math.Abs(targetPixel.R - pixel.Color.R)
				        + Math.Abs(targetPixel.G - pixel.Color.G)
				        + Math.Abs(targetPixel.B - pixel.Color.B);


				colordiff += Math.Max(Math.Max(
					Math.Abs(targetPixel.R - pixel.Color.R), 
					Math.Abs(targetPixel.G - pixel.Color.G)),
					Math.Abs(targetPixel.B - pixel.Color.B));
				//if (diff > diffTreshold)
				//	return false;
			}
			Console.WriteLine($"diff:{(double)diff / (sample.Length * 255 * 3)}");
			Console.WriteLine($"colordiff:{(double)colordiff / (sample.Length * 255)}");
			if (colordiff > colorTreshold)
				return false;
			return true;
		}

		//public static double IsMatchTest(LockBitmap.LockBitmap screenshot, MatchPoint[] sample)
		//{
		//	int diff = 0;
		//	foreach (var pixel in sample)
		//	{
		//		var targetPixel = screenshot.GetPixel(pixel.X, pixel.Y);

		//		diff += Math.Abs(targetPixel.R - pixel.Color.R)
		//		        + Math.Abs(targetPixel.G - pixel.Color.G)
		//		        + Math.Abs(targetPixel.B - pixel.Color.B);
		//	}
		//	return diff / (sample.Length * 255 * 3d);
		//}
	}
}