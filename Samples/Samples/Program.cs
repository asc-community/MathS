using System;
using AngouriMath;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Boolean;
using static AngouriMath.MathS;
using AngouriMath.Extensions;
using static System.Console;
using static AngouriMath.Entity.Set;
using System.Diagnostics;

WriteLine("a^x + b^x = c^x".Solve("x"));
//WriteLine("x e ^ x = a".Solve("x"));
//WriteLine("e ^ x + e ^ (2x) + c = d".Solve("x"));
//WriteLine("x ^ x + x ^ (2x) = c".Solve("x"));

//var sols = (FiniteSet)"4 ^ (x3 + x) + 2 ^ (x3 + x) = c".Solve("x");
//
////foreach (var sol in sols)
////    WriteLine(sol.Substitute("c", 1).Substitute("n_1", 1).Evaled);
//
//Entity sol = "4 ^ (x3 + x) + 2 ^ (x3 + x) = c";
//
//Console.WriteLine(MathS.ToSympyCode(sol));

var expr = "1 + 2^x + 4^x + 8^x - c".SolveEquation("x");
foreach(var sol in (FiniteSet)expr)
{
    WriteLine(sol.Simplify());
}

//WriteLine("(a x3 + b x) / (c x3 + c x)".Limit("x", "+oo").Integrate("c").Derive("c").Simplify());
