
/* Copyright (c) 2019-2020 Angourisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,
 * modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
 * is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using AngouriMath.Core.Numerix;
using AngouriMath.Core.Sys;
using GenericTensor.Core;
using PeterO.Numbers;

namespace AngouriMath
{
    public interface ILatexiseable { public string Latexise(); }
    public enum Priority { Sum = 2, Minus = 2, Mul = 4, Div = 4, Pow = 6, Func = 8, Var = 10, Num = 10 }
    /// <summary>
    /// The main class in AngouriMath
    /// Every node, expression, or number is an <see cref="Entity"/>
    /// However, you cannot create an instance of this class, look for the nested classes instead
    /// </summary>
    // Note: When editing record parameter lists on Visual Studio 16.7.x or 16.8 Preview 1,
    // watch out for Visual Studio crash: https://github.com/dotnet/roslyn/issues/46123
    // Workaround is to use Notepad for editing.
    public abstract partial record Entity : IEnumerable<Entity>, ILatexiseable
    {
        Entity[]? _directChildren;
        protected internal IReadOnlyList<Entity> DirectChildren => _directChildren ??= InitDirectChildren();
        protected abstract Entity[] InitDirectChildren();
        /// <remarks>A depth-first enumeration is required by
        /// <see cref="Core.TreeAnalysis.TreeAnalyzer.GetMinimumSubtree(Entity, Var)"/></remarks>
        public IEnumerator<Entity> GetEnumerator() =>
            Enumerable.Repeat(this, 1).Concat(DirectChildren.SelectMany(c => c)).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract Entity Replace(Func<Entity, Entity> func);
        public Entity Replace(Func<Entity, Entity> func1, Func<Entity, Entity> func2) =>
            Replace(child => func2(func1(child)));
        public Entity Replace(Func<Entity, Entity> func1, Func<Entity, Entity> func2, Func<Entity, Entity> func3) =>
            Replace(child => func3(func2(func1(child))));

        /// <summary>Replaces all <see cref="x"/> with <see cref="value"/></summary>
        public Entity Substitute(Entity x, Entity value) => Replace(e => e == x ? value : e);
        /// <summary>Replaces all <see cref="replacements"/></summary>
        public Entity Substitute<TFrom, TTo>(IReadOnlyDictionary<TFrom, TTo> replacements)
            where TFrom : Entity where TTo : Entity =>
            Replace(e => e is TFrom from && replacements.TryGetValue(from, out var value) ? value : e);

        public abstract Priority Priority { get; }

        public static Entity operator +(Entity a, Entity b) => new Sumf(a, b);
        public static Entity operator +(Entity a) => a;
        public static Entity operator -(Entity a, Entity b) => new Minusf(a, b);
        public static Entity operator -(Entity a) => new Mulf(-1, a);
        public static Entity operator *(Entity a, Entity b) => new Mulf(a, b);
        public static Entity operator /(Entity a, Entity b) => new Divf(a, b);
        public Entity Pow(Entity n) => new Powf(this, n);
        public Entity Sin() => new Sinf(this);
        public Entity Cos() => new Cosf(this);
        public Entity Tan() => new Tanf(this);
        public Entity Cotan() => new Cotanf(this);
        public Entity Arcsin() => new Arcsinf(this);
        public Entity Arccos() => new Arccosf(this);
        public Entity Arctan() => new Arctanf(this);
        public Entity Arccotan() => new Arccotanf(this);
        public Entity Factorial() => new Factorialf(this);
        public Entity Log(Entity n) => new Logf(this, n);
        public bool IsLowerThan(Entity a) => Priority < a.Priority;
        /// <summary>Deep but stupid comparison</summary>
        public static bool operator ==(Entity? a, Entity? b)
        {
            // Since C# 7 we can compare objects to null without casting them into object
            if (a is null && b is null)
                return true;
            // We expect the EqualsTo implementation to check if a's type is equal to b's type
            return a?.Equals(b) ?? false;
        }
        public static bool operator !=(Entity? a, Entity? b) => !(a == b);

        /// <summary>
        /// Checks whether both parts of the complex number are finite
        /// meaning that it could be safely used for calculations
        /// </summary>
        public bool IsFinite => _isFinite ??= ThisIsFinite && DirectChildren.All(x => x.IsFinite);
        protected virtual bool ThisIsFinite => true;
        bool? _isFinite;

        /// <returns>
        /// Number of nodes in tree
        /// TODO: improve measurement of Entity complexity, for example
        /// (1 / x ^ 2).Complexity() < (x ^ (-0.5)).Complexity()
        /// </returns>
        public int Complexity => _complexity ??= 1 + DirectChildren.Sum(x => x.Complexity);
        int? _complexity;

        /// <summary>
        /// Returns list of unique variables, for example 
        /// it extracts <c>`x`</c>, <c>`goose`</c> from <c>(x + 2 * goose) - pi * x</c>
        /// </summary>
        /// <returns>
        /// <see cref="Set"/> of unique variables excluding mathematical constants
        /// such as <see cref="pi"/>, <see cref="e"/> and <see cref="i"/>
        /// </returns>
        /// <remarks>
        /// Remember that <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/> will
        /// use <see cref="ICollection{T}.Contains(T)"/> which in this case is <see cref="HashSet{T}.Contains(T)"/>
        /// so it is O(1)
        /// </remarks>
        public IReadOnlyCollection<Var> Vars => _vars ??=
            new HashSet<Var>(this is Var v ? Enumerable.Repeat(v, 1) : DirectChildren.SelectMany(x => x.Vars));
        HashSet<Var>? _vars;

        /// <summary>Checks if <paramref name="x"/> is a subnode inside this <see cref="Entity"/> tree.
        /// Optimized for <see cref="Var"/>.</summary>
        public bool Contains(Entity x) => x is Var v ? Vars.Contains(v) : Enumerable.Contains(this, x);

        public static implicit operator Entity(sbyte value) => IntegerNumber.Create(value);
        public static implicit operator Entity(byte value) => IntegerNumber.Create(value);
        public static implicit operator Entity(short value) => IntegerNumber.Create(value);
        public static implicit operator Entity(ushort value) => IntegerNumber.Create(value);
        public static implicit operator Entity(int value) => IntegerNumber.Create(value);
        public static implicit operator Entity(uint value) => IntegerNumber.Create(value);
        public static implicit operator Entity(long value) => IntegerNumber.Create(value);
        public static implicit operator Entity(ulong value) => IntegerNumber.Create(value);
        public static implicit operator Entity(EInteger value) => IntegerNumber.Create(value);
        public static implicit operator Entity(ERational value) => RationalNumber.Create(value);
        public static implicit operator Entity(EDecimal value) => RealNumber.Create(value);
        public static implicit operator Entity(float value) => RealNumber.Create(EDecimal.FromSingle(value));
        public static implicit operator Entity(double value) => RealNumber.Create(EDecimal.FromDouble(value));
        public static implicit operator Entity(decimal value) => RealNumber.Create(EDecimal.FromDecimal(value));
        public static implicit operator Entity(Complex value) =>
            ComplexNumber.Create(EDecimal.FromDouble(value.Real), EDecimal.FromDouble(value.Imaginary));
        public static implicit operator Entity(string expr) => MathS.FromString(expr);

        /// <summary>Number node.
        /// This class represents all possible numerical values as a hierarchy,
        /// <list>
        ///   <see cref="Num"/>
        ///   <list type="bullet">
        ///     <see cref="ComplexNumber"/>
        ///       <list type="bullet">
        ///         <see cref="RealNumber"/>
        ///         <list type="bullet">
        ///           <see cref="RationalNumber"/>
        ///           <list type="bullet">
        ///             <see cref="IntegerNumber"/>
        ///           </list>
        ///         </list>
        ///       </list>
        ///     </list>
        ///   </list>
        /// </summary>
        public abstract partial record Num : Entity
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(this);
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();
            public static implicit operator Num(sbyte value) => IntegerNumber.Create(value);
            public static implicit operator Num(byte value) => IntegerNumber.Create(value);
            public static implicit operator Num(short value) => IntegerNumber.Create(value);
            public static implicit operator Num(ushort value) => IntegerNumber.Create(value);
            public static implicit operator Num(int value) => IntegerNumber.Create(value);
            public static implicit operator Num(uint value) => IntegerNumber.Create(value);
            public static implicit operator Num(long value) => IntegerNumber.Create(value);
            public static implicit operator Num(ulong value) => IntegerNumber.Create(value);
            public static implicit operator Num(EInteger value) => IntegerNumber.Create(value);
            public static implicit operator Num(ERational value) => RationalNumber.Create(value);
            public static implicit operator Num(EDecimal value) => RealNumber.Create(value);
            public static implicit operator Num(float value) => RealNumber.Create(EDecimal.FromSingle(value));
            public static implicit operator Num(double value) => RealNumber.Create(EDecimal.FromDouble(value));
            public static implicit operator Num(decimal value) => RealNumber.Create(EDecimal.FromDecimal(value));
            public static implicit operator Num(Complex value) =>
                ComplexNumber.Create(EDecimal.FromDouble(value.Real), EDecimal.FromDouble(value.Imaginary));
        }

        /// <summary>Variable node. It only has a name</summary>
        public partial record Var(string Name) : Entity
        {
            public override Priority Priority => Priority.Var;
            public override Entity Replace(Func<Entity, Entity> func) => func(this);
            protected override Entity[] InitDirectChildren() => Array.Empty<Entity>();
            internal static readonly IReadOnlyDictionary<Var, ComplexNumber> ConstantList =
                new Dictionary<Var, ComplexNumber>
                {
                    { nameof(MathS.DecimalConst.pi), MathS.DecimalConst.pi },
                    { nameof(MathS.DecimalConst.e), MathS.DecimalConst.e }
                };
            /// <summary>
            /// Determines whether something is a variable or contant, e. g.
            /// <list type="table">
            ///     <listheader><term>Expression</term> <description>Is it a constant?</description></listheader>
            ///     <item><term>e</term> <description>Yes</description></item>
            ///     <item><term>x</term> <description>No</description></item>
            ///     <item><term>x + 3</term> <description>No</description></item>
            ///     <item><term>pi + 4</term> <description>No</description></item>
            ///     <item><term>pi</term> <description>Yes</description></item>
            /// </list>
            /// </summary>
            public bool IsConstant => ConstantList.ContainsKey(this);
            /// <summary>
            /// Extracts this <see cref="Var"/>'s name and index
            /// from its <see cref="Name"/> (e. g. "qua" or "phi_3" or "qu_q")
            /// </summary>
            /// <returns>
            /// If this contains _ and valid name and index, returns a pair of
            /// (<see cref="string"/> Prefix, <see cref="string"/> Index),
            /// <see langword="null"/> otherwise
            /// </returns>
            internal (string Prefix, string Index)? SplitIndex()
            {
                var pos_ = Name.IndexOf('_');
                if (pos_ != -1)
                {
                    var varName = Name.Substring(0, pos_);
                    return (varName, Name.Substring(pos_ + 1));
                }
                return null;
            }
            /// <summary>
            /// Finds next var index name starting with 1, e. g.
            /// x + n_0 + n_a + n_3 + n_1
            /// will find n_2
            /// </summary>
            internal static Var CreateUnique(Entity expr, string prefix)
            {
                var indices = new HashSet<int>();
                foreach (var var in expr.Vars)
                    if (var.SplitIndex() is var (varPrefix, index)
                        && varPrefix == prefix
                        && int.TryParse(index, out var num))
                        indices.Add(num);
                var i = 1;
                while (indices.Contains(i))
                    i++;
                return new Var(prefix + "_" + i);
            }
            public static implicit operator Var(string name) => new(name);
        }
        /// <summary>
        /// Basic tensor implementation
        /// https://en.wikipedia.org/wiki/Tensor
        /// </summary>
        public partial record Tensor(GenTensor<Entity, Tensor.EntityTensorWrapperOperations> InnerTensor) : Entity
        {
            public override Priority Priority => Priority.Num;
            internal Tensor Elementwise(Func<Entity, Entity> operation) =>
                new Tensor(GenTensor<Entity, EntityTensorWrapperOperations>.CreateTensor(
                    InnerTensor.Shape, indices => operation(InnerTensor.GetValueNoCheck(indices))));
            internal Tensor Elementwise(Tensor other, Func<Entity, Entity, Entity> operation) =>
                Shape != other.Shape
                ? throw new InvalidShapeException("Arguments should be of the same shape")
                : new Tensor(GenTensor<Entity, EntityTensorWrapperOperations>.CreateTensor(
                    InnerTensor.Shape, indices =>
                        operation(InnerTensor.GetValueNoCheck(indices), other.InnerTensor.GetValueNoCheck(indices))));
            public override Entity Replace(Func<Entity, Entity> func) => Elementwise(element => element.Replace(func));
            protected override Entity[] InitDirectChildren() => InnerTensor.Iterate().Select(tup => tup.Value).ToArray();

            public struct EntityTensorWrapperOperations : IOperations<Entity>
            {
                public Entity Add(Entity a, Entity b) => a + b;
                public Entity Subtract(Entity a, Entity b) => a - b;
                public Entity Multiply(Entity a, Entity b) => a * b;
                public Entity Negate(Entity a) => -a;
                public Entity Divide(Entity a, Entity b) => a / b;
                public Entity CreateOne() => IntegerNumber.One;
                public Entity CreateZero() => IntegerNumber.Zero;
                public Entity Copy(Entity a) => a;
                public Entity Forward(Entity a) => a;
                public bool AreEqual(Entity a, Entity b) => a == b;
                public bool IsZero(Entity a) => a == 0;
                public string ToString(Entity a) => a.ToString();
            }
            /// <summary>
            /// List of ints that stand for dimensions
            /// </summary>
            public TensorShape Shape => InnerTensor.Shape;

            /// <summary>
            /// Numbere of dimensions. 2 for matrix, 1 for vector
            /// </summary>
            public int Dimensions => Shape.Count;

            /// <summary>
            /// List of dimensions
            /// If you need matrix, list 2 dimensions 
            /// If you need vector, list 1 dimension (length of the vector)
            /// You can't list 0 dimensions
            /// </summary>
            /// <param name="dims"></param>
            public Tensor(params int[] dims)
                : this(GenTensor<Entity, EntityTensorWrapperOperations>.CreateTensor(new(dims), inds => 0)) { }

            public Entity this[params int[] dims]
            {
                get => InnerTensor[dims];
                set => InnerTensor[dims] = value;
            }

            public bool IsVector => InnerTensor.IsVector;
            public bool IsMatrix => InnerTensor.IsMatrix;

            /// <summary>
            /// Changes the order of axes
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            public void Transpose(int a, int b) => InnerTensor.Transpose(a, b);

            /// <summary>
            /// Changes the order of axes in matrix
            /// </summary>
            public void Transpose()
            {
                if (IsMatrix)
                    InnerTensor.TransposeMatrix();
                else
                    throw new Core.Exceptions.MathSException("Specify axes numbers for non-matrices");
            }

            // We do not need to use Gaussian elimination here
            // since we anyway get N! memory use
            public Entity Determinant() => InnerTensor.DeterminantLaplace();

            /// <summary>
            /// Inverts all matrices in a tensor
            /// </summary>
            public Tensor Inverse()
            {
                var cp = InnerTensor.Copy(copyElements: true);
                cp.TensorMatrixInvert();
                return new Tensor(cp);
            }
        }
        public partial record Sumf(Entity Augend, Entity Addend) : Entity
        {
            public override Priority Priority => Priority.Sum;
            public override Entity Replace(Func<Entity, Entity> func) => func(new Sumf(Augend.Replace(func), Addend.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Augend, Addend };
            /// <summary>
            /// Gathers linear children of a sum, e.g.
            /// <code>1 + (x - a/2) + b - 4</code>
            /// would return
            /// <code>{ 1, x, (-1) * a / 2, b, (-1) * 4 }</code>
            /// </summary>
            internal static IEnumerable<Entity> LinearChildren(Entity tree) => tree switch
            {
                Sumf(var augend, var addend) => LinearChildren(augend).Concat(LinearChildren(addend)),
                Minusf(var subtrahend, var minuend) =>
                    LinearChildren(subtrahend).Concat(LinearChildren(minuend).Select(entity => -1 * entity)),
                _ => Enumerable.Repeat(tree, 1)
            };
        }
        public partial record Minusf(Entity Subtrahend, Entity Minuend) : Entity
        {
            public override Priority Priority => Priority.Minus;
            public override Entity Replace(Func<Entity, Entity> func) => func(new Minusf(Subtrahend.Replace(func), Minuend.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Subtrahend, Minuend };
        }
        public partial record Mulf(Entity Multiplier, Entity Multiplicand) : Entity
        {
            public override Priority Priority => Priority.Mul;
            public override Entity Replace(Func<Entity, Entity> func) => func(new Mulf(Multiplier.Replace(func), Multiplicand.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Multiplier, Multiplicand };
            /// <summary>
            /// Gathers linear children of a product, e.g.
            /// <code>1 * (x / a^2) * b / 4</code>
            /// would return
            /// <code>{ 1, x, (a^2)^(-1), b, 4^(-1) }</code>
            /// </summary>
            internal static IEnumerable<Entity> LinearChildren(Entity tree) => tree switch
            {
                Mulf(var multiplier, var multiplicand) => LinearChildren(multiplier).Concat(LinearChildren(multiplicand)),
                Divf(var dividend, var divisor) =>
                    LinearChildren(dividend).Concat(LinearChildren(divisor).Select(entity => new Powf(entity, -1))),
                _ => Enumerable.Repeat(tree, 1)
            };
        }
        public partial record Divf(Entity Dividend, Entity Divisor) : Entity
        {
            public override Priority Priority => Priority.Div;
            public override Entity Replace(Func<Entity, Entity> func) => func(new Divf(Dividend.Replace(func), Divisor.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Dividend, Divisor };
        }
        public partial record Powf(Entity Base, Entity Exponent) : Entity
        {
            public override Priority Priority => Priority.Pow;
            public override Entity Replace(Func<Entity, Entity> func) => func(new Powf(Base.Replace(func), Exponent.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Base, Exponent };
        }
        public abstract record Function : Entity
        {
            public override Priority Priority => Priority.Func;
        }
        public partial record Sinf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Sinf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Cosf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Cosf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Tanf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Tanf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Cotanf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Cotanf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Logf(Entity Base, Entity Antilogarithm) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Logf(Base.Replace(func), Antilogarithm.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Base, Antilogarithm };
        }
        public partial record Arcsinf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Arcsinf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Arccosf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Arccosf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Arctanf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Arctanf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Arccotanf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Arccotanf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Factorialf(Entity Argument) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) => func(new Factorialf(Argument.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Argument };
        }
        public partial record Derivativef(Entity Expression, Entity Variable, Entity Iterations) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(new Derivativef(Expression.Replace(func), Variable.Replace(func), Iterations.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Expression, Variable, Iterations };
        }
        public partial record Integralf(Entity Expression, Entity Variable, Entity Iterations) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(new Integralf(Expression.Replace(func), Variable.Replace(func), Iterations.Replace(func)));
            protected override Entity[] InitDirectChildren() => new[] { Expression, Variable, Iterations };
        }
        public partial record Limitf(Entity Expression, Entity Variable, Entity Destination, Limits.ApproachFrom ApproachFrom) : Function
        {
            public override Entity Replace(Func<Entity, Entity> func) =>
                func(new Limitf(Expression.Replace(func), Variable.Replace(func), Destination.Replace(func), ApproachFrom));
            protected override Entity[] InitDirectChildren() => new[] { Expression, Variable, Destination };
        }
    }
}