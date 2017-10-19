/*------------------------------------------
 * 客户端类
 * 属性
 * IP:客户端唯一标志
 * 事件
 * OnClientdisConnect:当客户端断开时触发
 * OnclientMessage:当客户端有消息过来触发,返回消息字符串
 * 方法:
 * setMessage:向客户端发送消息,参数是需要传递的字符串
 * killSelf:客户端自杀
 ------------------------------------------ */
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace server
{
    class client
    {
        //定义IP属性，唯一界定客户端
        public string IP = null;
        public string UserName = null;

        //注册断开客户端事件        
        public delegate void clientdisConnect(object sender, EventArgs e);
        public event clientdisConnect OnClientdisConnect;

        //注册收到客户端消息事件
        public delegate void clientMessage(object sender, EventArgs e, byte[] message);
        public event clientMessage OnclientMessage;

        //变量
        private Socket clientSocket = null;
        private Thread thread;
        private bool flag = true;

        //构造函数
        public client(Socket Socket)
        {   
            clientSocket = Socket;           
            thread = new Thread(new ThreadStart(WaitForSendData));
            thread.IsBackground = true;
            thread.Name = clientSocket.RemoteEndPoint.ToString();
            thread.Start();            
        }

        //等待数据通信函数
        private void WaitForSendData()
        {
            string message = null;
            while (flag)
            {
                if (clientSocket.Connected)
                {
                    try
                    {
                        byte[] bytes = new byte[1024];
                        int bytesRec = clientSocket.Receive(bytes);
                        
                        byte[] temp = new byte[bytesRec];
                        for (int i = 0; i < bytesRec; i++)
                        {
                            temp[i] = bytes[i];
                        }

                        if (bytesRec > 0)
                        {
                            if (OnclientMessage != null)
                            {
                                OnclientMessage(this, new EventArgs(), temp);
                            }
                        }
                    }
                    catch
                    {
                        killSelf();
                    }
                }
                else
                {
                    killSelf();
                }
            }
        }

        //客户端自毁方法
        public void killSelf()
        {
            flag = false;

            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            //一定要写在线程结束前，否则不触发
            if (OnClientdisConnect != null)
            {
                OnClientdisConnect(this, new EventArgs());
            }

            if (thread != null && thread.IsAlive)
            {
                thread.Abort();
                thread = null;
            }
        }

        //向客户端发送信息方法
        public void setMessage(string message)
        {
            try
            {
                byte[] sendbytes = System.Text.Encoding.UTF8.GetBytes(message);
                int successSendBtyes = clientSocket.Send(sendbytes, sendbytes.Length, SocketFlags.None);
            }
            catch
            {

            }
        }

        //向客户端发送字节信息方法
        public void setMessage(byte[] sendbytes)
        {
            try
            {
                int successSendBtyes = clientSocket.Send(sendbytes, sendbytes.Length, SocketFlags.None);
            }
            catch
            {

            }
        }
        //代码结束
    }
}
