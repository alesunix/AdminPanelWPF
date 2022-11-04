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
using System.Threading;
using System.Dynamic;
using System.Windows.Automation;
using System.Windows.Threading;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml;

namespace AdminPanelWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainModel mModel = new MainModel();
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
            //UpdateRTB(richTextBox1);
            Regex reg = new Regex(@"[;+\-\*/\{}:<>]|#|title|keywords|description|template|page_blocks|Содержание страницы|EOF", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var start = richTextBox1.Document.ContentStart;
            while (start != null && start.CompareTo(richTextBox1.Document.ContentEnd) < 0)
            {
                if (start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    var match = reg.Match(start.GetTextInRun(LogicalDirection.Forward));
                    var textrange = new TextRange(start.GetPositionAtOffset(match.Index, LogicalDirection.Forward), start.GetPositionAtOffset(match.Index + match.Length, LogicalDirection.Backward));
                    textrange.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.DarkGreen));
                    start = textrange.End; 
                }
                start = start.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        private void btnB_Click(object sender, RoutedEventArgs e)// Жирный шрифт
        {
            //richTextBox1.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            TextEdit('b');
        }
        private void btnI_Click(object sender, RoutedEventArgs e)// Курсив
        {
            //richTextBox1.Selection.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
            TextEdit('i');
        }
        private void btnU_Click(object sender, RoutedEventArgs e)// Подчеркнутый
        {
            //richTextBox1.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, TextDecorations.Underline);
            TextEdit('u');
        }
        private void btnP_Click(object sender, RoutedEventArgs e)// Параграф
        {
            TextEdit('p');
        }
        void TextEdit(char tag)
        {           
            string selectedText = richTextBox1.Selection.Text;
            int count = selectedText.Count();
            richTextBox1.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.OrangeRed);
            richTextBox1.Selection.Text = String.Empty;
            richTextBox1.CaretPosition.InsertTextInRun($"<{tag}>{selectedText}</{tag}>");
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            mModel.Page = ComboPages.Text;
            mModel.FileContent = new TextRange(richTextBox1.Document.ContentStart, richTextBox1.Document.ContentEnd).Text;/// Извлечь текстовое содержимое из RichTextBox
            mModel.SavePage();
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
        }

        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("PDF Files (*.pdf)|*.pdf|All Files(*.*)|*.*");
            richTextBox1.CaretPosition.InsertTextInRun($"<a href=\"/Files/{mModel.FileName}\" target=\"_blank\">Открыть файл</a>");/// Вставить текст в положение курсора
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
        }

        private async void btnDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            btnDeleteFiles.IsEnabled = false;
            await Task.Run(() => mModel.DeleteFiles());
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
            btnDeleteFiles.IsEnabled = true;
        }
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("IMG Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png");
            richTextBox1.CaretPosition.InsertTextInRun($"<img src=\"/Files/{mModel.FileName}\" width=\"150\" \"alt=\"\" >");/// Вставить текст в положение курсора
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
        }
        void Timer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 7);
            timer.Start();
            timer.Tick += new EventHandler(InitialState);
        }
        void InitialState(object sender, EventArgs e)
        {
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }
        private void Hyperlink_Request(object sender, RequestNavigateEventArgs e)
        {
            //System.Diagnostics.Process.Start("https://alesunix.github.io/");
            //System.Diagnostics.Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            //e.Handled = true;
        }

    }
}
