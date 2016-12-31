using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    /// <summary>
    /// 2次元配列の参照とインデックスを保持するためのクラス
    /// </summary>
    public class Rank2ArrayPointer<T> : IArrayPointer<T>
    {
        private int _dim1Len;

        private T[,] array;
        public T[,] Array
        {
            get
            {
                return array;
            }
            private set
            {
                array = value;
                _dim1Len = array.GetLength(1);
            }
        }

        public int Top0 { get; private set; }
        public int Top1 { get; private set; }

        public int Current0 { get; private set; }
        public int Current1 { get; private set; }

        /// <summary>
        /// 指定した配列の先頭を指すオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public Rank2ArrayPointer(T[,] array)
        {
            Array = array;
            Current0 = Top0 = 0;
            Current1 = Top1 = 0;
        }

        /// <summary>
        /// 指定した配列の指定したインデックスを指すオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public Rank2ArrayPointer(T[,] array, int top0, int top1)
        {
            Array = array;
            Current0 = Top0 = top0;
            Current1 = Top1 = top1;
        }

        /// <summary>
        /// 指定したオブジェクトをコピーしたオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public Rank2ArrayPointer(Rank2ArrayPointer<T> ptr)
        {
            Array = ptr.Array;
            Top0 = ptr.Top0;
            Top1 = ptr.Top1;
            Current0 = ptr.Current0;
            Current1 = ptr.Current1;
        }

        public T this[int i, int j]
        {
            get
            {
                return Array[Current0 + i, Current1 + j];
            }
            set
            {
                Array[Current0 + i, Current1 + j] = value;
            }
        }

        /// <summary>
        /// 一次元のインデックスでアクセス
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public T this[int idx]
        {
            get
            {
                var pos = Current0 * _dim1Len + Current1 + idx;
                var dim0 = idx / _dim1Len;
                var dim1 = idx % _dim1Len;
                return Array[dim0, dim1];
            }
            set
            {
                var pos = Current0 * _dim1Len + Current1 + idx;
                var dim0 = idx / _dim1Len;
                var dim1 = idx % _dim1Len;
                Array[dim0, dim1] = value;
            }
        }

        public T Value
        {
            get
            {
                return Array[Current0, Current1];
            }
            set
            {
                Array[Current0, Current1] = value;
            }
        }

        public T Read()
        {
            var v = Array[Current0, Current1++];
            if (Current1 >= _dim1Len)
            {
                Current1 = 0;
                Current0++;
            }
            return v;
        }

        public void Write(T value)
        {
            Array[Current0, Current1++] = value;
            if (Current1 >= _dim1Len)
            {
                Current1 = 0;
                Current0++;
            }
        }

        public void Forward(int value)
        {
            var pos = Current0 * _dim1Len + Current1 + value;
            Current0 = pos / _dim1Len;
            Current1 = pos % _dim1Len;
        }

        public void Backward(int value)
        {
            var pos = Current0 * _dim1Len + Current1 - value;
            Current0 = pos / _dim1Len;
            Current1 = pos % _dim1Len;
        }

        public static Rank2ArrayPointer<T> operator ++(Rank2ArrayPointer<T> p)
        {
            p.Current1++;
            if (p.Current1 >= p.Array.GetLength(1))
            {
                p.Current1 = 0;
                p.Current0++;
            }
            return p;
        }

        public static Rank2ArrayPointer<T> operator --(Rank2ArrayPointer<T> p)
        {
            p.Current1--;
            if (p.Current1 < 0)
            {
                p.Current1 = p.Array.GetLength(1) - 1;
                p.Current0--;
            }
            return p;
        }

        public static Rank2ArrayPointer<T> operator +(Rank2ArrayPointer<T> p, int value)
        {
            var dim1len = p.Array.GetLength(1);
            var pos = p.Current0 * dim1len + p.Current1 + value;
            p.Current0 = pos / dim1len;
            p.Current1 = pos % dim1len;
            return p;
        }

        public static Rank2ArrayPointer<T> operator -(Rank2ArrayPointer<T> p, int value)
        {
            var dim1len = p.Array.GetLength(1);
            var pos = p.Current0 * dim1len + p.Current1 - value;
            p.Current0 = pos / dim1len;
            p.Current1 = pos % dim1len;
            return p;
        }
    }

}
