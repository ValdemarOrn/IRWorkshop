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

namespace IrWorkshop.Ui
{
    /// <summary>
    /// Interaction logic for SpectrumStageView.xaml
    /// </summary>
    public partial class SpectrumStageView : UserControl
    {
        public SpectrumStageView()
        {
            InitializeComponent();
        }

	    private void ResetMid(object sender, MouseButtonEventArgs e)
	    {
		    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		    {
			    ((Slider)sender).Value = 0.5;
			    e.Handled = true;
		    }
	    }

	    private void ResetZero(object sender, MouseButtonEventArgs e)
	    {
		    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		    {
			    ((Slider)sender).Value = 0.0;
			    e.Handled = true;
		    }
	    }

	    private void ResetMax(object sender, MouseButtonEventArgs e)
	    {
		    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		    {
			    ((Slider)sender).Value = 1.0;
			    e.Handled = true;
		    }
	    }

	    private void ResetGain(object sender, MouseButtonEventArgs e)
	    {
		    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		    {
			    ((Slider)sender).Value = 6.0 / 10.0;
			    e.Handled = true;
		    }
	    }
	}
}
