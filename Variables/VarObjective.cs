﻿using MCSharp.Compilation;
using MCSharp.GameSerialization.Text;
using System;
using System.Collections.Generic;
using System.IO;

namespace MCSharp.Variables {

	/// <summary>
	/// Represents a Minecraft scoreboard objective.
	/// </summary>
	public class VarObjective : Variable {

		/// <summary>
		/// A collection of every <see cref="VarObjective"/> organized by their <see cref="ID"/>.
		/// </summary>
		private static Dictionary<string, VarObjective> ObjectiveIDs { get; } = new Dictionary<string, VarObjective>();

#if DEBUG_OUT
		public static string NextID { get; set; }
#else
		public static int NextID { get; private set; }
#endif

		public override int Order => base.Order - 10;
		public override string TypeName => StaticTypeName;
		public static string StaticTypeName => "objective";
		/// <summary>The scoreboard name of this objective in-game.</summary>
		public string ID { get; private set; }
		/// <summary>The scoreboard type of this objective in-game.</summary>
		public string Type { get; private set; }

		public override ICollection<Access> AllowedAccessModifiers => new Access[] { Access.Private, Access.Public };
		public override ICollection<Usage> AllowedUsageModifiers => new Usage[] { Usage.Default, Usage.Constant, Usage.Static };


		public VarObjective() : base() { }
		public VarObjective(Access access, Usage usage, string name, Compiler.Scope scope) : base(access, usage, name, scope) { }


		public override Variable Initialize(Access access, Usage usage, string name, Compiler.Scope scope, ScriptTrace trace) => new VarObjective(access, usage, name, scope);

		private Compiler.Scope[] ConstructorScopes;
		private ParameterInfo[] ConstructorOverloads;
		public override Variable Construct(ArgumentInfo arguments) {
			if(ConstructorScopes is null) ConstructorScopes = InnerScope.CreateChildren(3);
			if(ConstructorOverloads is null) ConstructorOverloads = new ParameterInfo[] {
				new ParameterInfo(),
				new ParameterInfo((true, VarString.StaticTypeName, "type", ConstructorScopes[0])),
				new ParameterInfo((true, VarString.StaticTypeName, "type", ConstructorScopes[1]), (true, VarString.StaticTypeName, "name", ConstructorScopes[1])),
			};
			(ParameterInfo match, int index) = ParameterInfo.HighestMatch(ConstructorOverloads, arguments);
			match.Grab(arguments);

			string name, type;
			switch(index) {

				case 1:
					name = GetNextID();
					type = "dummy";
					goto Construct;
				case 2:
					name = GetNextID();
					type = (match["type"].Value as VarString).GetConstant();
					goto Construct;
				case 3:
					name = (match["name"].Value as VarString).GetConstant();
					type = (match["type"].Value as VarString).GetConstant();
					goto Construct;

					Construct:
					var value = new VarObjective(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope) { Type = type, ID = name };
					if(ObjectiveIDs.ContainsKey(value.ID)) throw new Compiler.InternalError($"Duplicate {StaticTypeName} ID created.", arguments.ScriptTrace);
					else ObjectiveIDs.Add(value.ID, this);
					value.Constructed = true;
					return value;

				default: throw new MissingOverloadException("Objective constructor", index, arguments);
			}

			static string GetNextID() {
				string id;
#if DEBUG_OUT
				if(NextID == null) throw new Compiler.InternalError($"{nameof(NextID)} was not set.");
				else { id = NextID; NextID = null; }
#else
				id = $"mcs.{BaseConverter.Convert(NextID++, 62)}";
#endif
				return id;
			}

		}

		public override void ConstructAsPasser() => throw new NotImplementedException();

		public override Variable InvokeOperation(Operation operation, Variable operand, ScriptTrace trace) {
			switch(operation) {
				case Operation.Set:
					if(operand is VarObjective right || operand.TryCast(TypeName, out right)) {
						if(ID == null && Type == null) {
							ID = right.ID;
							Type = right.Type;
							return this;
						} else {
							throw new Exception();
						}
					} else throw new Compiler.SyntaxException($"Cannot cast '{operand}' into '{TypeName}'.", trace);
				default: return base.InvokeOperation(operation, operand, trace);
			}
		}

		public override IDictionary<string, Caster> GetCasters_To() {
			IDictionary<string, Caster> casters = base.GetCasters_To();
			casters.Add(TypeName, value => {
				var result = new VarInt(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
				result.SetValue((VarSelector)"@e", this);
				return result;
			});
			casters.Add(VarBool.StaticTypeName, value => {
				var result = new VarBool(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope);
				result.SetValue((VarSelector)"@e", this);
				return result;
			});
			return casters;
		}

		public override void WritePrep(StreamWriter function) {
			base.WritePrep(function);
			function.WriteLine($"scoreboard objectives add {ID} {Type}");
		}

		public override void WriteDemo(StreamWriter function) {
			base.WriteDemo(function);
			function.WriteLine($"scoreboard objectives remove {ID}");
		}

		public override string GetConstant() => ID;
		public override RawText GetRawText() => new RawText() { Text = ID };

		public static void ResetID() {
			ObjectiveIDs.Clear();
#if DEBUG_OUT
			NextID = null;
#else
			NextID = 0;
#endif
		}
	}

}
