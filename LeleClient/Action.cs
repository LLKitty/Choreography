using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LeleServo
{
    /// <summary>
    /// 关键帧列表，动作名，动作编号，关键帧数目，骨骼数目，帧开始时间，动作过渡时间
    /// </summary>
    public class Action
    {
        /// <summary>
        /// 可动关节数
        /// </summary>
        public int BoneCount = 17;

        /// <summary>
        /// 关键帧
        /// </summary>
        public List<Frame> KeyFrames;
        
        /// <summary>
        /// 动作名
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// 动作号
        /// </summary>
        public int ActionNum { get; set; }

        /// <summary>
        /// 关键帧数
        /// </summary>
        public int FrameCount
        {
            get
            {
                return KeyFrames.Count;
            }
        }

        /// <summary>
        /// 给定帧索引返回此帧开始时间
        /// </summary>
        /// <param name="Index">帧索引（关键帧列表中第一帧索引为0）</param>
        /// <returns></returns>
        public float FrameStartTime(int Index)
        {
            float sum = 0;
            for (int t = 1; t <= Index; t++)
            {
                sum += KeyFrames[t].Duration; //每个帧的持续时间表示从前一状态到当前帧的运动转变时间！！！！！！！！
            }
            return sum;
        }

        //动作总时间=（第一帧->第二帧）+（第二帧->第三帧）+...+（倒数第二帧->倒数第一帧）的过渡时间
        //关键帧列表中第一帧对应索引为0
        public float ActionDuration
        {
            get
            {
                float sum = 0;
                for (int t = 1; t < KeyFrames.Count; t++)
                {
                    sum += KeyFrames[t].Duration;
                }
                return sum;
            }
        }

        //---------------第一帧假设总是站立状态，并且此帧的持续时间没有意义！！！！！！！！！
        /// <summary>
        /// 生成某时刻的帧,并更新这个时刻的帧的变动关节索引
        /// </summary>
        /// <param name="time">指定的时刻</param>
        /// <returns></returns>
        public Frame LerpFrame(float time)
        {
            if (time <= FrameStartTime(0))
                return KeyFrames[0];//第一帧
            if (time >= ActionDuration)
                return KeyFrames[FrameCount - 1];//最后一帧

            int i = 0;
            for (int t = 0; t < KeyFrames.Count; t++)
            {
                if (FrameStartTime(t) < time && time < FrameStartTime(t + 1))
                    i = t;
            }

            Frame tmpf = Frame.Lerp(KeyFrames[i], KeyFrames[i + 1], BoneCount, (FrameStartTime(i + 1) - time) / KeyFrames[i + 1].Duration);

            MoveBonelist = AngleChangeIndex(time);

            return tmpf;
        }

        /// <summary>
        /// 任意时刻运动关节索引列表
        /// </summary>
        public List<int> MoveBonelist = new List<int>();

        /// <summary>
        /// 指定时间返回此刻的帧控制的关节索引
        /// </summary>
        /// <param name="time">某一个时刻</param>
        /// <returns></returns>
        private List<int> AngleChangeIndex(float time)
        {
            MoveBonelist.Clear();            

            if (time <= FrameStartTime(0))//是0时刻或之前
                return MoveBonelist;
            if (time >= ActionDuration)//是动作结束时间或之后
                return MoveBonelist;

            int i = 0;
            for (int t = 0; t < KeyFrames.Count; t++)
            {
                if (FrameStartTime(t) < time && time < FrameStartTime(t + 1))
                    i = t;
            }

            for (int j = 0; j < KeyFrames[i].BoneAngle.Length; j++)
            {
                if (KeyFrames[i].BoneAngle[j] != KeyFrames[i + 1].BoneAngle[j])
                {
                    MoveBonelist.Add(j);// 需要运动的那个关节号，从0开始
                }
            }
            return MoveBonelist;
        }

        /// <summary>
        /// 转换成总线舵机执行数据
        /// </summary>
        /// <param name="frame">当前帧（任意帧）</param>
        /// <returns>
        /// 返回多个舵机的目标角度
        /// </returns>
        public int[] ToSteeringFrame(Frame frame)
        {
            //for (int j = 0; j < 17; j++)
            //{
            //    MoveBonelist.Add(j);// 需要运动的那个关节号，从0开始
            //}

            int exeindex = 0;
            int[] exeframe = new int[MoveBonelist.Count*2];

            foreach (int i in MoveBonelist)//舵机编号
            {
                int angle = (int)(frame[i]);
                // 将浮点角度值转换为UInt16形式
                exeframe[exeindex++] = i+2;
                exeframe[exeindex++] = angle;
            }
            return exeframe;
        }
    }
}