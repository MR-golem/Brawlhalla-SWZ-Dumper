using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BrawlhallaDumperGUI;

public sealed class TextBoxWriter : TextWriter
{
    private readonly TextBox _textBox;

    public TextBoxWriter(TextBox textBox)
    {
        _textBox = textBox;
    }

    public override void Write(string? value)
    {
        if (value != null)
            AppendText(value);
    }

    public override void WriteLine(string? value)
    {
        AppendText(value + Environment.NewLine);
    }

    public override void WriteLine()
    {
        AppendText(Environment.NewLine);
    }

    public override Encoding Encoding => Encoding.UTF8;

    private void AppendText(string value)
    {
        if (_textBox.InvokeRequired)
        {
            _textBox.Invoke((MethodInvoker)(() => _textBox.AppendText(value)));
        }
        else
        {
            _textBox.AppendText(value);
            _textBox.ScrollToCaret();
        }
    }
}
