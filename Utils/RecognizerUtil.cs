using System;
using System.Linq;
using System.Globalization;
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
        public static event EventHandler<EventArgs> BeforeUpdateKeyWord;

        private static int runcount = 0;

        /* Dict{ command, (list of keywords) }
           Three preset difficulties: 1 being easiest, 3 being hardest
           Order DOES matter, as there is priority usage when OneAtATime is enabled */
        private static Dictionary<string, List<string>> keywords_1 = new Dictionary<string, List<string>>()
        {
            { "reset", new List<string>() {"neutralize","reset","undo","forget"} },
            { "grub", new List<string>() {"grub","mimic","god","academy"} },
            { "wind", new List<string>() {"push","blow","wind","shove"} },
            { "invertcontrols", new List<string>() {"turn","wrong","direct","control"} },
            { "bounce", new List<string>() {"bounce","shroom","fungus","balloon"} },
            { "cameffect Pixelate", new List<string>() {"old","retro","censor","pixel"} },
            { "sleep", new List<string>() {"tired","sleep","drowsy","bed"} },
            { "slippery", new List<string>() {"wet","slip","water","hydrate"} },
            { "sheo purple", new List<string>() {"purple","violet","lavender","royal"} },
            { "sheo blue", new List<string>() {"blue","cyan","indigo","deep"} },
            { "geo", new List<string>() {"geo","money","coin","dollar"} },
            { "disable claw", new List<string>() {"wall","claw","cling","magnet"} },
            { "disable wings", new List<string>() {"wing","double","hop","leap"} },
            { "disable dnail", new List<string>() {"dream","fantasy","guardian","thought"} },
            { "disable superdash", new List<string>() {"heart","zip","horizon","cardio"} },
            { "grimmchild", new List<string>() {"child","kid","follow","chase"} },
            { "enemy crystal", new List<string>() {"shoot","crystal","trigger","gun"} },
            { "enemy petra", new List<string>() {"disc","blade","petra","frisbee"} },
            { "enemy roller", new List<string>() {"roll","bald","shell","pole"} },
            { "cameffect Invert", new List<string>() {"switch","invert","color","negative"} },
            { "walkspeed 0.5", new List<string>() {"walk","jog","stroll","crawl"} },
            { "timescale 0.5", new List<string>() {"slow","wait","slug","delay"} },
            { "hwurmpU", new List<string>() {"pretty","curse","ugly","gross"} }
        };
        private static Dictionary<string, List<string>> keywords_2 = new Dictionary<string, List<string>>()
        {
            { "party", new List<string>() {"party","dab","hard","core"} },
            { "bees", new List<string>() {"bee","beat","hive","queen"} },
            { "lasers", new List<string>() {"peak","peek","laser","mountain"} },
            { "timewarp", new List<string>() {"time","move","warp","teleport"} },
            { "aspidrancher", new List<string>() {"spit","primal","triple","aspid"} },
            { "sheo red", new List<string>() {"red","pink","brick","crimson"} },
            { "sheo yellow", new List<string>() {"yellow","gold","dandelion","banana"} },
            { "enemy drillbee", new List<string>() {"sting","screw","drill","spiral"} },
            { "spikefloor", new List<string>() {"point","spike","jagged","spire"} },
            { "disable dash", new List<string>() {"dash","cloak","veil","cape"} },
            { "radiance", new List<string>() {"light","sphere","orb","moth"} },
            { "cameffect Nausea", new List<string>() {"wave","dizzy","blur","spin"} },
            { "cameffect Zoom", new List<string>() {"close","zoom","personal","intimate"} },
            { "cameffect Flip", new List<string>() {"flip","over","roof","ceiling"} },
            { "cameffect Mirror", new List<string>() {"two","mirror","left","right"} },
            { "nailscale 0.5", new List<string>() {"small","tiny","mini","compensate"} },
            { "zap", new List<string>() {"shock","electric","volt","zap"} },
            { "jars", new List<string>() {"trap","jar","collect","enemy"} },
            { "enemy angrybuzzer", new List<string>() {"angry","mad","furious","infect"} },
            { "walkspeed 2.5", new List<string>() {"run","sprint","race","dart"} },
            { "nopogo", new List<string>() {"pogo","down","stick","trick"} }
        };
        private static Dictionary<string, List<string>> keywords_3 = new Dictionary<string, List<string>>()
        {
            { "jelly", new List<string>() {"jelly","fog","spill","orange"} },
            { "revek", new List<string>() {"grave","attack","ghost","protect"} },
            { "ax2uBlind", new List<string>() {"dark","blind","axe","black"} },
            { "timescale 1.5", new List<string>() {"fast","speed","quick","swift"} },
            { "bindings", new List<string>() {"bind","pantheon","chain","hold"} },
            { "belfly", new List<string>() {"fly","explode","annoy","boom"} },
            { "marmu", new List<string>() {"march","ball","cat","spring"} },
            { "gorb", new List<string>() {"brain","ascend","rise","elevate"} },
            { "xero", new List<string>() {"zero","none","nothing","traitor"} },
            { "respawn", new List<string>() {"hurt","ouch","redo","fail"} },
            { "nonail", new List<string>() {"nail","swing","strike","sword"} },
            { "noheal", new List<string>() {"focus","heal","care","help"} },
            { "nailonly", new List<string>() {"spell","shaman","shriek","dive"} },
            { "doubledamage", new List<string>() {"damage","fragile","weak","week"} },
            { "purevessel", new List<string>() {"night","white","pure","vessel"} },
            { "nightmare", new List<string>() {"grim","fire","bat","flame"} },
            { "hungry", new List<string>() {"hungry","food","hunger","starve"} },
            { "charmcurse", new List<string>() {"charm","equip","notch","power"} },
            { "enemy bigbee", new List<string>() {"big","large","unit","strong"} },
            { "enemy kingsmould", new List<string>() {"guard","mold","king","mould"} },
            { "gravup", new List<string>() {"top","gravity","reverse","up"} },
            { "bench", new List<string>() {"bench","rest","spawn","home"} },
            { "die", new List<string>() {"die","dead","death","dye"} }
        };
        private static Dictionary<string, ValueTuple<Dictionary<string, List<string>>, Dictionary<string, List<string>>, Dictionary<string, List<string>>>> langdict = new()
        {
            {
                "en",
                new ValueTuple<Dictionary<string, List<string>>, Dictionary<string, List<string>>, Dictionary<string, List<string>>>(keywords_1,keywords_2,keywords_3)
            }
        };
        public static void AddLangDict(string lang, ValueTuple<Dictionary<string, List<string>>, Dictionary<string, List<string>>, Dictionary<string, List<string>>> mylangdict)
        {
            if(mylangdict.Item1==null)
            {
                mylangdict.Item1 = keywords_1;
            }
            if (mylangdict.Item2 == null)
            {
                mylangdict.Item2 = keywords_2;
            }
            if (mylangdict.Item3 == null)
            {
                mylangdict.Item3 = keywords_3;
            }
            langdict[lang] = mylangdict;
        }
        private static Dictionary<string, List<string>> keywords_all;

		public RecognizerUtil()
		{
            go = new GameObject();
            GameObject.DontDestroyOnLoad(go);
            runner = go.AddComponent<NonBouncer>();

            timer = timerMax;
            statusUpdate += new StatusUpdateHandler(TimerReset);
            runner.StartCoroutine(FreezeTimer());
            UpdateKeywords_All();
            SetTextVars();
            KeyIndexerUtil.WriteToFile();
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

            if (dictRecognizer.Status == SpeechSystemStatus.Running)
                KillRecognizer();
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

        private const int confirmBuffer = 4;
        private int searchIndex;
        private string line;
        private List<string> choppedLine = new List<string>();
        private Queue<string[]> inputBuffer = new Queue<string[]>();

        private void SetTextVars()
        {
            searchIndex = 0;
            line = " ";
            choppedLine.Clear();
            choppedLine.Add(" ");
            inputBuffer.Clear();
        }

        public void Hypothesis(string text)
        {
            if (statusUpdate != null)
                statusUpdate(this, new EventArgs());

            Logger.Log("HYP: " + text);

            int countNewWords = 0;
            string[] splitText = text.Split(' ');
            if (inputBuffer.Count() == confirmBuffer - 1)
            {
                while (searchIndex < inputBuffer.First().Count())
                {
                    bool allMatch = true;
                    foreach (string[] storedText in inputBuffer)
                    {
                        if (storedText[searchIndex] != splitText[searchIndex])
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch)
                    {
                        line += splitText[searchIndex];
                        if (line.Length > 80)
                        {
                            if (countNewWords != 0) break;
                            line = " " + splitText[searchIndex];
                            choppedLine.Clear();
                            choppedLine.Add(" ");
                        }
                        line += " ";
                        choppedLine[choppedLine.Count() - 1] += splitText[searchIndex] + " ";
                        searchIndex++;
                        countNewWords++;
                    } else
                    {
                        break;
                    }
                }
                inputBuffer.Enqueue(splitText);
                inputBuffer.Dequeue();
            }
            else if (inputBuffer.Count() > confirmBuffer - 1)
            {
                Logger.LogError("Input buffer queue got too big!");
                return;
            }
            else
            {
                inputBuffer.Enqueue(splitText);
            }

            if (inputBuffer.Count() == confirmBuffer - 1)
                ProcessText();

            string dispLine = line;
            string fText = String.Join("", choppedLine.ToArray());
            for (int n = searchIndex; n < text.Split(' ').Length; n++)
            {
                dispLine += splitText[n];
                if (dispLine.Length > 80)
                {
                    break;
                } else
                {
                    fText += splitText[n];
                }
                fText += " ";
                dispLine += " ";
            }

            Logger.Log("Display text: " + fText);
            VocalKnight.Instance.dictText.GetComponent<TextMesh>().text = fText;
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            bool resetFlag = true;

            Logger.Log("RES: " + text);

            int countNewWords = 0;
            string[] splitText = text.Split(' ');
            while (searchIndex < splitText.Count())
            {
                line += splitText[searchIndex];
                if (line.Length > 80)
                {
                    if (countNewWords != 0)
                    {
                        runner.StartCoroutine(RecallResult(text, confidence));
                        resetFlag = false;
                        break;
                    }
                    line = " " + splitText[searchIndex];
                    choppedLine.Clear();
                    choppedLine.Add(" ");
                }
                line += " ";
                choppedLine[choppedLine.Count() - 1] += splitText[searchIndex] + " ";
                searchIndex++;
                countNewWords++;
            }

            ProcessText();
            Logger.Log("Display text: " + String.Join("", choppedLine.ToArray()));
            VocalKnight.Instance.dictText.GetComponent<TextMesh>().text = String.Join("", choppedLine.ToArray());

            if (resetFlag) SetTextVars();
        }

        private IEnumerator RecallResult(string text, ConfidenceLevel confidence)
        {
            yield return null;
            Result(text, confidence);
        }

        private void ProcessText()
        {
            foreach (string command in keywords_all.Keys)
            {
                List<string> keywords;
                try
                {
                    keywords = keywords_all[command].GetRange(0, VocalKnight.GS.potentialKWs);
                }
                catch (ArgumentException e)
                {
                    keywords = keywords_all[command];
                }
                foreach (string kw in keywords)
                {
                    int chunkCount = choppedLine.Count;
                    int i = -1;
                    while (++i < chunkCount)
                    {
                        string chunk = choppedLine[i];
                        if (chunk.Contains("color="))
                            continue;
                        switch (VocalKnight.GS.wordMatching)
                        {
                            case 0: //ATTUNED (EASY) - Exact
                                if (chunk.Contains(" " + kw + " "))
                                    HypothesisHelper(ref chunkCount, chunk, chunk.IndexOf(kw), kw.Length, i, command, kw);
                                break;
                            case 1: //ASCENDED (MEDIUM) - Contains
                                if (chunk.Contains(kw))
                                    HypothesisHelper(ref chunkCount, chunk, chunk.IndexOf(kw), kw.Length, i, command, kw);
                                break;
                            case 2: //RADIANT (HARD) - Contains, ignore spaces
                                int kwIndex;
                                if (ComplexContains(chunk, kw, out kwIndex))
                                {
                                    string splitKW = "";
                                    int diff = 0, j = 0;
                                    while (j < kw.Length + diff)
                                    {
                                        splitKW += chunk[kwIndex + j];
                                        if (chunk[kwIndex + j] != kw[j - diff]) diff++;
                                        j++;
                                    }
                                    HypothesisHelper(ref chunkCount, chunk, kwIndex, j, i, command, splitKW);
                                }
                                break;
                            default:
                                Logger.LogError("Word Matching set to unknown value: " + VocalKnight.GS.wordMatching);
                                return;
                        }
                        foundCommands.Clear();
                    }
                }
            }
        }

        private static bool ComplexContains(string source, string sub, out int index)
        {
            index = CultureInfo.InvariantCulture.CompareInfo.IndexOf(source, sub, CompareOptions.IgnoreSymbols);
            return index != -1;
        }

        private void HypothesisHelper(ref int chunkCount, string chunk, int kwIndex, int kwLen, int i, string command, string kw)
        {
            if (CommandProcessor.onUniversalCooldown || CommandProcessor.OnCooldown(command))
            {
                choppedLine[i] = "<color=cyan>" + kw + "</color>";
            }
            else
            {
                choppedLine[i] = "<color=red>" + kw + "</color>";
                foundCommands.Add(command);
            }
            if (kwIndex != chunk.Length - kwLen)
            {
                choppedLine.Insert(i + 1, chunk.Substring(kwIndex + kwLen));
                chunkCount++;
            }
            if (kwIndex != 0)
            {
                choppedLine.Insert(i, chunk.Substring(0, kwIndex));
                chunkCount++;
            }
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
            string[] commands = GetCommandsEasy().Concat(GetCommandsMed()).Concat(GetCommandsHard()).ToArray();
            Array.Sort(commands, StringComparer.InvariantCulture);
            return commands;
        }

        public static string[] GetCommandsEasy()
        {
            string[] commands = new string[keywords_1.Count];
            keywords_1.Keys.CopyTo(commands, 0);
            return commands;
        }

        public static string[] GetCommandsMed()
        {
            string[] commands = new string[keywords_2.Count];
            keywords_2.Keys.CopyTo(commands, 0);
            return commands;
        }

        public static string[] GetCommandsHard()
        {
            string[] commands = new string[keywords_3.Count];
            keywords_3.Keys.CopyTo(commands, 0);
            return commands;
        }

        public static List<string> GetKeywords(string command)
        {
            if (keywords_1.ContainsKey(command)) return keywords_1[command];
            else if (keywords_2.ContainsKey(command)) return keywords_2[command];
            else if (keywords_3.ContainsKey(command)) return keywords_3[command];
            Logger.Log("GetKeywords could not access command " + command);
            return null;
        }

        public static void UpdateKeywords_All()
        {
            BeforeUpdateKeyWord?.Invoke(VocalKnight.Instance, new EventArgs()); 
            var currlang = Language.Language.CurrentLanguage().ToString().ToLower();
            var currkeyword = langdict.TryGetValue(currlang, out var keywordvalue) ? keywordvalue : langdict["en"];
            var k1 = currkeyword.Item1;
            var k2 = currkeyword.Item2;
            var k3 = currkeyword.Item3;
            keywords_all = k3.Concat(k2).Concat(k1)
                                     .ToLookup(x => x.Key, x => x.Value)
                                     .ToDictionary(x => x.Key, g => g.First());

            List<string> kwKeys = keywords_all.Keys.ToList();
            foreach (string command in kwKeys)
            {
                string baseCommand = command.Split(' ')[0];
                if (!VocalKnight.GS.commandToggles.ContainsKey(baseCommand))
                {
                    Logger.LogError("UpdateKeywords_All could not find base command " + baseCommand + " in commandToggles");
                    keywords_all.Remove(command);
                    continue;
                }
                // Command is disabled in settings, do not let it pass to the recognizer engine
                if (!VocalKnight.GS.commandToggles[baseCommand])
                    keywords_all.Remove(command);
            }
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

