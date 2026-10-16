using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;

namespace OpenSSTP.Windows;

public sealed class MainForm : Form
{
    TextBox host = new() { PlaceholderText = "vpn.example.com", Width = 280 };
    TextBox user = new() { PlaceholderText = "Username", Width = 280 };
    TextBox pass = new() { PlaceholderText = "Password", Width = 280, UseSystemPasswordChar = true };
    Label state = new() { Text = "NOT CONNECTED", AutoSize = true, ForeColor = Color.FromArgb(255, 190, 90), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    Label rx = new() { Text = "↓ 0 KB/s", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 17) };
    Label tx = new() { Text = "↑ 0 KB/s", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 17) };
    Label note = new() { AutoSize = false, Height = 52, Dock = DockStyle.Bottom, ForeColor = Color.FromArgb(255, 205, 130), TextAlign = ContentAlignment.MiddleLeft };
    TrafficChart chart = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(19, 25, 36) };
    System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    long lastIn, lastOut;

    public MainForm()
    {
        Text = "Open-SSTP for Windows — Preview"; MinimumSize = new Size(760, 650); Size = new Size(900, 760);
        BackColor = Color.FromArgb(13, 18, 28); ForeColor = Color.White; Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 205)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        Controls.Add(root);
        var title = new Label { Text = "Open-SSTP", Font = new Font("Segoe UI Semibold", 25), AutoSize = true, ForeColor = Color.White, Dock = DockStyle.Fill };
        root.Controls.Add(title, 0, 0);
        var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(24, 32, 46), Padding = new Padding(18) };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        var fields = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        fields.Controls.Add(MakeField("SERVER / HOST", host)); fields.Controls.Add(MakeField("USERNAME", user)); fields.Controls.Add(MakeField("PASSWORD", pass));
        details.Controls.Add(fields, 0, 0);
        var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12, 4, 0, 0), WrapContents = false };
        statusPanel.Controls.Add(new Label { Text = "CONNECTION STATUS", AutoSize = true, ForeColor = Color.FromArgb(155, 170, 190) }); statusPanel.Controls.Add(state);
        var connect = new Button { Text = "Connect", Width = 170, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 119, 235), ForeColor = Color.White, Margin = new Padding(0, 14, 0, 0) };
        connect.FlatAppearance.BorderSize = 0; connect.Click += (_, _) => ShowEngineNotice(); statusPanel.Controls.Add(connect);
        details.Controls.Add(statusPanel, 1, 0); root.Controls.Add(details, 0, 1);
        var graphPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 18, 0, 0) };
        graphPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); graphPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var metrics = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        metrics.Controls.Add(rx); metrics.Controls.Add(new Label { Width = 28 }); metrics.Controls.Add(tx); graphPanel.Controls.Add(metrics, 0, 0); graphPanel.Controls.Add(chart, 0, 1); root.Controls.Add(graphPanel, 0, 2);
        note.Text = "Preview only: SSTP tunnel engine is not yet ported. No VPN connection will be started by this build."; root.Controls.Add(note, 0, 3);
        timer.Tick += (_, _) => UpdateTraffic(); timer.Start();
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces()) { var s = n.GetIPStatistics(); lastIn += s.BytesReceived; lastOut += s.BytesSent; }
    }

    Control MakeField(string caption, TextBox box) { var panel = new FlowLayoutPanel { Width = 330, Height = 58, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 0, 0, 3) }; panel.Controls.Add(new Label { Text = caption, AutoSize = true, ForeColor = Color.FromArgb(155, 170, 190), Font = new Font("Segoe UI", 8, FontStyle.Bold) }); box.BackColor = Color.FromArgb(13, 18, 28); box.ForeColor = Color.White; box.BorderStyle = BorderStyle.FixedSingle; panel.Controls.Add(box); return panel; }
    void ShowEngineNotice() { MessageBox.Show("The original project uses Android VpnService to create the tunnel. The SSTP/PPP protocol code is included in reference/ for porting, but the Windows tunnel adapter and engine integration are not implemented yet. This preview cannot connect to a VPN.", "Engine not ready", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    void UpdateTraffic() { long input = 0, output = 0; foreach (var n in NetworkInterface.GetAllNetworkInterfaces()) { try { var s = n.GetIPStatistics(); input += s.BytesReceived; output += s.BytesSent; } catch { } } long di = Math.Max(0, input - lastIn), dout = Math.Max(0, output - lastOut); lastIn = input; lastOut = output; rx.Text = $"↓ {Fmt(di)}"; tx.Text = $"↑ {Fmt(dout)}"; chart.Add(di, dout); }
    static string Fmt(long b) => b >= 1024 * 1024 ? $"{b / 1024d / 1024:0.00} MB/s" : $"{b / 1024d:0.0} KB/s";
}

public sealed class TrafficChart : Control
{
    readonly Queue<double> incoming = new(), outgoing = new();
    public TrafficChart() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
    public void Add(long rx, long tx) { incoming.Enqueue(rx / 1024d); outgoing.Enqueue(tx / 1024d); while (incoming.Count > 90) incoming.Dequeue(); while (outgoing.Count > 90) outgoing.Dequeue(); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.Clear(BackColor);
        using var grid = new Pen(Color.FromArgb(42, 55, 73)); for (int i = 1; i < 5; i++) e.Graphics.DrawLine(grid, 0, Height * i / 5, Width, Height * i / 5);
        Draw(incoming, Color.FromArgb(72, 170, 255), e.Graphics); Draw(outgoing, Color.FromArgb(63, 220, 164), e.Graphics);
        using var font = new Font("Segoe UI", 8); using var brush = new SolidBrush(Color.FromArgb(155, 170, 190)); e.Graphics.DrawString("Traffic history (system-wide network counters; not VPN-specific)", font, brush, 8, 8);
    }
    void Draw(IEnumerable<double> vals, Color color, Graphics g) { var a = vals.ToArray(); if (a.Length < 2) return; double max = Math.Max(10, a.Max()) * 1.15; var pts = a.Select((v, i) => new PointF(i * (Width - 2f) / Math.Max(1, a.Length - 1), Height - 8 - (float)(v / max * (Height - 24)))).ToArray(); using var pen = new Pen(color, 2.2f); g.DrawLines(pen, pts); }
}
