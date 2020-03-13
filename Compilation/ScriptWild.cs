﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace MCSharp.Compilation {

	/// <summary>
	/// Can either be a single <see cref="ScriptWord"/> or... more <see cref="ScriptWild"/>s, in which case you can consider it a <see cref="ScriptLine"/>.
	/// </summary>
	public struct ScriptWild : IReadOnlyCollection<ScriptWord> {

		//todo: add "block type" property as string

		private readonly ScriptWord? word;
		private readonly ScriptWild[] wilds;
		private readonly string str;

		/// <summary>
		/// 
		/// </summary>
		public bool IsWord => word != null;

		/// <summary>
		/// When a <see cref="ScriptWild"/> is just a <see cref="Compilation.ScriptWord"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException()">Thrown when <see cref="IsWord"/> is false.</exception>
		public ScriptWord Word => IsWord ? word.Value
			: throw new InvalidOperationException($"Cannot get '{nameof(Word)}' because '{nameof(IsWord)}' is false!");

		/// <summary>
		/// 
		/// </summary>
		public bool IsWilds => wilds != null;

		/// <summary>
		/// When a <see cref="ScriptWild"/> is just more <see cref="ScriptWild"/>s.
		/// </summary>
		/// <exception cref="InvalidOperationException()">Thrown when <see cref="IsWilds"/> is false.</exception>
		public IReadOnlyList<ScriptWild> Wilds => IsWilds ? wilds 
			: throw new InvalidOperationException($"Cannot get '{nameof(Wilds)}' because '{nameof(IsWilds)}' is false!");
		public ScriptWild[] Array {
			get {
				if(IsWilds) {
					var wilds = new ScriptWild[this.wilds.Length];
					this.wilds.CopyTo(wilds, 0);
					return wilds;
				} else {
					return new ScriptWild[] { Word };
				}
			}
		}

		/// <summary>
		/// Creates a new <see cref="ScriptWild"/> from the given <see cref="Range"/> applied to <see cref="Wilds"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException()">Thrown when <see cref="IsWilds"/> is false.</exception>
		public ScriptWild this[Range range] => new ScriptWild(wilds[range], BlockType, SeparationType.Value);

		/// <summary>
		/// Format: "[OPEN]\[CLOSE]"
		/// </summary>
		public string BlockType { get; }
		public char? SeparationType { get; }
		public string FullBlockType => $"{BlockType[0]}\\{SeparationType}\\{BlockType[2]}";

		public int Count {
			get {
				if(IsWord) return 1;
				else {
					int sum = 0;
					foreach(ScriptWild wild in wilds)
						sum += wild.Count;
					return sum;
				}
			}
		}

		public ScriptTrace ScriptTrace => IsWord ? word.Value.ScriptTrace : (wilds.Length > 0 ? wilds[0].ScriptTrace : throw new Exception("123502252020"));


		/// <summary>
		/// Creates a new <see cref="ScriptWild"/> that is just a <see cref="ScriptWord"/>.
		/// </summary>
		public ScriptWild(ScriptWord word) {
			this.word = word;
			wilds = null;
			BlockType = null;
			SeparationType = null;
			str = (string)word;
		}

		/// <summary>
		/// Creates a new <see cref="ScriptWild"/> that is just more <see cref="ScriptWild"/>s.
		/// </summary>
		public ScriptWild(IList<ScriptWild> wilds, string block, char separation) {

			if(wilds is null)
				throw new ArgumentNullException(nameof(wilds));
			if(string.IsNullOrEmpty(block))
				throw new ArgumentException("Argument cannot be null or empty.", nameof(block));

			if(wilds.Count == 0) {

				word = null;
				this.wilds = new ScriptWild[] { };
				BlockType = block;
				SeparationType = separation;
				str = $"{block[0]}{block[2]}";

			} else if(wilds.Count <= 1 && !wilds[0].IsWilds && block == " \\ " && separation == ' ') {

				word = wilds[0].Word;
				this.wilds = null;
				BlockType = null;
				SeparationType = null;
				str = (string)word;

			} else {

				this.wilds = new ScriptWild[wilds.Count];
				wilds.CopyTo(this.wilds, 0);
				word = null;

				BlockType = block;
				SeparationType = separation;

				string str = "";
				foreach(string wld in wilds)
					str += separation + wld;
				if(separation == ';') this.str = str.Length > 0 ? $"{block[0]}{str[1..]};{block[2]}" : $"{block[0]};{block[2]}";
				else this.str = str.Length > 0 ? $"{block[0]}{str[1..]}{block[2]}" : $"{block[0]}{block[2]}";

			}
		}

		public IEnumerator<ScriptWord> GetEnumerator() {
			if(IsWord) return ((IEnumerable<ScriptWord>)new ScriptWord[] { Word }).GetEnumerator();
			else {
				var words = new LinkedList<ScriptWord>();
				foreach(ScriptWild wild in Wilds) {
					foreach(ScriptWord word in wild) {
						words.AddLast(word);
					}
				}
				return words.GetEnumerator();
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString() => $"{ScriptTrace}: {str}";

		public static implicit operator string(ScriptWild wild) => wild.str;
		public static implicit operator ScriptWild(ScriptWord word) => new ScriptWild(word);

	}

}
