using NesAsmSharp.Assembler.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesAsmSharp.Assembler.Processors
{
    public class ProcProcessor : ProcessorBase
    {
        public ProcProcessor(NesAsmContext ctx) : base(ctx)
        {
        }

        /* protos */
        // struct t_proc *proc_look(void);
        // int proc_install(void);
        // void poke(int addr, int data);

        /// <summary>
        /// call pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoCall(ref int ip)
        {
            NesAsmProc ptr;
            string name;
            int value;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* update location counter */
            ctx.DataLocCnt = ctx.LocCnt;
            ctx.LocCnt += 3;

            /* generate code */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                /* skip spaces */
                while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                /* extract name */
                if (string.IsNullOrEmpty((name = symPr.ReadSymbolNameFromPrLnBuf(ref ip))))
                {
                    if (name == "")
                    {
                        outPr.FatalError("Syntax error!");
                    }
                    return;
                }

                /* check end of line */
                asmPr.CheckEOL(ref ip);

                /* lookup proc table */
                if ((ptr = ProcLook(name)) != null)
                {
                    /* check banks */
                    if (ctx.Bank == ptr.Bank)
                    {
                        value = ptr.Org + 0xA000;
                    }
                    else
                    {
                        /* different */
                        if (ptr.Call != 0)
                        {
                            value = ptr.Call;
                        }
                        else
                        {
                            /* new call */
                            value = ctx.CallPtr + 0x8000;
                            ptr.Call = value;

                            /* init */
                            if (ctx.CallPtr == 0) ctx.CallBank = ++ctx.MaxBank;

                            /* install */
                            Poke(ctx.CallPtr++, 0xA8);         // tay

                            Poke(ctx.CallPtr++, 0x43);         // tma #5

                            Poke(ctx.CallPtr++, 0x20);

                            Poke(ctx.CallPtr++, 0x48);         // pha

                            Poke(ctx.CallPtr++, 0xA9);         // lda #...

                            Poke(ctx.CallPtr++, ptr.Bank);

                            Poke(ctx.CallPtr++, 0x53);         // tam #5

                            Poke(ctx.CallPtr++, 0x20);

                            Poke(ctx.CallPtr++, 0x98);         // tya

                            Poke(ctx.CallPtr++, 0x20);         // jsr ...

                            Poke(ctx.CallPtr++, (ptr.Org & 0xFF));

                            Poke(ctx.CallPtr++, (ptr.Org >> 8) + 0xA0);

                            Poke(ctx.CallPtr++, 0xA8);         // tay

                            Poke(ctx.CallPtr++, 0x68);         // pla

                            Poke(ctx.CallPtr++, 0x53);         // tam #5

                            Poke(ctx.CallPtr++, 0x20);

                            Poke(ctx.CallPtr++, 0x98);         // tya

                            Poke(ctx.CallPtr++, 0x60);         // rts
                        }
                    }
                }
                else
                {
                    /* lookup symbol table */
                    if ((ctx.LablPtr = symPr.LookUpSymbolTable(name, false)) == null)
                    {
                        outPr.FatalError("Undefined destination!");
                        return;
                    }

                    /* get symbol value */
                    value = ctx.LablPtr.Value;
                }

                /* opcode */
                outPr.PutByte(ctx.DataLocCnt, 0x20);

                outPr.PutWord(ctx.DataLocCnt + 1, value);

                /* output line */
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .proc pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoProc(ref int ip)
        {
            NesAsmProc ptr;
            string name;

            /* check if nesting procs/groups */
            if (ctx.ProcPtr != null)
            {
                if (ctx.OpType == (int)AsmDirective.P_PGROUP)
                {
                    outPr.FatalError("Can not declare a group inside a proc/group!");
                    return;
                }
                else
                {
                    if (ctx.ProcPtr.Type == AsmDirective.P_PROC)
                    {
                        outPr.FatalError("Can not nest procs!");
                        return;
                    }
                }
            }

            /* get proc name */
            if (ctx.LablPtr != null)
            {
                name = ctx.LablPtr.Name;
            }
            else
            {
                /* skip spaces */
                while (CharUtil.IsSpace(ctx.PrLnBuf[ip])) ip++;

                /* extract name */
                if ((string.IsNullOrEmpty(name = symPr.ReadSymbolNameFromPrLnBuf(ref ip))))
                {
                    if (name != "") return;
                    if (ctx.OpType == (int)AsmDirective.P_PROC)
                    {
                        outPr.FatalError("Proc name is missing!");
                        return;
                    }

                    /* default name */
                    name = $"__group_{ctx.ProcNb + 1}__";
                }

                /* lookup symbol table */
                if ((ctx.LablPtr = symPr.LookUpSymbolTable(name, true)) == null) return;
            }

            /* check symbol */
            if (name[0] == '.')
            {
                outPr.FatalError("Proc/group name can not be local!");
                return;
            }

            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0) return;

            /* search (or create new) proc */
            if ((ptr = ProcLook(name)) != null)
            {
                ctx.ProcPtr = ptr;
            }
            else
            {
                if (ProcInstall(name) == 0) return;
            }
            if (ctx.ProcPtr.RefCnt != 0)
            {
                outPr.FatalError("Proc/group multiply defined!");
                return;
            }

            /* incrememte proc ref counter */
            ctx.ProcPtr.RefCnt++;

            /* backup current bank infos */
            ctx.BankGLabl[(int)ctx.Section, ctx.Bank] = ctx.GLablPtr;
            ctx.BankLocCnt[(int)ctx.Section, ctx.Bank] = ctx.LocCnt;
            ctx.BankPage[(int)ctx.Section, ctx.Bank] = ctx.Page;
            ctx.ProcPtr.OldBank = ctx.Bank;
            ctx.ProcNb++;

            /* set new bank infos */
            ctx.Bank = ctx.ProcPtr.Bank;
            ctx.Page = 5;
            ctx.LocCnt = ctx.ProcPtr.Org;
            ctx.GLablPtr = ctx.LablPtr;

            /* define label */
            symPr.AssignValueToLablPtr(ctx.LocCnt, true);

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.LoadLc((ctx.Page << 13) + ctx.LocCnt, 0);
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// .endp pseudo
        /// </summary>
        /// <param name="ip"></param>
        public void DoEndp(ref int ip)
        {
            if (ctx.ProcPtr == null)
            {
                outPr.FatalError("Unexpected ENDP/ENDPROCGROUP!");
                return;
            }
            if (ctx.OpType != (int)ctx.ProcPtr.Type)
            {
                outPr.FatalError("Unexpected ENDP/ENDPROCGROUP!");
                return;
            }

            /* check end of line */
            if (asmPr.CheckEOL(ref ip) == 0)
                return;

            /* record proc size */
            ctx.Bank = ctx.ProcPtr.OldBank;
            ctx.ProcPtr.Size = ctx.LocCnt - ctx.ProcPtr.Base;
            ctx.ProcPtr = ctx.ProcPtr.Group;

            /* restore previous bank settings */
            if (ctx.ProcPtr == null)
            {
                ctx.Page = ctx.BankPage[(int)ctx.Section, ctx.Bank];
                ctx.LocCnt = ctx.BankLocCnt[(int)ctx.Section, ctx.Bank];
                ctx.GLablPtr = ctx.BankGLabl[(int)ctx.Section, ctx.Bank];
            }

            /* output */
            if (ctx.Pass == PassFlag.LAST_PASS)
            {
                outPr.PrintLn();
            }
        }

        /// <summary>
        /// proc_reloc()
        /// </summary>
        public void ProcReloc()
        {
            if (ctx.ProcNb == 0) return;

            /* init */
            ctx.ProcPtr = ctx.ProcFirst;
            ctx.Bank = ctx.MaxBank + 1;
            var addr = 0;

            /* alloc memory */
            while (ctx.ProcPtr != null)
            {
                /* proc */
                if (ctx.ProcPtr.Group == null)
                {
                    var tmp = addr + ctx.ProcPtr.Size;

                    /* bank change */
                    if (tmp > 0x2000)
                    {
                        ctx.Bank++;
                        addr = 0;
                    }
                    if (ctx.Bank > ctx.BankLimit)
                    {
                        outPr.FatalError("Not enough ROM space for procs!");
                        return;
                    }

                    /* reloc proc */
                    ctx.ProcPtr.Bank = ctx.Bank;
                    ctx.ProcPtr.Org = addr;
                    addr += ctx.ProcPtr.Size;
                }
                /* group */
                else
                {
                    /* reloc proc */
                    var group = ctx.ProcPtr.Group;
                    ctx.ProcPtr.Bank = ctx.Bank;
                    ctx.ProcPtr.Org += (group.Org - group.Base);
                }

                /* next */
                ctx.MaxBank = ctx.Bank;
                ctx.ProcPtr.RefCnt = 0;
                ctx.ProcPtr = ctx.ProcPtr.Link;
            }

            /* remap proc symbols */
            foreach (var sym in ctx.HashTbl.Values)
            {
                    ctx.ProcPtr = sym.Proc;

                    /* remap addr */
                    if (sym.Proc != null)
                    {
                        sym.Bank = ctx.ProcPtr.Bank;
                        sym.Value += (ctx.ProcPtr.Org - ctx.ProcPtr.Base);

                        /* local symbols */
                        sym.Local?.ForEach(lsym =>
                        {
                            ctx.ProcPtr = lsym.Proc;
                            if (lsym.Proc != null)
                            {
                                lsym.Bank = ctx.ProcPtr.Bank;
                                lsym.Value += ctx.ProcPtr.Org - ctx.ProcPtr.Base;
                            }
                        });
                    }
            }
            /* reserve call bank */
            symPr.SetReservedLabel("_call_bank", ctx.MaxBank + 1);

            /* reset */
            ctx.ProcPtr = null;
            ctx.ProcNb = 0;
        }

        /// <summary>
        /// proc_look()
        /// </summary>
        /// <returns></returns>
        public NesAsmProc ProcLook(string name)
        {
            /* search the procedure in the hash table */
            return ctx.ProcTbl.GetValueOrDefault(name);
        }

        /// <summary>
        /// install a procedure in the hash table
        /// </summary>
        /// <returns></returns>
        public int ProcInstall(string name)
        {
            var ptr = new NesAsmProc()
            {
                Name = name,
                Bank = (ctx.OpType == (int)AsmDirective.P_PGROUP) ? Definition.GROUP_BANK : Definition.PROC_BANK,
                Base = ctx.ProcPtr != null ? ctx.LocCnt : 0,
                Size = 0,
                Call = 0,
                RefCnt = 0,
                Link = null,
                Group = ctx.ProcPtr,
                Type = (AsmDirective)ctx.OpType,
            };
            ptr.Org = ptr.Base;

            ctx.ProcPtr = ptr;
            ctx.ProcTbl[name] = ptr;

            /* link it */
            if (ctx.ProcFirst == null)
            {
                ctx.ProcFirst = ptr;
                ctx.ProcLast = ptr;
            }
            else
            {
                ctx.ProcLast.Link = ptr;
                ctx.ProcLast = ptr;
            }

            /* ok */
            return 1;
        }

        /// <summary>
        /// poke()
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="data"></param>
        public void Poke(int addr, int data)
        {
            ctx.Rom[ctx.CallBank, addr] = (byte)data;
            ctx.Map[ctx.CallBank, addr] = (byte)(SectionType.S_CODE + (4 << 5));
        }
    }
}
