using System.Windows;
using System.Windows.Input;

namespace POSKU.Desktop
{
    public partial class PosWindow : Window
    {
        public PosWindow(PosViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is PosViewModel vm && vm.AddByEntryCommand.CanExecute(null))
                {
                    vm.AddByEntryCommand.Execute(null);
                    EntryBox.SelectAll(); // siap scan berikutnya
                }
            }
        }
    }
}
