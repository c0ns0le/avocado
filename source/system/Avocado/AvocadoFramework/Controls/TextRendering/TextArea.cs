﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UtilityLib.WPF;

namespace AvocadoFramework.Controls.TextRendering
{
    public abstract class TextArea : BaseControl
    {
        static TextArea()
        {
            // Associate this window with the default theme.
            var frameType = typeof(TextArea);
            DefaultStyleKeyProperty.OverrideMetadata(
                frameType,
                new FrameworkPropertyMetadata(frameType));
        }

        protected RichTextBox TextBase 
            => this.GetTemplateElement<RichTextBox>("textBase");

        protected void Write(string text, Brush foreground)
        {
            var contentEnd = TextBase.Document.ContentEnd;
            var range = new TextRange(contentEnd, contentEnd) { Text = text };
            range.ApplyPropertyValue(
                TextElement.ForegroundProperty, 
                foreground);
            MoveToDocumentEnd();

            SetDefaultForeground();
        }

        protected void WriteLine()
        {
            TextBase.AppendText(Environment.NewLine);
            MoveToDocumentEnd();
        }

        protected void WriteLine(string text, Brush foreground)
        {
            Write(text, foreground);
            WriteLine();
        }

        protected void SetDefaultForeground()
            => TextBase.Selection.ApplyPropertyValue(
                TextElement.ForegroundProperty,
                Foreground);

        protected void MoveToDocumentEnd()
            => TextBase.CaretPosition = TextBase.CaretPosition.DocumentEnd;

        protected int CaretX
        {
            get
            {
                var current = TextBase.CaretPosition;
                var lineStart = current.Paragraph.ContentStart;
                return Math.Max(0, lineStart.GetOffsetToPosition(current) - 1);
            }
        }

        protected string CurrentLineString
        {
            get
            {
                var caretPos = TextBase.CaretPosition;
                var lineStart = caretPos.Paragraph.ContentStart;
                var lineEnd = caretPos.Paragraph.ContentEnd;
                return new TextRange(lineStart, lineEnd).Text;
            }
        }
    }
}