using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using xna = Microsoft.Xna.Framework;
using URWPGSim2D.Common;
using URWPGSim2D.StrategyLoader;

namespace URWPGSim2D.Strategy
{
    public class Strategy : MarshalByRefObject, IStrategy
    {
        #region reserved code never be changed or removed
        /// <summary>
        /// override the InitializeLifetimeService to return null instead of a valid ILease implementation
        /// to ensure this type of remote object never dies
        /// </summary>
        /// <returns>null</returns>
        public override object InitializeLifetimeService()
        {
            //return base.InitializeLifetimeService();
            return null; // makes the object live indefinitely
        }
        #endregion

        /// <summary>
        /// 决策类当前对象对应的仿真使命参与队伍的决策数组引用 第一次调用GetDecision时分配空间
        /// </summary>
        private Decision[] decisions = null;

        /// <summary>
        /// 获取队伍名称 在此处设置参赛队伍的名称
        /// </summary>
        /// <returns>队伍名称字符串</returns>
        public string GetTeamName()
        {
            return "HOLE-IN-ONE";
        }

        /// <summary>
        /// 获取当前仿真使命（比赛项目）当前队伍所有仿真机器鱼的决策数据构成的数组
        /// </summary>
        /// <param name="mission">服务端当前运行着的仿真使命Mission对象</param>
        /// <param name="teamId">当前队伍在服务端运行着的仿真使命中所处的编号
        /// 用于作为索引访问Mission对象的TeamsRef队伍列表中代表当前队伍的元素</param>
        /// <returns>当前队伍所有仿真机器鱼的决策数据构成的Decision数组对象</returns>
        #region
        //调用部分


        //变量
        private xna.Vector3[] Goals; // 球门的坐标数组
        private double[] tTable; // 角速度实际值
        private double[] vTable; // 线速度实际值
        private double r; // 球半径

        public Strategy()
        {
            this.Goals = new xna.Vector3[] { new xna.Vector3(-2137.5f, 0, -1420), //左上球门
                                        new xna.Vector3(10f, 0, -1446), //中上球门
                                         new xna.Vector3(-20f, 0, 1420), //中下球门
                                        new xna.Vector3(-2352f, 0, 1428), //左下球门
                                         new xna.Vector3(2137.5f, 0, -1420), //右下球门
                                        new xna.Vector3(2137.5f, 0, 1420) }; //右上球门

            this.decisions = null;
            this.r = 58.0; // 球半径
            this.vTable = new double[] { 1, 56.0, 67.0, 112.0, 112.0, 112.0, 154.0, 175.0, 227.0, 273.0, 291.0, 298.0, 294.0, 307.0, 317.0 };
            this.tTable = new double[] { -0.37, -0.32, -0.27, -0.22, -0.18, -0.10, -0.06, 0.0, 0.06, 0.1, 0.18, 0.22, 0.27, 0.32, 0.37 };

        }


        //全局变量定义
        int bestball = -2;
        int bestgoal = -2;
        double ballgo = -2;
        int scores = 0;
        int scores2 = 0;
        int count = 0;
        int repeat = 0;
        int exchange = 0;
        int arrivalflag = 0; //未到达临时目标点


        //两点距离
        public double GetDistance(double x1, double z1, double x2, double z2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2.0) + Math.Pow(z1 - z2, 2.0));
        }

        //距离和最近
        public void GetBest(int fishNo, Mission mission, int teamId)
        {
            if (repeat == 0) // 红球已经推完
            {
                if (count > 0 && bestball < 6) // 已经开始推但是红球未推完
                { if (mission.EnvRef.Balls[bestball].PositionMm.X < 2250 && mission.EnvRef.Balls[bestball].PositionMm.X > -2250 && mission.EnvRef.Balls[bestball].PositionMm.Z < 1403 && mission.EnvRef.Balls[bestball].PositionMm.Z > -1403) return; }
                if (bestball == 9) // 已经推到9号球粉球
                { if (mission.EnvRef.Balls[bestball].PositionMm.X < 2250 && mission.EnvRef.Balls[bestball].PositionMm.X > -2250 && mission.EnvRef.Balls[bestball].PositionMm.Z < 1403 && mission.EnvRef.Balls[bestball].PositionMm.Z > -1403) return; }
                if (scores2 == scores && count > 0) return;
            }

            scores = mission.TeamsRef[teamId].Para.Score;
            int i;
            int ballNo = -1;
            int goalNo = -1;


            if (scores < 31) // 还要循环推红球和粉球
            {
                if (scores % 6 == 0) //(6分为1红球+1粉球)红球和粉球推的次数一样，即循环完一次
                {
                    double min = 10000.0;
                    for (i = 0; i < 6; i++)
                    {
                        double ballx = mission.EnvRef.Balls[i].PositionMm.X; // 球x坐标
                        double ballz = mission.EnvRef.Balls[i].PositionMm.Z;
                        int ballsInFieldFlag2 = Convert.ToInt32(mission.HtMissionVariables["BallInFieldFlag"]);
                        if (((ballsInFieldFlag2 << (31 - i)) >> 31) == 1 || ballz > 1500)
                            continue;


                        double goalx = Goals[2].X;
                        double goalz = Goals[2].Z;
                        ballgo = this.GetDistance(ballx, ballz, goalx, goalz);

                        if (ballgo < min)
                        {
                            min = ballgo;
                            ballNo = i;
                            goalNo = 2;
                        }
                    }
                }

                else
                {
                    ballNo = 9;
                    goalNo = 1;
                }
            }
            else if (scores == 31)
            { ballNo = 6; goalNo = 1; }
            else if (scores == 33)
            { ballNo = 7; goalNo = 1; }
            else if (scores == 36)
            { ballNo = 8; goalNo = 1; }
            else
            { ballNo = 9; goalNo = 1; }

            bestgoal = goalNo;
            bestball = ballNo;
            count += 1;
        }

        //得到最近红球
        public void GetBestRedBallNo(int fishNo, Mission mission, int teamId)
        {
            double num = 100000.0;
            int ballNo = -1;
            double x = mission.TeamsRef[teamId].Fishes[fishNo].PolygonVertices[0].X;
            double z = mission.TeamsRef[teamId].Fishes[fishNo].PolygonVertices[0].Z;


            for (int i = 0; i < 6; i++)
            {

                double ballx = mission.EnvRef.Balls[i].PositionMm.X;
                double ballz = mission.EnvRef.Balls[i].PositionMm.Z;

                double fishball = this.GetDistance(x, z, ballx, ballz);

                if (fishball < num && (ballNo != bestball) && !(mission.EnvRef.Balls[i].PositionMm.X > -250 && mission.EnvRef.Balls[i].PositionMm.X < 250 && mission.EnvRef.Balls[i].PositionMm.Z < -700))
                {

                    ballNo = i;
                    num = fishball;

                }

            }
            bestball = ballNo;

        }

        //先求最佳球 得到最近球门
        public void GetBestGoalNo(int fishNo, Mission mission, int teamId) // 没有ballNo的定义
        {
            this.GetBestRedBallNo(0, mission, teamId); // 先得到最近红球
            double num = 100000.0;
            int goalNo = -1;
            double x = mission.EnvRef.Balls[bestball].PositionMm.X; // 水球位置
            double z = mission.EnvRef.Balls[bestball].PositionMm.Z;


            for (int i = 0; i < 6; i++)
            {

                double goalx = Goals[i].X; // 球门位置
                double goalz = Goals[i].Z;

                double ballgoal = this.GetDistance(x, z, goalx, goalz); // 球和门的距离

                if (ballgoal < num && (goalNo != bestgoal)) // 求最小球和门的距离
                {

                    goalNo = i;
                    num = ballgoal;

                }

            }
            bestgoal = goalNo;

        }




        // 固定球 得到最近球门
        public void GetBestGoalNo(int fishNo, Mission mission, int teamId, int ballNo)
        {
            double num = 100000.0;
            int goalNo = -1;
            double x = mission.EnvRef.Balls[ballNo].PositionMm.X;
            double z = mission.EnvRef.Balls[ballNo].PositionMm.Z;


            for (int i = 0; i < 6; i++)
            {

                double goalx = Goals[i].X;
                double goalz = Goals[i].Z;

                double ballgoal = this.GetDistance(x, z, goalx, goalz);

                if (ballgoal < num && (goalNo != bestgoal))
                {

                    goalNo = i;
                    num = ballgoal;

                }

            }
            bestgoal = goalNo;

        }


        #endregion

        public Decision[] GetDecision(Mission mission, int teamId)
        {
            // 决策类当前对象第一次调用GetDecision时Decision数组引用为null
            if (decisions == null)
            {// 根据决策类当前对象对应的仿真使命参与队伍仿真机器鱼的数量分配决策数组空间
                decisions = new Decision[mission.CommonPara.FishCntPerTeam];
            }

            #region 决策计算过程 需要各参赛队伍实现的部分
            #region 策略编写帮助信息
            #endregion
            //请从这里开始编写代码
            int i = 0;
            if (this.decisions == null) // 初始化
            {
                this.decisions = new Decision[mission.CommonPara.FishCntPerTeam];
                this.r = mission.EnvRef.Balls[0].RadiusMm;   // 水球半径
            }

            double x = mission.TeamsRef[teamId].Fishes[0].PositionMm.X; // 当前鱼的位置X坐标
            double z = mission.TeamsRef[teamId].Fishes[0].PositionMm.Z; // 当前鱼的位置Z坐标
            double fish_x = mission.TeamsRef[teamId].Fishes[0].PolygonVertices[0].X;  // 当前鱼头的X坐标
            double fish_z = mission.TeamsRef[teamId].Fishes[0].PolygonVertices[0].Z;  // 当前鱼头的Z坐标


            //最佳函数调用
            scores2 = mission.TeamsRef[teamId].Para.Score;
            //推完粉球要推红球
            if (scores2 % 6 == 0 && scores2 > 0 && bestball == 9) { repeat = 1; arrivalflag = 0; }
            //推完红球要推粉球了
            if (scores2 % 6 != 0 && bestball < 6 && scores2 > 0) { repeat = 1; }
            if (exchange == 1) { repeat = 1; }
            this.GetBest(i, mission, teamId);

            // this.GetBestGoalNo(i, mission, teamId, bestball);
            if (bestball == 9)
            {// 如果要推粉球，设球门为3号球门
                bestgoal = 3;
            }
            else if (bestball < 6)
            {// 如果要推红球，设球门为2号球门
                bestgoal = 2;
            }
            /* if( bestgoal == 3 ){ // 控制球不进3号球门
                 bestgoal = 2;
             }*/
            double ball_x = mission.EnvRef.Balls[bestball].PositionMm.X; // 水球位置X坐标
            double ball_z = mission.EnvRef.Balls[bestball].PositionMm.Z; // 水球位置Z坐标

            double bodyDirectionRad = mission.TeamsRef[teamId].Fishes[0].BodyDirectionRad;  // 当前鱼体方向
            double angularVelocityRadPs = mission.TeamsRef[teamId].Fishes[0].AngularVelocityRadPs;  // 当前鱼角速度
            double d = Math.Atan((Goals[bestgoal].Z - ball_z) / (Goals[bestgoal].X - ball_x));  // 水球与球门连线与X轴夹角
            double temp_x = 0;
            double temp_z = 0;
            double yuZhi = 5 * r; // 临时目标点到击球点的距离*cos(Thete)(投影到x轴的长度)阈值（需调） 5个水球半径


            if (arrivalflag == 0)  //如果还未到达临时击球点
            {
                double tanTheta = (Math.Abs(ball_z - Goals[bestgoal].Z)) / (Math.Abs(ball_x - Goals[bestgoal].X));
                double a = yuZhi;
                double b = a * tanTheta;
                temp_x = ball_x + a;
                temp_z = ball_z - b;
                double fishball = Math.Sqrt(Math.Pow(temp_x - fish_x, 2.0) + Math.Pow(temp_z - fish_z, 2.0));//临时击球点与鱼头的距离
                double midfishballx = Math.Atan((temp_z - z) / (temp_x - x));
                double d1 = Math.Atan((temp_z - ball_z) / (temp_x - ball_x));  // 水球与临时击球点连线与X轴夹角
                //鱼体中心坐标与顶球目标点与X夹角
                if ((temp_x - x) < 0.0)
                {//击球点在鱼的左侧
                    if (midfishballx > 0.0)
                    { // 击球点在鱼的左下方
                        midfishballx -= 3.1415926535897931;
                    }
                    else
                    { // 击球点在鱼的右下方
                        midfishballx += 3.1415926535897931;
                    }
                }
                midfishballx = d1;
                double fishballangle = midfishballx - bodyDirectionRad;//鱼体到顶球目标点的角度（调整角度）
                //鱼体到顶球目标点的角度角度保持在-pi,pi;
                if (fishballangle > 3.1415926535897931)
                {
                    fishballangle -= 6.2831853071795862;
                }
                else if (fishballangle < -3.1415926535897931)
                {
                    fishballangle += 6.2831853071795862;
                }
                double time = fishball / this.vTable[14]; // vTable线速度, time 为 距离/最大速度 得最短时间 //确定最短时间
                if (time > 2.3)
                {
                    time = 2.3;
                }
                double num17 = fishballangle / time; // 角速度 // 确定角速度

                int index = 7;


                if (bestball == 9)
                { //调整角速度
                    if (fishballangle <= 0.0) // 需要左转调整
                    {
                        while ((index > 0) && (this.tTable[index] > num17))  // 确定角速度档位，离散化处理比较，选取最接近角速度的角速度档位
                        {
                            index--;
                        }
                        if ((angularVelocityRadPs / 2.0) < fishballangle)//如果期望转过的角度很大，给最大档位；
                        {  //fishballangle 调整角度, angularVelocityRadPs 当前鱼的角速度
                            index = 12;
                        }
                    }
                    else // 需右转调整
                    {
                        while ((index < 14) && (this.tTable[index] < num17))
                        {
                            index++;
                        }
                        if ((angularVelocityRadPs / 2.0) > fishballangle)
                        {
                            index = 2;
                        }
                    }
                    this.decisions[i].TCode = index;




                    //调整线速度
                    if (fishball > (this.r * 2.0))//鱼与顶球目标点距离大于球的直径
                    {
                        //希望转过的角度绝对值小于pi/6
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 8)
                        {
                            this.decisions[i].VCode = 14;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 6 && Math.Abs(fishballangle) > 3.1415926535897931 / 8)
                        {
                            this.decisions[i].VCode = 13;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 4 && Math.Abs(fishballangle) > 3.1415926535897931 / 6)
                        {
                            this.decisions[i].VCode = 10;
                        }
                        else
                        {
                            this.decisions[i].VCode = 2;
                        }
                    }
                    else
                    {
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 30)//希望转过的角度绝对值小于pi/30
                        {
                            this.decisions[i].VCode = 7;
                        }
                        else
                        {
                            this.decisions[i].VCode = 1;
                        }
                    }
                }







                if (temp_x == (((Goals[bestgoal].X - ball_x) * temp_z + ball_x * Goals[bestgoal].Z - Goals[bestgoal].X * ball_z) / (Goals[bestgoal].Z - ball_z)))  //判断鱼是否已经到反向延长线上
                {
                    arrivalflag = 1;  //已经到达临时击球点附近
                }
            }




            else if (arrivalflag == 1)
            {
                if ((Goals[bestgoal].X - ball_x) < 0.0)
                {// 目标点在水球左侧
                    if (d > 0.0)
                    {
                        d -= 3.1415926535897931;
                    }
                    else
                    {
                        d += 3.1415926535897931;
                    }
                }
                // 水球与目标点连线反向延长线与水球圆周的交点
                double ballgoal_x = ball_x - (this.r * Math.Cos(d));   // X坐标//击球点的X坐标
                double ballgoal_z = ball_z - (this.r * Math.Sin(d));   // Z坐标//击球点的Z坐标
                //顶球目标点与鱼头的距离 fishball
                double fishball = Math.Sqrt(Math.Pow(ballgoal_x - fish_x, 2.0) + Math.Pow(ballgoal_z - fish_z, 2.0));//顶球目标点与鱼头的距离
                double midfishballx = Math.Atan((ballgoal_z - z) / (ballgoal_x - x));
                //鱼体中心坐标与顶球目标点与X夹角
                if ((ballgoal_x - x) < 0.0)
                {//击球点在鱼的左侧
                    if (midfishballx > 0.0)
                    { // 击球点在鱼的左下方
                        midfishballx -= 3.1415926535897931;
                    }
                    else
                    { // 击球点在鱼的右下方
                        midfishballx += 3.1415926535897931;
                    }
                }
                /*else if( (ballgoal_x - x) < 0.0 ){
                    if (midfishballx < 0.0)
                    {
                        midfishballx -= 3.1415926535897931;
                    }
                    else
                    {
                        midfishballx += 3.1415926535897931;
                    }
                }*/
                if (fishball < 15.0)//顶球目标点与鱼头的距离比较小
                {
                    midfishballx = d;//给球与目标点的夹角
                }
                double fishballangle = midfishballx - bodyDirectionRad;//鱼体到顶球目标点的角度（调整角度）
                //鱼体到顶球目标点的角度角度保持在-pi,pi;
                if (fishballangle > 3.1415926535897931)
                {
                    fishballangle -= 6.2831853071795862;
                }
                else if (fishballangle < -3.1415926535897931)
                {
                    fishballangle += 6.2831853071795862;
                }

                double time = fishball / this.vTable[14]; // vTable线速度, time 为 距离/最大速度 得最短时间 //确定最短时间
                if (time > 2.3)
                {
                    time = 2.3;
                }
                double num17 = fishballangle / time; // 角速度 // 确定角速度

                int index = 7;

                //改写过程；
                if (bestball == 9)
                { //调整角速度
                    if (fishballangle <= 0.0) // 需要左转调整
                    {
                        while ((index > 0) && (this.tTable[index] > num17))  // 确定角速度档位，离散化处理比较，选取最接近角速度的角速度档位
                        {
                            index--;
                        }
                        if ((angularVelocityRadPs / 2.0) < fishballangle)//如果期望转过的角度很大，给最大档位；
                        {  //fishballangle 调整角度, angularVelocityRadPs 当前鱼的角速度
                            index = 12;
                        }
                    }
                    else // 需右转调整
                    {
                        while ((index < 14) && (this.tTable[index] < num17))
                        {
                            index++;
                        }
                        if ((angularVelocityRadPs / 2.0) > fishballangle)
                        {
                            index = 2;
                        }
                    }
                    this.decisions[i].TCode = index;




                    //调整线速度
                    if (fishball > (this.r * 2.0))//鱼与顶球目标点距离大于球的直径
                    {
                        //希望转过的角度绝对值小于pi/6
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 8)
                        {
                            this.decisions[i].VCode = 14;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 6 && Math.Abs(fishballangle) > 3.1415926535897931 / 8)
                        {
                            this.decisions[i].VCode = 12;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 4 && Math.Abs(fishballangle) > 3.1415926535897931 / 6)
                        {
                            this.decisions[i].VCode = 8;
                        }
                        else
                        {
                            this.decisions[i].VCode = 1;
                        }
                    }
                    else
                    {
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 30)//希望转过的角度绝对值小于pi/30
                        {
                            this.decisions[i].VCode = 7;
                        }
                        else
                        {
                            this.decisions[i].VCode = 1;
                        }
                    }
                }
                else //if (bestball < 6) // 要推红球
                {
                    if (fishballangle <= 0.0)
                    {
                        while ((index > 0) && (this.tTable[index] > num17))
                        {
                            index--;
                        }
                        if ((angularVelocityRadPs / 2.0) < fishballangle)//如果期望转过的角度很大，给最大档位；
                        {
                            index = 14;
                        }
                    }
                    else
                    {
                        while ((index < 14) && (this.tTable[index] < num17))
                        {
                            index++;
                        }
                        if ((angularVelocityRadPs / 2.0) > fishballangle)
                        {
                            index = 2;
                        }
                    }
                    this.decisions[i].TCode = index;
                    if (fishball > (this.r * 2.0))//鱼与顶球目标点距离大于球的直径
                    {
                        //希望转过的角度绝对值小于pi/6
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 6)
                        {
                            this.decisions[i].VCode = 14;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 5 && Math.Abs(fishballangle) > 3.1415926535897931 / 6)
                        {
                            this.decisions[i].VCode = 12;
                        }
                        else if (Math.Abs(fishballangle) < 3.1415926535897931 / 4 && Math.Abs(fishballangle) > 3.1415926535897931 / 5)
                        {
                            this.decisions[i].VCode = 8;
                        }
                        else
                        {
                            this.decisions[i].VCode = 1;
                        }
                    }
                    else
                    {
                        if (Math.Abs(fishballangle) < 3.1415926535897931 / 24)//希望转过的角度绝对值小于pi/25
                        {
                            this.decisions[i].VCode = 8;
                        }



                        else
                        {
                            this.decisions[i].VCode = 1;
                        }
                    }
                }
            }
            return decisions;
        }

            #endregion
    }
}
