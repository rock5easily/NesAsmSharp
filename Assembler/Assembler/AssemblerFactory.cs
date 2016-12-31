using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler
{
    public class AssemblerFactory
    {
        /// <summary>
        /// Create Assembler Instance
        /// </summary>
        /// <param name="macType"></param>
        /// <param name="opt"></param>
        /// <returns></returns>
        public static IAssembler CreateAssembler(MachineType macType, NesAsmOption opt)
        {
            if (macType == MachineType.MACHINE_NES)
            {
                return new NesAssembler(opt);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
