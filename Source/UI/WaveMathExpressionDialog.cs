﻿using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using WaveTracker.Tracker;
using WaveTracker.Source;

namespace WaveTracker.UI {
    public class WaveMathExpressionDialog : WaveModifyDialog {
        public Textbox ExpressionInput;
        public CheckboxLabeled WaveFoldCheckbox;
        public NumberBoxDecimal InputA, InputB, InputC;

        WaveExpression mathExpression;
        private double compileTime = 0;
        private bool compileSuccess = true;
        private string lastCompileError = string.Empty;

        public WaveMathExpressionDialog() : base("Generate from maths expression...", 350) {
            ExpressionInput = new Textbox("", 8, 25, 169, this);
            ExpressionInput.SetTooltip("", "Expression");
            ExpressionInput.Text = "0";

            WaveFoldCheckbox = new CheckboxLabeled("Wave folding", 7, 42, 40, this);
            WaveFoldCheckbox.SetTooltip("", "Wraps the waveform");
            WaveFoldCheckbox.Value = false;

            mathExpression = new(ExpressionInput.Text);
            ExpressionInput.Update(); //Unsets ValueWasChanged flag

            int inputValueX = 8;
            InputA = new NumberBoxDecimal("A", inputValueX, 58, this);
            inputValueX += 60;
            InputB = new NumberBoxDecimal("B", inputValueX, 58, this);
            inputValueX += 60;
            InputC = new NumberBoxDecimal("C", inputValueX, 58, this);
        }

        public new void Open(Wave wave) {
            base.Open(wave);
        }

        public override void Update() {
            if (WindowIsOpen) {
                base.Update();
                ExpressionInput.Update();
                WaveFoldCheckbox.Update();
                InputA.Update();
                InputB.Update();
                InputC.Update();

                if (ExpressionInput.ValueWasChanged) {
                    compileSuccess = true; //Set to true as a catch-all
                    try {
                        Stopwatch sw = Stopwatch.StartNew();

                        mathExpression.Expression = ExpressionInput.Text;

                        compileTime = sw.Elapsed.TotalMilliseconds;
                        Apply();
                    } catch (Exception e) {
                        compileSuccess = false;
                        if(e.InnerException != null) {
                            lastCompileError = e.InnerException.Message;
                        }
                        else {
                            lastCompileError = e.Message;
                        }
                    }
                }
                if (WaveFoldCheckbox.Clicked) {
                    mathExpression.WaveFold = WaveFoldCheckbox.Value;
                    Apply();
                }
                if (InputA.ValueWasChanged || InputB.ValueWasChanged || InputC.ValueWasChanged) {
                    Apply();
                }
            }
        }

        protected override byte GetSampleValue(int index) {
            EvaluationContext context = new() {
                x = (index << 1) * Math.PI / waveToEdit.samples.Length,
                a = InputA.Value,
                b = InputB.Value,
                c = InputC.Value
            };
            return mathExpression.GetSampleValue(context);
        }

        public new void Draw() {
            if (WindowIsOpen) {
                base.Draw();
                ExpressionInput.Draw();
                WaveFoldCheckbox.Draw();
                InputA.Draw();
                InputB.Draw();
                InputC.Draw();

                if (compileSuccess) {
                    Write($"Compilation successful ({Math.Round(compileTime, 3)} ms)", 8, 76, Color.Green);
                }
                else {
                    WriteMultiline("Compilation failed: " + lastCompileError, 8, 76, 169, Color.OrangeRed);
                }
            }
        }
    }
}
