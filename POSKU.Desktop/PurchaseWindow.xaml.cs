using System.Windows;
using System.Windows.Input;

namespace POSKU.Desktop
{
    public partial class PurchaseWindow : Window
    {
        public PurchaseWindow(PurchaseViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void EntryBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PurchaseViewModel vm)
            {
                if (vm.AddByEntryCommand.CanExecute(null))
                {
                    vm.AddByEntryCommand.Execute(null);
                    EntryBox.SelectAll();
                }
            }
        }
    }
}
