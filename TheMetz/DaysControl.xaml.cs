using System.Windows;
using System.Windows.Controls;

namespace TheMetz
{
    public partial class DaysControl : UserControl
    {
        public DaysControl()
        {
            InitializeComponent();
        }

        private void SliderControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DaysTextBox != null)
            {
                DaysTextBox.Text = DaysSliderControl.Value.ToString("N0");
            }
        }
        
        private void DaysControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            DaysSliderControl.Value = 14;
        }
        
        public int GetDaysSliderValue()
        {
            return (int)DaysSliderControl.Value;
        }

        private void DaysTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (DaysTextBox != null)
            {
                bool success = int.TryParse(DaysTextBox.Text, out int numericValue);
                if (success)
                {
                    DaysSliderControl.Value = numericValue;
                }
            }
        }
    }
}