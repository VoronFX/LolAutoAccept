using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LolAutoAccept.Properties;
using Microsoft.Win32;

namespace LolAutoAccept
{
	class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			Program pg = new Program();
			Application.Run();
		}

		private readonly NotifyIcon notifyIcon;
		private ContextMenu contextMenu;
		RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

		Program()
		{

			autoAcceptPatterns = new[]
			{
				SequenceFromSample(Resources.oldClientSample),
				SequenceFromSample(Resources.newClientSample),
			};
			autoPickPatterns = new[] {
				SequenceFromSample(Resources.oldClientSamplePick),
				SequenceFromSample(Resources.oldClientSamplePick2),
				SequenceFromSample(Resources.oldClientSamplePickOld) };

			contextMenu = new ContextMenu();

			contextMenu.MenuItems.Add(new MenuItem("Auto accept", CheckAndSave)
			{ Checked = Settings.Default.AutoAccept });
			contextMenu.MenuItems.Add(new MenuItem("Auto pick", CheckAndSave)
			{ Checked = Settings.Default.AutoPick });

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

			var dele = new WinEventDelegate(WinEventProc);

			SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
			SetWinEventHook(EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);

			// The Icon property sets the icon that will appear
			// in the systray for this application.
			//notifyIcon.Icon = IconBad;

			// The ContextMenu property sets the menu that will
			// appear when the systray icon is right clicked.

			// The Text property sets the text that will be displayed,
			// in a tooltip, when the mouse hovers over the systray icon.
		}

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

		private readonly byte[][] autoPickPatterns;
		private readonly byte[][] autoAcceptPatterns;

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

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

		[DllImport("user32.dll")]
		static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

		private const uint WINEVENT_OUTOFCONTEXT = 0;
		private const uint EVENT_SYSTEM_FOREGROUND = 3;
		private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
		private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

		TaskCompletionSource<bool> awaiter = new TaskCompletionSource<bool>();

		public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			Task.Run(() =>
			{
				var current = GetActiveWindowsProcessname();
				if (!string.IsNullOrWhiteSpace(current))
				{
					currentApp = System.IO.Path.GetFileName(current).ToLowerInvariant();

					var copy = awaiter;
					while (!copy.Task.IsCompleted)
					{
						copy = awaiter;
						copy.TrySetResult(true);
					}
				}
			});
		}

		private string currentApp;

		private async Task AutoWorker()
		{
			while (true)
			{
				try
				{
					if (!contextMenu.MenuItems[0].Checked && !contextMenu.MenuItems[1].Checked)
						continue;

					if (currentApp != "lolclient.exe")
					{
						await awaiter.Task;
						awaiter = new TaskCompletionSource<bool>();
						continue;
					}

					foreach (Screen screen in Screen.AllScreens)
					{
						//Create a new bitmap.
						var bmpScreenshot = new Bitmap(screen.Bounds.Width,
							screen.Bounds.Height,
							PixelFormat.Format32bppArgb);

						// Create a graphics object from the bitmap.
						using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
						{
							// Take the screenshot from the upper left corner to the right bottom corner.
							gfxScreenshot.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, screen.Bounds.Size,
								CopyPixelOperation.SourceCopy);
						}

						using (var bitmap = new LockBitmap.LockBitmap(bmpScreenshot))
						{
							notifyIcon.BalloonTipIcon = ToolTipIcon.Info;

							if (contextMenu.MenuItems[0].Checked
								&& FindAndClick(bitmap, screen.WorkingArea.Location, autoAcceptPatterns))
							{
								notifyIcon.BalloonTipText = "Auto accepted!";
								notifyIcon.ShowBalloonTip(1000);
								break;
							}

							if (contextMenu.MenuItems[1].Checked
								&& FindAndClick(bitmap, screen.WorkingArea.Location, autoPickPatterns))
							{
								notifyIcon.BalloonTipText = "Auto picked!";
								notifyIcon.ShowBalloonTip(1000);
								break;
							}
							//if (FindOccurrence(bitmap, oldClientSample, new Point(3, 3), out matchPosition))
							//	if (FindOccurrenceFast(bitmap.Pixels, bitmap.Width, bitmap.Depth, oldClientSample, out matchPosition))
							//{
							//	matchPosition.Offset(screen.WorkingArea.Location);
							//	LeftMouseClick(matchPosition.X, matchPosition.Y);
							//	LeftMouseClick(matchPosition.X, matchPosition.Y);
							//	break;
							//}

						}
						//BeginInvoke((Action)(() => pictureBox1.Image = bmpScreenshot));
						//BeginInvoke((Action)(() => pictureBox1.Image = Properties.Resources.oldClientSample3));
					}
					GC.Collect();
				}
				catch (Exception exception)
				{
					notifyIcon.BalloonTipIcon = ToolTipIcon.Error;
					notifyIcon.BalloonTipText = exception.Message;
					notifyIcon.ShowBalloonTip(1000);
				}
				await Task.Delay(200);
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


		private static bool FindOccurrence(LockBitmap.LockBitmap picture, LockBitmap.LockBitmap subPicture,
			Point speedMultiplier, out Point position)
		{
			position = Point.Empty;
			bool match = false;

			while (!match && position.X < picture.Width)
			{
				position.Y = 0;
				while (!match && position.Y < picture.Height)
				{
					int matched = 0;
					bool localMatch = true;
					for (int x = 0; x < Math.Min(picture.Width - position.X, subPicture.Width); x += speedMultiplier.X)
					{
						for (int y = 0; y < Math.Min(picture.Height - position.Y, subPicture.Height); y += speedMultiplier.Y)
						{
							if (picture.GetPixel(x + position.X, y + position.Y) != subPicture.GetPixel(x, y))
							{
								localMatch = false;
								break;
							}
						}
						if (!localMatch)
							break;
					}

					match = localMatch;

					position.Y++;
				}
				position.X++;
			}

			return match;
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

	}
}
