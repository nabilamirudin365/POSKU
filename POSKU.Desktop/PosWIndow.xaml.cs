using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace POSKU.Desktop
{
    public partial class PosWindow : Window
    {
        private static readonly Regex _reInt = new(@"^[0-9]+$");
        private static readonly Regex _reDec = new(@"^[0-9]*([.][0-9]{0,2})?$"); // kalau pakai koma: ([.,][0-9]{0,2})?

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
                    EntryBox.SelectAll();
                }
            }
        }

        // Tambahan untuk filter angka & blok paste
        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !_reInt.IsMatch(e.Text);
        private void Numeric_PreviewTextInput(object sender, TextCompositionEventArgs e) => e.Handled = !_reDec.IsMatch(e.Text);
        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (!Clipboard.ContainsText() || !_reInt.IsMatch(Clipboard.GetText()))
                    e.Handled = true;
            }
        }
    }
}
