using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace VocalKnight.Utils
{
	public class RecognizerUtil
	{
		private DictationRecognizer dictRecognizer;

        private delegate void StatusUpdateHandler(object sender, EventArgs e);
        private event StatusUpdateHandler statusUpdate;

        public GameObject go;
        public MonoBehaviour runner;
        private float timer;
        private const float timerMax = 20f;

        public static ObservableCollection<string> foundCommands = new ObservableCollection<string>();
        
        public static bool commandOnCooldown = false;
        private static int runcount = 0;

        /* Dict{ command, (list of keywords) }
           Three preset difficulties: 1 being easiest, 3 being hardest
           Order DOES matter, as there is priority usage when OneAtATime is enabled */
        private static Dictionary<string, List<string>> keywords_1 = new Dictionary<string, List<string>>()
        {
            { "reset", new List<string>() {"neutralize"} },
            { "cameffect Invert", new List<string>() {"switch","invert"} },
            { "cameffect Pixelate", new List<string>() {"old","retro","censor","pixel"} },
            { "sleep", new List<string>() {"tired","sleep","drowsy"} },
            { "wind", new List<string>() {"push","blow","wind"} },
            { "invertcontrols", new List<string>() {"1","turn","wrong"} },
            { "slippery", new List<string>() {"1","wet","slip","water","hydrate"} },
            { "timescale 0.5", new List<string>() {"1","slow"} },
            { "hwurmpU", new List<string>() {"1","pretty","curse","ugly" } },
            { "walkspeed 0.5", new List<string>() {"1","walk","jog"} },
            { "sheo purple", new List<string>() {"1","purple","violet","lavender","royal"} },
            { "sheo blue", new List<string>() {"1","blue","cyan","indigo","deep"} },
            { "geo", new List<string>() {"1","geo","money","coin"} },
            { "bounce", new List<string>() {"1","bounc","shroom","fung"} },
            { "grub", new List<string>() {"1","grub","mimic","god","academy"} },
            { "disable superdash", new List<string>() {"1","heart"} },
            { "disable claw", new List<string>() {"1","wall","claw","cling"} },
            { "disable wings", new List<string>() {"1","wing","double"} },
            { "disable dnail", new List<string>() {"1","dream"} },
            { "setText Potty Mouth ", new List<string>() {"1","***","damn"} },
            { "enemy crystal", new List<string>() {"1","shoot","crystal"} },
            { "enemy petra", new List<string>() {"1","disc","blade","petra"} },
            { "enemy roller", new List<string>() {"1","roll"} },
            { "grimmchild", new List<string>() {"1","child","kid","follow","chase"} }
        };
        private static Dictionary<string, List<string>> keywords_2 = new Dictionary<string, List<string>>()
        {
            { "enemy angrybuzzer", new List<string>() {"2","angry","mad","furious"} },
            { "timewarp", new List<string>() {"2","time","move","warp"} },
            { "aspidrancher", new List<string>() {"2","spit","primal","triple","aspid"} },
            { "walkspeed 2.5", new List<string>() {"2","run","sprint"} },
            { "enemy drillbee", new List<string>() {"2","sting","screw","drill"} },
            { "spikefloor", new List<string>() {"2","point","spike"} },
            { "party", new List<string>() {"2","party","dab","hard","core"} },
            { "bees", new List<string>() {"2", "bee","bea","hive"} },
            { "lasers", new List<string>() {"2","peak","peek","laser"} },
            { "disable dash", new List<string>() {"2","dash"} },
            { "radiance", new List<string>() {"2","light","sphere","orb","lite"} },
            { "cameffect Flip", new List<string>() {"2","flip"} },
            { "cameffect Nausea", new List<string>() {"2","wave","dizzy","blur"} },
            { "cameffect Mirror", new List<string>() {"2","two","mirror"} },
            { "cameffect Zoom", new List<string>() {"2","close","zoom"} },
            { "nopogo", new List<string>() {"2","pogo","down"} },
            { "nailscale 0.5", new List<string>() {"2","small","tiny"} },
            { "zap", new List<string>() {"2","shock","electric","volt","zap"} },
            { "jars", new List<string>() {"2","trap","jar","collect","enemy"} },
            { "sheo red", new List<string>() {"2","red","pink","brick","crimson"} },
            { "sheo yellow", new List<string>() {"2","yellow","gold","dandelion","banana"} }
        };
        private static Dictionary<string, List<string>> keywords_3 = new Dictionary<string, List<string>>()
        {
            { "ax2uBlind", new List<string>() {"3","dark","blind","daniel"} },
            { "timescale 1.5", new List<string>() {"3","fast"} },
            { "bindings", new List<string>() {"3","bind","pantheon","chain"} },
            { "respawn", new List<string>() {"3","hurt","ouch"} },
            { "bench", new List<string>() {"3","bench","rest","spawn"} },
            { "die", new List<string>() {"3","die","dead","death","dye"} },
            { "gravup", new List<string>() {"3","up","top","gravit"} },
            { "nonail", new List<string>() {"3","nail","swing"} },
            { "noheal", new List<string>() {"3","focus","heal"} },
            { "nailonly", new List<string>() {"3","spell","shaman","shriek","dive"} },
            { "doubledamage", new List<string>() {"3","damage","fragile","weak","week"} },
            { "purevessel", new List<string>() {"3","night","white","pure","vessel"} },
            { "revek", new List<string>() {"3","grave","attack","ghost","protect"} },
            { "belfly", new List<string>() {"3","fly","explod","annoy","boom"} },
            { "marmu", new List<string>() {"3","mar","ball","cat"} },
            //{ "enemy hu", new List<string>() {"who","flat","pancake"} }, //FIX
            { "gorb", new List<string>() {"3","brain","ascend","rise"} }, //FIX
            //{ "enemy noeyes", new List<string>() {"eye","baby"} }, //FIX
            //{ "enemy galien", new List<string>() {"spin","alien"} }, //FIX
            { "xero", new List<string>() {"3","zero","none","nothing"} },
            //{ "enemy markoth", new List<string>() {"shield","mark"} }, //FIX
            { "nightmare", new List<string>() {"3","grim","fire","bat","flame"} },
            { "enemy bigbee", new List<string>() {"3","big","large","unit"} },
            { "enemy kingsmould", new List<string>() {"3","king","guard","mold","mould"} },
            //{ "enemy radiance", new List<string>() {"god","rad","raid","moth"} }, //FIX
            { "hungry", new List<string>() {"3","hungry","food","hunger","starv"} },
            { "charmcurse", new List<string>() {"3","charm","equip","notch"} },
            { "jelly", new List<string>() {"3","jelly","fog","spill","orange"} }
        };

		public RecognizerUtil()
		{
            go = new GameObject();
            GameObject.DontDestroyOnLoad(go);
            runner = go.AddComponent<NonBouncer>();

            timer = timerMax;
            statusUpdate += new StatusUpdateHandler(TimerReset);
            runner.StartCoroutine(FreezeTimer());

            NewRecognizer();
        }

        ~RecognizerUtil()
        {
            GameObject.Destroy(go);
        }

        public void NewRecognizer()
        {
            Logger.Log("Starting new DR " + ++runcount);
            dictRecognizer = new DictationRecognizer();

            dictRecognizer.DictationHypothesis += Hypothesis;
            dictRecognizer.DictationResult += Result;
            dictRecognizer.DictationComplete += Completion;
            dictRecognizer.DictationError += Error;

            //So far changing these variables causes frequent crashing/freezing
            //dictRecognizer.AutoSilenceTimeoutSeconds = 10f;
            //dictRecognizer.InitialSilenceTimeoutSeconds = 20f;

            dictRecognizer.Start();
        }

        public void KillRecognizer()
        {
            if (dictRecognizer != null)
            {
                if (dictRecognizer.Status == SpeechSystemStatus.Running)
                    dictRecognizer.Stop();
                dictRecognizer.DictationHypothesis -= Hypothesis;
                dictRecognizer.DictationResult -= Result;
                dictRecognizer.DictationComplete -= Completion;
                dictRecognizer.DictationError -= Error;
                dictRecognizer.Dispose();
                dictRecognizer = null;
                Logger.Log("Disposed of DR " + runcount);
            }
            else Logger.LogWarn("Tried to kill Recognizer, but found NULL");
        }

        public void Hypothesis(string text)
        {
            if (statusUpdate != null)
                statusUpdate(this, new EventArgs());

            VocalKnight.Instance.dictText.GetComponent<TextMesh>().text = text;
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            Logger.Log("Result: " + text);

            string textFormatted = "";
            int lineLen = 0;
            List<string> kws;
            foreach (string word in text.Split(' '))
            {
                string wordBuff = "";
                if (HeroController.instance != null && HeroController.instance.CanInput())
                {
                    foreach (string command in keywords.Keys)
                    {
                        if (foundCommands.Contains(command)) continue;

                        bool found = false;

                        //Get our list of potential keywords
                        try
                        {
                            kws = keywords[command].GetRange(0, VocalKnight.GS.potentialKWs);
                        }
                        catch //ArgumentOutOfRangeException
                        {
                            kws = keywords[command];
                        }

                        //Try to match those keywords to the current word
                        foreach (string keyword in kws)
                        {
                            if (word.Contains(keyword))
                            {
                                found = true;
                                Logger.Log("Executing command: " + command);
                                VocalKnight.Instance.Processor.Execute(command, null);

                                string splitA = "";
                                string splitB = "";

                                int lenA = word.IndexOf(keyword);
                                if (lenA > 0)
                                    splitA = word.Substring(0, word.IndexOf(keyword));

                                int lenB = word.Length - (word.IndexOf(keyword) + keyword.Length);
                                if (lenB > 0)
                                    splitB = word.Substring(word.IndexOf(keyword) + keyword.Length, lenB);

                                if (commandOnCooldown)
                                    wordBuff = splitA + "<color=cyan>" + keyword + "</color>" + splitB;
                                else
                                    wordBuff = splitA + "<color=red>" + keyword + "</color>" + splitB;

                                commandOnCooldown = false;
                                break;
                            }
                        }

                        if (found)
                        {
                            foundCommands.Add(command);
                            break;
                        }
                    }
                }

                if (wordBuff == "") wordBuff = word;

                lineLen += word.Length;
                if (lineLen > 80)
                {
                    lineLen = word.Length;
                    textFormatted += "\n";
                }
                lineLen += 1;
                textFormatted += wordBuff + " ";
            }

            VocalKnight.Instance.dictText.GetComponent<TextMesh>().text = textFormatted;
            foundCommands.Clear();
        }

        public void Completion(DictationCompletionCause cause)
        {
            if (statusUpdate != null)
                statusUpdate(this, new EventArgs());

            switch (cause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                case DictationCompletionCause.Canceled:
                case DictationCompletionCause.Complete:
                    // Nothing really wrong, just a restart required
                    Logger.Log("Recognizer completed without error");
                    break;

                //TODO: Add visual feedback for the player for errors listed
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                default:
                    Logger.LogError("Error killed Dictation Recognizer");
                    Logger.Log("Attempting to restart recognizer...");
                    break;
            }
            KillRecognizer();
            NewRecognizer();
        }

        public void Error(string error, int hresult)
        {
            Logger.Log("Dictation Error: " + error);
            KillRecognizer();
            NewRecognizer();
        }

        public static string[] GetCommands()
        {
            string[] commands = new string[keywords_1.Count + keywords_2.Count + keywords_3.Count];
            keywords_1.Keys.CopyTo(commands, 0);
            keywords_2.Keys.CopyTo(commands, keywords_1.Count);
            keywords_3.Keys.CopyTo(commands, keywords_1.Count + keywords_2.Count);
            return commands;
        }

        private void TimerReset(object source, EventArgs e)
        {
            timer = timerMax;
        }

        public void ForceDestroy()
        {
            dictRecognizer = null;
            GC.Collect();
        }

        private IEnumerator FreezeTimer()
        {
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            Logger.LogWarn("Rec timer ran up! Forcefully restarting");

            //Force a threaded Destruct of the recognizer with the GC, since Dispose() will cause freezing/crash
            ForceDestroy();
            yield return null;

            //Allow the recognizer to restart normally
            NewRecognizer();

            //Recursively restart this coroutine
            timer = timerMax;
            yield return FreezeTimer();
        }
    }
}

