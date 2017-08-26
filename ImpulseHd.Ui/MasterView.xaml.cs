using System;
using System.Collections.Generic;
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

namespace ImpulseHd.Ui
{
	/// <summary>
	/// Interaction logic for MasterView.xaml
	/// </summary>
	public partial class MasterView : UserControl
	{
		public MasterView()
		{
			InitializeComponent();
		}

		private void ResetWindowLength(object sender, MouseButtonEventArgs e)
		{
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
			{
				((Slider)sender).Value = 0.0;
				e.Handled = true;
			}
		}
	}
}
