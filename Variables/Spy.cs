﻿using MCSharp.Compilation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MCSharp.Variables {
	[DebuggerDisplay("*SPY*")]
	public class Spy : Variable {

		public Action<StreamWriter> Prep { get; }
		public Action<StreamWriter> Init { get; }
		public Action<StreamWriter> Demo { get; }

		public override int Order => 100;
		public override string TypeName => throw new InvalidOperationException("Spies are not variables.");
		public override ICollection<Access> AllowedAccessModifiers => throw new InvalidOperationException("Spies are not variables.");
		public override ICollection<Usage> AllowedUsageModifiers => throw new InvalidOperationException("Spies are not variables.");

		public Spy() { }

		public Spy(string prep, string init, string demo) :
		base(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope) {
			Init = (function) => { if(init != null) function.WriteLine(init); };
			Prep = (function) => { if(prep != null) function.WriteLine(prep); };
			Demo = (function) => { if(demo != null) function.WriteLine(demo); };
		}

		public Spy(string[] prep, string[] init, string[] demo) :
		base(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope) {
			Prep = (function) => { if(prep != null) foreach(string command in prep) function.WriteLine(command); };
			Init = (function) => { if(init != null) foreach(string command in init) function.WriteLine(command); };
			Demo = (function) => { if(demo != null) foreach(string command in demo) function.WriteLine(command); };
		}

		public Spy(Action<StreamWriter> prep, Action<StreamWriter> init, Action<StreamWriter> demo) :
		base(Access.Private, Usage.Default, GetNextHiddenID(), Compiler.CurrentScope) {
			Prep = prep;
			Init = init;
			Demo = demo;
		}

		public override Variable Initialize(Access access, Usage usage, string name, Compiler.Scope scope, ScriptTrace trace) => null;
		public override Variable Construct(ArgumentInfo passed) => null;
		public override void ConstructAsPasser() => throw new NotImplementedException();

		public override void WriteInit(StreamWriter function) {
			base.WriteInit(function);
			if(Init != null) Init.Invoke(function);
		}

		public override void WritePrep(StreamWriter function) {
			base.WritePrep(function);
			if(Prep != null) Prep.Invoke(function);
		}

		public override void WriteDemo(StreamWriter function) {
			base.WriteDemo(function);
			if(Demo != null) Demo.Invoke(function);
		}

	}
}
