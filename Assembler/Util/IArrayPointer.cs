using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Util
{
    public interface IArrayPointer<T>
    {
        T this[int i] { get; set; }
        T Value { get; set; }
        T Read();
        void Write(T value);
        void Forward(int value);
        void Backward(int value);
    }

}
