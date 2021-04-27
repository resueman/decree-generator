using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DecreeGeneratorUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GeneratorViewModel generatorViewModel;

        public MainWindow()
        {
            InitializeComponent();
            generatorViewModel = new GeneratorViewModel();
            DataContext = generatorViewModel;
        }

        private string GetFileName(string extensionFilter)
        {
            var fileDialog = new OpenFileDialog { Filter = extensionFilter };
            fileDialog.ShowDialog();
            return fileDialog.FileName;
        }

        private void LoadContingentButtonClick(object sender, RoutedEventArgs e) 
            => generatorViewModel.ContingentFileName = GetFileName("Excel documents (.xlsx)|*.xlsx");

        private void LoadCurriculumButtonClick(object sender, RoutedEventArgs e)
            => generatorViewModel.CurriculumFileName = GetFileName("Text documents (.docx, .txt)|*.docx;*.txt");

        private void LoadChoiceApplicationsButtonClick(object sender, RoutedEventArgs e)
            => generatorViewModel.DisciplineChoiceApplicationsFileName = GetFileName("Text documents (.docx)|*.docx;");

        private void LoadChangeApplicationsButtonClick(object sender, RoutedEventArgs e)
            => generatorViewModel.DisciplineChangeApplicationsFileName = GetFileName("Text documents (.docx)|*.docx;");

        private void RadioButtonChecked(object sender, RoutedEventArgs e)
            => generatorViewModel.IsDisciplineChangeOption = (RadioButton)sender == ChangedChoiceRadioButton;

        private async void CreateDecreeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await generatorViewModel.GenerateDecree();
            }
            catch (Exception exception)
            {
                var errorMessage = "";
                while (exception != null)
                {
                    errorMessage += $"{exception.GetType().Name}: {exception.Message}\n";
                    exception = exception.InnerException;
                }
                MessageBox.Show(errorMessage);
            }
        }
    }
}
