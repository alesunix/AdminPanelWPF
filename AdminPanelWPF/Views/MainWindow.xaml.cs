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
using System.Windows.Controls.Primitives;
using static AdminPanelWPF.Models.MainModel;

namespace AdminPanelWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainModel mModel = new MainModel();
        private EditMenu flag;
        public MainWindow()
        {
            Config.CreateConfig();
            InitializeComponent();
            this.Title += Version.Ver;

            ComboPages.ItemsSource = mModel.ListPages();
            ComboPages.SelectionChanged += ComboPages_SelectionChanged;
        }
        private void ComboPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            richTextBox1.Document.Blocks.Clear();
            mModel.Page = ComboPages.SelectedItem.ToString();
            richTextBox1.Document.Blocks.Add(new Paragraph(new Run(mModel.ReadPage())));

            Regex reg = new Regex(@"[;+\-\*/\{}:<>]|#|title|keywords|description|template|page_blocks|Содержание страницы|EOF", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var start = richTextBox1.Document.ContentStart;
            while (start != null && start.CompareTo(richTextBox1.Document.ContentEnd) < 0)
            {
                if (start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    var match = reg.Match(start.GetTextInRun(LogicalDirection.Forward));
                    var textrange = new TextRange(start.GetPositionAtOffset(match.Index, LogicalDirection.Forward), start.GetPositionAtOffset(match.Index + match.Length, LogicalDirection.Backward));
                    textrange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkGreen);
                    start = textrange.End;
                }
                start = start.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
        private void btnEditMenu_Click(object sender, RoutedEventArgs e)
        {
            var btnName = (sender as Button).Name;
            switch (btnName)
            {
                case "btnB": flag = EditMenu.Bold; break;
                case "btnI": flag = EditMenu.Italic; break;
                case "btnU": flag = EditMenu.Underlined; break;
                case "btnP": flag = EditMenu.Paragraph; break;
            }
            TextEdit(flag);
        }
        void TextEdit(EditMenu flag)
        {
            StringBuilder tag = new StringBuilder();
            switch (flag)
            {
                case EditMenu.Bold: tag.Append('b'); break;
                case EditMenu.Italic: tag.Append('i'); break;
                case EditMenu.Underlined: tag.Append('u'); break;
                case EditMenu.Paragraph: tag.Append('p'); break;
            }
            string selected = richTextBox1.Selection.Text;
            richTextBox1.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.OrangeRed);
            richTextBox1.Selection.Text = String.Empty;
            richTextBox1.CaretPosition.InsertTextInRun($"<{tag}>{selected}</{tag}>");
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            mModel.Page = ComboPages.Text;
            mModel.FileContent = new TextRange(richTextBox1.Document.ContentStart, richTextBox1.Document.ContentEnd).Text;/// Извлечь текстовое содержимое из RichTextBox          
            if (MessageBox.Show("Вы действительно хотите сохранить страницу?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                mModel.SavePage();
            }
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
        }
        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            if (mModel.OpenFile("PDF Files (*.pdf)|*.pdf|All Files(*.*)|*.*") == true)
            {
                richTextBox1.CaretPosition.InsertTextInRun($"<a href=\"/Files/{mModel.FileName}\" target=\"_blank\">Открыть файл</a>");/// Вставить текст в положение курсора
                MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                Timer();
            }
        }
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            if (mModel.OpenFile("IMG Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png") == true)
            {
                richTextBox1.CaretPosition.InsertTextInRun($"<img src=\"/Files/{mModel.FileName}\" width=\"150\" \"alt=\"\" >");/// Вставить текст в положение курсора
                MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
                Timer();
            }
        }
        private void btnLineBreak_Click(object sender, RoutedEventArgs e)// Перенос строки
        {
            richTextBox1.CaretPosition.InsertTextInRun("<br>");
        }
        private void btnLine_Click(object sender, RoutedEventArgs e)// Горизонтальная линия
        {
            richTextBox1.CaretPosition.InsertTextInRun("<hr>");
        }
        
        private void btnAlignment_Click(object sender, RoutedEventArgs e)
        {
            var btnName = (sender as Button).Name;
            switch (btnName)
            {
                case "btnLeftSide": flag = EditMenu.Left; break;
                case "btnRightSide": flag = EditMenu.Right; break;
                case "btnCenterSide": flag = EditMenu.Center; break;
                case "btnJustify": flag = EditMenu.Justify; break;
            }
            Alignment(flag);
        }
        void Alignment(EditMenu flag)
        {
            StringBuilder alignment = new StringBuilder();
            switch (flag)
            {
                case EditMenu.Left: alignment.Append("left"); break;
                case EditMenu.Right: alignment.Append("right"); break;
                case EditMenu.Center: alignment.Append("center"); break;
                case EditMenu.Justify: alignment.Append("justify"); break;
            }
            string selected = richTextBox1.Selection.Text;
            richTextBox1.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkSlateBlue);
            richTextBox1.Selection.Text = String.Empty;
            richTextBox1.CaretPosition.InsertTextInRun($"<div class=\"t-{alignment}\">{selected}</div>");
        }
        private async void btnDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            btnDeleteFiles.IsEnabled = false;
            await Task.Run(() => mModel.DeleteFiles());
            MessageBox.Show(mModel.Console, "Внимание!", MessageBoxButton.OK, MessageBoxImage.Information);
            Timer();
            btnDeleteFiles.IsEnabled = true;
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
            var sInfo = new ProcessStartInfo("https://alesunix.github.io/")
            {
                UseShellExecute = true,
            };
            Process.Start(sInfo);
        }


    }
}
