using System.Windows;

namespace POSKU.Desktop
{
    public partial class ReportWindow : Window
    {
        public ReportWindow(ReportViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
