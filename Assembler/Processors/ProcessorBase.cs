using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    /// <summary>
    /// Processor Base class
    /// This class has properties to get each processor instance.
    /// </summary>
    public abstract class ProcessorBase
    {
        protected NesAsmContext ctx;
        protected NesAsmOption opt;

        public ProcessorBase(NesAsmContext ctx)
        {
            this.ctx = ctx;
            this.opt = ctx.Option;
        }

        #region processors
        private AssembleProcessor _asmPr;
        protected AssembleProcessor asmPr
        {
            get
            {
                if (_asmPr == null)
                {
                    _asmPr = ctx.GetProcessor<AssembleProcessor>();
                }
                return _asmPr;
            }
        }

        private CodeProcessor _codePr;
        protected CodeProcessor codePr
        {
            get
            {
                if (_codePr == null)
                {
                    _codePr = ctx.GetProcessor<CodeProcessor>();
                }
                return _codePr;
            }
        }

        private CRCProcessor _crcPr;
        protected CRCProcessor crcPr
        {
            get
            {
                if (_crcPr == null)
                {
                    _crcPr = ctx.GetProcessor<CRCProcessor>();
                }
                return _crcPr;
            }
        }

        private CommandProcessor _cmdPr;
        protected CommandProcessor cmdPr
        {
            get
            {
                if (_cmdPr == null)
                {
                    _cmdPr = ctx.GetProcessor<CommandProcessor>();
                }
                return _cmdPr;
            }
        }

        private ExprProcessor _exprPr;
        protected ExprProcessor exprPr
        {
            get
            {
                if (_exprPr == null)
                {
                    _exprPr = ctx.GetProcessor<ExprProcessor>();
                }
                return _exprPr;
            }
        }

        private FuncProcessor _funcPr;
        protected FuncProcessor funcPr
        {
            get
            {
                if (_funcPr == null)
                {
                    _funcPr = ctx.GetProcessor<FuncProcessor>();
                }
                return _funcPr;
            }
        }

        private InputProcessor _inPr;
        protected InputProcessor inPr
        {
            get
            {
                if (_inPr == null)
                {
                    _inPr = ctx.GetProcessor<InputProcessor>();
                }
                return _inPr;
            }
        }

        private MacroProcessor _macroPr;
        protected MacroProcessor macroPr
        {
            get
            {
                if (_macroPr == null)
                {
                    _macroPr = ctx.GetProcessor<MacroProcessor>();
                }
                return _macroPr;

            }
        }

        private OutputProcessor _outPr;
        protected OutputProcessor outPr
        {
            get
            {
                if (_outPr == null)
                {
                    _outPr = ctx.GetProcessor<OutputProcessor>();
                }
                return _outPr;
            }
        }

        private PCXProcessor _pcxPr;
        protected PCXProcessor pcxPr
        {
            get
            {
                if (_pcxPr == null)
                {
                    _pcxPr = ctx.GetProcessor<PCXProcessor>();
                }
                return _pcxPr;
            }
        }

        private ProcProcessor _procPr;
        protected ProcProcessor procPr
        {
            get
            {
                if (_procPr == null)
                {
                    _procPr = ctx.GetProcessor<ProcProcessor>();
                }
                return _procPr;
            }
        }

        private SymbolProcessor _symPr;
        protected SymbolProcessor symPr
        {
            get
            {
                if (_symPr == null)
                {
                    _symPr = ctx.GetProcessor<SymbolProcessor>();
                }
                return _symPr;
            }
        }
        #endregion processors
    }
}
