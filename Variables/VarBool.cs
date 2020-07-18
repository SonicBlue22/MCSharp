﻿using MCSharp.Compilation;
using System.IO;
using static MCSharp.Compilation.ScriptObject;

namespace MCSharp.Variables {

	public class VarBool : PrimitiveType {

		public override string TypeName => "bool";

		public static VarBool TrueValue { get; private set; }
		public static VarBool FalseValue { get; private set; }


		public VarBool() : base() { }

		public VarBool(Access access, Usage usage, string name, Compiler.Scope scope) : base(access, usage, name, scope) { }


		public override Variable Initialize(Access access, Usage usage, string name, Compiler.Scope scope, ScriptTrace trace) {
			base.Initialize(access, usage, name, scope, trace);
			return new VarBool(access, usage, name, scope);
		}

		public override void WriteCopyTo(StreamWriter function, Variable variable) {
			if(variable is Pointer<VarBool> pointer) pointer.Variable = this;
			else if(variable is VarInt varInt || variable.TryCast(out varInt)) {
				function.WriteLine($"scoreboard players operation var {varInt.Objective.ID} = var {Objective.ID}");
			} else throw new InvalidArgumentsException($"Unknown how to interpret '{variable}' as '{TypeName}'.", Compiler.CurrentScriptTrace);
		}

		public override Variable InvokeOperation(Operation operation, Variable operand, ScriptTrace scriptTrace) {

			if(!(operand is VarBool right) && !operand.TryCast(out right))
				throw new Compiler.SyntaxException($"Cannot cast '{operand}' into '{TypeName}'.", scriptTrace);

			switch(operation) {

				case Operation.Set:
					new Spy(null, $"scoreboard players operation " +
						$"{Selector.GetConstant()} {Objective.GetConstant()} = " +
						$"{right.Selector.GetConstant()} {right.Objective.GetConstant()}", null);
					return this;

				case Operation.BooleanNot: {
					string id = GetNextHiddenID();
					//Create temp variable from this.
					var anon = new VarBool(Access.Private, Usage.Default, id, Compiler.CurrentScope);
					anon.SetValue(Selector, Objective);
					//Write function: 'set anon to the opposite of this'.
					var function = new ScriptMethod($"{Compiler.CurrentScope}\\{Compiler.CurrentScope.GetNextInnerID()}",
						"void", new Variable[] { }, Compiler.CurrentScope.DeclaringType,
						new ScriptString($"if({anon.ObjectName}) {{ {anon.ObjectName} = false; }} else {{ {anon.ObjectName} = true; }}"));
					Compiler.WriteFunction<VarVoid>(Compiler.CurrentScope, null, function);
					//Write command: 'run that function'.
					new Spy(null, $"function {function.GameName}", null);
					return anon;
				}

				case Operation.BooleanAnd: {
					//Create temp variable from this.
					var anon = new VarBool(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
					anon.SetValue(Selector, Objective);
					//Write function: 'if anon is true, set anon to args'.
					var function = new ScriptMethod($"{Compiler.CurrentScope}\\{Compiler.CurrentScope.GetNextInnerID()}",
						"void", new Variable[] { }, Compiler.CurrentScope.DeclaringType,
						new ScriptString($"if({anon.ObjectName}) {{ {anon.ObjectName} = {right.ObjectName}; }}"));
					Compiler.WriteFunction<VarVoid>(Compiler.CurrentScope, null, function);
					//Write command: 'run that function'.
					new Spy(null, $"function {function.GameName}", null);
					return anon;
				}

				default: return base.InvokeOperation(operation, operand, scriptTrace);

			}
		}

		public override string GetJSON() => $"{{\"score\":{{\"name\":\"var\",\"objective\":\"{Objective.ID}\"}}}}";

	}

}
