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
	/// Interaction logic for ImpulseConfigView.xaml
	/// </summary>
	public partial class ImpulseConfigView : UserControl
	{
		public ImpulseConfigView()
		{
			InitializeComponent();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var textBox = ((TextBox)sender);
			textBox.CaretIndex = textBox.Text.Length;
			var rect = textBox.GetRectFromCharacterIndex(textBox.CaretIndex);
			textBox.ScrollToHorizontalOffset(rect.Right);
		}
	}
}
