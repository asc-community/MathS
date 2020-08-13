﻿using AngouriMath;
using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using System.Linq.Expressions;
using AngouriMath.Core.Numerix;

namespace DotnetBenchmark
{
    public class CacheCompiledFuncTest
    {
        private readonly FastExpression complexFunc;
        private readonly Func<Complex, Complex> linqComp;
        private readonly Entity notCompiled;
        private readonly Entity.Var x = MathS.Var("x");
        private readonly ComplexNumber ComNumToSub = 3;
        private readonly Complex ComToSub = 3;
        public CacheCompiledFuncTest()
        {

            notCompiled = MathS.Sin(MathS.Sqr(x)) + MathS.Cos(MathS.Sqr(x)) + MathS.Sqr(x) + MathS.Sin(MathS.Sqr(x));
            complexFunc = notCompiled.Compile(x);

            Expression<Func<Complex, Complex>> linqExpr = x => Complex.Sin(Complex.Pow(x, 2)) + Complex.Cos(Complex.Pow(x, 2)) + Complex.Pow(x, 2) + Complex.Sin(Complex.Pow(x, 2));
            linqComp = linqExpr.Compile();
        }
        [Benchmark]
        public Complex MyCompiled() => complexFunc.Call(ComToSub);
        [Benchmark]
        public Complex SysIncode() => Complex.Sin(Complex.Pow(3, 2)) + Complex.Cos(Complex.Pow(3, 2)) + Complex.Pow(3, 2) + Complex.Sin(Complex.Pow(3, 2));
        [Benchmark]
        public Complex LinqCompiled() => linqComp.Invoke(3);
        [Benchmark]
        public ComplexNumber NotCompiled() => notCompiled.Substitute(x, 3).Eval();
    }
}
