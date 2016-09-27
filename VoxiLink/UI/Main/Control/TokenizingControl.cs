using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace VoxiLink
{
    public class TokenizingControl : RichTextBox
    {
        public static readonly DependencyProperty TokenTemplateProperty =
            DependencyProperty.Register("TokenTemplate", typeof(DataTemplate), typeof(TokenizingControl));

        public DataTemplate TokenTemplate
        {
            get { return (DataTemplate)GetValue(TokenTemplateProperty); }
            set { SetValue(TokenTemplateProperty, value); }
        }

        public List<string> tokens;
            
        public Func<string, object> TokenMatcher { get; set; }

        public TokenizingControl()
        {
            tokens = new List<string>();
            TextChanged += OnTokenTextChanged;
        }

        public void OnTokenTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = CaretPosition.GetTextInRun(LogicalDirection.Backward);
            if (TokenMatcher != null)
            {
                var token = TokenMatcher(text);
                if (token != null)
                {
                    ReplaceTextWithToken(text, token);
                    tokens.Add(token.ToString());
                }
            }
        }

        public void ReplaceTextWithToken(string inputText, object token)
        {
            // Remove the handler temporarily as we will be modifying tokens below, causing more TextChanged events
            TextChanged -= OnTokenTextChanged;

            var para = CaretPosition.Paragraph;

            var matchedRun = para.Inlines.FirstOrDefault(inline =>
            {
                var run = inline as Run;
                return (run != null && run.Text.EndsWith(inputText));
            }) as Run;
            if (matchedRun != null) // Found a Run that matched the inputText
            {
                var tokenContainer = CreateTokenContainer(inputText, token);
                para.Inlines.InsertBefore(matchedRun, tokenContainer);

                // Remove only if the Text in the Run is the same as inputText, else split up
                if (matchedRun.Text == inputText)
                {
                    para.Inlines.Remove(matchedRun);
                }
                else // Split up
                {
                    var index = matchedRun.Text.IndexOf(inputText) + inputText.Length;
                    var tailEnd = new Run(matchedRun.Text.Substring(index));
                    para.Inlines.InsertAfter(matchedRun, tailEnd);
                    para.Inlines.Remove(matchedRun);
                }
            }

            TextChanged += OnTokenTextChanged;
        }

        private Dictionary<int, object> dic = new Dictionary<int, object>();


        private InlineUIContainer CreateTokenContainer(string inputText, object token)
        {
            // Note: we are not using the inputText here, but could be used in future

            var presenter = new ContentPresenter()
            {
                Content = token,
                ContentTemplate = TokenTemplate,
            };

            presenter.ApplyTemplate();
            Button bt = TokenTemplate.FindName("btn_delCtc", presenter) as Button;
            bt.Click += bt_Click;

            InlineUIContainer inlin = new InlineUIContainer(presenter) { BaselineAlignment = BaselineAlignment.TextBottom };
            bt.Tag = inlin;
            // BaselineAlignment is needed to align with Run
            return inlin;
        }

        void bt_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            InlineUIContainer inputText = btn.Tag as InlineUIContainer;
            Paragraph pr = null;
            foreach (var block in this.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    var paragraph = block as Paragraph;

                    if (paragraph.Inlines.Contains(inputText))
                    {
                        pr = paragraph;
                    }
                }
            }
            pr.Inlines.Remove(inputText);
        }

        public List<string> get_tokens()
        {
            var doc = this.Document as FlowDocument;

            var range = new TextRange(doc.ContentStart, doc.ContentEnd);

            if (range.Start.Paragraph != null)

            {

                //Already in items
                tokens = range.Start.Paragraph.Inlines.OfType<InlineUIContainer>().Select(c => c.Child).OfType<ContentPresenter>().Select(c => c.Content).OfType<string>().ToList();

                //Not in item yet
                var emails2 = range.Start.Paragraph.Inlines.OfType<Run>().Select(c => c.Text).ToList();

            }

            return tokens;
        }
    }
}