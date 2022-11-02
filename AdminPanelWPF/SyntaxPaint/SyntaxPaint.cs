﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace AdminPanelWPF.SyntaxPaint
{
    public class SyntaxPaint
    {
        public async void UpdateRTB(RichTextBox richTextBox)
        {
            richTextBox.IsEnabled = false;
            var doc = richTextBox.Document;
            //foreach (var par in GetParagraphs(doc.Blocks).ToList())
            //    await UpdateParagraph(par);
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
                TokenType.Comment => Brushes.LightGray,
                TokenType.Keyword => Brushes.OrangeRed,
                TokenType.Number => Brushes.Cyan,
                TokenType.Punct => Brushes.Gray,
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
    }
}
