﻿using System;
using System.Reflection;
using System.Runtime;

namespace Utils
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
                throw new InvalidOperationException("Specify class' name whose method to call");
            
            var className = args[0];

            var typeToCall = Type.GetType("Utils." + className);

            if (typeToCall is null)
                throw new EntryPointNotFoundException($"Class {className} not found");

            MethodInfo? methodDo;

            try
            {
                methodDo = typeToCall.GetMethod("Do");
            }
            catch (AmbiguousMatchException amb)
            {
                throw new AmbiguousImplementationException("There should be one Do", amb);
            }

            if (methodDo is null)
                throw new AmbiguousImplementationException("There should be one Do");

            methodDo.Invoke(null, new object[] { });
        }
    }
}
