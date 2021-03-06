﻿//-----------------------------------------------------------------------
// <copyright file="Skeleton2DdataCoordEventArgs.cs" company="Rhemyst and Rymix">
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
    using System.Windows;

    /// <summary>
    /// Takes Kinect SDK Skeletal Frame coordinates and converts them intoo a format useful to th DTW 
    /// 获取Kinect SDK Skeletal Frame坐标并将其转换为对DTW有用的格式
    /// </summary>
    internal class Skeleton3DdataCoordEventArgs
    {
        /// <summary>
        /// Positions of the elbows, the wrists and the hands (placed from left to right)  肘部，手腕和手的位置（从左到右放置）
        /// </summary>
        //private readonly Point[] _points;

        /// <summary>
        ///  肘部，手腕和手的位置（从左到右放置）
        /// </summary>
        private readonly ThrPoint[] _points;

        /// <summary>
        /// 初始化Skeleton2DdataCoordEventArgs类的新实例
        /// </summary>
        /// <param name="points">The points we need to handle in this class</param>
        public Skeleton3DdataCoordEventArgs(ThrPoint[] points)
        {
            //TODO 如果不用浅表副本复制会怎么样
            _points = (ThrPoint[])points.Clone();
        }

        /// <summary>
        /// Gets the point at a certain index 获取某个索引处的点
        /// </summary>
        /// <param name="index">The index we wish to retrieve</param>
        /// <returns>The point at the sent index</returns>
        public ThrPoint GetPoint(int index)
        {
            return _points[index];
        }


        /// <summary>
        /// Gets the coordinates of our _points 获取_points的坐标 
        /// </summary>
        /// <returns>The coordinates of our _points</returns>
        internal double[] GetCoords()
        {
            var tmp = new double[_points.Length * 3];
            for (int i = 0; i < _points.Length; i++)
            {
                tmp[3 * i] = _points[i].X;
                tmp[(3 * i) + 1] = _points[i].Y;
                tmp[(3 * i) + 2] = _points[i].Z;
            }

            return tmp;
        }
    }
}