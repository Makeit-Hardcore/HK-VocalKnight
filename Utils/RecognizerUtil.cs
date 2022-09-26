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
        private MonoBehaviour runner;
        private float timer;
        private const float timerMax = 20f;

		public static ObservableCollection<string> foundCommands = new ObservableCollection<string>();
        private static List<string> foundWords = new List<string>();
        // Dict{ command, (list of keywords) }
        private static Dictionary<string, List<string>> keywords = new Dictionary<string, List<string>>()
        {
            { "reset", new List<string>() {"reset reset reset"} },
            { "spikefloor", new List<string>() {"spike","point"} },
            { "bees", new List<string>() {"bee","bea","hive"} },
            { "lasers", new List<string>() {"laser","peak","peek"} },
            { "radiance", new List<string>() {"orb","sphere","light","lite"} },
            { "cameffect Invert", new List<string>() {"switch","invert"} },
            { "cameffect Flip", new List<string>() {"flip"} },
            { "cameffect Nausea", new List<string>() {"blur","wave","dizzy"} },
            //{ "cameffect Backwards", new List<string>() {"reverse","back"} }, //BREAKS THE GAME
            { "cameffect Mirror", new List<string>() {"mirror","two"} },
            { "cameffect Pixelate", new List<string>() {"old","retro","censor","pixel"} },
            { "cameffect Zoom", new List<string>() {"close","zoom"} },
            { "ax2uBlind", new List<string>() {"dark","blind","daniel"} },
            { "nopogo", new List<string>() {"pogo","down"} },
            { "sleep", new List<string>() {"sleep","tired","drowsy"} },
            { "wind", new List<string>() {"wind","blow","push"} },
            { "timescale 0.5", new List<string>() {"slow"} },
            { "timescale 2", new List<string>() {"fast"} },
            { "weight 0.5", new List<string>() {"moon","space","float"} },
            { "weight 1.9", new List<string>() {"heavy","weight","strong","fat"} },
            { "invertcontrols", new List<string>() {"turn","wrong","damn"} },
            { "slippery", new List<string>() {"slip","wet","water","hydrate"} },
            { "nailscale 0.5", new List<string>() {"small","tiny"} },
            { "bindings", new List<string>() {"bind","pantheon","chain"} },
            { "hwurmpU", new List<string>() {"cursed","ugly","pretty" } },
            { "walkspeed 4", new List<string>() {"run","sprint"} },
            { "walkspeed 0.5", new List<string>() {"walk","jog"} },
            { "geo", new List<string>() {"geo","money","coin"} },
            { "respawn", new List<string>() {"ouch","hurt"} },
            { "bench", new List<string>() {"bench","rest","spawn"} },
            { "die", new List<string>() {"die","dye","death","dead"} },
            { "bounce", new List<string>() {"bounce","shroom","fung"} },
            { "gravup", new List<string>() {"up","gravit","top"} },
            { "disable dash", new List<string>() {"dash"} },
            { "disable superdash", new List<string>() {"heart"} },
            { "disable claw", new List<string>() {"claw","wall","cling" } },
            { "disable wings", new List<string>() {"wing","double"} },
            //{ "toggle tear", new List<string>() {"isma","tear","acid"} }, //NOT WORKING
            { "disable dnail", new List<string>() {"dream"} },
            { "nonail", new List<string>() {"nail","swing"} },
            { "noheal", new List<string>() {"focus","heal"} },
            { "nailonly", new List<string>() {"spell","shaman","shriek","dive"} },
            { "doubledamage", new List<string>() {"damage","fragile","weak","week"} },
            { "zap", new List<string>() {"zap","shock","volt","electric"} },
            { "jars", new List<string>() {"jar","enemy","collect","trap"} },
            { "purevessel", new List<string>() {"night","vessel","pure","white"} },
            { "revek", new List<string>() {"grave","attack","ninja","protect"} },
            //{ "shade", new List<string>() {"shade","ghost","regret"} },
            { "belfly", new List<string>() {"fly","boom","explode","annoy"} },
            { "enemy marmu", new List<string>() {"marm","cat","ball"} },
            //{ "enemy hu", new List<string>() {"who","pancake","flat"} }, //FIX
            { "gorb", new List<string>() {"brain","ascend"} }, //FIX
            //{ "enemy noeyes", new List<string>() {"eye","baby"} }, //FIX
            //{ "enemy galien", new List<string>() {"alien","spin"} }, //FIX
            { "xero", new List<string>() {"0","none","nothing"} },
            //{ "enemy markoth", new List<string>() {"shield","mark"} }, //FIX
            { "enemy bigbee", new List<string>() {"big","large"} },
            { "enemy drillbee", new List<string>() {"drill","screw","sting"} },
            { "enemy crystal", new List<string>() {"shoot","crystal"} },
            { "enemy petra", new List<string>() {"petra","disc","blade"} },
            { "enemy sword", new List<string>() {"sword"} },
            { "enemy javelin", new List<string>() {"throw"} },
            { "enemy roller", new List<string>() {"roll"} },
            { "enemy angrybuzzer", new List<string>() {"angry","mad","furious"} },
            { "enemy mawlek", new List<string>() {"maw","lick"} },
            { "aspidrancher", new List<string>() {"spit","primal","triple","aspid"} },
            //{ "enemy radiance", new List<string>() {"rad","raid","moth","god"} }, //FIX
            { "grimmchild", new List<string>() {"child","kid","grim"} },
            { "hungry", new List<string>() {"hunger","hungry","food"} },
            { "charmcurse", new List<string>() {"charm","salubra","equip"} },
            { "timewarp", new List<string>() {"time","warp","move"} },
            { "setText Potty Mouth ", new List<string>() {"****"} }
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

            StartRecognizer();
        }

        ~RecognizerUtil()
        {
            GameObject.Destroy(go);
        }

        public void StartRecognizer()
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
                {
                    dictRecognizer.Stop();
                }
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

            //Find any commands in the text and log them
            List<string> kws;
            foreach (string command in keywords.Keys)
            {
                try
                {
                    kws = keywords[command].GetRange(0, VocalKnight.GS.potentialKWs);
                } catch //ArgumentOutOfRangeException
                {
                    kws = keywords[command];
                }
                foreach (string keyword in kws)
                    if (text.Contains(keyword))
                    {
                        foundWords.Add(keyword);
                        if (!foundCommands.Contains(command))
                        {
                            foundCommands.Add(command);
                        }
                    }
            }

            //Display the retrieved text with commands highlighted
            string textFormatted = "";
            int lineLen = 0;
            string wordBuff = "";
            foreach (string word in text.Split(' '))
            {
                wordBuff = word;
                foreach (string command in foundWords)
                    if (wordBuff.Contains(command))
                        wordBuff = wordBuff.Insert(word.IndexOf(command) + command.Length, "</color>")
                                           .Insert(word.IndexOf(command), "<color=red>");
                lineLen += word.Length;
                if (lineLen > 80)
                {
                    lineLen = word.Length;
                    textFormatted += "\n" + wordBuff;
                }
                else
                {
                    lineLen += 1;
                    textFormatted += wordBuff + " ";
                }
            }
            VocalKnight._instance.dictText.GetComponent<TextMesh>().text = textFormatted;
            foundWords.Clear();
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            Logger.Log("Result: " + text);
            Hypothesis(text);
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
            StartRecognizer();
        }

        public void Error(string error, int hresult)
        {
            Logger.Log("Dictation Error: " + error);
            KillRecognizer();
            StartRecognizer();
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
            StartRecognizer();

            //Recursively restart this coroutine
            timer = timerMax;
            yield return FreezeTimer();
        }
    }
}

