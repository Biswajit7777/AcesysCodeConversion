using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.ComponentModel;
using System.Windows;

namespace Fls.AcesysConversion.UI.CustomControls
{
    public class CodeEditor : TextEditor, INotifyPropertyChanged
    {
        private readonly XmlFoldingStrategy _foldingStrategy;
        private readonly FoldingManager _foldingManager;

        public CodeEditor() : base()
        {
            _foldingManager = FoldingManager.Install(base.TextArea);
            _foldingStrategy = new XmlFoldingStrategy();
            base.IsReadOnly = true;
            TextArea.TextView.BackgroundRenderers.Add(new HighlightCurrentLineBackgroundRenderer(this));
        }

        public new string Text
        {
            get => (string)GetValue(TextProperty);
            set
            {
                SetValue(TextProperty, value);
                _foldingStrategy?.UpdateFoldings(_foldingManager, base.Document);
                RaisePropertyChanged("Text");
            }
        }

        public int CurrentLine
        {
            get => (int)GetValue(CurrentLineProperty);
            set
            {
                SetValue(CurrentLineProperty, value);
                RaisePropertyChanged("CurrentLine");
            }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text",
                typeof(string),
                typeof(CodeEditor),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = default(string),
                    BindsTwoWayByDefault = true,
                    PropertyChangedCallback = OnTextDependencyPropertyChanged
                }
            );

        protected static void OnTextDependencyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CodeEditor target = (CodeEditor)obj;

            if (target.Document != null)
            {
                target.Document.Text = args.NewValue?.ToString() ?? "";
                target.CaretOffset = 0;
                target.TextArea.Caret.BringCaretToView();
            }
        }

        public static readonly DependencyProperty CurrentLineProperty =
            DependencyProperty.Register(
                "CurrentLine",
                typeof(int),
                typeof(CodeEditor),
                new FrameworkPropertyMetadata
                {
                    DefaultValue = default(int),
                    BindsTwoWayByDefault = true,
                    PropertyChangedCallback = OnCurrentLineDependencyPropertyChanged
                }
            );

        protected static void OnCurrentLineDependencyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            CodeEditor target = (CodeEditor)obj;

            if (target.Document != null)
            {
                int newValue = (int)args.NewValue;

                if (newValue <= 0)
                {
                    newValue = 0;
                }


                target.TextArea.Caret.Line = newValue;
                target.TextArea.Caret.Column = 0;
                target.ScrollToLine(newValue);
                target.TextArea.Caret.BringCaretToView();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            if (Document != null)
            {
                Text = Document.Text;
            }

            base.OnTextChanged(e);
        }

        public void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
