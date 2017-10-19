using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CustomListView_Demo
{
    class listv :ListView 
    {
        // 自定义事件
        public delegate void EditOk(int row,int col,string str);
        public event EditOk listvEditOk;

        internal struct rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        internal class Win32
        {          
            public const int LVM_GETSUBITEMRECT = (0x1000) + 56;
            
            public const int LVIR_BOUNDS = 0;
        
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int SendMessage(IntPtr hWnd, int messageID, int wParam, ref rect lParam);
        }

        private int col = -1;
        private int row = -1;

        private TextBox text = new TextBox();
        private bool mouseDown = false;

        public listv()
        {
            this.initcomponent();
        }

        public void initcomponent()
        {
            this.text.Visible = false;
            text.BorderStyle = BorderStyle.FixedSingle;
            this.text.Leave += new EventHandler(textBox_Leave);
            this.Controls.Add(this.text);        
        }

        private rect getrect(Point po)
        {
            rect rect1 = new rect();
            this.row = this.col = -1;
            ListViewItem lvi = this.GetItemAt(po.X ,po.Y );
            if (lvi !=null)
            {
                for (int i = 0; i <= this.Columns .Count ;i++ )
                {
                    rect1.top = i + 1;
                    rect1.left = Win32.LVIR_BOUNDS;
                    try
                    {
                        int result = Win32.SendMessage(this.Handle ,Win32 .LVM_GETSUBITEMRECT ,lvi.Index ,ref rect1 );
                        if (result !=0)
                        {
                            if (po.X < rect1 .left )
                            {
                                this.row = lvi.Index;
                                this.col = 0;
                                break;
                            }
                            if (po.X >=rect1 .left && po.X <=rect1.right  )
                            {
                                this.row = lvi .Index ;
                                this.col = i+1;
                                break;
                            }
                        }
                        else
                        {
                            // This call will create a new Win32Exception with the last Win32 Error.
                            throw new Win32Exception();
                        }
                    }catch (Win32Exception ex)
                    {
                    }
                }
            }
            return rect1;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            try
            {
                this.text.Visible = false;
                if (!mouseDown)
                    return;
                if (!this.FullRowSelect || this.View != View.Details)
                {
                    return;
                }
                mouseDown = false;
                rect rect2 = this.getrect(new Point(e.X, e.Y));
                if (this.row != -1 && this.col != -1)
                {
                    Size sz = new Size(this.Columns[col].Width, Items[row].Bounds.Height);
                    Point po1 = col == 0 ? new Point(0, rect2.top) : new Point(rect2.left, rect2.top);
                    this.showtext(po1, sz);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void showtext(Point location,Size sz)
        {
            text.Size = sz;
            text.Location = location;
            text.Text = this.Items[row].SubItems[col].Text;
            text.Show();
            text.Focus();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            try
            {
                // Mouse down happened inside listview
                mouseDown = true;
                // Hide the controls
                this.text.Hide();                
            }
            catch (Exception ex)
            {
                
            }
        }

        private void textBox_Leave(object sender, EventArgs e)
        {
            try
            {
                if (this.row != -1 && this.col != -1)
                {
                    //this.Items[row].SubItems[col].Text = this.text.Text;
                    this.text.Hide();
                    // 触发事件
                    listvEditOk(row,col, this.text.Text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
