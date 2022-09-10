using Modding;
using System;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;

namespace VocalKnight
{
    public class VocalKnight : Mod
    {
        private DictationRecognizer dictRecognizer;
        private Dictionary<string, Action> keywords = new Dictionary<string, Action>();
        private List<string> guessedKeys = new List<string>();

        private static VocalKnight? _instance;

        public static VocalKnight Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(VocalKnight)} was never initialized");
                }
                return _instance;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public VocalKnight() : base()
        {
            _instance = this;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");
            ObjectLoader.Load(preloadedObjects);
            //ObjectLoader.LoadAssets();
            keywords.Add("zap", () => { Commands.Enemies.StartZapping(); });
            keywords.Add("shock", () => { Commands.Enemies.StartZapping(); });
            keywords.Add("log", () => { Commands.Enemies.Jars(); });
            keywords.Add("attack", () => { Commands.Enemies.Revek(); });
            keywords.Add("night", () => { Commands.Enemies.SpawnPureVessel(); });
            keywords.Add("shade", () => { Commands.Enemies.SpawnShade(); });
            keywords.Add("fly", () => { Commands.Enemies.SpawnEnemy("buzzer"); });
            //ModHooks.SavegameLoadHook += LaunchRecognizer;
            //TEMPORARY NEW GAME HOOK FOR TESTING
            ModHooks.NewGameHook += LaunchRecognizer;
            Log("Initialized");
        }

        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();

        public void LaunchRecognizer()
        {
            dictRecognizer = new DictationRecognizer();
            dictRecognizer.DictationHypothesis += Hypothesis;
            dictRecognizer.DictationResult += Result;
            dictRecognizer.DictationComplete += Completion;
            dictRecognizer.DictationError += Error;
            dictRecognizer.Start();
        }

        public void KillRecognizer()
        {
            if (dictRecognizer != null)
            {
                dictRecognizer.DictationHypothesis -= Hypothesis;
                dictRecognizer.DictationResult -= Result;
                dictRecognizer.DictationComplete -= Completion;
                dictRecognizer.DictationError -= Error;
                if (dictRecognizer.Status == SpeechSystemStatus.Running)
                {
                    dictRecognizer.Stop();
                }
                dictRecognizer.Dispose();
            }
        }

        public void Hypothesis(string text)
        {
            foreach (string key in keywords.Keys)
            {
                if (text.Contains(key))
                {
                    ExecuteAction(key);
                    guessedKeys.Add(key);
                }
            }
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            foreach (string key in keywords.Keys)
            {
                if (text.Contains(key) && !guessedKeys.Contains(key))
                {
                    ExecuteAction(key);
                }
            }
            guessedKeys.Clear();
        }

        public void Completion(DictationCompletionCause cause)
        {
            switch (cause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                case DictationCompletionCause.Canceled:
                case DictationCompletionCause.Complete:
                    // Restart required
                    KillRecognizer();
                    LaunchRecognizer();
                    break;
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    // Error
                    Log("Error killed Dictation Recognizer");
                    KillRecognizer();
                    break;
            }
        }

        public void Error(string error, int hresult)
        {
            Log("Dictation Error: " + error);
        }

        private void ExecuteAction(string key)
        {
            Action kwAction;
            if (keywords.TryGetValue(key, out kwAction))
            {
                kwAction.Invoke();
            }
        }
    }
}
