using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows;
using System.Windows.Media;

namespace Fls.AcesysConversion.UI.CustomControls
{
    public class HighlightCurrentLineBackgroundRenderer : IBackgroundRenderer
    {
        private readonly TextEditor _editor;

        public HighlightCurrentLineBackgroundRenderer(TextEditor editor)
        {
            _editor = editor;
        }

        public KnownLayer Layer => KnownLayer.Caret;

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_editor.Document == null)
            {
                return;
            }

            try
            {
                textView.EnsureVisualLines();
                ICSharpCode.AvalonEdit.Document.DocumentLine currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset);
                foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, currentLine))
                {
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0xFF)), null,
                        new Rect(rect.Location, new Size(textView.ActualWidth - 2, rect.Height)));
                }

                //_editor.ScrollToLine(currentLine.LineNumber);
                //_editor.TextArea.Caret.BringCaretToView();

            }
            catch { }
        }
    }
}
