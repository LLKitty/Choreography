using System.Collections.Generic;

namespace LeleServo
{
    public class ResolveAction
    {
        /// <summary>
        /// 动作名
        /// </summary>
        public string name = null;

        /// <summary>
        /// 动作号
        /// </summary>
        public int number = 0;

        /// <summary>
        /// 动作总数
        /// </summary>
        public int all = 0;

        /// <summary>
        /// 当前动作总帧数
        /// </summary>
        public int fnum = 0;
        
        /// <summary>
        /// 机器人零点
        /// </summary>
        public static string[] zeroPosition = new string[17];

        /// <summary>
        /// 动作序列
        /// </summary>
        public static List<Action> actionlist = new List<Action>(500);

        /// <summary>
        /// 时间速度比例参数
        /// </summary>
        public float c = 1;

        /// <summary>
        /// 读入动作文件，转为执行文件，求取各帧结束时间
        /// </summary>
        /// <param name="actionfile">读入的机器人动作文件</param>
        public ResolveAction(string actionfile)
        {
            System.IO.StreamReader stream = new System.IO.StreamReader(actionfile);
            string strline = null;

            while ((strline = stream.ReadLine()) != null)//是否结束
            {
                if (strline.Length == 0)//空行继续
                {
                    continue;
                }
                string[] head = strline.Split(new char[] { ' ', '{', ',', ':', '}'});//分割
                if (head[0] == "Total")//
                {
                    this.all = int.Parse(head[1]);
                    continue;
                }
                else if (head[0] == "Zero")
                {
                    for (int i = 0; i < 17; i++)
                        zeroPosition[i] = head[i + 1];
                    continue;
                }                
                else if (head[0] == "Num")
                {
                    this.number = int.Parse(head[1]);                    
                    continue;
                }
                else if (head[0] == "Name")
                {
                    this.name = head[1];                    
                    continue;
                }
                else if (head[0] == "N")
                {
                    this.fnum =int.Parse( head[1]);

                    Action action = new Action();
                    action.KeyFrames = new List<Frame>(500);

                    action.ActionNum = this.number;
                    action.ActionName = this.name;

                    for(int a=0;a<this.fnum;a++)
                    {
                        strline = stream.ReadLine();
                        string[] headd = strline.Split(new char[] { ' ', '{', ',', ':', '}' });//分割                        
                        Frame frame = new Frame(17);

                        for (int i = 0; i < 17; i++)
                        {
                            //frame.BoneAngle[i] = Math.Abs(float.Parse(headd[i + 1]) - float.Parse(zeroPosition[i]));//这里可以改骨骼数目
                            //frame.BoneAngle[i] = Math.Abs(float.Parse(headd[i + 1]));//这里可以改骨骼数目
                            //frame.BoneAngle[i] = float.Parse(headd[i + 1]);
                            frame.BoneAngle[i] = float.Parse(headd[i + 1])+ float.Parse(zeroPosition[i]);
                        }
                        frame.Speed = int.Parse(headd[19]);
                        frame.Duration = float.Parse(headd[20])*1.0F;
                        action.KeyFrames.Add(frame);
                    }
                    actionlist.Add(action);//放入动作列表中
                }
            }
            stream.Close();
            stream.Dispose();
        }
    }
}