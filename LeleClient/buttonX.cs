using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace LeleClient
{
    public class ButtonX : Button
    {
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            base.OnPaint(e);
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, this.Width, this.Height);
            this.Region = new Region(path);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            Graphics g = this.CreateGraphics();
            g.DrawEllipse(new Pen(Color.Blue), 0, 0, this.Width, this.Height);
            g.Dispose();
        }
    }
}
