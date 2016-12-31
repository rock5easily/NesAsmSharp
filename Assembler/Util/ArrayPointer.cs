using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    /// <summary>
    /// 配列の参照とインデックスを保持するためのクラス
    /// </summary>
    public class ArrayPointer<T> : IArrayPointer<T>
    {
        public T[] Array { get; private set; }

        public int Top { get; private set; }

        public int Current { get; private set; }

        /// <summary>
        /// 指定した配列の先頭を指すオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public ArrayPointer(T[] array)
        {
            Array = array;
            Current = Top = 0;
        }

        /// <summary>
        /// 指定した配列の指定したインデックスを指すオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public ArrayPointer(T[] array, int top)
        {
            Array = array;
            Current = Top = top;
        }

        /// <summary>
        /// 指定したオブジェクトをコピーしたオブジェクトを作成する
        /// </summary>
        /// <param name="array"></param>
        public ArrayPointer(ArrayPointer<T> ptr)
        {
            Array = ptr.Array;
            Top = ptr.Top;
            Current = ptr.Current;
        }

        public T this[int i]
        {
            get
            {
                return Array[Current + i];
            }
            set
            {
                Array[Current + i] = value;
            }
        }

        public T Value
        {
            get
            {
                return Array[Current];
            }
            set
            {
                Array[Current] = value;
            }
        }

        public T Read()
        {
            var v = Array[Current++];
            return v;
        }

        public void Write(T value)
        {
            Array[Current++] = value;
        }

        public void Forward(int value)
        {
            Current += value;
        }

        public void Backward(int value)
        {
            Current -= value;
        }

        public static ArrayPointer<T> operator ++(ArrayPointer<T> p)
        {
            p.Current++;
            return p;
        }

        public static ArrayPointer<T> operator --(ArrayPointer<T> p)
        {
            p.Current--;
            return p;
        }

        public static ArrayPointer<T> operator +(ArrayPointer<T> p, int value)
        {
            p.Current += value;
            return p;
        }

        public static ArrayPointer<T> operator -(ArrayPointer<T> p, int value)
        {
            p.Current -= value;
            return p;
        }
    }

}
