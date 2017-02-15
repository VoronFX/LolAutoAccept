using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using LolAutoAccept.Properties;
using Microsoft.Win32;
using System.Management;

namespace LolAutoAccept
{
	class Program
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
		private PatterCaptureForm patterCaptureForm;

		private readonly NotifyIcon notifyIcon;
		private ContextMenu contextMenu;
		RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

		Program()
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
			var region = new Rect() { Left = 1000, Right = 0, Top = 1000, Bottom = 0 };
			foreach (var point in autoPickFastPatterns.Concat(autoAcceptFastPatterns).SelectMany(p => p.MatchPoints))
			{
				region.Left = Math.Min(region.Left, point.Item1.X);
				region.Right = Math.Max(region.Right, point.Item1.X);
				region.Top = Math.Min(region.Top, point.Item1.Y);
				region.Bottom = Math.Max(region.Bottom, point.Item1.Y);
			}
			captureRegion = new Rectangle(region.Left, region.Top, region.Right - region.Left, region.Bottom - region.Top);

			contextMenu = new ContextMenu();

			contextMenu.MenuItems.Add(new MenuItem("Auto accept", CheckAndSave)
			{ Checked = Settings.Default.AutoAccept });
			contextMenu.MenuItems.Add(new MenuItem("Auto pick", CheckAndSave)
			{ Checked = Settings.Default.AutoPick });
			contextMenu.MenuItems.Add(new MenuItem("Force pick", (sender, args) => ((MenuItem)sender).Checked = !((MenuItem)sender).Checked));

			contextMenu.MenuItems.Add("-");

			string currentRegValue = (string)rkApp.GetValue(nameof(LolAutoAccept));
			if (currentRegValue != null && currentRegValue != Application.ExecutablePath)
			{
				rkApp.SetValue(nameof(LolAutoAccept), Application.ExecutablePath);
			}
			contextMenu.MenuItems.Add(new MenuItem("Autoload on startup",
				(sender, args) =>
				{
					((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
					if (((MenuItem)sender).Checked)
					{
						//string src = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
						//string dest = "C:\\temp\\" + System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
						//System.IO.File.Copy(src, dest);

						// Add the value in the registry so that the application runs at startup
						rkApp.SetValue(nameof(LolAutoAccept), Application.ExecutablePath);
					}
					else
					{
						// Remove the value from the registry so that the application doesn't start
						rkApp.DeleteValue(nameof(LolAutoAccept), false);
					}
				})
			{
				Checked = currentRegValue != null
			});
			contextMenu.MenuItems.Add("-");
			contextMenu.MenuItems.Add(new MenuItem("CapturePattern (for dev only)",
				(sender, args) =>
				{
					patterCaptureForm = new PatterCaptureForm();
					patterCaptureForm.FormClosed += (o, eventArgs) => patterCaptureForm = null;
					var thread = new Thread(() => Application.Run(patterCaptureForm));
					thread.SetApartmentState(ApartmentState.STA);
					thread.Start();
				}));
			contextMenu.MenuItems.Add("-");
			contextMenu.MenuItems.Add("Exit", (sender, args) => { Application.Exit(); });

			notifyIcon = new NotifyIcon
			{
				Icon = SystemIcons.Application,
				ContextMenu = contextMenu,
				Text = nameof(LolAutoAccept),
				BalloonTipTitle = nameof(LolAutoAccept),
				Visible = true,
			};

			Task.Run(AutoWorker);

			windowChangeEvents = WinEventProc;

			SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, windowChangeEvents, 0, 0, WINEVENT_OUTOFCONTEXT);
			SetWinEventHook(EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, windowChangeEvents, 0, 0, WINEVENT_OUTOFCONTEXT);

			//startWatcher.EventArrived += StartWatcher_EventArrived;
			//startWatcher.Start();

			// The Icon property sets the icon that will appear
			// in the systray for this application.
			//notifyIcon.Icon = IconBad;

			// The ContextMenu property sets the menu that will
			// appear when the systray icon is right clicked.

			// The Text property sets the text that will be displayed,
			// in a tooltip, when the mouse hovers over the systray icon.
		}

		//private readonly ManagementEventWatcher startWatcher = new ManagementEventWatcher(new WqlEventQuery
		//{
		//	EventClassName = "Win32_ProcessStartTrace",
		//	Condition = "ProcessName='LoLClient.exe'"
		//});

		private void CheckAndSave(object sender, EventArgs eventArgs)
		{
			((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
			Settings.Default.AutoAccept = contextMenu.MenuItems[0].Checked;
			Settings.Default.AutoPick = contextMenu.MenuItems[1].Checked;
			Settings.Default.Save();
		}

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
			public readonly Tuple<Point, Color>[] MatchPoints;
			public readonly SizeF PatternSize;

			public FastPattern(Point clickPoint, SizeF patternSize, Tuple<Point, Color>[] matchPoints)
			{
				this.ClickPoint = clickPoint;
				this.MatchPoints = matchPoints;
				PatternSize = patternSize;
			}
		}

		private Rectangle captureRegion;
		private const int Xerror = 0;//-64;
		private const int Yerror = 0;//-40;
		private readonly FastPattern[] autoPickFastPatterns =
		{
			// Old client draft pick rus
			new FastPattern(new Point(523+Xerror, 587+Yerror), new Size(1280, 800), new []
			{
				new Tuple<Point, Color>(new Point(496+Xerror, 584+Yerror), Color.FromArgb(175, 54, 2)),
				new Tuple<Point, Color>(new Point(503+Xerror, 582+Yerror), Color.FromArgb(183, 60, 2)),
				new Tuple<Point, Color>(new Point(503+Xerror, 589+Yerror), Color.FromArgb(165, 42, 0)),
				new Tuple<Point, Color>(new Point(511+Xerror, 585+Yerror), Color.FromArgb(173, 52, 0)),
				new Tuple<Point, Color>(new Point(507+Xerror, 584+Yerror), Color.FromArgb(175, 57, 0)),
				new Tuple<Point, Color>(new Point(518+Xerror, 581+Yerror), Color.FromArgb(182, 62, 3)),
				new Tuple<Point, Color>(new Point(525+Xerror, 584+Yerror), Color.FromArgb(176, 53, 0)),
				new Tuple<Point, Color>(new Point(523+Xerror, 587+Yerror), Color.FromArgb(173, 46, 0)),
				new Tuple<Point, Color>(new Point(541+Xerror, 587+Yerror), Color.FromArgb(170, 48, 0)),
				new Tuple<Point, Color>(new Point(531+Xerror, 580+Yerror), Color.FromArgb(186, 67, 4)),
				new Tuple<Point, Color>(new Point(548+Xerror, 584+Yerror), Color.FromArgb(179, 53, 0)),
				new Tuple<Point, Color>(new Point(645+Xerror, 585+Yerror), Color.FromArgb(176, 52, 1)),
				new Tuple<Point, Color>(new Point(634+Xerror, 586+Yerror), Color.FromArgb(174, 48, 0)),
				new Tuple<Point, Color>(new Point(645+Xerror, 579+Yerror), Color.FromArgb(190, 67, 1)),
				new Tuple<Point, Color>(new Point(640+Xerror, 572+Yerror), Color.FromArgb(201, 83, 0)),
			}),
			//// Old client draft pick JP
			new FastPattern(new Point(564+Xerror, 651+Yerror), new Size(1280, 800), new []
			{
				new Tuple<Point, Color>(new Point(547+Xerror, 639+Yerror), Color.FromArgb(80, 87, 72)),
				new Tuple<Point, Color>(new Point(547+Xerror, 643+Yerror), Color.FromArgb(69, 77, 62)),
				new Tuple<Point, Color>(new Point(547+Xerror, 645+Yerror), Color.FromArgb(62, 69, 55)),
				new Tuple<Point, Color>(new Point(547+Xerror, 650+Yerror), Color.FromArgb(52, 60, 47)),
				new Tuple<Point, Color>(new Point(549+Xerror, 655+Yerror), Color.FromArgb(167, 41, 0)),
				new Tuple<Point, Color>(new Point(548+Xerror, 656+Yerror), Color.FromArgb(117, 67, 36)),
				new Tuple<Point, Color>(new Point(564+Xerror, 651+Yerror), Color.FromArgb(174, 50, 0)),
				new Tuple<Point, Color>(new Point(576+Xerror, 650+Yerror), Color.FromArgb(176, 52, 0)),
				new Tuple<Point, Color>(new Point(559+Xerror, 640+Yerror), Color.FromArgb(196, 76, 0)),
				new Tuple<Point, Color>(new Point(595+Xerror, 649+Yerror), Color.FromArgb(178, 55, 0)),
				new Tuple<Point, Color>(new Point(615+Xerror, 654+Yerror), Color.FromArgb(169, 43, 0)),
				new Tuple<Point, Color>(new Point(639+Xerror, 654+Yerror), Color.FromArgb(169, 43, 0)),
				new Tuple<Point, Color>(new Point(727+Xerror, 655+Yerror), Color.FromArgb(167, 41, 0)),
			}),

			//// Old client draft pick new sample
			new FastPattern(new Point(637, 637), new Size(1280, 800), new []
			{
				new Tuple<Point, Color>(new Point(724, 623), Color.FromArgb(228, 115, 0)),
				new Tuple<Point, Color>(new Point(711, 622), Color.FromArgb(230, 117, 0)),
				new Tuple<Point, Color>(new Point(573, 621), Color.FromArgb(232, 120, 0)),
				new Tuple<Point, Color>(new Point(552, 651), Color.FromArgb(174, 50, 0)),
				new Tuple<Point, Color>(new Point(571, 652), Color.FromArgb(172, 48, 0)),
				new Tuple<Point, Color>(new Point(674, 653), Color.FromArgb(171, 46, 0)),
				new Tuple<Point, Color>(new Point(725, 654), Color.FromArgb(169, 43, 0)),
				new Tuple<Point, Color>(new Point(551, 623), Color.FromArgb(228, 115, 0)),
			}),
			// test people button
			//new FastPattern(new Point(1085+Xerror, 784+Yerror), new Size(1280, 800), new []
			//{
			//	new Tuple<Point, Color>(new Point(1076, 774), Color.FromArgb(20, 46, 54)),
			//	new Tuple<Point, Color>(new Point(1091, 790), Color.FromArgb(8, 19, 22)),
			//	new Tuple<Point, Color>(new Point(1095, 787), Color.FromArgb(9, 22, 25)),
			//	new Tuple<Point, Color>(new Point(1085, 784), Color.FromArgb(153, 108, 55)),
			//	new Tuple<Point, Color>(new Point(1076, 786), Color.FromArgb(12, 28, 32)),
			//	new Tuple<Point, Color>(new Point(1098, 787), Color.FromArgb(11, 28, 32)),
			//	new Tuple<Point, Color>(new Point(1086, 769), Color.FromArgb(19, 43, 52)),
			//	new Tuple<Point, Color>(new Point(1084, 773), Color.FromArgb(226, 191, 133)),
			//}),
		};

		private readonly FastPattern[] autoAcceptFastPatterns =
		{
			// Old client accept
			new FastPattern(new Point(580, 465), new Size(1280, 800), new []
			{
				new Tuple<Point, Color>(new Point(567, 457), Color.FromArgb(202, 83, 0)),
				new Tuple<Point, Color>(new Point(574, 462), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(575, 458), Color.FromArgb(196, 78, 0)),
				new Tuple<Point, Color>(new Point(587, 457), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(588, 461), Color.FromArgb(192, 71, 0)),
				new Tuple<Point, Color>(new Point(581, 462), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(580, 465), Color.FromArgb(164, 130, 113)),
				new Tuple<Point, Color>(new Point(585, 466), Color.FromArgb(181, 57, 0)),
				new Tuple<Point, Color>(new Point(592, 449), Color.FromArgb(220, 105, 0)),
				new Tuple<Point, Color>(new Point(586, 450), Color.FromArgb(218, 102, 0)),
				new Tuple<Point, Color>(new Point(584, 458), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(595, 461), Color.FromArgb(192, 71, 0)),
				new Tuple<Point, Color>(new Point(576, 458), Color.FromArgb(200, 80, 0)),

			}),
			// Old client accept Vlad edition
			new FastPattern(new Point(504, 417), new Size(1152, 720), new []
			{
				new Tuple<Point, Color>(new Point(507, 420), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(512, 420), Color.FromArgb(173, 65, 0)),
				new Tuple<Point, Color>(new Point(519, 420), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(525, 421), Color.FromArgb(192, 71, 0)),
				new Tuple<Point, Color>(new Point(528, 412), Color.FromArgb(162, 93, 37)),
				new Tuple<Point, Color>(new Point(504, 417), Color.FromArgb(199, 82, 0)),
				new Tuple<Point, Color>(new Point(513, 424), Color.FromArgb(252, 255, 254)),
				new Tuple<Point, Color>(new Point(506, 426), Color.FromArgb(181, 57, 0)),
				new Tuple<Point, Color>(new Point(494, 421), Color.FromArgb(192, 71, 0)),
				new Tuple<Point, Color>(new Point(495, 412), Color.FromArgb(214, 97, 0)),
				new Tuple<Point, Color>(new Point(539, 424), Color.FromArgb(185, 63, 0)),
			}),

			// New client accept
			new FastPattern(new Point(627+Xerror, 551+Yerror), new Size(1280, 800), new []
			{
				new Tuple<Point, Color>(new Point(599+Xerror, 546+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(602+Xerror, 550+Yerror), Color.FromArgb(135, 166, 167)),
				new Tuple<Point, Color>(new Point(607+Xerror, 550+Yerror), Color.FromArgb(160, 195, 195)),
				new Tuple<Point, Color>(new Point(610+Xerror, 554+Yerror), Color.FromArgb(81, 99, 102)),
				new Tuple<Point, Color>(new Point(615+Xerror, 553+Yerror), Color.FromArgb(163, 199, 199)),
				new Tuple<Point, Color>(new Point(622+Xerror, 550+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(626+Xerror, 554+Yerror), Color.FromArgb(68, 83, 86)),
				new Tuple<Point, Color>(new Point(617+Xerror, 551+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(635+Xerror, 551+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(627+Xerror, 551+Yerror), Color.FromArgb(77, 95, 98)),
				new Tuple<Point, Color>(new Point(629+Xerror, 545+Yerror), Color.FromArgb(31, 39, 44)),
				new Tuple<Point, Color>(new Point(642+Xerror, 545+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(639+Xerror, 552+Yerror), Color.FromArgb(134, 163, 164)),
				new Tuple<Point, Color>(new Point(650+Xerror, 554+Yerror), Color.FromArgb(145, 177, 178)),
				new Tuple<Point, Color>(new Point(654+Xerror, 549+Yerror), Color.FromArgb(158, 193, 193)),
				new Tuple<Point, Color>(new Point(646+Xerror, 549+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(663+Xerror, 549+Yerror), Color.FromArgb(163, 199, 199)),
				new Tuple<Point, Color>(new Point(671+Xerror, 548+Yerror), Color.FromArgb(48, 59, 63)),
				new Tuple<Point, Color>(new Point(675+Xerror, 547+Yerror), Color.FromArgb(30, 37, 42)),
				new Tuple<Point, Color>(new Point(673+Xerror, 551+Yerror), Color.FromArgb(64, 79, 82)),
			}),
		};

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

		private WinEventDelegate windowChangeEvents;

		delegate void WinEventDelegate(
			IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

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

		private void StartWatcher_EventArrived(object sender, EventArrivedEventArgs e)
		{
			Task.Run(() => awaiter.TrySetResult(true));
		}

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
				return GetWindowThreadProcessId(handle, out processId) > 0 ?
					Process.GetProcessById((int)processId).MainModule.FileName : "";
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
					if (!contextMenu.MenuItems[0].Checked && !contextMenu.MenuItems[1].Checked && !contextMenu.MenuItems[2].Checked)
						continue;

					IntPtr foregroundWindow = GetForegroundWindow();

					var lolprocesses = Process.GetProcessesByName("lolclient");
					if (lolprocesses.All(p => p.MainWindowHandle != foregroundWindow))
					{
						bmpScreenshot = null;
						GC.Collect();
						notifyIcon.Icon = SystemIcons.Application;

						await Task.WhenAny(awaiter.Task, Task.Delay(lolprocesses.Any() ? 1000 : 10000));

						awaiter = new TaskCompletionSource<bool>();
						continue;
					}
					notifyIcon.Icon = SystemIcons.Shield;

					var tempRect = new Rect();
					GetWindowRect(foregroundWindow, ref tempRect);
					var rect = new Rectangle(tempRect.Left, tempRect.Top, tempRect.Right - tempRect.Left,
						tempRect.Bottom - tempRect.Top);
					if (rect.IsEmpty)
					{
						continue;
					}
					if (bmpScreenshot == null || bmpScreenshot.Width != rect.Width || bmpScreenshot.Height != rect.Height)
					{
						//Create a new bitmap.
						bmpScreenshot = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
					}

					// Create a graphics object from the bitmap.
					using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
					{
						// Take the screenshot from the upper left corner to the right bottom corner.
						//gfxScreenshot.CopyFromScreen(
						//	rect.Left + captureRegion.Left, rect.Top + captureRegion.Top,
						//	captureRegion.Left, captureRegion.Top,
						//	captureRegion.Size,// new Size(bmpScreenshot.Width, bmpScreenshot.Height),
						//	CopyPixelOperation.SourceCopy);
						gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0,
							 new Size(bmpScreenshot.Width, bmpScreenshot.Height),
							CopyPixelOperation.SourceCopy);
					}

					patterCaptureForm?.AddBitmap(bmpScreenshot);
					//int i = 0;
					//while (File.Exists($"lolautopicker{i}.png")) i++;
					//	bmpScreenshot.Save($"lolautopicker{i}.png");

					notifyIcon.BalloonTipIcon = ToolTipIcon.Info;

					if (contextMenu.MenuItems[0].Checked
						&& FindAndClick(bmpScreenshot, rect.Location, autoAcceptFastPatterns))
					{
						notifyIcon.BalloonTipText = "Auto accepted!";
						notifyIcon.ShowBalloonTip(1000);
						await Task.Delay(1000);
					}
					else if (contextMenu.MenuItems[1].Checked
						&& FindAndClick(bmpScreenshot, rect.Location, autoPickFastPatterns))
					{
						notifyIcon.BalloonTipText = "Auto picked!";
						notifyIcon.ShowBalloonTip(1000);
						await Task.Delay(1000);
					}
					else if (contextMenu.MenuItems[2].Checked)
					{
						int x = (int)(rect.Left + rect.Width * 580 / 1280f);
						int y = (int)(rect.Top + rect.Height * 465 / 800f);
						mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
						mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
						await Task.Delay(1500);
					}

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
						(int)((mp.Item1.X / pattern.PatternSize.Width) * bitmap.Width),
						(int)((mp.Item1.Y / pattern.PatternSize.Height) * bitmap.Height));
					return 
					(Math.Abs(pixel.R - mp.Item2.R) 
					+ Math.Abs(pixel.G - mp.Item2.G) 
					+ Math.Abs(pixel.B - mp.Item2.B)) 
					/ 255f / 3 <= 0.1;
				}))
				{
					Point matchPosition = new Point(
						(int)((pattern.ClickPoint.X / pattern.PatternSize.Width) * bitmap.Width),
						(int)((pattern.ClickPoint.Y / pattern.PatternSize.Height) * bitmap.Height));
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
}
