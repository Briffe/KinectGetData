using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace KinectGetData
{
    class Utils
    {
        public static double[,] GetNewCord()
        {
            double[,] newArr = new double[6, 3];
            return newArr;
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

           
            double cosValue = Math.Cos(angle);
            double sinValue = Math.Sin(angle);

            //旋转矩阵
            //这是绕Y轴的旋转矩阵
            //double[,] arr = new double[3, 3] { { cosValue, 0, -sinValue }, { 0, 1, 0 }, { sinValue, 0, cosValue } };
            //double[,] arr = new double[3, 3] { { cosValue, 0, sinValue }, { 0, 1, 0 }, { -sinValue, 0, cosValue } };//方向反转

            //这是绕X轴的旋转矩阵
            //double[,] arr = new double[3, 3] { { 1, 0, 0 }, { 0, cosValue, -sinValue }, { 0, sinValue, cosValue } };
            //这是绕Z轴的旋转矩阵
            //double[,] arr = new double[3, 3] { { cosValue, sinValue, 0 }, { -sinValue, cosValue, 0 }, { 0, 0, 1 } };
            double[,] arr = new double[3, 3] { { cosValue, -sinValue, 0 }, { sinValue, cosValue, 0 }, { 0, 0, 1 } };//方向反转
            //arr = new double[3, 3] { { 1, 0, -0 }, { 0, 0, 1 }, { 0, -1, 0 } };
            var rymatrix = mb.DenseOfArray(arr);

            //TODO //将XYZ轴置换为需要的XYZ轴，最后在将其置换回来
            //旋转之前对坐标的处理,将其更换为通用坐标系，也可以用矩阵作处理，但是会比较费时间，这里将进行自行转换
            if (true)
            {
                for (int i = 0; i < oldArr.Length; i++)
                {
                    double x = oldArr[i].Z;
                    double y = oldArr[i].X;
                    double z = oldArr[i].Y;
                    oldArr[i].X = x;
                    oldArr[i].Y = y;
                    oldArr[i].Z = z;
                }
            }

                //旋转之后，得到新的矩阵
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
                //double[,] kinect_arr = new double[3, 1] { { spineMid.X }, { spineMid.Y }, { spineMid.Z } };
                //TODO 这里对进行旋转，并且将坐标更换为通用坐标系
                double[,] kinect_arr = new double[3, 1] { { spineMid.Z }, { spineMid.X }, { spineMid.Y } };

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

            //TODO 将坐标系替换回来
            for (int i = 0; i < newArr.Length; ++i)
            {
               
                double x = oldArr[i].Y;
                double y = oldArr[i].Z;
                double z = oldArr[i].X;
                //Debug.WriteLine(newArr[i].X + " " + newArr[i].Y + " " + newArr[i].Z);
            }
            return newArr;
        }

    }
}
