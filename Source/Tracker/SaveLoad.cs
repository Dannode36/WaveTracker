﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WaveTracker.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;
using System.IO;
using System.Text.Encodings;
using WaveTracker.Tracker;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using ProtoBuf;
using WaveTracker.Rendering;
using WaveTracker;
using WaveTracker.Audio;

namespace WaveTracker {
    public static class SaveLoad {
        public const bool USE_PROTO_BUF = true;

        public static WTModule savedModule;


        public static bool isSaved { get { if (savedModule == null) return false; else return savedModule.Equals(App.CurrentModule); } }
        public static string filePath = "";

        public static string fileName { get { if (filePath == "") return "Untitled.wtm"; return Path.GetFileName(filePath); } }
        public static string fileNameWithoutExtension { get { if (filePath == "") return "Untitled"; return Path.GetFileNameWithoutExtension(filePath); } }
        public static int savecooldown = 0;


        static void SaveTo(string path) {
            Debug.WriteLine("Saving to: " + path);
            Stopwatch stopwatch = Stopwatch.StartNew();
            //BinaryFormatter formatter = new BinaryFormatter();

            try {
                //savedModule = App.CurrentModule.Clone();
            } catch {
                Debug.WriteLine("failed to save");
                return;
            }

            //savedSong.InitializeForSerialization();
            //path = Path.ChangeExtension(path, ".wtm");
            using (FileStream fs = new FileStream(path, FileMode.Create)) {
                Serializer.Serialize(fs, App.CurrentModule);
            }
            //using (FileStream fs = new FileStream(path, FileMode.Create))
            //{
            //    formatter.Serialize(fs, savedSong);
            //}
            stopwatch.Stop();
            Debug.WriteLine("saved in " + stopwatch.ElapsedMilliseconds + " ms");
            return;

        }

        public static void SaveFile() {
            if (savecooldown == 0)
                if (!File.Exists(filePath)) {
                    SaveFileAs();
                }
                else {
                    SaveTo(filePath);
                }
            savecooldown = 4;

        }

        public static void NewFile() {
            if (Input.focus != null)
                return;

            if (!isSaved) {
                if (PromptUnsaved() == DialogResult.Cancel) return;
            }
            Playback.Stop();
            filePath = "";
            //FrameEditor.ClearHistory();
            //FrameEditor.Goto(0, 0);
            Playback.Goto(0, 0);
            App.PatternEditor.OnSwitchSong();
            ChannelManager.UnmuteAllChannels();
            //FrameEditor.cursorColumn = 0;
            //FrameEditor.UnmuteAllChannels();
            Song.currentSong = null;
            savedModule = new WTModule();
            App.CurrentModule = savedModule;
        }

        public static void SaveFileAs() {
            if (Input.focus != null)
                return;
            Playback.Stop();
            // set filepath to dialogresult
            if (SetFilePathThroughSaveAsDialog(out filePath)) {
                Debug.WriteLine("Saving as: " + filePath);
                SaveTo(filePath);
                Debug.WriteLine("Saved as: " + filePath);
            }

        }

        public static void OpenFile() {
            if (Input.internalDialogIsOpen)
                return;
            Playback.Stop();
            if (savecooldown == 0) {
                // set filepath to dialog result
                string currentPath = filePath;
                if (!isSaved) {
                    if (PromptUnsaved() == DialogResult.Cancel) {
                        return;
                    }
                    Input.dialogOpenCooldown = 0;
                }
                if (SetFilePathThroughOpenDialog())
                    if (LoadFrom(filePath)) {
                        Visualization.GetWaveColors();
                        ChannelManager.Reset();
                        App.PatternEditor.OnSwitchSong();
                        //FrameEditor.Goto(0, 0);
                        Playback.Goto(0, 0);
                        //FrameEditor.cursorColumn = 0;
                        //FrameEditor.UnmuteAllChannels();
                        //FrameEditor.ClearHistory();
                    }
                    else {
                        LoadError();
                        filePath = currentPath;

                    }

            }
            savecooldown = 4;
        }

        public static bool LoadFrom(string path) {
            if (!File.Exists(path))
                return false;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //BinaryFormatter formatter = new BinaryFormatter();

            //MemoryStream ms = new MemoryStream();
            //savedSong = new Song();

            
            try {
                using (FileStream fs = new FileStream(path, FileMode.Open)) {
                    fs.Position = 0;
                    App.CurrentModule = Serializer.Deserialize<WTModule>(fs);
                }
            } catch {
                using (FileStream fs = new FileStream(path, FileMode.Open)) {
                    fs.Position = 0;
                    Song.currentSong = Serializer.Deserialize<Song>(fs);
                }
                Song.currentSong.Deserialize();
                App.CurrentModule = WTModule.FromOldSongFormat(Song.currentSong);
                Song.currentSong = null;

            }


            //Song.currentSong = savedSong.Clone();
            App.PatternEditor.OnSwitchSong();
            ChannelManager.Reset();
            //FrameEditor.Goto(0, 0);
            //FrameEditor.cursorColumn = 0;
            stopwatch.Stop();
            Debug.WriteLine("opened in " + stopwatch.ElapsedMilliseconds + " ms");
            filePath = path;

            return true;
        }



        public static bool SetFilePathThroughOpenDialog() {
            bool didIt = false;
            if (Input.dialogOpenCooldown == 0) {
                Thread t = new Thread((ThreadStart)(() => {

                    Input.DialogStarted();
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "WaveTracker modules (*wtm)|*.wtm";
                    openFileDialog.Multiselect = false;
                    openFileDialog.Title = "Open";
                    openFileDialog.ValidateNames = true;
                    if (openFileDialog.ShowDialog() == DialogResult.OK) {
                        filePath = openFileDialog.FileName;

                        didIt = true;
                    }

                }));

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();

            }
            return didIt;
        }

        public static DialogResult PromptUnsaved() {
            DialogResult ret = DialogResult.Cancel;
            if (Input.dialogOpenCooldown == 0) {
                Input.DialogStarted();
                ret = MessageBox.Show("Save changes to " + fileName + "?", "WaveTracker", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            }
            if (ret == DialogResult.Yes) {
                SaveFile();
            }
            return ret;
        }

        public static void LoadError() {
            if (Input.dialogOpenCooldown == 0) {
                Input.DialogStarted();

                MessageBox.Show("Could not open " + fileName, "WaveTracker", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }

        public static bool SetFilePathThroughSaveAsDialog(out string filepath) {
            string ret = "";
            bool didIt = false;
            filepath = filePath;
            if (Input.dialogOpenCooldown == 0) {
                Input.DialogStarted();
                Thread t = new Thread((ThreadStart)(() => {

                    Input.DialogStarted();
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.DefaultExt = "wtm";
                    saveFileDialog.Filter = "WaveTracker modules (*.wtm)|*.wtm|All files (*.*)|*.*";
                    saveFileDialog.OverwritePrompt = true;
                    saveFileDialog.FileName = fileName;
                    saveFileDialog.Title = "Save As";
                    saveFileDialog.AddExtension = true;
                    saveFileDialog.CheckPathExists = true;
                    saveFileDialog.ValidateNames = true;


                    if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                        ret = saveFileDialog.FileName;
                        didIt = true;
                    }

                }));

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
                filepath = ret;
            }
            return didIt;
        }

        public static bool ChooseExportPath(out string filepath) {
            string ret = "";
            bool didIt = false;
            if (Input.dialogOpenCooldown == 0) {
                Thread t = new Thread((ThreadStart)(() => {

                    Input.DialogStarted();
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.DefaultExt = "wav";
                    saveFileDialog.OverwritePrompt = true;
                    saveFileDialog.FileName = fileNameWithoutExtension;
                    saveFileDialog.Title = "Export .wav";
                    saveFileDialog.Filter = "Waveform Audio File Format (*.wav)|*.wav|All files (*.*)|*.*";
                    saveFileDialog.AddExtension = true;
                    saveFileDialog.CheckPathExists = true;
                    saveFileDialog.ValidateNames = true;


                    if (saveFileDialog.ShowDialog() == DialogResult.OK) {
                        ret = saveFileDialog.FileName;
                        didIt = true;
                    }

                }));

                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
            filepath = ret;
            return didIt;
        }
    }
}
