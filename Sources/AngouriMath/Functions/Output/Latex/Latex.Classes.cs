﻿/* 
 * Copyright (c) 2019-2021 Angouri.
 * AngouriMath is licensed under MIT. 
 * Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
 * Website: https://am.angouri.org.
 */
using AngouriMath.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AngouriMath
{
    partial record Entity
    {
        public partial record Variable
        {
            /// <summary>
            /// List of constants LaTeX will correctly display
            /// Yet to be extended
            /// Case does matter, not all letters have both displays in LaTeX
            /// </summary>
            [ConstantField]
            private static readonly HashSet<string> LatexisableConstants = new HashSet<string>
            {
                "alpha", "beta", "gamma", "delta", "epsilon", "varepsilon", "zeta", "eta", "theta", "vartheta",
                "iota", "kappa", "varkappa", "lambda", "mu", "nu", "xi", "omicron", "pi", "varpi", "rho",
                "varrho", "sigma", "varsigma", "tau", "upsilon", "phi", "varphi", "chi", "psi", "omega",

                "Gamma", "Delta", "Theta", "Lambda", "Xi", "Pi", "Sigma", "Upsilon", "Phi", "Psi", "Omega",
            };

            /// <summary>
            /// Returns latexised const if it is possible to latexise it,
            /// or its original name otherwise
            /// </summary>
            public override string Latexise() =>
                SplitIndex() is var (prefix, index)
                ? (LatexisableConstants.Contains(prefix) ? @"\" + prefix : prefix)
                  + "_{" + index + "}"
                : LatexisableConstants.Contains(Name) ? @"\" + Name : Name;
        }

        public partial record Tensor
        {
            /// <inheritdoc/>
            public override string Latexise()
            {
                if (IsMatrix)
                {
                    var sb = new StringBuilder();
                    sb.Append(@"\begin{pmatrix}");
                    var lines = new List<string>();
                    for (int x = 0; x < Shape[0]; x++)
                    {
                        var items = new List<string>();

                        for (int y = 0; y < Shape[1]; y++)
                            items.Add(this[x, y].Latexise());

                        var line = string.Join(" & ", items);
                        lines.Add(line);
                    }
                    sb.Append(string.Join(@"\\", lines));
                    sb.Append(@"\end{pmatrix}");
                    return sb.ToString();
                }
                else if (IsVector)
                {
                    var sb = new StringBuilder();
                    sb.Append(@"\begin{bmatrix}");
                    sb.Append(string.Join(" & ", InnerTensor.Iterate().Select(k => k.Value.Latexise())));
                    sb.Append(@"\end{bmatrix}");
                    return sb.ToString();
                }
                else
                {
                    return this.Stringize();
                }
            }
        }
    }
}
