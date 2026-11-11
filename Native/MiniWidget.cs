using System.Windows.Forms;
using System.Drawing;

namespace ORBIT;

internal sealed class MiniWidget : Form
{
    private readonly Label title = new() { Text = "◉  ORBIT  /  LIVE SYSTEM", Dock = DockStyle.Top, Height=38, Font=new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor=Color.FromArgb(123,222,255), TextAlign=ContentAlignment.MiddleCenter };
    private readonly Label stats = new() { Text = "Sampling performance…", Dock=DockStyle.Fill, Font=new Font("Consolas", 12), ForeColor=Color.WhiteSmoke, TextAlign=ContentAlignment.MiddleCenter };
    private readonly System.Windows.Forms.Timer timer = new() { Interval=2200 };
    private bool dragging;
    private Point origin;
    internal MiniWidget()
    {
        Text="ORBIT Performance"; FormBorderStyle=FormBorderStyle.None; BackColor=Color.FromArgb(15,26,44);
        Size=new Size(270,116); ShowInTaskbar=false; TopMost=true; Opacity=0.94;
        StartPosition=FormStartPosition.Manual;
        Location=new Point(Screen.PrimaryScreen!.WorkingArea.Right-290,Screen.PrimaryScreen.WorkingArea.Bottom-135);
        Controls.Add(stats);Controls.Add(title);
        foreach (Control c in new Control[]{this, title, stats})
        {
            c.MouseDown += (_,e)=>{if(e.Button==MouseButtons.Left){dragging=true;origin=Cursor.Position;}};
            c.MouseMove += (_,_)=>{if(!dragging)return;var now=Cursor.Position;Location=new Point(Location.X+now.X-origin.X,Location.Y+now.Y-origin.Y);origin=now;};
            c.MouseUp += (_,_)=>dragging=false;
        }
        timer.Tick += (_,_) => UpdateStats();
        Shown += (_,_) => {timer.Start(); UpdateStats();};
        FormClosed += (_,_) => timer.Dispose();
    }
    private void UpdateStats()
    {
        try
        {
            string json = System.Text.Json.JsonSerializer.Serialize(WindowsControl.Snapshot());
            using var doc=System.Text.Json.JsonDocument.Parse(json);
            var root=doc.RootElement;
            string cpu=root.GetProperty("cpu").ValueKind==System.Text.Json.JsonValueKind.Null?"…":root.GetProperty("cpu").ToString()+"%";
            string ram=root.GetProperty("ram").ToString()+"%";
            stats.Text=$"CPU  {cpu}      RAM  {ram}";
        }
        catch { stats.Text="Performance unavailable"; }
    }
}
