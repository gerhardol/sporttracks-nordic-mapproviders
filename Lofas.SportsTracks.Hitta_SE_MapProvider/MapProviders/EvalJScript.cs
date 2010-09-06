/*
Copyright (C) 2008, 2009, 2010 Peter Löfås

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.JScript;

namespace Utility
{
   internal class JScriptEvaluator
   {
      public int EvalToInteger(string statement)
      {
         string s = EvalToString(statement);
         return int.Parse(s.ToString());
      }

      public double EvalToDouble(string statement)
      {
         string s = EvalToString(statement);
         return double.Parse(s);
      }

      public string EvalToString(string statement)
      {
         object o = EvalToObject(statement);
         return o.ToString();
      }

      public object EvalToObject(string statement)
      {
         return _evaluatorType.InvokeMember(
                     "Eval", 
                     BindingFlags.InvokeMethod, 
                     null, 
                     _evaluator, 
                     new object[] { statement } 
                  );
      }

       public JScriptEvaluator(string text)
      {
         ICodeCompiler compiler;
         compiler = new JScriptCodeProvider().CreateCompiler();

         CompilerParameters parameters;
         parameters = new CompilerParameters();
         parameters.GenerateInMemory = true;
         
         CompilerResults results;
         string code = @"package Evaluator
            {
               class Evaluator
               {";
           code   += text;
           code += @"
                  public function Eval(expr : String) : String 
                  { 
                     return eval(expr); 
                  }
               }
            }";
         results = compiler.CompileAssemblyFromSource(parameters, code);

         Assembly assembly = results.CompiledAssembly;
         _evaluatorType = assembly.GetType("Evaluator.Evaluator");
         
         _evaluator = Activator.CreateInstance(_evaluatorType);
      }
      
      private object _evaluator = null;
      private Type _evaluatorType = null;
      private readonly string _jscriptSource = 
         
          @"package Evaluator
            {
               class Evaluator
               {
                  public function Eval(expr : String) : String 
                  { 
                     return eval(expr); 
                  }
               }
            }";
   }
}
