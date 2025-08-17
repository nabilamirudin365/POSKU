using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace POSKU.Desktop
{
    public partial class MainWindow : Window
    {
        // Regex untuk validasi input
        private static readonly Regex _reInt = new(@"^[0-9]+$");                         // bilangan bulat
        private static readonly Regex _reDec = new(@"^[0-9]*([.][0-9]{0,2})?$");         // desimal max 2 digit

        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm; // DI: ViewModel di-inject dari App.xaml.cs
        }

        // Hanya izinkan angka bulat (untuk Stock)
        private void Integer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_reInt.IsMatch(e.Text);
        }

        // Hanya izinkan angka desimal (untuk Price)
        private void Numeric_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_reDec.IsMatch(e.Text);
        }

        // (Opsional) Blok paste teks yang tidak valid ke TextBox angka
        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    var text = System.Windows.Clipboard.GetText();

                    // Cek apakah target TextBox adalah integer-only atau decimal
                    // Jika Anda ingin membedakan, beri Name pada TextBox dan cek di sini.
                    // Default: validasi decimal 2 digit.
                    if (!_reDec.IsMatch(text))
                    {
                        e.Handled = true; // batalkan paste
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        private void OpenPos_Click(object sender, RoutedEventArgs e)
        {
            var pos = App.Services.GetRequiredService<PosWindow>();
            pos.Owner = this;
            pos.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            pos.Show();
        }

        private void OpenDailyReport_Click(object sender, RoutedEventArgs e)
        {
            var win = App.Services.GetRequiredService<ReportWindow>();
            win.Owner = this;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.ShowDialog();
        }

    }
}
