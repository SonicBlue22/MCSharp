﻿using MCSharp.Compilation;
using MCSharp.Variables;
using System;
using System.Collections.Generic;
using System.Text;

namespace MCSharp.Statements {

    public class StmtIf : Statement {

        public override string Call => "if";

        public override void Read(ref List<ScriptLine> list, ref int start, ref int end, ref string function) {

            var wilds = new List<ScriptWild> { new ScriptWord("if") };

            end--;
            var stack = new Stack<string>();
            bool expectingCondition = true;
            bool expectingInstruction = false;

            while(++end < function.Length) {
                char chr = function[end];

                //Skip whitespaces, since the parsing is done mostly done in the separate loops below.
                if(char.IsWhiteSpace(chr)) continue;

                if(expectingCondition) {
                    // <<Expecting Condition>>
                    if(chr == '(') stack.Push("(\\)");
                    else throw new Compiler.SyntaxException("Expected '(...)' after keyword 'if'.");
                    start = end;
                    //Find the end of the condition parenthesies.
                    while(++end < function.Length) {
                        chr = function[end];
                        if(ScriptLine.IsBlockCharStart(chr, out string block)) {
                            //Start a new block.
                            stack.Push(block);
                        } else if(ScriptLine.IsBlockCharEnd(chr, out block)) {
                            //End the current block.
                            string b = stack.Pop();
                            if(block != b) throw new Compiler.SyntaxException($"Expected '{b[2]}', but got '{block[2]}'.");
                            if(stack.Count == 0) /* <<End of Conditional>> */ break;
                        }
                    }
                    //Get the full conditional as a string.
                    string s = function[start..(end + 1)];
                    //Parse the conditional using ScriptLine.GetWilds(...).
                    var parsed = ScriptLine.GetWilds(s);
                    //Should always be a single ScriptWild, since it is completely in a (\\).
                    if(parsed.Length != 1) throw new Exception();
                    //Add the conditional to the list.
                    wilds.Add(parsed[0]);

                    //No longer expecting condition.
                    expectingCondition = false;
                    //Next is an instruction.
                    expectingInstruction = true;


                } else if(expectingInstruction) {
                    // <<Expecting Instruction>>
                    if(chr == '{') {
                        // <<Parsing Code Block>>
                        stack.Push("{\\}");
                        start = end;
                        //Find the end of the code block brackets.
                        while(++end < function.Length) {
                            chr = function[end];
                            if(ScriptLine.IsBlockCharStart(chr, out string block)) {
                                //Start a new block.
                                stack.Push(block);
                            } else if(ScriptLine.IsBlockCharEnd(chr, out block)) {
                                //End the current block.
                                string b = stack.Pop();
                                if(block != b) throw new Compiler.SyntaxException($"Expected '{b[2]}', but got '{block[2]}'.");
                                if(stack.Count == 0) /* <<End of Code Block>> */ break;
                            }
                        }
                        //Get the full code block as a string.
                        string s = function[start..(end + 1)];
                        //Parse the code block using ScriptLine.GetWilds(...).
                        var parsed = ScriptLine.GetWilds(s);
                        //Should always be a single ScriptWild, since it is completely in a {\\}.
                        if(parsed.Length != 1) throw new Exception();
                        //Add the code block to the list.
                        wilds.Add(parsed[0]);

                    } else {
                        // <<Parsing Single Line>>
                        start = end;
                        //Find the ';'.
                        while(++end < function.Length) {
                            chr = function[end];
                            if(ScriptLine.IsBlockCharStart(chr, out string block)) {
                                //Start a new block.
                                stack.Push(block);
                            } else if(ScriptLine.IsBlockCharEnd(chr, out block)) {
                                //End the current block.
                                string b = stack.Pop();
                                if(block != b) throw new Compiler.SyntaxException($"Expected '{b[2]}', but got '{block[2]}'.");
                            } else if(stack.Count == 0) {
                                if(chr != ';') throw new Compiler.SyntaxException("Expected ';'.");
                                else /* <<End of Statement>> */ break;
                            }
                        }
                        //Parse the conditional using ScriptLine.GetWilds(...).
                        var parsed = ScriptLine.GetWilds(function[start..end]);
                        //Group the parsed conditional into a ScriptWild.
                        var wild = new ScriptWild(parsed, "{\\}", ';');
                        //Add the conditional to the list.
                        wilds.Add(wild);
                    }

                    //No longer expecting an instruction.
                    expectingInstruction = false;
                    //Next is 'else', if at all.
                    // (process of elimination)


                } else {
                    // <<Expecting 'else' Statement>>
                    if(!LookFor("else", function, start, end, out int i)) {
                        // <<Parsing 'else' Statement>>
                        start = end = i - 1;
                        bool parsingBlock = false, parsingSingle = false;
                        while(++end < function.Length) {
                            chr = function[end];

                            if(ScriptLine.IsBlockCharStart(chr, out string block)) {
                                stack.Push(block);
                            } else if(ScriptLine.IsBlockCharEnd(chr, out block)) {
                                string b = stack.Pop();
                                if(block != b) throw new Compiler.SyntaxException($"Expected '{b[2]}', but got '{block[2]}'.");
                            }

                            if(parsingBlock) {
                                // <<Finding an End of Block>>
                                if(chr == '}' && stack.Count == 0) /* <<Found End of Block>> */ break;
                            } else if(parsingSingle) {
                                // <<Finding an End of Instruction>>
                                if(chr == ';' && stack.Count == 0) /* <<Found End of Instruction>> */ break;
                            } else {
                                //Find what to find.
                                if(char.IsWhiteSpace(chr)) continue;
                                if(chr == '{') parsingBlock = true;
                                else parsingSingle = true;
                            }
                        }
                        //Parse the instruction using ScriptLine.GetWilds(...).
                        var parsed = ScriptLine.GetWilds(function[start..end]);
                        //Group the parsed instruction into a ScriptWild.
                        var wild = new ScriptWild(parsed, " \\ ", ' ');
                        //Add the instruction to the list.
                        wilds.Add(wild);
                        break;

                    } else {
                        // <<No 'else' Statement>>
                        start = end++;
                        break;
                    }
                }

            }

            //Add 'wilds' as a single ScriptWild group.
            var final = new ScriptWild(wilds.ToArray(), " \\ ", ' ');
            list.Add(new ScriptLine(final));

        }

        public override void Write(ScriptLine line) {
            ScriptWild conditionWild = line[1];
            ScriptWild statementWild = line[2];
            ScriptWild? elseWild = line.Length > 3 ? (ScriptWild?)line[4] : null;
            VarBool condition;
            if(Compiler.TryParseValue(conditionWild, Compiler.CurrentScope, out Variable conditionVariable)) {
                if(conditionVariable is VarBool varBool || conditionVariable.TryCast(out varBool)) {
                    condition = varBool;
                    var statement = new ScriptFunction($"{Compiler.CurrentScope}\\{Compiler.CurrentScope.GetNextInnerID()}", statementWild);
                    Compiler.WriteFunction(Compiler.CurrentScope, statement);
                    new Spy(null, $"execute if score {condition.Selector.GetConstant()} {condition.Objective.GetConstant()} matches 1.. " +
                        $"run function {statement.GamePath}", null);
                } else throw new Variable.InvalidArgumentsException($"Could not cast '{conditionVariable}' as a 'bool'.");
            } else throw new Exception(); //TODO:  add details
            if(elseWild.HasValue) {
                var statement = new ScriptFunction($"{Compiler.CurrentScope}\\{Compiler.CurrentScope.GetNextInnerID()}", elseWild.Value);
                Compiler.WriteFunction(Compiler.CurrentScope, statement);
                new Spy(null, $"execute if score {condition.Selector.GetConstant()} {condition.Objective.GetConstant()} matches ..0 " +
                    $"run function {statement.GamePath}", null);
            }
        }

        private static bool LookFor(string item, string function, int start, int end, out int i) {
            bool whitespace = true;
            while(++end < function.Length) {
                i = end - 1;
                if(char.IsWhiteSpace(function[end]) && whitespace) start = end + 1;
                else {
                    whitespace = false;
                    string s = function[start..end];
                    if(s.Length >= item.Length) return s == item;
                    else continue;
                }
            }
            i = end - 1;
            return false;
        }

    }

}