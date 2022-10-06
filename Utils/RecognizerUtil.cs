using Modding;
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
        private static List<string> foundCommands = new List<string>();
        public static bool commandOnCooldown = false;
        
        // Dict{ command, (list of keywords) }
        private static Dictionary<string, List<string>> keywords = new Dictionary<string, List<string>>()
        {
            { "reset", new List<string>() {"neutralize"} },
            { "spikefloor", new List<string>() {"point","spike"} },
            { "bees", new List<string>() {"bee","bea","hive"} },
            { "lasers", new List<string>() {"peak","peek","laser"} },
            { "radiance", new List<string>() {"light","sphere","orb","lite"} },
            { "cameffect Invert", new List<string>() {"switch","invert"} },
            { "cameffect Flip", new List<string>() {"flip"} },
            { "cameffect Nausea", new List<string>() {"wave","dizzy","blur"} },
            { "cameffect Mirror", new List<string>() {"two","mirror","2"} },
            { "cameffect Pixelate", new List<string>() {"old","retro","censor","pixel"} },
            { "cameffect Zoom", new List<string>() {"close","zoom"} },
            { "ax2uBlind", new List<string>() {"dark","blind","daniel"} },
            { "nopogo", new List<string>() {"pogo","down"} },
            { "sleep", new List<string>() {"tired","sleep","drowsy"} },
            { "wind", new List<string>() {"push","blow","wind"} },
            { "timescale 0.5", new List<string>() {"slow"} },
            { "timescale 2", new List<string>() {"fast"} },
            { "weight 0.5", new List<string>() {"space","moon","float"} },
            { "weight 1.9", new List<string>() {"heavy","weigh","strong","fat"} },
            { "invertcontrols", new List<string>() {"turn","wrong"} },
            { "slippery", new List<string>() {"wet","slip","water","hydrate"} },
            { "nailscale 0.5", new List<string>() {"small","tiny"} },
            { "bindings", new List<string>() {"bind","pantheon","chain"} },
            { "hwurmpU", new List<string>() {"pretty","curse","ugly" } },
            { "walkspeed 4", new List<string>() {"run","sprint"} },
            { "walkspeed 0.5", new List<string>() {"walk","jog"} },
            { "geo", new List<string>() {"geo","money","coin"} },
            { "respawn", new List<string>() {"hurt","ouch"} },
            { "bench", new List<string>() {"bench","rest","spawn"} },
            { "die", new List<string>() {"die","dead","death","dye"} },
            { "bounce", new List<string>() {"bounc","shroom","fung"} },
            { "gravup", new List<string>() {"up","top","gravit"} },
            { "disable dash", new List<string>() {"dash"} },
            { "disable superdash", new List<string>() {"heart"} },
            { "disable claw", new List<string>() {"wall","claw","cling"} },
            { "disable wings", new List<string>() {"wing","double"} },
            { "disable dnail", new List<string>() {"dream"} },
            { "nonail", new List<string>() {"nail","swing"} },
            { "noheal", new List<string>() {"focus","heal"} },
            { "nailonly", new List<string>() {"spell","shaman","shriek","dive"} },
            { "doubledamage", new List<string>() {"damage","fragile","weak","week"} },
            { "zap", new List<string>() {"shock","electric","volt","zap"} },
            { "jars", new List<string>() {"trap","jar","collect","enemy"} },
            { "purevessel", new List<string>() {"night","white","pure","vessel"} },
            { "revek", new List<string>() {"grave","attack","ghost","protect"} },
            { "belfly", new List<string>() {"fly","explod","annoy","boom"} },
            { "enemy marmu", new List<string>() {"mar","ball","cat"} },
            //{ "enemy hu", new List<string>() {"who","flat","pancake"} }, //FIX
            { "gorb", new List<string>() {"brain","ascend","rise"} }, //FIX
            //{ "enemy noeyes", new List<string>() {"eye","baby"} }, //FIX
            //{ "enemy galien", new List<string>() {"spin","alien"} }, //FIX
            { "xero", new List<string>() {"0","zero","none","nothing"} },
            //{ "enemy markoth", new List<string>() {"shield","mark"} }, //FIX
            { "sheo red", new List<string>() {"red","pink","brick","crimson"} },
            { "sheo purple", new List<string>() {"purple","violet","lavendar","royal"} },
            { "sheo blue", new List<string>() {"blue","cyan","indigo","deep"} },
            { "sheo yellow", new List<string>() {"yellow","gold","dandelion","banana"} },
            { "nightmare", new List<string>() {"grim","fire","bat","flame"} },
            { "enemy bigbee", new List<string>() {"big","large","unit"} },
            { "enemy drillbee", new List<string>() {"sting","screw","drill"} },
            { "enemy crystal", new List<string>() {"shoot","crystal"} },
            { "enemy petra", new List<string>() {"disc","blade","petra"} },
            { "enemy kingsmould", new List<string>() {"king","guard","mold","mould"} },
            { "enemy roller", new List<string>() {"roll"} },
            { "enemy angrybuzzer", new List<string>() {"angry","mad","furious"} },
            { "enemy mawlek", new List<string>() {"maw","lick"} },
            { "aspidrancher", new List<string>() {"spit","primal","triple","aspid"} },
            //{ "enemy radiance", new List<string>() {"god","rad","raid","moth"} }, //FIX
            { "grimmchild", new List<string>() {"child","kid","follow","chase"} },
            { "hungry", new List<string>() {"hungry","food","hunger","starv"} },
            { "charmcurse", new List<string>() {"charm","equip","notch"} },
            { "timewarp", new List<string>() {"time","move","warp"} },
            { "setText Potty Mouth ", new List<string>() {"***","damn"} },
            { "jelly", new List<string>() {"jelly","fog","spill","orange"} }, //IMPLEMENT
            { "party", new List<string>() {"party","dab","hard","core"} } //IMPLEMENT
        };
        private static int runcount = 0;

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
            string[] commands = new string[keywords.Count];
            keywords.Keys.CopyTo(commands, 0);
            return commands;
        }

        private void TimerReset(object source, EventArgs e)
        {
            timer = timerMax;
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
            dictRecognizer = null;
            GC.Collect();
            yield return null;

            //Allow the recognizer to restart normally
            NewRecognizer();

            //Recursively restart this coroutine
            timer = timerMax;
            yield return FreezeTimer();
        }
    }
}

