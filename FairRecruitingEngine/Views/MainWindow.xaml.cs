using FairRecruitingEngine.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace FairRecruitingEngine.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    if (this.DataContext is MainViewModel vm)
                    {
                        vm.PasteImageCommand.Execute(null);
                        if (vm.HasImage) e.Handled = true;
                    }
                }
            };
        }
    }
}