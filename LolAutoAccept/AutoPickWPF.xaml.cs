using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LolAutoAccept.Properties;

namespace LolAutoAccept
{
	/// <summary>
	/// Interaction logic for AutoPickWPF.xaml
	/// </summary>
	public partial class AutoPickWPF : Window
	{
		public AutoPickWPF()
		{
			InitializeComponent();
			this.DataContext = Settings.Default.AutoPickData ?? new AutoPickModel();
		}

		protected override void OnClosed(EventArgs e)
		{
			Settings.Default.AutoPickData = (AutoPickModel) DataContext;
			Settings.Default.Save();
			base.OnClosed(e);
		}
	}

	//LolAutoAccept.AutoPickModel
	[SettingsSerializeAs(SettingsSerializeAs.Xml)]
	public sealed class AutoPickModel
	{
		public class BanItem
		{
			public string ChampionName { get; set; }
		}

		public sealed class PickItem : BanItem
		{
			public string Runes { get; set; }
			public string Masteries { get; set; }
			public string SummonerSpell1 { get; set; }
			public string SummonerSpell2 { get; set; }
			public string Role { get; set; }
		}

		public bool PostChatMessage { get; set; }
		public string Message { get; set; }

		public bool EnableAutoPick { get; set; }

		public ObservableCollection<PickItem> Picks { get; }
			= new ObservableCollection<PickItem>();

		public bool EnableAutoBan { get; set; }

		public ObservableCollection<BanItem> Bans { get; }
			= new ObservableCollection<BanItem>();
	}
}
