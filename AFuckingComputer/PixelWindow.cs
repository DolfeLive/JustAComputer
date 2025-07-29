using System.Drawing.Drawing2D;

namespace Computer;

public class PixelWindow : Form
{
    private Bitmap displayCanvas;
    private GPU gpu;
    private Input input;
    private System.Windows.Forms.Timer refreshTimer;
    private const int PIXEL_SIZE = 8;
    private const int SCREEN_WIDTH = 64;
    private const int SCREEN_HEIGHT = 64;

    public PixelWindow(GPU gpu, Input input)
    {
        this.gpu = gpu;
        this.input = input;
        this.Text = "Display, bitch";

        int clientWidth = SCREEN_WIDTH * PIXEL_SIZE;
        int clientHeight = SCREEN_HEIGHT * PIXEL_SIZE;

        this.ClientSize = new Size(clientWidth, clientHeight);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.KeyPreview = true;
        this.Focus();

        displayCanvas = new Bitmap(clientWidth, clientHeight);

        this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);

        refreshTimer = new System.Windows.Forms.Timer();
        refreshTimer.Interval = 16; // ~60 FPS
        refreshTimer.Tick += (s, e) => RefreshDisplay();
        refreshTimer.Start();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        input.OnKeyPressed(e.KeyCode);
        
        e.Handled = true;
        //base.OnKeyDown(e);
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        this.Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.DrawImage(displayCanvas, 0, 0);
    }

    private void RefreshDisplay()
    {
        if (gpu.VideoMemoryDirty)
        {
            using (Graphics g = Graphics.FromImage(displayCanvas))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                for (int y = 0; y < SCREEN_HEIGHT; y++)
                {
                    for (int x = 0; x < SCREEN_WIDTH; x++)
                    {
                        var color = gpu.GetPixelColor(x, y);
                        using (var brush = new SolidBrush(color))
                        {
                            g.FillRectangle(brush,
                                x * PIXEL_SIZE,
                                y * PIXEL_SIZE,
                                PIXEL_SIZE,
                                PIXEL_SIZE);
                        }
                    }
                }
            }
            this.Invalidate();
            gpu.VideoMemoryDirty = false;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        refreshTimer?.Stop();
        refreshTimer?.Dispose();
        displayCanvas?.Dispose();
        base.OnFormClosed(e);
    }
}
