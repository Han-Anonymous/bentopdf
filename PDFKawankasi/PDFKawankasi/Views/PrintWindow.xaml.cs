using System.Windows;
using System.Windows.Input;
using PDFKawankasi.ViewModels;

namespace PDFKawankasi.Views;

public partial class PrintWindow : Window
{
    public PrintWindow()
    {
        InitializeComponent();
        MouseLeftButtonDown += (s, e) => DragMove();
    }

    public void Configure(PrintWindowViewModel viewModel)
    {
        DataContext = viewModel;
        viewModel.CloseAction = (result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
