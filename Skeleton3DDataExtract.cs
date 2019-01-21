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
                    case JointType.HandLeft:
                        p[0] = new ThrPoint(j.Position.X, j.Position.Y,j.Position.Z);
                        break;
                    case JointType.WristLeft:
                        p[1] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowLeft:
                        p[2] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowRight:
                        p[3] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight:
                        p[4] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight:
                        p[5] = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft:
                        shoulderLeft = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderRight:
                        shoulderRight = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.SpineShoulder:
                        spineShoulder = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.SpineMid://增加中部的骨骼节点的关节点
                        spineMid = new ThrPoint(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                }
            }

            //新添加方法 对原坐标进行旋转平移处理,获得新的数组
            p = CoordinateNormalization(p, shoulderLeft, shoulderRight, spineMid);



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

        /// <summary>
        /// 坐标标准化操作 对原坐标系进行旋转平移，转换为人体自身的局部坐标系
        /// </summary>
        /// <param name="oldArr"></param>
        /// <param name="shoulderLeft"></param>
        /// <param name="shoulderRight"></param>
        /// <param name="spineMid"></param>
        /// <returns></returns>
        public static ThrPoint[] CoordinateNormalization(ThrPoint[] oldArr, ThrPoint shoulderLeft, ThrPoint shoulderRight, ThrPoint spineMid)
        {
            ThrPoint[] newArr = new ThrPoint[6];
            var mb = Matrix<double>.Build;
            //TODO
            //double shoulderDist =
            //    Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
            //              Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2) +
            //              Math.Pow((shoulderLeft.Z - shoulderRight.Z), 2));


            //求解旋转角度
            double angle = Math.Atan((shoulderRight.Z - shoulderLeft.Z) / (shoulderRight.X - shoulderLeft.X));
            angle = Math.Abs(angle);
            double aa = angle / Math.PI * 180;

            //旋转矩阵
            double cosValue = Math.Cos(angle);
            double sinValue = Math.Sin(angle);
            double[,] arr = new double[3, 3] { { cosValue, 0, -sinValue }, { 0, 1, 0 }, { sinValue, 0, cosValue } };
            var rymatrix = mb.DenseOfArray(arr);

            //旋转
            for (int i = 0; i < oldArr.Length; ++i)
            {
                double[,] kinect_arr = new double[3, 1] { { oldArr[i].X }, { oldArr[i].Y }, { oldArr[i].Z } };
                var kinect_mattrix = mb.DenseOfArray(kinect_arr);
                double[,] tempArr = rymatrix.Multiply(kinect_mattrix).ToArray();
                var thr = new ThrPoint();
                thr.X = tempArr[0, 0];
                thr.Y = tempArr[1, 0];
                thr.Z = tempArr[2, 0];
                newArr[i] = thr;
            }
            //spineMid的旋转
            if (true)
            {
                double[,] kinect_arr = new double[3, 1] { { spineMid.X }, { spineMid.Y }, { spineMid.Z } };
                var kinect_mattrix = mb.DenseOfArray(kinect_arr);
                double[,] tempArr = rymatrix.Multiply(kinect_mattrix).ToArray();
                var thr = new ThrPoint();
                thr.X = tempArr[0, 0];
                thr.Y = tempArr[1, 0];
                thr.Z = tempArr[2, 0];
                spineMid = thr;//spineMid的新值
            }

            //shoulderLeft的旋转
            //if (true)
            //{
            //    double[,] kinect_arr = new double[3, 1] { { shoulderLeft.X }, { shoulderLeft.Y }, { shoulderLeft.Z } };
            //    var kinect_mattrix = mb.DenseOfArray(kinect_arr);
            //    double[,] tempArr = rymatrix.Multiply(kinect_mattrix).ToArray();
            //    var thr = new ThrPoint();
            //    thr.X = tempArr[0, 0];
            //    thr.Y = tempArr[1, 0];
            //    thr.Z = tempArr[2, 0];
            //    shoulderLeft = thr;//spineMid的新值
            //}

            //shoulderRight的旋转
            //if (true)
            //{
            //    double[,] kinect_arr = new double[3, 1] { { shoulderRight.X }, { shoulderRight.Y }, { shoulderRight.Z } };
            //    var kinect_mattrix = mb.DenseOfArray(kinect_arr);
            //    double[,] tempArr = rymatrix.Multiply(kinect_mattrix).ToArray();
            //    var thr = new ThrPoint();
            //    thr.X = tempArr[0, 0];
            //    thr.Y = tempArr[1, 0];
            //    thr.Z = tempArr[2, 0];
            //    shoulderRight = thr;//spineMid的新值
            //}

            //TODO 
            //double shoulderDist2 =
            //    Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
            //              Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2) +
            //              Math.Pow((shoulderLeft.Z - shoulderRight.Z), 2));
            //Console.WriteLine(shoulderDist2 - shoulderDist);

            //偏移
            for (int i = 0; i < newArr.Length; i++)
            {
                newArr[i].X -= spineMid.X;
                newArr[i].Y -= spineMid.Y;
                newArr[i].Z -= spineMid.Z;
            }
            return newArr;
        }

    }
}