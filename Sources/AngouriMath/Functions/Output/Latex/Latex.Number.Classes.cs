/*
 * Copyright (c) 2019-2020 Angourisoft
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            partial record Complex
            {
                /// <inheritdoc/>
                public override string Latexise()
                {
                    static string RenderNum(Real number)
                    {
                        if (number == Integer.MinusOne)
                            return "-";
                        else if (number == Integer.One)
                            return "";
                        else
                            return number.Latexise();
                    }
                    if (ImaginaryPart is Integer(0))
                        return RealPart.Latexise();
                    else if (RealPart is Integer(0))
                        return RenderNum(ImaginaryPart) + "i";
                    var (im, sign) = ImaginaryPart > 0 ? (ImaginaryPart, "+") : (-ImaginaryPart, "-");
                    return RealPart.Latexise() + " " + sign + " " +
                        (im == 1 ? "" : im.Latexise(ImaginaryPart is Rational and not Integer)) + "i";
                }
            }

            partial record Real
            {
                /// <inheritdoc/>
                public override string Latexise() => this switch
                {
                    { IsFinite: true } => EDecimal.ToString(),
                    { IsNaN: true } => @"\mathrm{undefined}",
                    { IsNegative: true } => @"-\infty ",
                    _ => @"\infty ",
                };
            }

            partial record Rational
            {
                /// <inheritdoc/>
                public override string Latexise() => $@"\frac{{{ERational.Numerator}}}{{{ERational.Denominator}}}";

            }

            partial record Integer
            {
                /// <inheritdoc/>
                public override string Latexise() => EInteger.ToString();
            }
        }

        

        

        
    }
}