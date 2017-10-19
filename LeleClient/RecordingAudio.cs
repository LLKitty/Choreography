using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.IO;

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

        /// <summary>
        /// 开始录制
        /// </summary>
        public static void Start()
        {
            mciSendString("set wave bitpersample 16", "", 0, 0);
            mciSendString("set wave samplespersec 16000", "", 0, 0);
            mciSendString("set wave channels 1", "", 0, 0);
            mciSendString("set wave format tag pcm", "", 0, 0);
            mciSendString("open new type WAVEAudio alias movie", "", 0, 0);
            mciSendString("record movie", "", 0, 0);

            timer = new Timer(200);// 间隔200ms 采集一次
            timer.Elapsed += timer_Tick;
            sum = 0;
            timer.Enabled = true;
            IsRecordingAudio = true;
        }

        private static void timer_Tick(object sender, ElapsedEventArgs e)
        {
            sum++;
        }

        /// <summary>
        /// 停止录制，返回录制时间(进1原则)
        /// </summary>
        /// <returns></returns>
        public static int Stop()
        {
            string path = Directory.GetCurrentDirectory() + @"\Voice\";
                        
            if (Directory.Exists(path) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(path);
            }

            mciSendString("stop movie", "", 0, 0);
            mciSendString("save movie " + path + DateTime.Now.ToString("yyyyMMddHHmmss") + ".wav", "", 0, 0);
            mciSendString("close movie", "", 0, 0);

            IsRecordingAudio = false;
            timer.Enabled = false;
            timer.Dispose();
            int s = sum * 200;
            return s % 1000 == 0 ? s / 1000 : s / 1000 + 1;///进1原则
        }
    }
}
