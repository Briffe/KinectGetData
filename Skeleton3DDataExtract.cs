//-----------------------------------------------------------------------
// <copyright file="Skeleton2DDataExtract.cs" company="Rhemyst and Rymix">
//     Open Source. Do with this as you will. Include this statement or 
//     don't - whatever you like.
//
//     No warranty or support given. No guarantees this will work or meet
//     your needs. Some elements of this project have been tailored to
//     the authors' needs and therefore don't necessarily follow best
//     practice. Subsequent releases of this project will (probably) not
//     be compatible with different versions, so whatever you do, don't
//     overwrite your implementation with any new releases of this
//     project!
//
//     Enjoy working with Kinect!
// </copyright>
//-----------------------------------------------------------------------

namespace KinectGetData
{
    using System;
    using System.Windows;
    using Microsoft.Kinect;
    using MathNet.Numerics.LinearAlgebra;

    /// <summary>
    /// This class is used to transform the data of the skeleton
    /// </summary>
    internal class Skeleton3DDataExtract
    {
        /// <summary>
        /// Skeleton2DdataCoordEventHandler delegate  该类用于转换骨架的数据
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 2Ddata Coord Event Args</param>
        public delegate void Skeleton3DdataCoordEventHandler(object sender, Skeleton3DdataCoordEventArgs a);

        /// <summary>
        /// The Skeleton 2Ddata Coord Ready event
        /// </summary>
        public static event Skeleton3DdataCoordEventHandler Skeleton3DdataCoordReady;

        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW  压缩Kinect SDK的Skeleton Data并吐出一种对DTW更有用的格式
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static void ProcessData(Body data)
        // public static void ProcessData(Skeleton data)
        {
            // Extract the coordinates of the points.  提取点的坐标。
            var p = new ThrPoint[6];
            ThrPoint shoulderRight = new ThrPoint(), shoulderLeft = new ThrPoint();
            ThrPoint spineShoulder = new ThrPoint();
            ThrPoint spineMid  = new ThrPoint();  
            // foreach (Joint j in data.Joints)
            foreach (Joint j in data.Joints.Values)
            {
                switch (j.JointType)
                {
                    case JointType.HandLeft://7
                        p[0] = new ThrPoint(j.Position.X, j.Position.Y,j.Position.Z);
                        break;
                    case JointType.WristLeft:// 6
                        p[1] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowLeft:// 5
                        p[2] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowRight://9
                        p[3] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight://10
                        p[4] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight://11
                        p[5] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft://4
                        shoulderLeft = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderRight://8
                        shoulderRight = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.SpineShoulder:
                        spineShoulder = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.SpineMid://1 增加中部的骨骼节点的关节点
                        spineMid = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            //新添加方法 对原坐标进行旋转平移处理,获得新的数组
            p = Utils.CoordinateNormalization(p, shoulderLeft, shoulderRight, spineMid);



            //TODO 已经被新的坐标旋转平移取代
            // Centre the data  使数据居中 这里采用的是
            //var center = new ThrPoint((shoulderLeft.X + shoulderRight.X) / 2, (shoulderLeft.Y + shoulderRight.Y) / 2, (shoulderLeft.Z + shoulderRight.Z) / 2);
            //var center = new ThrPoint(spineShoulder.X, spineShoulder.Y,spineShoulder.Z);
            //for (int i = 0; i < 6; i++)
            //{
            //    p[i].X -= center.X;
            //    p[i].Y -= center.Y;
            //    p[i].Z -= center.Z;
            //}

            // Normalization of the coordinates  坐标的标准化  shoulderDist不会因为坐标的转换而发生改变
            double shoulderDist =
                Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                          Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2) +
                          Math.Pow((shoulderLeft.Z - shoulderRight.Z), 2));
            for (int i = 0; i < 6; i++)
            {
                p[i].X /= shoulderDist;
                p[i].Y /= shoulderDist;
                p[i].Z /= shoulderDist;
            }
            
            // Launch the event! 发起活动！
            if (p!=null)
            {
                Skeleton3DdataCoordReady(null, new Skeleton3DdataCoordEventArgs(p));
            }
        }

       

    }
}