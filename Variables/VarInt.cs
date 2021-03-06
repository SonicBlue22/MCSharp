﻿using MCSharp.Compilation;
using MCSharp.GameSerialization.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static MCSharp.Compilation.ScriptObject;

namespace MCSharp.Variables {

	public class VarInt : VarPrimitive {

		public override string TypeName => StaticTypeName;
		public static string StaticTypeName => "int";

		public override ICollection<Access> AllowedAccessModifiers => new Access[] { Access.Private, Access.Public };
		public override ICollection<Usage> AllowedUsageModifiers => new Usage[] { Usage.Default, Usage.Constant, Usage.Static };


		public VarInt() : base() { }

		public VarInt(Access access, Usage usage, string name, Compiler.Scope scope) : base(access, usage, name, scope) { }


		public override Variable Initialize(Access access, Usage usage, string name, Compiler.Scope scope, ScriptTrace trace) => new VarInt(access, usage, name, scope);
		public override Variable Construct(ArgumentInfo passed) => throw new Compiler.SyntaxException($"'{TypeName}' types cannot be constructed.", Compiler.CurrentScriptTrace);
		public override void ConstructAsPasser() => SetValue(0);

		public override void WriteCopyTo(StreamWriter function, Variable variable) {
			if(variable is Pointer<VarInt> pointer) pointer.Variable = this;
			else if(variable is VarInt varInt || variable.TryCast(StaticTypeName, out varInt)) {
				if(Usage == Usage.Constant) {
					function.WriteLine($"scoreboard players set {varInt.Selector.GetConstant()} {varInt.Objective.GetConstant()} {Constant}");
				} else {
					function.WriteLine($"scoreboard players operation {varInt.Selector.GetConstant()} {varInt.Objective.GetConstant()} = {Selector.GetConstant()} {Objective.GetConstant()}");
				}
			} else throw new InvalidArgumentsException($"Unknown how to interpret '{variable}' as '{TypeName}'.", Compiler.CurrentScriptTrace);
		}

		public override Variable InvokeOperation(Operation operation, Variable operand, ScriptTrace scriptTrace) {

			if(operand is VarInt right || operand.TryCast(StaticTypeName, out right)) {

				string op;

				switch(operation) {

					case Operation.Add:
						op = "+=";
						goto Bitwise;
					case Operation.Subtract:
						op = "-=";
						goto Bitwise;
					case Operation.Multiply:
						op = "*=";
						goto Bitwise;
					case Operation.Divide:
						op = "/=";
						goto Bitwise;
					case Operation.Modulo:
						op = "%=";
						goto Bitwise;

						Bitwise:
						{
							VarInt result;
							if(Usage == Usage.Constant) {
								result = new VarInt(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
								int val1 = Constant, val2 = right.Constant;
								if(right.Usage == Usage.Constant) {
									switch(operation) {
										case Operation.Add:
											result.SetValue(val1 + val2);
											break;
										case Operation.Subtract:
											result.SetValue(val1 - val2);
											break;
										case Operation.Multiply:
											result.SetValue(val1 * val2);
											break;
										case Operation.Divide:
											result.SetValue(val1 / val2);
											break;
										case Operation.Modulo:
											result.SetValue(val1 % val2);
											break;
										default: throw new Compiler.InternalError("095208302020");
									}
								} else {
									result = new VarInt(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
									result.SetValue(Constant);
									new Spy(null, $"scoreboard players operation " +
										$"{result.Selector.GetConstant()} {result.Objective.GetConstant()} {op} " +
										$"{right.Selector.GetConstant()} {right.Objective.GetConstant()}", null);
								}
							} else {
								result = new VarInt(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
								result.SetValue(Selector, Objective);
								if(right.Usage == Usage.Constant) {
									int value = right.Constant;
									right = new VarInt(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
									right.SetValue(value);
								}
								new Spy(null, $"scoreboard players operation " +
									$"{result.Selector.GetConstant()} {result.Objective.GetConstant()} {op} " +
									$"{right.Selector.GetConstant()} {right.Objective.GetConstant()}", null);
							}
							return result;
						}


					case Operation.GreaterThan:
						op = ">";
						goto Comparison;
					case Operation.GreaterThanOrEqual:
						op = ">=";
						goto Comparison;
					case Operation.Equal:
						op = "=";
						goto Comparison;
					case Operation.LessThan:
						op = "<";
						goto Comparison;
					case Operation.LessThanOrEqual:
						op = "<=";
						goto Comparison;

						Comparison:
						{
							var result = new VarBool(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
							result.SetValue(0);
							new Spy(null, $"execute if score {Selector.GetConstant()} {Objective.GetConstant()} {op} {right.Selector.GetConstant()} {right.Objective.GetConstant()} run " +
								$"scoreboard players set {result.Selector.GetConstant()} {result.Objective.GetConstant()} 1", null);
							return result;
						}


					default: return base.InvokeOperation(operation, operand, scriptTrace);

				}

			} else throw new Compiler.SyntaxException($"Cannot cast '{operand}' into '{TypeName}'.", scriptTrace);

		}

		public override IDictionary<string, Caster> GetCasters_To() {
			IDictionary<string, Caster> casters = base.GetCasters_To();
			casters.Add(VarBool.StaticTypeName, value => {
				var original = value as VarInt;
				bool constant = original.Usage == Usage.Constant;
				var result = new VarBool(Access.Private, constant ? Usage.Constant : Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
				if(constant) result.SetValue(original.Constant);
				else result.SetValue(original.Selector, original.Objective);
				return result;
			});
			return casters;
		}

		public override RawText GetRawText() => new RawText() {
			Score = Usage == Usage.Constant ? null
			: new ScoreData() {
				Name = Selector.GetConstant(),
				Objective = Objective.GetConstant()
			},
			Text = Usage != Usage.Constant ? null
			: Constant.ToString()
		};

	}

}
