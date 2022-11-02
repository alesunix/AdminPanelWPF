using AdminPanelWPF.Models;
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

namespace AdminPanelWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainModel mModel = new MainModel();
        SyntaxPaint 
        public MainWindow()
        {
            Config.CreateConfig();
            InitializeComponent();
            ComboPages.ItemsSource = mModel.ListPages();
            ComboPages.SelectionChanged += ComboPages_SelectionChanged;
        }

        private void ComboPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            richTextBox1.Document.Blocks.Clear();
            mModel.Page = ComboPages.SelectedItem.ToString();
            richTextBox1.Document.Blocks.Add(new Paragraph(new Run(mModel.ReadPage())));
            labelConsole.Content = mModel.Console;
            UpdateRTB(richTextBox1);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            mModel.Page = ComboPages.Text;
            mModel.FileContent = new TextRange(richTextBox1.Document.ContentStart, richTextBox1.Document.ContentEnd).Text;/// Извлечь текстовое содержимое из RichTextBox
            mModel.SavePage();
            labelConsole.Content = mModel.Console;
        }

        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("PDF Files (*.pdf)|*.pdf|All Files(*.*)|*.*");
            richTextBox1.CaretPosition.InsertTextInRun($"<a href=\"/Files/{mModel.FileName}\" target=\"_blank\">Открыть файл</a>");/// Вставить текст в положение курсора
            labelConsole.Content = mModel.Console;
        }

        private void btnDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            mModel.DeleteFiles();
            labelConsole.Content = mModel.Console;
        }

        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("IMG Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png");
            richTextBox1.CaretPosition.InsertTextInRun($"<img src=\"/Files/{mModel.FileName}\" width=\"150\" \"alt=\"\" >");/// Вставить текст в положение курсора
            labelConsole.Content = mModel.Console;
        }
    }
}
