using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace LeleServo
{
    /// <summary>
    /// 各个骨骼角度，当前帧持续时间
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// 每个可动关节的角度
        /// </summary>
        public float[] BoneAngle;
        /// <summary>
        /// 运动速度
        /// </summary>
        public int Speed { get; set; }
        /// <summary>
        /// 运动到当前帧的时间
        /// </summary>
        public float Duration { get; set; }

        public float this[int Index]
        {
            get
            {
                return BoneAngle[Index];
            }
            set
            {
                BoneAngle[Index] = value;
            }
        }

        /// <summary>
        /// 指定骨骼数，新建一个姿态。所有的角度数据都是0,持续时间是1s
        /// </summary>
        /// <param name="Count">骨骼数目</param>
        public Frame(int Count)
        {
            BoneAngle = new float[Count];
            Duration = 1;
        }

        /// <summary>
        /// 计算两个帧的插值。
        /// </summary>
        /// <param name="f1">前一帧</param>
        /// <param name="f2">后一帧</param>
        /// <param name="C">可动关节数</param>
        /// <param name="d">过度剩余时间/总过度时间</param>
        /// <returns></returns>
        static public Frame Lerp(Frame f1, Frame f2, int C, float d)
        {
            Frame f3 = new Frame(C);
            for (int t = 0; t < C; t++)
            {
                f3[t] = f1[t] + (f2[t] - f1[t]) * (1 - d);//在前一帧的基础上变动多少
            }
            return f3;
        }
    }
}