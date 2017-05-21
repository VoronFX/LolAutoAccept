using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
		private AutoPickWPF autoPickFormWPF;

		private readonly NotifyIcon notifyIcon;
		private readonly ProgramContextMenu contextMenu;

		private TaskCompletionSource<bool> awaiter = new TaskCompletionSource<bool>();

		private Program()
		{
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


			//var lolLuncherProcesses = Process.GetProcessesByName("Discord")
			//	.Where(p => p.MainWindowHandle != IntPtr.Zero);

			//var windowRect = (Rectangle)Rect.FromWindowHandleWindowRect(lolLuncherProcesses.First().MainWindowHandle);

			////Create a new bitmap.
			//var bmpScreenshot = new Bitmap(windowRect.Width, windowRect.Height, PixelFormat.Format32bppArgb);


			//// Create a graphics object from the bitmap.
			//using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
			//{
			//	// Take the screenshot from the upper left corner to the right bottom corner.
			//	gfxScreenshot.CopyFromScreen(windowRect.Location, Point.Empty,
			//		bmpScreenshot.Size, CopyPixelOperation.SourceCopy);
			//}

			//bmpScreenshot.Save("test.bmp");
			//var x = new LockBitmap.LockBitmap(bmpScreenshot);
		}

		//private readonly ManagementEventWatcher startWatcher = new ManagementEventWatcher(new WqlEventQuery
		//{
		//	EventClassName = "Win32_ProcessStartTrace",
		//	Condition = "ProcessName='LoLClient.exe'"
		//});


		//This simulates a left mouse click
		public static void LeftMouseClick(int xpos, int ypos)
		{
			SetCursorPos(xpos, ypos);
			mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
			mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
		}

		public static void LeftMouseClick(Rectangle rect, double xMultiplier, double yMultiplier)
			=> LeftMouseClick(
				(int)(rect.Left + rect.Width * xMultiplier),
				(int)(rect.Top + rect.Height * yMultiplier));

		private class ChampionSelectSession
		{
			public ChampionSelectSession(CachedBitmapPixels bitmap, Patterns patterns)
			{
				OurPickPosition = new Lazy<int>(() => patterns.DetectOurPickPosition(bitmap), false);
			}

			public Lazy<int> OurPickPosition { get; }

			public enum BanPickPhase
			{
				Initial = 0,
				ClearSearchPressed,
				ChampionEntered,
				ChampionSelected,
			}

			public BanPickState BanState { get; set; }
			public BanPickState PickState { get; set; }

			public struct BanPickState
			{
				public BanPickState(BanPickPhase phase, int championIndex)
				{
					Phase = phase;
					ChampionIndex = championIndex;
					LastChanged = DateTime.UtcNow;
				}

				public BanPickPhase Phase { get; }
				public int ChampionIndex { get; }
				public DateTime LastChanged { get; }
			}
		}

		private Bitmap bmpScreenshot;
		private Patterns patterns;
		private ChampionSelectSession championSelectSession;

		private async Task AutoWorker()
		{
			while (true)
			{
				try
				{
					if (!contextMenu.AutoAccept.Checked &&
						!contextMenu.AutoLock.Checked)
						continue;

					if (Process.GetProcessesByName("League of Legends").Any())
					{
						await Sleep(true);
						continue;
					}

					var lolLuncherProcesses = Process.GetProcessesByName("LeagueClientUx")
						.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

					if (lolLuncherProcesses == null)
					{
						await Sleep(true);
						continue;
					}

					notifyIcon.Icon = SystemIcons.Shield;

					var windowRect = (Rectangle)Rect.FromWindowHandleWindowRect(lolLuncherProcesses.MainWindowHandle);

					if (bmpScreenshot == null || patterns == null || bmpScreenshot.Size != windowRect.Size)
					{
						championSelectSession = null;

						if (!Patterns.SupportedResolutions.Contains(windowRect.Size))
						{
							await Sleep(false);
							continue;
						}

						//Create a new bitmap.
						bmpScreenshot = new Bitmap(windowRect.Width, windowRect.Height, PixelFormat.Format32bppArgb);
						patterns = new Patterns(windowRect.Size);
					}

					// Create a graphics object from the bitmap.
					using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
					{
						// Take the screenshot from the upper left corner to the right bottom corner.
						gfxScreenshot.CopyFromScreen(windowRect.Location, Point.Empty,
							bmpScreenshot.Size, CopyPixelOperation.SourceCopy);
					}

					patternCaptureForm?.AddBitmap(bmpScreenshot);

					await ProcessScreenshot(patterns, new CachedBitmapPixels(bmpScreenshot), windowRect);
				}
				catch (Exception exception)
				{
					await ShowBallonTip(exception.Message, ToolTipIcon.Error);
				}
				await Task.Delay(500);
			}
		}

		private async Task ProcessScreenshot(Patterns patterns, CachedBitmapPixels screenshot, Rectangle windowRect)
		{
			if ((contextMenu.AutoLock.Checked || autoPickForm != null)
				&& patterns.IsChampionSelect(screenshot))
			{
				if (autoPickForm != null && autoPickForm.Enabled) //TODO
				{
					championSelectSession = championSelectSession ?? new ChampionSelectSession(screenshot, patterns);

					if (patterns.HasBanLockButtonDisabled(screenshot)
						&& patterns.IsChampionSearch(screenshot))
					{
						var isBanPhase = patterns.IsFirstSelectBan(screenshot);
						var state = isBanPhase ? championSelectSession.BanState
							: championSelectSession.PickState;

						switch (state.Phase)
						{
							case ChampionSelectSession.BanPickPhase.Initial:
								LeftMouseClick(windowRect, 0.7203125, 0.1);
								state = new ChampionSelectSession.BanPickState(
									ChampionSelectSession.BanPickPhase.ClearSearchPressed, state.ChampionIndex);
								break;
							case ChampionSelectSession.BanPickPhase.ClearSearchPressed:

								break;
							case ChampionSelectSession.BanPickPhase.ChampionEntered:
								break;
							case ChampionSelectSession.BanPickPhase.ChampionSelected:
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (isBanPhase)
						{
							championSelectSession.BanState = state;
						}
						else
						{
							championSelectSession.PickState = state;
						}
					}
					else if (patterns.IsBanButton(screenshot))
					{
					}
					else if (patterns.IsLockButton(screenshot))
					{
					}
				}
				else if (patterns.IsLockButton(screenshot))
				{
					LeftMouseClick(windowRect, 0.5, 0.81);
					await ShowBallonTip("Auto locked!", ToolTipIcon.Info);
				}
				return;
			}
			else if (contextMenu.AutoAccept.Checked && patterns.IsAcceptMatchButton(screenshot))
			{
				LeftMouseClick(windowRect, 0.5, 0.775);
				await ShowBallonTip("Auto accepted!", ToolTipIcon.Info);
			}
			championSelectSession = null;
		}

		private async Task ShowBallonTip(string message, ToolTipIcon icon)
		{
			notifyIcon.BalloonTipIcon = icon;
			notifyIcon.BalloonTipText = message;
			notifyIcon.ShowBalloonTip(1000);
			await Task.Delay(1000);
		}

		private async Task Sleep(bool deep)
		{
			if (deep)
			{
				//bmpScreenshot = null;
				GC.Collect();
			}
			notifyIcon.Icon = SystemIcons.Application;

			await Task.WhenAny(awaiter.Task, Task.Delay(deep ? 10000 : 1000));

			awaiter = new TaskCompletionSource<bool>();
		}
	}
}