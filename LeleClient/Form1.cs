using LeleServo;
using Recording;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using System.Data;
using System.Collections;

using server;

namespace LeleClient
{
    public partial class Form1 : Form
    {
        KpowerSteer kp = new KpowerSteer();
        public string[] id = new string[17];
        public static string[] zeroPosition = new string[19];
        public int[] currentPosition = new int[19];
        public int[] currentTemperature = new int[19];
        public int[] currentTorgue = new int[19];
        public int[] currentEditPosition = new int[19];
        public int currentEditAcitonNum = 0;
        public string currentEditAcitonName = null;
        public static int key;
        public static bool run = false;
        private SoundRecord recorder = null;
        private static bool link = false;//连接状态

        public int steerMaxNum = 18;//最后舵机编号18



        public Form1()
        {
            Form1.CheckForIllegalCrossThreadCalls = false;//跨线程访问允许

            InitializeComponent();

            this.actionDisplayList.listvEditOk += ActionDisplayList_listvEditOk;
            kp.kpowrException += Kp_kpowrException;
            
            // 音频设备初始化
            recorder = new SoundRecord();
            // 显示音频设备列表
            comboBox5.Items.AddRange(SoundRecord.devList);
            comboBox5.SelectedIndex = comboBox5.Items.Count > 0 ? 0 : -1;//设置当前选定项，-1表示不选           
        }


        //应用程序变量
        private IPAddress HostIP;
        private bool flag = true;
        private Socket serverSocket;
        private Socket clientSocket = null;
        private Thread _createServer;
        public static Hashtable clientList = new Hashtable();
        private int userID = 0;

        //委托函数，添加用户
        private delegate void addDelegate(string clientIp);
        private void addUser(string clientIp)
        {
            if (this.robotList.InvokeRequired)
            {
                addDelegate md = new addDelegate(this.addUser);
                this.Invoke(md, new object[] { clientIp });
            }
            else
            {
                this.robotList.Items.Add(clientIp);
            }
        }

        //委托函数，删除用户
        private delegate void removeDelegate(string clientIp);
        private void removeUser(string clientIp)
        {
            if (this.robotList.InvokeRequired)
            {
                removeDelegate md = new removeDelegate(this.removeUser);
                this.Invoke(md, new object[] { clientIp });
            }
            else
            {
                this.robotList.Items.Remove(clientIp);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string str = AppDomain.CurrentDomain.BaseDirectory;

            if (fileExist(str + "Action"))
            {
                resolveActionFile(str + "Action\\action.txt");
            }

            getSerial();
            readZeroData();            
            setuserControl1Value(1);
            setuserControl1Value(2, currentEditPosition);            
            list1AddTitle();
            //list1AddData();
            list2AddTitle();
            list2ShowActionData();
            comboBox3.SelectedIndex = 2;
            incrementBarText.Text = incrementBar.Value.ToString();
            timer3.Enabled = true;

            radioButton2.Checked = true;
            steerMaxNum = 16;


            // socket
            //启动线程，开始侦听客户端连接消息
            _createServer = new Thread(new ThreadStart(StartListening));
            _createServer.IsBackground = true;
            _createServer.Start();
        }

        // 获取本地ip地址,优先取内网ip，剔除v6地址
        public static String GetLocalIp()
        {
            String[] Ips = GetLocalIpAddress();
            foreach (String ip in Ips) if (ip.StartsWith("192.168.1.") || ip.StartsWith("192.168.0.")) return ip;
            return "127.0.0.1";
        }

        // 本机IP地址，多个
        public static String[] GetLocalIpAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] addresses = Dns.GetHostAddresses(hostName);

            string[] IP = new string[addresses.Length];
            for (int i = 0; i < addresses.Length; i++) IP[i] = addresses[i].ToString();

            return IP;
        }

        //启动服务器
        private void StartListening()
        {
            HostIP = IPAddress.Parse(GetLocalIp());
            
            try
            {
                IPEndPoint iep = new IPEndPoint(HostIP, 9004);
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(iep);
                serverSocket.Listen(100);

                while (flag)
                {
                    clientSocket = serverSocket.Accept();
                    if (clientSocket != null)
                    {
                        string str = clientSocket.RemoteEndPoint.ToString();
                        string[] Ipstr = str.Split(':');
                        string clientIp = Ipstr[0];

                        client Client;
                        Client = new client(clientSocket);
                        Client.OnClientdisConnect += new client.clientdisConnect(this.removeclient);
                        Client.OnclientMessage += new client.clientMessage(this.getClientMessage);
                        Client.IP = clientIp;

                        //将此人添加到用户列表
                        clientList.Add(clientIp, Client);

                        //更新显示
                        this.addUser(clientIp);
                    }
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        //当客户端断开连接，删除这个客户端
        private void removeclient(object sender, EventArgs e)
        {
            client Client = (client)sender;
            string clientIp = Client.IP;

            try
            {
                clientList.Remove(clientIp);
                this.removeUser(clientIp);
            }
            catch (Exception exp)
            {
                MessageBox.Show("删除用户出错：" + exp.Message);
            }
        }

        //// 给客户端发送信息
        //private void send_Btn_Click(object sender, EventArgs e)
        //{
        //    client Client = (client)clientList[userlist.SelectedItem.ToString()];
        //    Client.setMessage("inf#" + "server#" + msg.Text.Trim() + "\r");
        //}
        //// 群发给客户端
        //private void allSend_Btn_Click(object sender, EventArgs e)
        //{
        //    foreach (DictionaryEntry clientObj in clientList)
        //    {
        //        client user = (client)clientObj.Value;
        //        user.setMessage("inf#" + "server#" + msg.Text.Trim() + "\r");
        //    }
        //}

        // 判断文件是否存在
        private bool fileExist(string filepath)
        {
            if (!System.IO.Directory.Exists(filepath))
            {
                System.IO.Directory.CreateDirectory(filepath);                
            }

            if (!System.IO.File.Exists(filepath + @"\action.txt"))
            {
                String filePath = filepath + @"\action.txt";
                System.IO.StreamWriter file1 = new System.IO.StreamWriter(filePath, false);
                file1.Close();
                file1.Dispose();

                return false;
            }
            else
            {
                return true;
            }
        }


        private void readZeroData()
        {
            // 读入文件原点
            for (int i = 0; i < 17; i++)
            {
                AppSettings.Default.zeroPosition[i+2]=ResolveAction.zeroPosition[i];
            }

            AppSettings.Default.zeroPosition.CopyTo(zeroPosition, 0);
            for (int i = 0; i < 19; i++)
            {
                currentEditPosition[i]= int.Parse(zeroPosition[i]);
            }
        }
        private void resolveActionFile(string path)
        {
            ResolveAction re = new ResolveAction(path);
        }
        private void getSerial()
        {
            string[] ports = SerialPort.GetPortNames();
            Array.Sort(ports);

            comboBox4.Items.AddRange(ports);
            comboBox4.SelectedIndex = comboBox4.Items.Count > 0 ? 0 : -1;//设置当前选定项，-1表示不选

            //openCom.PerformClick();
        }
        private void list1AddTitle()
        {
            this.actionFrameDisplayList.Columns.Add("帧编号", 50, HorizontalAlignment.Center);
            for (int i = 2; i < 19; i++)
                this.actionFrameDisplayList.Columns.Add("ID" + i, 57, HorizontalAlignment.Center);

            this.actionFrameDisplayList.Columns.Add("速度", 50, HorizontalAlignment.Center);
            this.actionFrameDisplayList.Columns.Add("时间", 60, HorizontalAlignment.Center);
        }
        private void list1AddData()
        {
            this.actionFrameDisplayList.BeginUpdate();
            ListViewItem lvi = new ListViewItem();
            lvi.Text = "0";
            for (int i = 2; i < 19; i++)
                lvi.SubItems.Add(zeroPosition[i].ToString());

            lvi.SubItems.Add(speedValueInput.Value.ToString());
            lvi.SubItems.Add(intervalValueInput.Value.ToString());//时间
            this.actionFrameDisplayList.Items.Add(lvi);
            this.actionFrameDisplayList.EndUpdate();
        }
        private void list2AddTitle()
        {
            this.actionDisplayList.Columns.Add("动作名", 120, HorizontalAlignment.Center); //一步添加
            this.actionDisplayList.Columns.Add("动作号", 60, HorizontalAlignment.Center); //一步添加
        }
        private void Kp_kpowrException(string str)
        {
            this.Invoke(new MethodInvoker(delegate
            {
                servoEcho.AppendText(str.Trim() + " !!!\r\n");
            }
            ));
        }
        public void list2ShowActionData()
        {
            foreach (LeleServo.Action action in ResolveAction.actionlist)
            {
                ListViewItem lvii = new ListViewItem();

                this.actionDisplayList.BeginUpdate();

                lvii.Text = action.ActionName;//动作名
                lvii.SubItems.Add(action.ActionNum.ToString());//动作号
                this.actionDisplayList.Items.Add(lvii);

                this.actionDisplayList.EndUpdate();
            }
        }
        private void setZero_Click(object sender, EventArgs e)
        {
            if (!radioButton3.Checked && !radioButton4.Checked)
            {
                MessageBox.Show("请先选择原点的来源！");
            }
            else
            {
                MessageBoxButtons mess = MessageBoxButtons.OKCancel;
                DialogResult dr = MessageBox.Show("你确定要改变原点吗？", "提示", mess);
                if (dr == DialogResult.OK)
                {
                    if (radioButton3.Checked)
                    {
                        for (int i = 0; i < 19; i++)
                        {
                            AppSettings.Default.zeroPosition[i] = currentEditPosition[i].ToString();
                        }
                    }
                    if (radioButton4.Checked)
                    {
                        for (int i = 0; i < 19; i++)
                        {
                            AppSettings.Default.zeroPosition[i] = currentPosition[i].ToString();
                        }
                    }
                    AppSettings.Default.Save();
                    MessageBox.Show("原点设置完成，请重新打开软件！");
                }
            }
        }
        
        private void createActionFile_Click(object sender, EventArgs e)
        {
            if (actionFrameDisplayList.Items.Count == 0)
            {
                MessageBox.Show("列表为空!");
            }
            else
            {
                List<string> list = new List<string>();
                foreach (ListViewItem item in actionFrameDisplayList.Items)
                {
                    string temp = "{";
                    for (int i = 1; i < 17; i++)
                    {
                        temp += item.SubItems[i].Text + ",";
                    }
                    temp += item.SubItems[17].Text;
                    temp += "}:" + item.SubItems[18].Text;
                    temp += ":" + item.SubItems[19].Text;//时间

                    list.Add(temp);
                }
                Thread thexp = new Thread(() => export(list)) { IsBackground = true };
                thexp.Start();
            }
        }
        private void export(List<string> list)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Action" + "\\action.txt";//Guid.NewGuid().ToString()
            StringBuilder sb = new StringBuilder();
            this.Invoke((EventHandler)delegate
            {
                //打印头
                richTextBox1.Text = "";
                sb.AppendLine("Total:" + ResolveAction.actionlist.Count);
                richTextBox1.Text += "Total:" + ResolveAction.actionlist.Count + "\r\n";
                if (checkBox1.Checked)
                {
                    sb.AppendLine("Fall:on");
                    richTextBox1.Text += "Total:on\r\n";
                }
                else
                {
                    sb.AppendLine("Fall:off");
                    richTextBox1.Text += "Total:off\r\n";
                }
                string str = "Zero:";
                for (int i = 2; i < 18; i++)
                {
                    str += AppSettings.Default.zeroPosition[i] + ",";
                }
                str += AppSettings.Default.zeroPosition[18];
                sb.AppendLine(str);
                richTextBox1.Text += str + "\r\n";
                //
                foreach (LeleServo.Action action in ResolveAction.actionlist)
                {
                    //打印动作内容
                    //Num: 100
                    //Name: zero1
                    //N:2
                    sb.AppendLine("Num:" + action.ActionNum);
                    richTextBox1.Text += "Num:" + action.ActionNum + "\r\n";

                    sb.AppendLine("Name:" + action.ActionName);
                    richTextBox1.Text += "Name:" + action.ActionName + "\r\n";

                    sb.AppendLine("N:" + action.FrameCount);
                    richTextBox1.Text += "N:" + action.FrameCount + "\r\n";

                    //{}:                        
                    for (int i = 0; i < action.FrameCount; i++)
                    {
                        string s = "{";
                        Frame frame = action.KeyFrames[i];
                        for (int j = 0; j < 16; j++)
                            s += (frame[j] - float.Parse(zeroPosition[j+2])).ToString() + ",";// 值 = 绝对位置 - 原点

                        s += (frame[16] - float.Parse(zeroPosition[16+2])).ToString() + "}";
                        s += ":" + frame.Speed + ":" + frame.Duration;

                        sb.AppendLine(s);
                        richTextBox1.Text += s + "\r\n";
                    }
                }
            });
            //System.IO.File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
            System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

            MessageBox.Show("动作文件生成完毕！");
        }
        private void connectServo_Click(object sender, EventArgs e)
        {
            Thread t1 = new Thread(new ThreadStart(delegate
            {
                Connect();
            }));
            t1.IsBackground = true;
            t1.Start();
        }

        /// <summary>
        /// 保存数据data到文件处理过程，返回值为保存的文件名
        /// </summary>
        public static String SaveProcess(String data, String name, bool mode)//false覆盖，true尾随
        {
            string CurDir = name; //System.AppDomain.CurrentDomain.BaseDirectory + @"SaveDir\";    //设置当前目录
            //if (!System.IO.Directory.Exists(CurDir)) System.IO.Directory.CreateDirectory(CurDir);   //该路径不存在时，在当前文件目录下创建文件夹"导出.."

            //不存在该文件时先创建
            String filePath = CurDir + ".txt";
            System.IO.StreamWriter file1 = new System.IO.StreamWriter(filePath, mode);     //文件已覆盖方式添加内容

            file1.Write(data);                                                              //保存数据到文件

            file1.Close();                                                                  //关闭文件
            file1.Dispose();                                                                //释放对象

            return filePath;
        }

        private void gotoZero_Click(object sender, EventArgs e)
        {
            kp.changeServoMode(1000, 3);
            System.Threading.Thread.Sleep(10);
            for (int i = 2; i <= steerMaxNum; i++)
            {
                kp.moveTo(i, int.Parse(zeroPosition[i]));
            }
            setuserControl1Value(2, Array.ConvertAll<string, int>(zeroPosition, s => int.Parse(s)));            
        }
 
        private void setuserControl1Value(int c,params int[] data)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };
            if (c == 1)
            {
                for (int i = 0; i < 17; i++)
                {
                    this.Invoke((EventHandler)(delegate
                    {
                        user[i].ID = (i + 2).ToString();
                    }));
                };
                return;
            }
            else
            {
                for (int i = 0; i < data.Length-2; i++)
                {
                    this.Invoke((EventHandler)(delegate
                    {
                        switch (c)
                        {
                            case 2://位置
                                user[i].Position = (data[i + 2]).ToString();
                                break;
                            case 3://速度
                                   //user[i].Position = (data[i + 2]).ToString();
                                break;
                            case 4://偏差
                                   //user[i].Position = (data[i + 2]).ToString();
                                break;
                            case 5:
                                break;
                            default:
                                break;
                        }
                    }));
                };
            }    
        }
        public bool runs = false;
        private void runFromHead_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
                runs = true;
            }
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            Stopwatch sw = new Stopwatch();
            kp.setAcc(1000, 3000);
            System.Threading.Thread.Sleep(5);
            while (runs)
            {
                for (int i = 0; i < actionFrameDisplayList.Items.Count; i++)
                {
                    sw.Reset();
                    sw.Start();
                    float time = 0;
                    this.Invoke((EventHandler)(delegate
                    {
                        kp.setVel(1000, int.Parse(actionFrameDisplayList.Items[i].SubItems[18].Text));
                        System.Threading.Thread.Sleep(5);
                    }));

                    for (int j = 1; j < steerMaxNum; j++)
                    {
                        this.Invoke((EventHandler)(delegate
                        {
                            kp.moveTo(j + 1, int.Parse(actionFrameDisplayList.Items[i].SubItems[j].Text));

                            time = float.Parse(actionFrameDisplayList.Items[i].SubItems[19].Text) * 0.1F;//时间
                        }));
                    }

                    backgroundWorker1.ReportProgress(1);

                    while (sw.Elapsed.TotalSeconds < time)
                    {
                        System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
                    }
                }
                runs=false;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 1:
                    this.Invoke((EventHandler)(delegate
                    {
                        //textBox1.Text += "run line\r\n";
                    }));
                    break;
                default:
                    break;
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        public static void Connect()
        {
            string hostName = "DESKTOP-KCOUVE4";//小电脑
            //string hostName = "DESKTOP-RE90LJ7";//笔记本
            string ipa = null;
            //用DNS将主机名解析为IPHostEntry实例
            IPHostEntry ipHost = Dns.GetHostByName(hostName);
            //IPHostEntry ipHost = Dns.GetHostEntry(hostName);
            foreach (IPAddress ipp in ipHost.AddressList)
            {
                //从IP地址列表中筛选出IPv4类型的IP地址  
                //AddressFamily.InterNetwork表示此IP为IPv4,  
                //AddressFamily.InterNetworkV6表示此地址为IPv6类型  
                if (ipp.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipa = ipp.ToString();
                }
            }

            TcpClient client;
            //启动多个服务器
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    client = new TcpClient();
                    client.Connect(IPAddress.Parse(ipa), 8500);
                    // 是否连接成功
                    if (client.Connected)
                    {
                        link = true;
                    }
                    else
                    {
                        link = false;
                    }                
                    NetworkStream streamToServer = client.GetStream();
                    do
                    {
                        if (0 < key && key < 1000 && run)
                        {
                            string msg = key.ToString();
                            //发送信息
                            byte[] buffer = Encoding.UTF8.GetBytes(msg);//获得缓存
                            streamToServer.Write(buffer, 0, buffer.Length);//发往服务器
                            run = false;
                        }
                    } while (true);
                }
                catch (Exception ex)
                {
                    link = false;
                    return;
                }
            }
        }
        private void sendActionNum_Click(object sender, EventArgs e)
        {
            if (link)
            {
                key = int.Parse(textBox4.Text == "" ? "100" : textBox4.Text);
                run = true;
            }
            else
            {
                MessageBox.Show("请先建立链接！");
            }
        }
        private void userControl117_UserControlValueChanged(object sender, usercontrol.UserControl1.TextChangeEventArgs e)
        {
            usercontrol.UserControl1 user = (usercontrol.UserControl1)sender;

            user.V = int.Parse(incrementBar.Value.ToString());

            currentEditPosition[int.Parse(user.ID)] = int.Parse(e.Message);

            if (user.Selected)
            {
                ///kp.moveTo(int.Parse(user.ID), currentEditPosition[int.Parse(user.ID)]);
                writeServoData_Tcp(1, (byte)(int.Parse(user.ID)), 0x22, (UInt16)currentEditPosition[int.Parse(user.ID)]);//新位置
            }
            // 更新选中项
            if (actionFrameDisplayList.SelectedItems.Count == 0)
                return;
            actionFrameDisplayList.BeginUpdate();
            for (int i = 2; i < 19; i++)
            {
                actionFrameDisplayList.SelectedItems[0].SubItems[i - 1].Text = currentEditPosition[i].ToString();
            }
            actionFrameDisplayList.SelectedItems[0].SubItems[18].Text = speedValueInput.Value.ToString();

            actionFrameDisplayList.SelectedItems[0].SubItems[19].Text = intervalValueInput.Value.ToString();
            actionFrameDisplayList.EndUpdate();
        }
        private void addFrame_Click(object sender, EventArgs e)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            Frame frame = new Frame(17);

            ListViewItem item = new ListViewItem();
            int i = this.actionFrameDisplayList.Items.Count;
            this.actionFrameDisplayList.BeginUpdate();
            item = actionFrameDisplayList.Items.Add(i.ToString());
            for (int j = 2; j < 19; j++)
            {
                //frame[j-2] = currentEditPosition[j];
                frame[j - 2] =int.Parse(user[j - 2].Position);
                item.SubItems.Add(user[j - 2].Position);
            }
            item.SubItems.Add(speedValueInput.Value.ToString());
            item.SubItems.Add(intervalValueInput.Value.ToString());//时间
            actionFrameDisplayList.EndUpdate();

            frame.Speed =int.Parse(speedValueInput.Value.ToString());
            frame.Duration = int.Parse(intervalValueInput.Value.ToString());

            // 将此数据存入相应动作的帧中
            foreach (LeleServo.Action action in ResolveAction.actionlist)
            {
                if (action.ActionName == currentEditAcitonName)
                {
                    action.KeyFrames.Add(frame);
                }
            }

            if (i > 0)
            {
                actionFrameDisplayList.Items[i - 1].Selected = false;
            }
            actionFrameDisplayList.Items[i].Selected = true;
        }
        private void deleteAllFrame_Click(object sender, EventArgs e)
        {
            MessageBoxButtons mess = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("你确定要清空当前动作全部帧吗？", "提示", mess);
            if (dr == DialogResult.OK)
            {
                foreach (LeleServo.Action action in ResolveAction.actionlist)
                {
                    if (action.ActionName == currentEditAcitonName)
                    {
                        action.KeyFrames.RemoveRange(0, action.KeyFrames.Count);
                    }
                }
                //调用Clear（）方法
                actionFrameDisplayList.Items.Clear();
            }
        }
        private void deleteAFrame_Click(object sender, EventArgs e)
        {
            if (actionFrameDisplayList.SelectedItems.Count == 0)
                return;

            int selindex = actionFrameDisplayList.SelectedItems[0].Index;

            foreach (LeleServo.Action action in ResolveAction.actionlist)
            {
                if (action.ActionName == currentEditAcitonName)
                {
                    action.KeyFrames.RemoveAt(selindex);
                }
            }
            // UI
            actionFrameDisplayList.Items.Remove(actionFrameDisplayList.SelectedItems[0]);
        }
        private void refreshSelectedFrame_Click(object sender, EventArgs e)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            if (actionFrameDisplayList.SelectedItems.Count == 0)
                return;
            actionFrameDisplayList.SelectedItems[0].SubItems[0].Text = (actionFrameDisplayList.SelectedItems[0].Index).ToString();

            int selindex = actionFrameDisplayList.SelectedItems[0].Index;

            foreach (LeleServo.Action action in ResolveAction.actionlist)
            {
                if (action.ActionName == currentEditAcitonName)
                {
                    for (int i = 2; i < 19; i++)
                    {
                        actionFrameDisplayList.SelectedItems[0].SubItems[i - 1].Text = user[i-2].Position;
                        action.KeyFrames[selindex][i - 2] = int.Parse(user[i-2].Position);
                    }
                    actionFrameDisplayList.SelectedItems[0].SubItems[18].Text = speedValueInput.Value.ToString();
                    actionFrameDisplayList.SelectedItems[0].SubItems[19].Text = intervalValueInput.Value.ToString();

                    action.KeyFrames[selindex].Speed=int.Parse(speedValueInput.Value.ToString());
                    action.KeyFrames[selindex].Duration =int.Parse(intervalValueInput.Value.ToString());
                }
            }
        }
        private void setMode_Click(object sender, EventArgs e)
        {         
            ///kp.mChangeMode(comboBox3.SelectedIndex + 1);
            writeServoData_Tcp(1, (byte)(255), 0x21, (UInt16)(comboBox3.SelectedIndex + 1));//模式设定
        }
        private void actionFrameDisplayList_Click(object sender, EventArgs e)
        {
            if (actionFrameDisplayList.Focused)
            {
                if (actionFrameDisplayList.SelectedItems.Count == 0)
                    return;
                for (int i = 1; i < 18; i++)
                {
                    currentEditPosition[i + 1] = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[i].Text);
                }
                Invoke((EventHandler)delegate
                {
                    speedValueInput.Value = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[18].Text);//速度
                    intervalValueInput.Value = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[19].Text);
                });

                setuserControl1Value(2, currentEditPosition);
            }
        }

        private void actionFrameDisplayList_DoubleClick(object sender, EventArgs e)
        {
            if (actionFrameDisplayList.Focused)
            {
                if (actionFrameDisplayList.SelectedItems.Count == 0)
                    return;
                for (int i = 1; i < 18; i++)
                {
                    currentEditPosition[i + 1] = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[i].Text);
                }
                Invoke((EventHandler)delegate
                {
                    speedValueInput.Value = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[18].Text);//速度
                    intervalValueInput.Value = int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[19].Text);
                });

                //kp.setAcc(1000, 3000);
                ///kp.mChangeMode(3);
                writeServoData_Tcp(1, (byte)(255), 0x21, 3);//曲线伺服
                ///kp.setVel(1000, int.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[18].Text));
                writeServoData_Tcp(1, (byte)(255), 0x25, UInt16.Parse(actionFrameDisplayList.SelectedItems[0].SubItems[18].Text));//速度
                System.Threading.Thread.Sleep(10);
                for (int i = 1; i < steerMaxNum; i++)
                {
                    ///kp.moveTo(i + 1, currentEditPosition[i + 1]);
                    writeServoData_Tcp(1, (byte)(i+1), 0x22, (UInt16)currentEditPosition[i + 1]);//新位置
                }
                setuserControl1Value(2, currentEditPosition);
            }
        }

        private void addAStartFrame_Click(object sender, EventArgs e)
        {           
            Frame frame = new Frame(17);
            ListViewItem item = new ListViewItem();
            int i = this.actionFrameDisplayList.Items.Count;
            this.actionFrameDisplayList.BeginUpdate();
            item = actionFrameDisplayList.Items.Add(i.ToString());
            for (int j = 2; j < 19; j++)
            {
                frame[j - 2] = int.Parse(zeroPosition[j]);
                item.SubItems.Add(currentEditPosition[j].ToString());
            }
            item.SubItems.Add(speedValueInput.Value.ToString());
            item.SubItems.Add(intervalValueInput.Value.ToString());//时间
            actionFrameDisplayList.EndUpdate();
            frame.Speed = int.Parse(speedValueInput.Value.ToString());
            frame.Duration = int.Parse(intervalValueInput.Value.ToString());
            foreach (LeleServo.Action action in ResolveAction.actionlist)
            {
                if (action.ActionName == currentEditAcitonName)
                {
                    action.KeyFrames.Add(frame);
                }
            }
            if (i > 0)
            {
                actionFrameDisplayList.Items[i - 1].Selected = false;
            }
            actionFrameDisplayList.Items[i].Selected = true;
        }
        private void actionDisplayList_Click(object sender, EventArgs e)
        {
            if (actionDisplayList.Focused)
            {
                if (actionDisplayList.SelectedItems.Count != 0)
                {
                    int num = int.Parse(actionDisplayList.SelectedItems[0].SubItems[1].Text);
                    string name = actionDisplayList.SelectedItems[0].SubItems[0].Text;
                    currentEditAcitonNum = num;
                    currentEditAcitonName = name;

                    foreach (LeleServo.Action action in ResolveAction.actionlist)
                    {
                        if (action.ActionNum == num)
                        {
                            actionFrameDisplayList.Items.Clear();
                            int c = action.FrameCount;
                            for (int i = 0; i < c; i++)
                            {
                                Frame frame = action.KeyFrames[i];
                                ListViewItem item = new ListViewItem();
                                this.actionFrameDisplayList.BeginUpdate();
                                item = actionFrameDisplayList.Items.Add(i.ToString());
                                for (int j = 0; j < 17; j++)
                                    item.SubItems.Add(frame[j].ToString());

                                item.SubItems.Add(frame.Speed.ToString());//速度
                                item.SubItems.Add(frame.Duration.ToString());//时间
                                actionFrameDisplayList.EndUpdate();
                            }
                        }
                    }
                    if (currentEditAcitonName != null)
                    {
                        addFrame.Enabled = true;
                        addAStartFrame.Enabled = true;
                        deleteAFrame.Enabled = true;
                        deleteAllFrame.Enabled = true;
                        refreshSelectedFrame.Enabled = true;

                        deleteSelectedAction.Enabled = true;
                    }
                }
            }
        }
        private void ActionDisplayList_listvEditOk(int row, int col,string str)
        {
            if (col == 0)
            {
                string oldActionName = this.actionDisplayList.Items[row].SubItems[col].Text;
                if (actionNameIsExistInActionList(str) && str != oldActionName)
                {
                    MessageBox.Show("已经存在此动作名，请用其他名称！", "注意!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {                        
                    foreach (LeleServo.Action action in ResolveAction.actionlist)
                    {
                        if (action.ActionName == oldActionName)
                        {
                            action.ActionName = str;
                            this.actionDisplayList.Items[row].SubItems[col].Text = str;
                        }
                    }
                }
            }
            if (col == 1)
            {
                string oldActionNum = this.actionDisplayList.Items[row].SubItems[col].Text;
                if (actionNumIsExistInActionList(str) && str != oldActionNum)
                {
                    MessageBox.Show("已经存在此动作号，请用其他号！", "注意!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    foreach (LeleServo.Action action in ResolveAction.actionlist)
                    {
                        if (action.ActionNum == int.Parse(oldActionNum))
                        {
                            action.ActionNum = int.Parse(str);
                            this.actionDisplayList.Items[row].SubItems[col].Text = str;
                        }
                    }
                }
            }
         }
        private bool actionNameIsExistInActionList(string str)
        {
            return ResolveAction.actionlist.Exists(action => action.ActionName == str);           
        }
        private bool actionNumIsExistInActionList(string str)
        {
            return ResolveAction.actionlist.Exists(action => action.ActionNum ==int.Parse(str));
        }
        private void getPosition_Click(object sender, EventArgs e)
        {
            for (int i = 2; i <= steerMaxNum; i++)
            {
                ///currentPosition[i]= kp.getPosition((UInt16)i);
                currentPosition[i] = readServoData_Tcp(1,(byte)i,0x22);//位置
                this.Invoke(new MethodInvoker(delegate
                {
                    servoEcho.AppendText(i + " " + (currentPosition[i]).ToString() + "\r\n");
                }));
            }
            if (checkBox2.Checked == true)
            {
                setuserControl1Value(2, currentPosition);
            }
        }
        private void releaseTorgue_Click(object sender, EventArgs e)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            if (robotType == 1)
            {
                for (int i = 1; i < steerMaxNum - 3; i++)
                {
                    if (user[i].Selected)
                    {
                        ///kp.changeServoMode(i + 2, 7);
                        writeServoData_Tcp(1, (byte)(i+2), 0x21,1);//刹车停止
                    }
                }
            }
            else
            {
                for (int i = 0; i < steerMaxNum - 1; i++)
                {
                    if (user[i].Selected)
                    {
                        ///kp.changeServoMode(i + 2, 7);
                        writeServoData_Tcp(1, (byte)(i + 2), 0x21, 1);//刹车停止
                    }
                }
            }
        }
        private void recoveryTorgue_Click_1(object sender, EventArgs e)
        {
            //kp.mChangeMode(3);
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            if (robotType == 1)
            {
                for (int i = 1; i < steerMaxNum - 3; i++)
                {
                    if (user[i].Selected)
                    {
                        ///kp.changeServoMode(i + 2, 3);
                        writeServoData_Tcp(1, (byte)(i + 2), 0x21, 3);//曲线伺服
                    }
                }
            }
            else
            {
                for (int i = 0; i < steerMaxNum - 1; i++)
                {
                    if (user[i].Selected)
                    {
                        ///kp.changeServoMode(i + 2, 3);
                        writeServoData_Tcp(1, (byte)(i + 2), 0x21, 3);//曲线伺服
                    }
                }
            }
        }
        private void getTorgue_Click(object sender, EventArgs e)
        {
            for (int i = 2; i <= steerMaxNum; i++)
            {
                ///currentTorgue[i] = kp.getTorque((UInt16)i);
                currentTorgue[i] = readServoData_Tcp(1, (byte)i, 0x23);//扭力
                this.Invoke(new MethodInvoker(delegate
                {
                    servoEcho.AppendText(i + " " + currentTorgue[i].ToString() + "\r\n");
                }));
            }
        }
        private void getTemperature_Click(object sender, EventArgs e)
        {
            for (int i = 2; i <= steerMaxNum; i++)
            {
                ///currentTemperature[i] = kp.getTemperature((UInt16)i);
                currentTemperature[i] = readServoData_Tcp(1, (byte)i, 0x24);//温度
                this.Invoke(new MethodInvoker(delegate
                {
                    servoEcho.AppendText(i + " " + currentTemperature[i].ToString() + "\r\n");
                }));
            }
        }
        private void clearEcho_Click(object sender, EventArgs e)
        {
            servoEcho.Clear();
        }
        private void newBuild_Click(object sender, EventArgs e)
        {
            // UI
            ListViewItem lvii = new ListViewItem();
            this.actionDisplayList.BeginUpdate();
            lvii.Text = "newAction";
            lvii.SubItems.Add("9999");
            this.actionDisplayList.Items.Add(lvii);
            this.actionDisplayList.EndUpdate();
            // 动作集
            LeleServo.Action action = new LeleServo.Action();
            action.KeyFrames = new List<Frame>(500);
            action.ActionNum = 9999;
            action.ActionName = "newAction";
            ResolveAction.actionlist.Add(action);
        }
        private void deleteSelectedAction_Click(object sender, EventArgs e)
        {
            MessageBoxButtons mess = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("你确定要删除此动作吗？", "提示", mess);
            if (dr == DialogResult.OK)
            {
                if (actionDisplayList.SelectedItems.Count != 0)
                {
                    // 动作集
                    for (int i = 0; i < ResolveAction.actionlist.Count; i++)
                    {
                        if (ResolveAction.actionlist[i].ActionName == actionDisplayList.SelectedItems[0].SubItems[0].Text)
                        {
                            ResolveAction.actionlist.Remove(ResolveAction.actionlist[i]);
                        }
                    }
                    // UI
                    actionDisplayList.Items.Remove(actionDisplayList.SelectedItems[0]);
                }
            }
        }

        private void incrementBar_Scroll(object sender, EventArgs e)
        {
            incrementBarText.Text = incrementBar.Value.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                timer1.Enabled = false;
            }
            else
            {
                timer1.Enabled = true;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int[] gesture = new int[3];
            gesture = kp.getRobotAccAndGyro();            

            label4.Text = "A1:"+gesture[0].ToString();
            label5.Text = "A2:"+gesture[1].ToString();
            label8.Text = "A3:"+gesture[2].ToString();
        }

        private void inputVoice_Click(object sender, EventArgs e)
        {
            // 录音设置
            string wavfile = System.IO.Directory.GetCurrentDirectory() + @"\Voice\sample.wav";
            recorder = new SoundRecord();                
            recorder.SetFileName(wavfile);
            recorder.RecStart();
            inputVoice.Text = "录音中..."; 
        }

        private void stopVoice_Click(object sender, EventArgs e)
        {
            recorder.RecStop();
            recorder = null;
            inputVoice.Text = "录音";
        }

        private void sendVoice_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取远程文件的方法。
                WebClient client = new WebClient();
                client.UploadFileCompleted += Client_UploadFileCompleted;
                //file://desktop-kcouve7/Users/admin-pc/Documents/Voice/
                NetworkCredential cred = new NetworkCredential("admin-pc", "a", "desktop-kcouve7");
                client.Credentials = cred;  
                Uri ur=new Uri("file://desktop-kcouve7/Users/admin-pc/Documents/Voice/sample.wav");
                client.UploadFileAsync(ur, @".\Voice\sample.wav");            
            }
            catch (Exception ex)
            {
                // 如果网络很慢，而文件又很大，这时可能有超时异常（Time out）。
                MessageBox.Show(ex.ToString());
            }

        }

        

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            
        }
        
        private void showM(string str,Color c)
        {
            this.Invoke((EventHandler)delegate 
            {
                buttonX1.Text = str;
                buttonX1.BackColor = c;
            });
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            timer2.Interval = 1000;
            if (!timer2.Enabled)
            {
                timer2.Enabled = true;
                d = 6;
            }
            else
            {
                timer2.Enabled = false;
                d = 6;
            }
        }

        private int d;
        private void timer2_Tick(object sender, EventArgs e)
        {
            switch (d)
            {
                case 6:
                    key = 998;//两声提示音表示开始录音
                    run = true;
                    inputVoice.PerformClick();
                    break;
                case 0:
                    stopVoice.PerformClick();
                    showM("结束", Color.Blue);
                    break;
                case -1:
                    sendVoice.PerformClick();
                    showM("结束", Color.White);
                    timer2.Enabled = false;
                    break;
                default:
                    showM("录音中" + d.ToString(), Color.White);
                    break;
            }
            d--;
        }

        private void Client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            //MessageBox.Show("发送完毕");
            System.Threading.Thread.Sleep(200);
            // 识别指令
            key = 999;
            run = true;
            buttonX1.BackColor = Color.Red;// 红色表示发送给乐乐机器人完毕
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            if (link)
            {
                connectServo.BackColor = Color.Red;
            }
            else
            {
                connectServo.BackColor = Color.Beige;
            }
        }
        public int robotType=1;
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rad = (RadioButton)(sender);
            if (rad.Text == "方方")
            {
                steerMaxNum = 16;
                userControl116.Visible = false;
                userControl117.Visible = false;
                robotType = 1;
            }
            else if (rad.Text == "乐乐")
            {
                steerMaxNum = 18;
                userControl116.Visible = true;
                userControl117.Visible = true;
                robotType = 2;
            }
        }

        public SerialPort lycom = new SerialPort();

        private void button4_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (lycom.IsOpen)
                {
                    lycom.Close();
                    System.Threading.Thread.Sleep(10);

                    this.button4.Text = "打开";
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    Stream manifestResourceStream = entryAssembly.GetManifestResourceStream("LeleClient.Resources.关闭.ico");
                    this.button4.Image = Image.FromStream(manifestResourceStream);
                }
                else
                {
                    lycom.DataBits = 8;
                    lycom.Parity = Parity.None;
                    lycom.BaudRate = 9600;
                    lycom.PortName = comboBox4.Text;

                    lycom.DataReceived += Lycom_DataReceived;

                    lycom.Open();
                    System.Threading.Thread.Sleep(10);

                    this.button4.Text = "关闭";
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    Stream manifestResourceStream = entryAssembly.GetManifestResourceStream("LeleClient.Resources.打开.ico");
                    this.button4.Image = Image.FromStream(manifestResourceStream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // 蓝牙串口接收数据处理
        private void Lycom_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //throw new NotImplementedException();
            while (lycom.BytesToRead < 1)
            {
                System.Threading.Thread.Sleep(1);
            }

            // 完整数据，为补码形式数据
            byte[] buf = new byte[lycom.BytesToRead];
            lycom.Read(buf, 0, buf.Length);

            // 完整性判断
            //if (buf[0] == 0x55 && buf[2] == 0xAA)
            if (buf[0] == 0xFF)
            {
                // 0xxx 控制指令
                this.Invoke((EventHandler)delegate
                {
                    buttonX1.PerformClick();
                });
            }
        }

        private void selectAll_Click(object sender, EventArgs e)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            for (int i = 0; i < 17; i++)
            {
                user[i].Selected = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            timer1.Interval = int.Parse(textBox1.Text==""?"50": textBox1.Text);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            usercontrol.UserControl1[] user = new usercontrol.UserControl1[]
            {
                this.userControl11,
                this.userControl12,
                this.userControl13,
                this.userControl14,
                this.userControl15,
                this.userControl16,
                this.userControl17,
                this.userControl18,
                this.userControl19,
                this.userControl110,
                this.userControl111,
                this.userControl112,
                this.userControl113,
                this.userControl114,
                this.userControl115,
                this.userControl116,
                this.userControl117
            };

            for (int i = 0; i < 17; i++)
            {
                user[i].Selected = false;
            }
        }
        
        private void downLoad_Btn_Click(object sender, EventArgs e)
        {
            client Client = (client)clientList[robotList.SelectedItem.ToString()];
            Client.setMessage("inf#" + "server#" + "hello" + "\r");
        }


        //============================================================================================================
        //当客户端发送消息，解析这个消息

        public static byte[] recData = new byte[100];
        public bool isRec = false;//是否收到返回数据？
        public int len = 0;//帧长度
        private void getClientMessage(object sender, EventArgs e, byte[] message)
        {
            //客户端IP地址
            client Client = (client)sender;
            string clientIp = Client.IP;
            // 有效性判断
            len = message.Length;
            if (message[0] == 0x68 && message[3] == 0x68 && message[len-1] == 0x16)
            {
                message.CopyTo(recData, 0);
                isRec = true;  
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        public UInt16 readServoData_Tcp(byte robotAddr,byte devAddr, byte cmd)
        {
            int r = 0;
            int maxr = 3;
            int timeout = 1000;
            byte[] frame = new byte[13];
            isRec = false;

            try
            {
                switch (cmd)
                {
                    case 0x21://数据域1B
                        frame = new byte[10];
                        // 帧头
                        frame[0] = 0x68;
                        // 机器人地址
                        frame[1] = (byte)(robotAddr % 256);
                        // 舵机地址
                        frame[2] = (byte)(devAddr % 256);
                        // 帧头
                        frame[3] = 0x68;
                        // 控制码
                        frame[4] = 0x11;
                        // 数据域长度
                        frame[5] = 2;
                        // 命令字
                        frame[6] = cmd;
                        // 数据域
                        frame[7] = 0;
                        // 校验
                        frame[8] = (byte)(frame[1] ^ frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7]);
                        // 帧尾
                        frame[9] = 0x16;

                        break;
                    case 0x02://数据域20B
                    case 0x03:                    
                        break;
                    default:  //数据域2B
                        frame = new byte[11];
                        // 帧头
                        frame[0] = 0x68;
                        // 机器人地址
                        frame[1] = (byte)(1 % 256);
                        // 舵机地址
                        frame[2] = (byte)(devAddr % 256);
                        // 帧头
                        frame[3] = 0x68;
                        // 控制码
                        frame[4] = 0x11;
                        // 数据域长度
                        frame[5] = 3;
                        // 命令字
                        frame[6] = cmd;
                        // 数据域
                        frame[7] = 0;
                        // 数据域
                        frame[8] = 0;
                        // 校验
                        frame[9] = (byte)(frame[1] ^ frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8]);
                        // 帧尾
                        frame[10] = 0x16;
                        break;
                }

                client Client = (client)clientList[robotList.SelectedItem.ToString()];
                Client.setMessage(frame);
               
                //超时重发
                for (r = 0; r < maxr; r++)
                {
                    DateTime dt = DateTime.Now;
                    while (!isRec)//没收到返回数据包
                    {
                        System.Threading.Thread.Sleep(2);
                        if (DateTime.Now.Subtract(dt).TotalMilliseconds > timeout)
                        {
                            Client.setMessage(frame);
                            break;
                        }
                    }
                    if (isRec)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ;//发送异常
            }
            if (r >= 3)
            {
                return 0x0000;
            }
            else
            {
                if (cmd == 0x21)//1B
                {
                    return (UInt16)(toUint16(recData[7]));
                }
                else if (cmd == 0x02 || cmd == 0x03)
                {
                    return 0x0000;
                }
                else//2B
                {
                    return (UInt16)(toUint16(recData[7]) * 256 + toUint16(recData[8]));
                }
            }
        }
        /// <summary>
        /// 将16进制字节数据转换为UInt16数据
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private UInt16 toUint16(byte bytes)
        {
            UInt16 a = (UInt16)(bytes & 0x0f);
            UInt16 b = (UInt16)((bytes >> 4) * 16);
            return (UInt16)(a + b);
        }
        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public UInt16 writeServoData_Tcp(byte robotAddr,byte devAddr, byte cmd,UInt16 data)
        {
            int r = 0;
            int maxr = 3;
            int timeout = 1000;
            byte[] frame = new byte[13];
            isRec = false;

            try
            {
                switch (cmd)
                {
                    case 0x21://数据域1B
                        frame = new byte[10];
                        // 帧头
                        frame[0] = 0x68;
                        // 机器人地址
                        frame[1] = (byte)(robotAddr % 256);
                        // 舵机地址
                        frame[2] = (byte)(devAddr % 256);
                        // 帧头
                        frame[3] = 0x68;
                        // 控制码
                        frame[4] = 0x12;
                        // 数据域长度
                        frame[5] = 2;
                        // 命令字
                        frame[6] = cmd;
                        // 数据域
                        frame[7] = (byte)(data % 256);
                        // 校验
                        frame[8] = (byte)(frame[1] ^ frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7]);
                        // 帧尾
                        frame[9] = 0x16;

                        break;
                    default:  //数据域2B
                        frame = new byte[11];
                        // 帧头
                        frame[0] = 0x68;
                        // 机器人地址
                        frame[1] = (byte)(1 % 256);
                        // 舵机地址
                        frame[2] = (byte)(devAddr % 256);
                        // 帧头
                        frame[3] = 0x68;
                        // 控制码
                        frame[4] = 0x12;
                        // 数据域长度
                        frame[5] = 3;
                        // 命令字
                        frame[6] = cmd;
                        // 数据域
                        frame[7] = (byte)(data / 256);
                        // 数据域
                        frame[8] = (byte)(data % 256);
                        // 校验
                        frame[9] = (byte)(frame[1] ^ frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8]);
                        // 帧尾
                        frame[10] = 0x16;
                        break;
                }

                client Client = (client)clientList[robotList.SelectedItem.ToString()];
                Client.setMessage(frame);

                //超时重发
                for (r = 0; r < maxr; r++)
                {
                    DateTime dt = DateTime.Now;
                    while (!isRec)//没收到返回数据包
                    {
                        System.Threading.Thread.Sleep(2);
                        if (DateTime.Now.Subtract(dt).TotalMilliseconds > timeout)
                        {
                            Client.setMessage(frame);
                            break;
                        }
                    }
                    if (isRec)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                ;//发送异常
            }
            if (r >= 3)
            {
                return 0x0000;//正常值
            }
            else
            {                
                if (cmd == 0x21)//1B
                {
                    return (UInt16)(toUint16(recData[7]));
                }
                else//2B
                {
                    return (UInt16)(toUint16(recData[7]) * 256 + toUint16(recData[8]));
                }
            }
        }
    }
}
