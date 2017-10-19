using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace LeleClient
{
    /// <summary>
    /// K_power总线舵机控制类
    /// </summary>
    class KpowerSteer
    {
        #region //寄存器地址定义
        public const byte POSITION_REGADDR = 0x01;  //位置寄存器 读写
        public const byte SPEED_REGADDR = 0x02;     //速度寄存器 读写
        public const byte ADDR_REGADDR = 0x03;      //舵机在通信总线上的地址寄存器 读写
        public const byte BAUDRATE_REGADDR = 0x04;  //波特率寄存器 可读写        
        public const byte P_GAIN_REGADDR = 0x05;    //参数寄存器  读写
        public const byte D_GAIN_REGADDR = 0x06;    //参数寄存器  读写
        public const byte I_GAIN_REGADDR = 0x07;    //参数寄存器  读写
        public const byte PDI_GAIN_REGADDR=0x08;    //PDI参数
        public const byte CW_FLEXIBLE_MARGIN_REGADDR = 0x09; //顺时针柔性边距寄存器   读写
        public const byte CCW_FLEXIBLE_MARGIN_REGADDR = 0x0a; //逆时针柔性边距寄存器 读写
        public const byte CW_FLEXIBLE_SLOPE_REGADDR = 0x0b; //顺时针柔性斜率寄存 器  读写I2C               
        public const byte CCW_FLEXIBLE_SLOPE_REGADDR = 0x0c; //逆时针柔性斜率寄存器 读写
        public const byte WORKING_MODE_REGADDR = 0x0d; //工作模式寄存器
        public const byte SERVO_STATE_REGADDR = 0x0e; //舵机状态寄存器 读写
        public const byte MAX_POSITION_REGADDR = 0x0f; //最大位置寄存器 读写
        public const byte MIN_POSITION_REGADDR = 0x10; //最小位置寄存器 读写
        public const byte MAX_TEMPERATURE_REGADDR = 0x11; //最高温度寄存器 读写
        public const byte MAX_VOLTAGE_REGADDR = 0x12; //最大电压寄存器 读写
        public const byte MIN_VOLTAGE_REGADDR = 0x13; //最小电压寄存器 读写
        public const byte MAX_TORQUE_REGADDR = 0x14; //最大扭矩寄存器 读写

        public const byte CURRENT_POSITION_REGADDR = 0x16; //当前位置寄存器 只读
        public const byte CURRENT_SPEED_REGADDR = 0x17; //当前速度寄存器 只读
        public const byte CURRENT_TORQUE_REGADDR = 0x18; //当前扭矩寄存器 只读
        public const byte CURRENT_VOLTAGE_REGADDR = 0x19; //当前电压寄存器 只读
        public const byte CURRENT_TEMPERATURE_REGADDR = 0x1a; //当前温度寄存器只读     
                 
        public const byte FIRMWARE_VERSION_REGADDR = 0x1c; //固件版本 只读
        public const byte MODEL_CODE_REGADDR = 0x1d; //舵机型号代码 只读
        public const byte WRITE_FLASH_REGADDR = 0x1e; //写寄存器地址 只读
        public const byte PDI_DEADBAND_REGADDR = 0x1f; //死区范围寄存器  读写
        public const byte ACCELERATION_REGADDR = 0x20; //加速度寄存器 读写
        public const byte COMMUNICATION_MODE_REGADDR = 0x21; //通信模式寄存器 读写

        #endregion

        #region//常用地址定义        
        public const byte MASTER_BUSADDRH = 0x00; //串口模式主机在通信总线上的地址
        public const byte MASTER_BUSADDRL = 0x01; //串口模式主机在通信总线上的地址0x001=1
        public const byte BROADCAST_BUSADDRH = 0x03; //串口模式广播地址
        public const byte BROADCAST_BUSADDRL = 0xe8; //串口模式广播地址0x3e8=1000
        #endregion

        #region// 帧类型
        public const byte READ_REG = 0x01;  // 主机发送的帧，用于读取对应地址的数据或参数
        public const byte WRITE_REG = 0x02; // 主机发送的帧，用于设置对应地址的数据或参数
        public const byte ANSWER = 0x03;    // 舵机发送的帧，用于舵机回复主机命令
        #endregion

        #region// 写入成功或失败
        public const byte WRITE_OK = 0x01;  // 写入成功
        public const byte WRITE_FAIL = 0x02; // 写入失败 
        #endregion
       

        // 属性与字段
        public SerialPort com = null;

        public string portName;
        public string portBaudRate;
        public string portDataBits;
        public Parity portParity;

        private List<byte> buffer = new List<byte>(4096);// 缓冲区
        private byte[] ReceiveBytes;                     // 接收到的数据

        // 事件组-------------------------------------------
        public delegate void servoException(string str);// 异常处理委托
        public event servoException kpowrException;

        public delegate void servoReceived(string str);// 收到数据事件
        public event servoReceived kpowerReceived;


        // 方法组-------------------------------------------

        public KpowerSteer()
        {
            com = new System.IO.Ports.SerialPort();
        }

        /// <summary>
        /// 设置串口属性
        /// </summary>
        public void setPortProperty()
        {
            //this.com.Encoding = Encoding.GetEncoding("GB18030");
            if (this.portName != "")
            {
                this.com.PortName = portName;
            }
            if (this.portBaudRate != "")
            {
                try
                {
                    this.com.BaudRate = Convert.ToInt32(portBaudRate);
                }
                catch (Exception ex)
                {
                    // 设置波特率异常
                    kpowrException(ex.Message);
                }
            }       
            this.com.DataBits = (int)Convert.ToInt16(portDataBits);
            this.com.Parity= portParity;
            com.ReceivedBytesThreshold = 1;
            com.ReadBufferSize = 250;
        }

        /// <summary>
        /// 打开/关闭串口
        /// </summary>
        public bool openSerialPort()
        {
            if (!this.com.IsOpen)
            {
                this.setPortProperty();

                try
                {
                    this.com.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    kpowrException(ex.Message);
                    return false;
                }
            }
            else
            {
                try
                {
                    this.com.Close();
                    return false;
                }
                catch (Exception ex)
                {
                    //关闭异常
                    kpowrException(ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// 读取舵机数据
        /// </summary>
        /// <param name="devAddr">舵机地址</param>
        /// <param name="regAddr">寄存器地址</param>
        /// <returns></returns>
        public UInt16 readServoData(UInt16 devAddr, byte regAddr)
        {
            byte[] frame = new byte[13];

            // 帧头
            frame[0] = 0xaa;
            frame[1] = 0x55;
            // 源地址
            frame[2] = (byte)(1 / 256);
            frame[3] = (byte)(1 % 256);
            // 目标地址
            frame[4] = (byte)(devAddr / 256);
            frame[5] = (byte)(devAddr % 256);
            // 帧长度
            frame[6] = 0x08;//8
            // 帧类型
            frame[7] = READ_REG;//主机发送
            // 寄存器地址
            frame[8] = regAddr;
            // 校验
            frame[9] = (byte)(frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8]);
            // 帧尾
            frame[10] = 0xaa;
            frame[11] = 0x81;

            try
            {
                if (frame[9] == 0xaa)// 如果校验为为0xaa不忘在后加0x00
                {
                    frame[10] = 0x00;
                    frame[11] = 0xaa;
                    frame[12] = 0x81;

                    com.Write(frame, 0, 13);
                }
                else
                {
                    com.Write(frame, 0, 12);
                }
            }
            catch (Exception ex)
            {
                //发送异常
                kpowrException(ex.Message);
            }

            // 读取返回数据
            DateTime dt = DateTime.Now;
            while (com.BytesToRead < 14)
            {
                System.Threading.Thread.Sleep(1);

                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 2000) //如果2秒后仍然无数据返回，则视为超时
                {
                    kpowrException("舵机" + devAddr+ "响应超时");
                    return 0xfffE;
                }
            }

            // 完整性判断
            byte[] buf = new byte[com.BytesToRead];
            com.Read(buf, 0, buf.Length);
            buffer.Clear();
            buffer.AddRange(buf);

            if (buffer[0] == 0xAA && buffer[1] == 0x55) //传输数据有帧头，用于判断
            {
                return (UInt16)(toUint16(buffer[9])*256+ toUint16(buffer[10]));
            }
            else //帧头不正确时，记得清除
            {
                buffer.RemoveAt(0);

                // 接收数据帧异常
                return 0xffff;
            }            
        }

        
        /// <summary>
        /// 给舵机写入数据
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="regAddr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool writeServoData(int devAddr, byte regAddr, int data)
        {
            byte[] frame = new byte[16];
            int len = 0;

            // 帧头
            frame[0] = 0xaa;
            frame[1] = 0x55;
            // 源地址
            frame[2] = (byte)(1 / 256);
            frame[3] = (byte)(1 % 256);
            // 目标地址
            frame[4] = (byte)(devAddr / 256);
            frame[5] = (byte)(devAddr % 256);
            // 帧长度
            frame[6] = 0x0a;//10
            // 帧类型
            frame[7] = WRITE_REG;//主机发送写
            // 寄存器地址
            frame[8] = regAddr;
            // 数据
            frame[9] = (byte)(data / 256);
            frame[10] = (byte)(data % 256);

            if (frame[10] == 0xaa)
            {
                frame[11] = 0x00;
                // 校验
                frame[12] = (byte)(frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8] ^ frame[9] ^ frame[10]);
                if (frame[12] == 0xaa)
                {
                    frame[13] = 0x00;
                    // 帧尾
                    frame[14] = 0xaa;
                    frame[15] = 0x81;
                    len = 16;
                }
                else
                {
                    // 帧尾
                    frame[13] = 0xaa;
                    frame[14] = 0x81;
                    len = 15;
                }
            }
            else
            {
                // 校验
                frame[11] = (byte)(frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8] ^ frame[9] ^ frame[10]);
                if (frame[11] == 0xaa)
                {
                    frame[12] = 0x00;
                    // 帧尾
                    frame[13] = 0xaa;
                    frame[14] = 0x81;
                    len = 15;
                }
                else
                {
                    // 帧尾
                    frame[12] = 0xaa;
                    frame[13] = 0x81;
                    len = 14;
                }
            }                                 
            
            com.DiscardInBuffer();
            try
            {
                com.Write(frame, 0, len);
                                           
                if (devAddr != 1000)
                {
                    // 读取返回数据
                    DateTime dt = DateTime.Now;
                    while (com.BytesToRead < 13)
                    {
                        System.Threading.Thread.Sleep(1);

                        if (DateTime.Now.Subtract(dt).TotalMilliseconds > 2000) //如果2秒后仍然无数据返回，则视为超时
                        {
                            kpowrException("舵机" + devAddr + "响应超时");
                            return false;
                        }
                    }

                    // 完整性判断
                    byte[] buf = new byte[com.BytesToRead];
                    com.Read(buf, 0, buf.Length);
                    buffer.Clear();
                    buffer.AddRange(buf);

                    if (buffer[0] == 0xAA && buffer[1] == 0x55) //传输数据有帧头，用于判断
                    {
                        if (buffer[9] == WRITE_OK)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }

                    }
                    else //帧头不正确时，记得清除
                    {
                        buffer.RemoveAt(0);

                        // 接收数据帧异常
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                //发送异常
                kpowrException(ex.Message);
                return false;
            }
        }

        //---------------------------------
        // 主要功能
        /// <summary>
        /// 改变舵机运动模式
        /// </summary>
        /// <param name="devAddr">舵机地址</param>
        /// <param name="mode">模式号</param>
        /// 1   刹车停止
        /// 2   普通伺服
        /// 3   曲线伺服
        /// 4   顺时针连续旋转
        /// 5   逆时针连续旋转
        /// 6   柔性控制
        /// 7   不刹车停止
        /// <returns></returns>
        public bool changeServoMode(int devAddr, int mode)
        {
            return writeServoData(devAddr, WORKING_MODE_REGADDR, mode);
        }

        /// <summary>
        /// 舵机运动到某个位置
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        /// 50-4050
        /// <returns></returns>
        public bool moveTo(int devAddr, int data)
        {
            return writeServoData(devAddr, POSITION_REGADDR, data);
        }

        /// <summary>
        /// 批量改变舵机模式
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public void mChangeMode(UInt16[] devAddr, byte[] mode)
        {
            for (int i = 0; i < devAddr.Length; i++)
            {
                writeServoData(devAddr[i], WORKING_MODE_REGADDR, (UInt16)mode[i]);
            }
        }

        /// <summary>
        /// 广播改变模式
        /// </summary>
        /// <param name="mode"></param>
        public void mChangeMode(int mode)
        {
            writeServoData(1000, WORKING_MODE_REGADDR, (UInt16)mode);
        }

        /// <summary>
        /// 批量改变舵机位置
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void mMoveTo(UInt16[] devAddr, UInt16[] data)
        {
            for (int i = 0; i < devAddr.Length; i++)
            {
                writeServoData(devAddr[i], POSITION_REGADDR, data[i]);
            }
        }

        /// <summary>
        /// 改变舵机速度
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void setVel(int devAddr, int data)
        {
            writeServoData(devAddr, SPEED_REGADDR, data);
        }

        /// <summary>
        /// 改变舵机加速度
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public void setAcc(int devAddr, int data)
        {
            writeServoData(devAddr, ACCELERATION_REGADDR, data);
        }

        /// <summary>
        /// 设置舵机ID
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        public void setAddress(int devAddr, int data)
        {
            writeServoData(devAddr, ADDR_REGADDR, data);
        }

        /// <summary>
        /// 设定舵机最大位置
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        public void setMaxPosition(int devAddr, int data)
        {
            writeServoData(devAddr, MAX_POSITION_REGADDR, data);
        }

        /// <summary>
        /// 设定舵机最小位置
        /// </summary>
        /// <param name="devAddr"></param>
        /// <param name="data"></param>
        public void setMinPosition(int devAddr, int data)
        {
            writeServoData(devAddr, MIN_POSITION_REGADDR, data);
        }

        /// <summary>
        /// 舵机状态清零
        /// </summary>
        /// <param name="devAddr"></param>
        public void servoRestError(int devAddr)
        {
            writeServoData(devAddr, SERVO_STATE_REGADDR, 0);
        }

        // 获取-----------------------------------------------
        /// <summary>
        /// 获取舵机状态
        /// </summary>
        /// <param name="devAddr">舵机地址</param>
        /// <returns>
        /// 0 正常
        /// 1 电压过高
        /// 2 电压过低
        /// 3 扭矩过高
        /// 4 温度过高       
        /// </returns>
        public UInt16 getState(UInt16 devAddr)
        {
            return readServoData(devAddr, SERVO_STATE_REGADDR);
        }

        /// <summary>
        /// 获取舵机位置
        /// </summary>
        /// <param name="devAddr"></param>
        /// <returns></returns>
        public UInt16 getPosition(UInt16 devAddr)
        {
            return readServoData(devAddr, CURRENT_POSITION_REGADDR);
        }

        /// <summary>
        /// 获取当前速度
        /// </summary>
        /// <param name="devAddr"></param>
        /// <returns></returns>
        public UInt16 getVel(UInt16 devAddr)
        {
            return readServoData(devAddr, CURRENT_SPEED_REGADDR);
        }

        /// <summary>
        /// 获取舵机当前扭矩
        /// </summary>
        /// <param name="devAddr"></param>
        /// <returns></returns>
        public UInt16 getTorque(UInt16 devAddr)
        {
            return readServoData(devAddr, CURRENT_TORQUE_REGADDR);
        }

        /// <summary>
        /// 获取舵机当前电压
        /// </summary>
        /// <param name="devAddr"></param>
        /// <returns></returns>
        public UInt16 getVoltage(UInt16 devAddr)
        {
            return readServoData(devAddr, CURRENT_VOLTAGE_REGADDR);
        }

        /// <summary>
        /// 获取舵机当前电压
        /// </summary>
        /// <param name="devAddr"></param>
        /// <returns></returns>
        public UInt16 getTemperature(UInt16 devAddr)
        {
            return readServoData(devAddr, CURRENT_TEMPERATURE_REGADDR);
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
            return (UInt16)(a+b);
        }
     
        /// <summary>
        /// 串口接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Com_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {           
            int n = com.BytesToRead;
            byte[] buf = new byte[n];
            com.Read(buf, 0, n);
            buffer.AddRange(buf);
            // 0xaa 0x55                        2
            // 源地址                           2
            // 目标地址                         2
            // 帧长度                           1
            // 帧类型 0x01读，0x02写，0x03回复  1
            // 寄存器地址                       1
            // 数据(确认数据是1B，其他是2B)     1/2
            // 校验                             1
            // 0xaa 0x81                        2
            // 确认帧13B，数据帧14B
            while (buffer.Count >= 13) 
            {
                if (buffer[0] == 0xAA && buffer[1] == 0x55)
                {
                    int len = buffer[6];
                    if (buffer.Count < len + 4) //数据区尚未接收完整
                    {
                        break;
                    }
                    ReceiveBytes = new byte[len + 4];
                    buffer.CopyTo(0, ReceiveBytes, 0, len + 4);
                    // 处理
                    handelRepyData(ReceiveBytes);

                    buffer.RemoveRange(0, len + 4);
                }
                else
                {
                    buffer.RemoveAt(0);
                }
            }
        }
        // 舵机反馈数据处理
        // xx 数据
        // -1 写入成功
        // -2 写入失败
        private void handelRepyData(byte[] repyData)
        {
            // 数据低字节可能是0xaa 10
            // 校验字节可能是0xaa   11
            //if (repyData[10] == 0xaa && repyData[12] != 0xaa)// 低位数据为0xaa
            //{ }
            //else if (repyData[11] == 0xaa)// 校验字节为0xaa
            //{ }
            //else if(repyData[10] == 0xaa && repyData[12] == 0xaa)// 都是0xaa
            //{ }
        }

        //===================================================
        // 临时添加 获取加速度传感器和陀螺仪数据
        //===================================================
        public int[] getRobotAccAndGyro()
        {
            int[] yprValue = new int[3];
            int[] accAndGyro = new int[6];

            byte[] frame = new byte[14];

            // 帧头
            frame[0] = 0xaa;
            frame[1] = 0x55;
            // 源地址
            frame[2] = (byte)(1 / 256);
            frame[3] = (byte)(1 % 256);
            // 目标地址
            frame[4] = (byte)(19 / 256);
            frame[5] = (byte)(19 % 256);
            // 帧长度
            frame[6] = 0x0a;//10
            // 帧类型
            frame[7] = WRITE_REG;//主机发送写
            // 寄存器地址
            frame[8] = 0x00;
            // 数据
            frame[9] = (byte)(0x00 / 256);
            frame[10] = (byte)(0x01 % 256);
            // 校验
            frame[11] = (byte)(frame[2] ^ frame[3] ^ frame[4] ^ frame[5] ^ frame[6] ^ frame[7] ^ frame[8] ^ frame[9] ^ frame[10]);
            // 帧尾
            frame[12] = 0xaa;
            frame[13] = 0x81;

            try
            {
                com.Write(frame, 0, 14);
            }
            catch (Exception ex)
            {
                //发送异常
                kpowrException(ex.Message);
                return accAndGyro;
            }

            // 读取返回数据
            DateTime dt = DateTime.Now;
            while (com.BytesToRead < 7)
            {
                System.Threading.Thread.Sleep(1);

                if (DateTime.Now.Subtract(dt).TotalMilliseconds > 2000) //如果2秒后仍然无数据返回，则视为超时
                {
                    //throw new Exception("舵机无响应");
                    kpowrException("陀螺仪响应超时");
                }
            }
            // 完整数据，为补码形式数据
            byte[] buf = new byte[com.BytesToRead];
            com.Read(buf, 0, buf.Length);

            if (buf[6] == (buf[5] ^ buf[4] ^ buf[3] ^ buf[2] ^ buf[1] ^ buf[0]))
            {
                /* Get acceleration */
                for (int i = 0; i < 3; i++)
                    yprValue[i] = ((Int16)((UInt16)buf[2 * i] << 8) + buf[2 * i + 1]);
            }

            ///* Get acceleration */
            //for (int i = 0; i < 3; i++)
            //    accAndGyro[i] = ((Int16)((UInt16)buf[2 * i] << 8) + buf[2 * i + 1]);
            ///* Get Angular rate */
            //for (int i = 4; i < 7; i++)
            //    accAndGyro[i - 1] = ((Int16)((UInt16)buf[2 * i] << 8) + buf[2 * i + 1]);

            return yprValue;
        }
    }
}
