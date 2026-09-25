using System.Text;

namespace Budget.Scraper;

public class TimestampedLogWriter : TextWriter
{
    private const long MaxBytes = 1_000_000;

    private readonly string _path;
    private readonly TextWriter _inner;
    private readonly object _lock = new();
    private bool _atLineStart = true;

    public TimestampedLogWriter(string path, TextWriter inner)
    {
        _path = path;
        _inner = inner;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) => WriteCore(value.ToString());

    public override void Write(string? value) => WriteCore(value ?? string.Empty);

    public override void WriteLine(string? value) => WriteCore((value ?? string.Empty) + Environment.NewLine);

    private void WriteCore(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        lock (_lock)
        {
            var segments = text.Split('\n');
            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var hasNewline = i < segments.Length - 1;

                if (!hasNewline && segment.Length == 0)
                {
                    continue;
                }

                if (_atLineStart && segment.Length > 0 && !segment.StartsWith('\r'))
                {
                    RotateIfTooBig();
                    var timestamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ";
                    _inner.Write(timestamp);
                    File.AppendAllText(_path, timestamp);
                }

                var suffix = hasNewline ? "\n" : string.Empty;
                _inner.Write(segment + suffix);
                File.AppendAllText(_path, segment + suffix);

                _atLineStart = hasNewline || segment == "\r";
            }
        }
    }

    private void RotateIfTooBig()
    {
        var file = new FileInfo(_path);
        if (!file.Exists || file.Length < MaxBytes)
        {
            return;
        }

        var previous = _path + ".1";
        if (File.Exists(previous))
        {
            File.Delete(previous);
        }

        File.Move(_path, previous);
    }
}