using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.IO;

using System.Collections.Generic;
using System.Collections;

namespace LeleClient
{
    class RecordingAudio
    {
        static int sum = 0;
        static Timer timer;

        public static uint SND_ASYNC = 0x0001;

        /// <summary>
        /// 获取当前是否是正在录制状态
        /// </summary>
        public static bool IsRecordingAudio { get; private set; }

        //调用API函数
        [DllImport("winmm.dll", EntryPoint = "waveOutGetNumDevs")]
        public static extern int waveOutGetNumDevs();

        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();

        [DllImport("winmm.dll", EntryPoint = "waveInGetDevCaps")]
        public static extern int waveInGetDevCapsA(int uDeviceID, ref WaveInCaps lpCaps, int uSize);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern bool PlaySound(
            string pszSound,
            UIntPtr hmod,
            uint fdwSound
        );

        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(
            string lpstrCommand,
            string lpstrReturnString,
            int uReturnLength,
            int hwndCallback
        );

        [DllImport("winmm.dll")]
        public static extern long sndPlaySound(
            string lpszSoundName,
            uint uFlags
        );

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct WaveInCaps
        {
            public short wMid;
            public short wPid;
            public int vDriverVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szPname;
            public uint dwFormats;
            public short wChannels;
            public short wReserved1;
        }

        public static string path;
        /// <summary>
        /// 开始录制
        /// </summary>
        public static void Start()
        {
            path = System.IO.Path.GetTempFileName().Replace(".tmp", ".wav");
            mciSendString("close movie", "", 0, 0);

            mciSendString("set wave bitpersample 16", "", 0, 0);
            mciSendString("set wave samplespersec 16000", "", 0, 0);
            mciSendString("set wave channels 1", "", 0, 0);
            mciSendString("set wave format tag pcm", "", 0, 0);
            mciSendString("open new type WAVEAudio alias movie", "", 0, 0);
            mciSendString("record movie", "", 0, 0);

            timer = new Timer(200);// 间隔200ms 时间递增
            timer.Elapsed += timer_Tick;
            sum = 0;
            timer.Enabled = true;
            IsRecordingAudio = true;
        }

        private static void timer_Tick(object sender, ElapsedEventArgs e)
        {
            sum++;
        }

        private static string path2;
        /// <summary>
        /// 停止录制，返回录制时间(进1原则)
        /// </summary>
        /// <returns></returns>
        public static int Stop()
        {
            mciSendString("stop movie", "", 0, 0);
            mciSendString("save movie " + path, "", 0, 0);
            mciSendString("close movie", "", 0, 0);

            //path2 = Directory.GetCurrentDirectory() + @"\Voice";
            path2 = @"C:\Users\wangxu\Documents\Voice";

            if (Directory.Exists(path2) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(path2);
            }
            else
            {
                bool isrewrite = true; // true=覆盖已存在的同名文件,false则反之
                //System.IO.File.Copy(path, path2 + @"\test.wav", isrewrite);
                System.IO.File.Copy(path, path2 + @"\sample.wav", isrewrite);
            }

            IsRecordingAudio = false;
            timer.Enabled = false;
            timer.Dispose();
            int s = sum * 200;
            return s % 1000 == 0 ? s / 1000 : s / 1000 + 1;///进1原则
        }
        
        /// <summary>
        /// 播放
        /// </summary>
        public static void  play()
        {
            //path2 = Directory.GetCurrentDirectory() + @"\Voice";
            path2 = @"C:\Users\wangxu\Documents\Voice\sample.wav";
            //sndPlaySound(path2 + @"\test.wav", SND_ASYNC);
            sndPlaySound(path2, SND_ASYNC);
        }
       
        public static ArrayList arrLst = new ArrayList();

        // 获取可以用于记录声音的设备的列表
        public static void clsRecDevices() //fill sound recording devices array
        {
            int waveInDevicesCount = waveInGetNumDevs(); //get total

            if (waveInDevicesCount > 0)
            {
                for (int uDeviceID = 0; uDeviceID < waveInDevicesCount; uDeviceID++)
                {
                    WaveInCaps waveInCaps = new WaveInCaps();
                    waveInGetDevCapsA(uDeviceID, ref waveInCaps, Marshal.SizeOf(typeof(WaveInCaps)));
                    arrLst.Add(new string(waveInCaps.szPname).Remove(new string(waveInCaps.szPname).IndexOf('\0')).Trim());
                }
            }
        }
    }
}
