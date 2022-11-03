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
using AdminPanelWPF.SyntaxPaint;
using System.Threading;
using System.Dynamic;
using System.Windows.Automation;
using System.Windows.Threading;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Net;
using System.Numerics;

namespace AdminPanelWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MainModel mModel = new MainModel();
        //SyntaxPaint syntax = new SyntaxPaint();
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
            Timer();
        }

        private void btnLoadFile_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("PDF Files (*.pdf)|*.pdf|All Files(*.*)|*.*");
            richTextBox1.CaretPosition.InsertTextInRun($"<a href=\"/Files/{mModel.FileName}\" target=\"_blank\">Открыть файл</a>");/// Вставить текст в положение курсора

            //TextRange textRange = new TextRange(richTextBox1.Document.ContentEnd, richTextBox1.Document.ContentEnd);
            //textRange.Text = $"<a href=\"/Files/{mModel.FileName}\" target=\"_blank\">Открыть файл</a>";
            //textRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            labelConsole.Content = mModel.Console;
            Timer();
        }

        private async void btnDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            btnDeleteFiles.IsEnabled = false;
            await Task.Run(() => mModel.DeleteFiles());
            labelConsole.Content = mModel.Console;
            Timer();
            btnDeleteFiles.IsEnabled = true;
        }
        private void Process()
        {
            //string links = mModel.ReadAllPages();
            //List<string> files = mModel.CollectionFile();
            //Dispatcher.Invoke(new Action(() => {

            //    int count = 0;
            //    progressBar1.Maximum = files.Count;
            //    foreach (var item in files)
            //    {
            //        Thread.Sleep(100);
            //        mModel.DeleteFiles(links, item);
            //        progressBar1.Value = count++;
            //    }              
            //}));
        }
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            mModel.OpenFile("IMG Files (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png");
            richTextBox1.CaretPosition.InsertTextInRun($"<img src=\"/Files/{mModel.FileName}\" width=\"150\" \"alt=\"\" >");/// Вставить текст в положение курсора
            labelConsole.Content = mModel.Console;
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
            labelConsole.Content = "Console";
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }










        #region SyntaxPaint
        public async void UpdateRTB(RichTextBox richTextBox)
        {
            richTextBox.IsEnabled = false;
            var doc = richTextBox.Document;
            await UpdateAllParagraphs(GetParagraphs(doc.Blocks).ToList());
            richTextBox.IsEnabled = true;
        }
        IEnumerable<Paragraph> GetParagraphs(BlockCollection blockCollection)
        {
            foreach (var block in blockCollection)
            {
                var para = block as Paragraph;
                if (para != null)
                {
                    yield return para;
                }
                else
                {
                    foreach (var innerPara in GetParagraphs(block.SiblingBlocks))
                        yield return innerPara;
                }
            }
        }
        async Task UpdateParagraph(Paragraph par)
        {
            var completeTextRange = new TextRange(par.ContentStart, par.ContentEnd);
            completeTextRange.ClearAllProperties();
            await UpdateInlines(par.Inlines);
        }
        async Task UpdateAllParagraphs(IEnumerable<Paragraph> paragraphs)
        {
            var materialParagraphs = paragraphs.ToList();
            if (materialParagraphs.Count == 0)
                return;
            var completeTextRange = new TextRange(materialParagraphs.First().ContentStart,
                                                  materialParagraphs.Last().ContentEnd);
            completeTextRange.ClearAllProperties();
            await UpdateInlines(materialParagraphs.SelectMany(par => par.Inlines));
        }
        async Task UpdateInlines(IEnumerable<Inline> inlines)
        {
            var texts = ExtractText(inlines);
            var positionsAndBrushes =
                (from qualifiedToken in await Lexer.Parse(texts)
                 let brush = GetBrushForTokenType(qualifiedToken.Type)
                 where brush != null
                 let start = qualifiedToken.StartPosition.GetPositionAtOffset(qualifiedToken.StartOffset)
                 let end = qualifiedToken.EndPosition.GetPositionAtOffset(qualifiedToken.EndOffset)
                 let position = new TextRange(start, end)
                 select new { position, brush }).ToList();

            foreach (var pb in positionsAndBrushes)
                pb.position.ApplyPropertyValue(TextElement.ForegroundProperty, pb.brush);
        }
        Brush GetBrushForTokenType(TokenType tokenType) =>
            tokenType switch
            {
                TokenType.Comment => Brushes.Brown,
                TokenType.Keyword => Brushes.OrangeRed,
                TokenType.Number => Brushes.Blue,
                TokenType.Punct => Brushes.Green,
                TokenType.String => Brushes.DarkRed,
                _ => null
            };
        IEnumerable<RawText> ExtractText(IEnumerable<Inline> inlines) =>
            inlines.SelectMany(inline => inline switch
            {
                Run run => new[] { new RawText() { Text = run.Text, Start = run.ContentStart } },
                LineBreak br => new[] { new RawText() { Text = "\n", Start = br.ContentStart } },
                Span span => ExtractText(span.Inlines),
                _ => Enumerable.Empty<RawText>()
            });
        #endregion
    }
}
