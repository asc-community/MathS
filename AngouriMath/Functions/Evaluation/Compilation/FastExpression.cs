﻿using AngouriMath.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace AngouriMath
{
    public abstract partial class Entity
    {
        /// <summary>
        /// Compile function so you can evaluate numerical value 15x faster,
        /// than subsitution
        /// </summary>
        /// <param name="variables">
        /// List string names of variables in the same order
        /// as you will list them when evaluating
        /// </param>
        /// <returns></returns>
        public FastExpression Compile(params VariableEntity[] variables)
        {
            var strings = new List<string>();
            foreach (var varEnt in variables)
                strings.Add(varEnt.Name);
            return Compile(strings.ToArray());
        }

        /// <summary>
        /// Compile function so you can evaluate numerical value 15x faster,
        /// than subsitution
        /// </summary>
        /// <param name="variables">
        /// List string names of variables in the same order
        /// as you will list them when evaluating
        /// </param>
        /// <returns></returns>
        public FastExpression Compile(params string[] variables)
        {
            var instructions = new InstructionSet();
            var varNamespace = new Dictionary<string, int>();
            int id = 0;
            foreach (var varName in variables)
            {
                varNamespace[varName] = id;
                id++;
            }
            InnerCompile(instructions, variables, varNamespace);
            return new FastExpression(instructions, variables.Length);
        }
        private void InnerCompile(InstructionSet instructions, string[] variables, Dictionary<string, int> varNamespace)
        {
            for (int i = Children.Count - 1; i >= 0; i--)
                Children[i].InnerCompile(instructions, variables, varNamespace);
            if (this is OperatorEntity || this is FunctionEntity)
                instructions.AddInstruction(Name, Children.Count);
            else if (this is NumberEntity)
                instructions.AddInstruction(GetValue());
            else if (this is VariableEntity)
                instructions.AddInstruction(varNamespace[Name]);
            else
                throw new Exception("Unknown entity");
        }
    }
    public class FastExpression
    {
        private Stack stack;
        private InstructionSet instructions;
        private int varCount;
        internal FastExpression(InstructionSet instructions, int varCount)
        {
            this.varCount = varCount;
            stack = new Stack(instructions.Count);
            this.instructions = instructions;
        }

        /// <summary>
        /// Calls the compiled function (synonim to Substitute)
        /// </summary>
        /// <param name="variables">
        /// List arguments in the same order in which you compiled the function
        /// </param>
        /// <returns></returns>
        public Number Call(params Number[] variables)
            => Substitute(variables);

        /// <summary>
        /// Calls the compiled function (synonim to Call)
        /// </summary>
        /// <param name="variables">
        /// List arguments in the same order in which you compiled the function
        /// </param>
        /// <returns></returns>
        public Number Substitute(params Number[] variables)
        {
            if (variables.Length != varCount)
                throw new Exception("Wrong amount of parameters");
            foreach(var instruction in instructions)
            {
                switch(instruction.Type)
                {
                    case Instruction.InstructionType.PUSHCONST:
                        stack.Push(instruction.Value);
                        break;
                    case Instruction.InstructionType.PUSHVAR:
                        stack.Push(variables[instruction.VarNumber]);
                        break;
                    default:
                        CompiledMathFunctions.functions[instruction.FuncNumber](stack);
                        break;
                }
            }
            if (stack.Depth != 1)
                throw new Exception("Stack error");
            var res = stack.Last;
            stack.Clear();
            return res;
        }
    }
}