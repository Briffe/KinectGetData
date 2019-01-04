﻿//-----------------------------------------------------------------------
// <copyright file="DtwGestureRecognizer.cs" company="Rhemyst and Rymix">
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

using System.Diagnostics;

namespace KinectGetData
{
    using System;
    using System.Collections;

    /// <summary>
    /// Dynamic Time Warping nearest neighbour sequence comparison class. 动态时间扭曲最近邻序列比较类。
    /// Called 'Gesture Recognizer' but really it can work with any vectors  被称为'手势识别器'，但实际上它可以与任何载体一起使用
    /// </summary>
    internal class DtwGestureRecognizer
    {
        /*
         * By Rhemyst. Dude's a freakin' genius. Also he can do the Rubik's Cube. I mean REALLY do the Rubik's Cube.
         * 
         * http://social.msdn.microsoft.com/Forums/en-US/kinectsdknuiapi/thread/4a428391-82df-445a-a867-557f284bd4b1
         * http://www.youtube.com/watch?v=XsIoN96yF3E
         */

        /// <summary>
        /// Size of obeservations vectors. 观察向量的大小 默认12
        /// </summary>
        private readonly int _dimension;
        //TODO 待更改 将所有的dtw的变量暂时都更改为可访问的 并且可更改 
        //private readonly double _globalThreshold;

        /// <summary>
        /// Maximum distance between the last observations of each sequence. 每个序列的最后观察之间的最大距离。 默认2
        /// </summary>
        public  double _firstThreshold;

        /// <summary>
        /// Minimum length of a gesture before it can be recognised 可以识别之前手势的最小长度 默认是10
        /// </summary>
        public double _minimumLength;

        /// <summary>
        /// Maximum DTW distance between an example and a sequence being classified. 示例与被分类的序列之间的最大DTW距离。 默认0.6
        /// </summary>
        public double _globalThreshold;

        /// <summary>
        /// The gesture names. Index matches that of the sequences array in _sequences  手势名称。 索引与_sequences中的序列数组匹配
        /// </summary>
        private readonly ArrayList _labels;

        /// <summary>
        /// Maximum vertical or horizontal steps in a row. 一行中的最大垂直或水平步长。默认2
        /// </summary>
        public int _maxSlope;

        /// <summary>
        /// The recorded gesture sequences  录制的手势序列
        /// </summary>
        private readonly ArrayList _sequences;

        /// <summary>
        /// Initializes a new instance of the DtwGestureRecognizer class 初始化DtwGestureRecognizer类的新实例
        /// First DTW constructor
        /// </summary>
        /// <param name="dim">Vector size</param>
        /// <param name="threshold">Maximum distance between the last observations of each sequence</param>
        /// <param name="firstThreshold">Minimum threshold</param>
        public DtwGestureRecognizer(int dim, double threshold, double firstThreshold, double minLen)
        {
            _dimension = dim;
            _sequences = new ArrayList();
            _labels = new ArrayList();
            _globalThreshold = threshold;
            _firstThreshold = firstThreshold;
            _maxSlope = int.MaxValue;
            _minimumLength = minLen;
        }

        /// <summary>
        /// Initializes a new instance of the DtwGestureRecognizer class 初始化DtwGestureRecognizer类的第二个DTW构造函数的新实例
        /// Second DTW constructor  第二个DTW构造函数
        /// </summary> 
        /// <param name="dim">Vector size</param>
        /// <param name="threshold">Maximum distance between the last observations of each sequence 每个序列的最后观察之间的最大距离</param>
        /// <param name="firstThreshold">Minimum threshold 最低门槛</param>
        /// <param name="ms">Maximum vertical or horizontal steps in a row 一行中的最大垂直或水平步长</param>
        public DtwGestureRecognizer(int dim, double threshold, double firstThreshold, int ms, double minLen)
        {
            // _dtw = new DtwGestureRecognizer(12, 0.6, 2, 2, 10);
            _dimension = dim;
            _sequences = new ArrayList();
            _labels = new ArrayList();
            _globalThreshold = threshold;
            _firstThreshold = firstThreshold;
            _maxSlope = ms;
            _minimumLength = minLen;
        }

        /// <summary>
        /// Add a seqence with a label to the known sequences library.
        /// The gesture MUST start on the first observation of the sequence and end on the last one.
        /// Sequences may have different lengths.
        /// 将带有标签的序列添加到已知序列库中。
        /// 手势必须从序列的第一次观察开始，到最后一次观察结束。
        /// 序列可能有不同的长度。
        /// </summary>
        /// <param name="seq">The sequence</param>
        /// <param name="lab">Sequence name</param>
        public void AddOrUpdate(ArrayList seq, string lab)
        {
            // First we check whether there is already a recording for this label. If so overwrite it, otherwise add a new entry 
            // 首先，我们检查是否已有此标签的录音。 如果是这样覆盖它，否则添加一个新条目
            int existingIndex = -1;

            for (int i = 0; i < _labels.Count; i++)
            {
                if ((string)_labels[i] == lab)
                {
                    existingIndex = i;
                }
            }

            // If we have a match then remove the entries at the existing index to avoid duplicates. We will add the new entries later anyway
            // 如果我们有匹配，则删除现有索引处的条目以避免重复。 无论如何，我们稍后会添加新条目
            if (existingIndex >= 0)
            {
                _sequences.RemoveAt(existingIndex);
                _labels.RemoveAt(existingIndex);
            }

            // Add the new entries
            _sequences.Add(seq);
            _labels.Add(lab);
        }

        /// <summary>
        /// Recognize gesture in the given sequence.
        /// It will always assume that the gesture ends on the last observation of that sequence.
        /// If the distance between the last observations of each sequence is too great, or if the overall DTW distance between the two sequence is too great, no gesture will be recognized.
        /// 以给定的顺序识别手势。它总是假设手势在该序列的最后一次观察结束时结束。如果每个序列的最后观察之间的距离太大，或者如果两个序列之间的整体DTW距离太大，则不会识别出手势。
        /// </summary>
        /// <param name="seq">The sequence to recognise</param>
        /// <returns>The recognised gesture name</returns>
        public string Recognize(ArrayList seq)
        {
            double minDist = double.PositiveInfinity;
            string classification = "__UNKNOWN";
            for (int i = 0; i < _sequences.Count; i++)
            {
                var example = (ArrayList) _sequences[i];
                ////Debug.WriteLine(Dist2((double[]) seq[seq.Count - 1], (double[]) example[example.Count - 1]));
                if (Dist2((double[]) seq[seq.Count - 1], (double[]) example[example.Count - 1]) < _firstThreshold)
                {
                    double d = Dtw(seq, example) / example.Count;
                    if (d < minDist)
                    {
                        minDist = d;
                        classification = (string)_labels[i];
                    }
                }
            }

            return (minDist < _globalThreshold ? classification : "__UNKNOWN") + " " /*+minDist.ToString()*/;
        }

        /// <summary>
        /// Recognize gesture in the given sequence.
        /// It will always assume that the gesture ends on the last observation of that sequence.
        /// If the distance between the last observations of each sequence is too great, or if the overall DTW distance between the two sequence is too great, no gesture will be recognized.
        /// 以给定的顺序识别手势。它总是假设手势在该序列的最后一次观察结束时结束。如果每个序列的最后观察之间的距离太大，或者如果两个序列之间的整体DTW距离太大，则不会识别出手势。
        /// </summary>
        /// <param name="seq">The sequence to recognise</param>
        /// <returns>The recognised gesture name</returns>
        public string RecognizeUse3D(ArrayList seq)
        {
            double minDist = double.PositiveInfinity;
            string classification = "__UNKNOWN";
            for (int i = 0; i < _sequences.Count; i++)
            {
                var example = (ArrayList)_sequences[i];
                ////Debug.WriteLine(Dist2((double[]) seq[seq.Count - 1], (double[]) example[example.Count - 1]));
                if (Dist2Use3D((double[])seq[seq.Count - 1], (double[])example[example.Count - 1]) < _firstThreshold)
                {
                    double d = DtwUse3D(seq, example) / example.Count;
                    if (d < minDist)
                    {
                        minDist = d;
                        classification = (string)_labels[i];
                    }
                }
            }
            return (minDist < _globalThreshold ? classification : "__UNKNOWN") + " " /*+minDist.ToString()*/;
        }


        #region RetrieveText 3d 和2d
        /// <summary>
        /// Retrieves a text represeantation of the _label and its associated _sequence
        /// For use in dispaying debug information and for saving to file
        /// 检索_label及其关联的_sequence的文本表示。用于显示调试信息和保存到文件
        /// </summary>
        /// <returns>A string containing all recorded gestures and their names</returns>
        public string RetrieveText()
        {
            string retStr = String.Empty;

            if (_sequences != null)
            {
                // Iterate through each gesture
                for (int gestureNum = 0; gestureNum < _sequences.Count; gestureNum++)
                {
                    // Echo the label
                    retStr += _labels[gestureNum] + "\r\n";

                    int frameNum = 0;

                    //Iterate through each frame of this gesture
                    foreach (double[] frame in ((ArrayList)_sequences[gestureNum]))
                    {
                        // Extract each double
                        foreach (double dub in (double[])frame)
                        {
                            retStr += dub + "\r\n";
                        }

                        // Signifies end of this double
                        retStr += "~\r\n";

                        frameNum++;
                    }

                    // Signifies end of this gesture
                    retStr += "----";
                    if (gestureNum < _sequences.Count - 1)
                    {
                        retStr += "\r\n";
                    }
                }
            }

            return retStr;
        }

        /// <summary>
        ///3D模式 
        /// 检索_label及其关联的_sequence的文本表示。用于显示调试信息和保存到文件
        /// </summary>
        /// <returns>A string containing all recorded gestures and their names</returns>
        public string RetrieveText3D()
        {
            string retStr = String.Empty;

            if (_sequences != null)
            {
                // Iterate through each gesture
                for (int gestureNum = 0; gestureNum < _sequences.Count; gestureNum++)
                {
                    // Echo the label
                    retStr += _labels[gestureNum] + "\r\n";

                    int frameNum = 0;

                    //Iterate through each frame of this gesture
                    foreach (double[] frame in ((ArrayList)_sequences[gestureNum]))
                    {
                        // Extract each double
                        foreach (double dub in (double[])frame)
                        {
                            retStr += dub + "\r\n";
                        }

                        // Signifies end of this double
                        retStr += "~\r\n";

                        frameNum++;
                    }

                    // Signifies end of this gesture
                    retStr += "----";
                    if (gestureNum < _sequences.Count - 1)
                    {
                        retStr += "\r\n";
                    }
                }
            }

            return retStr;
        }
        #endregion

        /// <summary>
        /// Compute the min DTW distance between seq2 and all possible endings of seq1.
        /// 计算seq2与seq1的所有可能结尾之间的最小DTW距离。
        /// </summary>
        /// <param name="seq1">The first array of sequences to compare</param>
        /// <param name="seq2">The second array of sequences to compare</param>
        /// <returns>The best match</returns>
        public double Dtw(ArrayList seq1, ArrayList seq2)
        {
            // Init
            var seq1R = new ArrayList(seq1);
            seq1R.Reverse();
            var seq2R = new ArrayList(seq2);
            seq2R.Reverse();
            var tab = new double[seq1R.Count + 1, seq2R.Count + 1];
            var slopeI = new int[seq1R.Count + 1, seq2R.Count + 1];
            var slopeJ = new int[seq1R.Count + 1, seq2R.Count + 1];

            for (int i = 0; i < seq1R.Count + 1; i++)
            {
                for (int j = 0; j < seq2R.Count + 1; j++)
                {
                    tab[i, j] = double.PositiveInfinity;
                    slopeI[i, j] = 0;
                    slopeJ[i, j] = 0;
                }
            }

            tab[0, 0] = 0;

            // Dynamic computation of the DTW matrix.
            for (int i = 1; i < seq1R.Count + 1; i++)
            {
                for (int j = 1; j < seq2R.Count + 1; j++)
                {
                    if (tab[i, j - 1] < tab[i - 1, j - 1] && tab[i, j - 1] < tab[i - 1, j] && slopeI[i, j - 1] < _maxSlope)
                    {
                        tab[i, j] = Dist2((double[]) seq1R[i - 1], (double[]) seq2R[j - 1]) + tab[i, j - 1];
                        slopeI[i, j] = slopeJ[i, j - 1] + 1;
                        slopeJ[i, j] = 0;
                    }
                    else if (tab[i - 1, j] < tab[i - 1, j - 1] && tab[i - 1, j] < tab[i, j - 1] && slopeJ[i - 1, j] < _maxSlope)
                    {
                        tab[i, j] = Dist2((double[]) seq1R[i - 1], (double[]) seq2R[j - 1]) + tab[i - 1, j];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = slopeJ[i - 1, j] + 1;
                    }
                    else
                    {
                        tab[i, j] = Dist2((double[]) seq1R[i - 1], (double[]) seq2R[j - 1]) + tab[i - 1, j - 1];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = 0;
                    }
                }
            }

            // Find best between seq2 and an ending (postfix) of seq1. 在seq2和seq1的结尾（后缀）之间找到最佳。
            double bestMatch = double.PositiveInfinity;
            for (int i = 1; i < (seq1R.Count + 1) - _minimumLength; i++)
            {
                if (tab[i, seq2R.Count] < bestMatch)
                {
                    bestMatch = tab[i, seq2R.Count];
                }
            }

            return bestMatch;
        }

        /// <summary>
        /// Compute the min DTW distance between seq2 and all possible endings of seq1.
        /// 计算seq2与seq1的所有可能结尾之间的最小DTW距离。
        /// </summary>
        /// <param name="seq1">The first array of sequences to compare</param>
        /// <param name="seq2">The second array of sequences to compare</param>
        /// <returns>The best match</returns>
        public double DtwUse3D(ArrayList seq1, ArrayList seq2)
        {
            // Init
            var seq1R = new ArrayList(seq1);
            seq1R.Reverse();
            var seq2R = new ArrayList(seq2);
            seq2R.Reverse();
            var tab = new double[seq1R.Count + 1, seq2R.Count + 1];
            var slopeI = new int[seq1R.Count + 1, seq2R.Count + 1];
            var slopeJ = new int[seq1R.Count + 1, seq2R.Count + 1];

            for (int i = 0; i < seq1R.Count + 1; i++)
            {
                for (int j = 0; j < seq2R.Count + 1; j++)
                {
                    tab[i, j] = double.PositiveInfinity;
                    slopeI[i, j] = 0;
                    slopeJ[i, j] = 0;
                }
            }

            tab[0, 0] = 0;

            // Dynamic computation of the DTW matrix.
            for (int i = 1; i < seq1R.Count + 1; i++)
            {
                for (int j = 1; j < seq2R.Count + 1; j++)
                {
                    if (tab[i, j - 1] < tab[i - 1, j - 1] && tab[i, j - 1] < tab[i - 1, j] && slopeI[i, j - 1] < _maxSlope)
                    {
                        tab[i, j] = Dist2Use3D((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i, j - 1];
                        slopeI[i, j] = slopeJ[i, j - 1] + 1;
                        slopeJ[i, j] = 0;
                    }
                    else if (tab[i - 1, j] < tab[i - 1, j - 1] && tab[i - 1, j] < tab[i, j - 1] && slopeJ[i - 1, j] < _maxSlope)
                    {
                        tab[i, j] = Dist2Use3D((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i - 1, j];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = slopeJ[i - 1, j] + 1;
                    }
                    else
                    {
                        tab[i, j] = Dist2Use3D((double[])seq1R[i - 1], (double[])seq2R[j - 1]) + tab[i - 1, j - 1];
                        slopeI[i, j] = 0;
                        slopeJ[i, j] = 0;
                    }
                }
            }

            // Find best between seq2 and an ending (postfix) of seq1. 在seq2和seq1的结尾（后缀）之间找到最佳。
            double bestMatch = double.PositiveInfinity;
            for (int i = 1; i < (seq1R.Count + 1) - _minimumLength; i++)
            {
                if (tab[i, seq2R.Count] < bestMatch)
                {
                    bestMatch = tab[i, seq2R.Count];
                }
            }

            return bestMatch;
        }


        /// <summary>
        /// Computes a 1-distance between two observations. (aka Manhattan distance).
        /// 计算两个观测值之间的1距离。 （又名曼哈顿距离）。
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Manhattan distance between the two points</returns>
        private double Dist1(double[] a, double[] b)
        {
            double d = 0;
            for (int i = 0; i < _dimension; i++)
            {
                d += Math.Abs(a[i] - b[i]);
            }

            return d;
        }

        /// <summary>
        /// Computes a 2-distance between two observations. (aka Euclidian distance).
        /// 计算两个观测值之间的2距离。 （又名欧几里德距离）。
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Euclidian distance between the two points</returns>
        private double Dist2(double[] a, double[] b)
        {
            double d = 0;
            for (int i = 0; i < _dimension; i++)
            {
                d += Math.Pow(a[i] - b[i], 2);
            }

            return Math.Sqrt(d);
        }

        /// <summary>
        /// 计算两个观测值之间的2距离。 （又名欧几里德距离）。
        /// </summary>
        /// <param name="a">Point a (double)</param>
        /// <param name="b">Point b (double)</param>
        /// <returns>Euclidian distance between the two points</returns>
        private double Dist2Use3D(double[] a, double[] b)
        {
            double d = 0;
            //for (int i = 0; i < _dimension; i++)
            //TODO 2D更改为3D。默认的_dimension大小为12更改为18
            for (int i = 0; i < 18; i++)
            {
                d += Math.Pow(a[i] - b[i], 2);
            }

            return Math.Sqrt(d);
        }

    }
}