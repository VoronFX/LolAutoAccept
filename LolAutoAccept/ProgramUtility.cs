using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LolAutoAccept
{
	public sealed partial class Program
	{
		//This is a replacement for Cursor.Position in WinForms
		[System.Runtime.InteropServices.DllImport("user32.dll")]
		private static extern bool SetCursorPos(int x, int y);

		[System.Runtime.InteropServices.DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

		public const int MOUSEEVENTF_LEFTDOWN = 0x02;
		public const int MOUSEEVENTF_LEFTUP = 0x04;

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

		// Don't inline cause it will be grabage collected and exception will be thrown
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly WinEventDelegate windowChangeEvents;

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
					? Process.GetProcessById((int)processId).MainModule.FileName
					: "";
			}
			catch (Exception exception)
			{
				return "";
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr FindWindow(string strClassName, string strWindowName);

		[StructLayout(LayoutKind.Sequential)]
		public struct Rect
		{
			public int Left { get; set; }
			public int Top { get; set; }
			public int Right { get; set; }
			public int Bottom { get; set; }

			public static explicit operator Rectangle(Rect obj)
				=> Rectangle.FromLTRB(obj.Left, obj.Top, obj.Right, obj.Bottom);

			public static Rect FromWindowHandleWindowRect(IntPtr handle)
			{
				Rect tempRect;
				GetWindowRect(handle, out tempRect);
				return tempRect;
			}

			[DllImport("user32.dll")]
			private static extern bool GetWindowRect(IntPtr hWnd, out Rect rectangle);
		}
	}
}