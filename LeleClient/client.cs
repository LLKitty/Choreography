/*------------------------------------------
 * �ͻ�����
 * ����
 * IP:�ͻ���Ψһ��־
 * �¼�
 * OnClientdisConnect:���ͻ��˶Ͽ�ʱ����
 * OnclientMessage:���ͻ�������Ϣ��������,������Ϣ�ַ���
 * ����:
 * setMessage:��ͻ��˷�����Ϣ,��������Ҫ���ݵ��ַ���
 * killSelf:�ͻ�����ɱ
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
        //����IP���ԣ�Ψһ�綨�ͻ���
        public string IP = null;
        public string UserName = null;

        //ע��Ͽ��ͻ����¼�        
        public delegate void clientdisConnect(object sender, EventArgs e);
        public event clientdisConnect OnClientdisConnect;

        //ע���յ��ͻ�����Ϣ�¼�
        public delegate void clientMessage(object sender, EventArgs e, byte[] message);
        public event clientMessage OnclientMessage;

        //����
        private Socket clientSocket = null;
        private Thread thread;
        private bool flag = true;

        //���캯��
        public client(Socket Socket)
        {   
            clientSocket = Socket;           
            thread = new Thread(new ThreadStart(WaitForSendData));
            thread.IsBackground = true;
            thread.Name = clientSocket.RemoteEndPoint.ToString();
            thread.Start();            
        }

        //�ȴ�����ͨ�ź���
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

        //�ͻ����Իٷ���
        public void killSelf()
        {
            flag = false;

            if (clientSocket != null && clientSocket.Connected)
            {
                clientSocket.Close();
                clientSocket = null;
            }

            //һ��Ҫд���߳̽���ǰ�����򲻴���
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

        //��ͻ��˷�����Ϣ����
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

        //��ͻ��˷����ֽ���Ϣ����
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
        //�������
    }
}
