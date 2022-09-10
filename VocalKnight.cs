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
        //Keyword recognition looks more for clear commands rather than accidental slips of the tongue
        //Try Dictation recognition instead: convert speech to text, then scan text for keywords
        private KeywordRecognizer kwRecognizer;
        private Dictionary<string, Action> keywords = new Dictionary<string, Action>();

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
            kwRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());
            kwRecognizer.OnPhraseRecognized += ExecuteAction;
            ModHooks.SavegameLoadHook += LaunchRecognizer;
            Log("Initialized");
        }

        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();

        public void LaunchRecognizer(int id)
        {
            kwRecognizer.Start();
        }

        private void ExecuteAction(PhraseRecognizedEventArgs args)
        {
            Action kwAction;
            if (keywords.TryGetValue(args.text, out kwAction))
            {
                kwAction.Invoke();
            }
        }
    }
}
