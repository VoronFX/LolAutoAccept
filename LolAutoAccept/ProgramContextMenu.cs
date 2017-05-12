using System;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using LolAutoAccept.Properties;
using Microsoft.Win32;

namespace LolAutoAccept
{
	partial class Program
	{
		public class ProgramContextMenu : ContextMenu
		{
			public MenuItem AutoAccept { get; }
			public MenuItem AutoLock { get; }
			public MenuItem Divider1 { get; } = new MenuItem("-");
			public MenuItem AutoPick { get; }
			public MenuItem Divider2 { get; } = new MenuItem("-");
			public MenuItem AutoLoad { get; }
			public MenuItem Divider3 { get; } = new MenuItem("-");
			public MenuItem CapturePattern { get; }
			public MenuItem SearchMatchingPatternSize { get; }
			public MenuItem Divider4 { get; } = new MenuItem("-");
			public MenuItem Exit { get; } = new MenuItem("Exit",
				(sender, args) => Environment.Exit(0));

			public ProgramContextMenu(Program program)
			{
				AutoAccept = new MenuItem("Auto accept", CheckAndSave)
				{ Checked = Settings.Default.AutoAccept };
				AutoLock = new MenuItem("Auto lock", CheckAndSave)
				{ Checked = Settings.Default.AutoLock };
				AutoPick = new MenuItem("Auto pick", (sender, args) =>
				{
					if (program.autoPickForm == null)
					{
						program.autoPickForm = new AutoPickForm();
						var thread = new Thread(() => Application.Run(program.autoPickForm));
						thread.SetApartmentState(ApartmentState.STA);
						thread.Start();
					}
					else
					{
						program.autoPickForm.BringToFront();
					}
				});

				var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

				string currentRegValue = (string)rkApp?.GetValue(nameof(LolAutoAccept));
				if (currentRegValue != null && currentRegValue != Application.ExecutablePath)
				{
					rkApp.SetValue(nameof(LolAutoAccept), Application.ExecutablePath);
				}
				AutoLoad = new MenuItem("Autoload on startup",
					(sender, args) =>
					{
						((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
						if (((MenuItem)sender).Checked)
						{
							//string src = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
							//string dest = "C:\\temp\\" + System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
							//System.IO.File.Copy(src, dest);

							// Add the value in the registry so that the application runs at startup
							rkApp?.SetValue(nameof(LolAutoAccept), Application.ExecutablePath);
						}
						else
						{
							// Remove the value from the registry so that the application doesn't start
							rkApp?.DeleteValue(nameof(LolAutoAccept), false);
						}
					})
				{
					Checked = currentRegValue != null
				};

				CapturePattern = new MenuItem("CapturePattern (for dev only)",
					(sender, args) =>
					{
						program.patternCaptureForm = new PatterCaptureForm();
						program.patternCaptureForm.FormClosed += (o, eventArgs) => program.patternCaptureForm = null;
						var thread = new Thread(() => Application.Run(program.patternCaptureForm));
						thread.SetApartmentState(ApartmentState.STA);
						thread.Start();
					});

				SearchMatchingPatternSize = new MenuItem("SearchMatchingPatternSize (for dev only)",
					(sender, args) =>
					{
						var thread = new Thread(() => Application.Run(new SearchMatchingSizeForm()));
						thread.SetApartmentState(ApartmentState.STA);
						thread.Start();
					});

				MenuItems.AddRange(new[]
				{
					AutoAccept, AutoLock, Divider1, AutoPick,Divider2, AutoLoad, Divider3, CapturePattern, SearchMatchingPatternSize, Divider4, Exit
				});
			}

			protected override void OnPopup(EventArgs e)
			{
				var showDev = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
				Divider1.Visible = showDev;
				CapturePattern.Visible = showDev;
				SearchMatchingPatternSize.Visible = showDev;
				base.OnPopup(e);
			}

			private void CheckAndSave(object sender, EventArgs eventArgs)
			{
				((MenuItem)sender).Checked = !((MenuItem)sender).Checked;
				Settings.Default.AutoAccept = AutoAccept.Checked;
				Settings.Default.AutoLock = AutoLock.Checked;
				Settings.Default.Save();
			}
		}
	}
}