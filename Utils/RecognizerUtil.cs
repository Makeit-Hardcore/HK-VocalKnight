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

        private static int runcount = 0;

		public RecognizerUtil()
		{
            go = new GameObject();
            GameObject.DontDestroyOnLoad(go);
            runner = go.AddComponent<NonBouncer>();

            timer = timerMax;
            statusUpdate += new StatusUpdateHandler(TimerReset);
            runner.StartCoroutine(FreezeTimer());

            SetTextVars();
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
            foreach (string command in KeywordUtil.GetActiveCommands())
            {
                List<string> keywords;
                keywords = KeywordUtil.GetKeywords(command).GetRange(0, VocalKnight.GS.potentialKWs);
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
                    Logger.LogError("Unknown error killed Dictation Recognizer");
                    break;
                case DictationCompletionCause.AudioQualityFailure:
                    Logger.LogError("Audio quality error killed Dictation Recognizer");
                    break;
                case DictationCompletionCause.MicrophoneUnavailable:
                    Logger.LogError("Microphone unavailable error killed Dictation Recognizer");
                    break;
                case DictationCompletionCause.NetworkFailure:
                    Logger.LogError("Network error killed Dictation Recognizer");
                    break;
                default:
                    Logger.LogError("Error killed Dictation Recognizer");
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

            yield return ForceReset();

            //Recursively restart this coroutine
            timer = timerMax;
            yield return FreezeTimer();
        }

        public IEnumerator ForceReset()
        {
            //Force a threaded Destruct of the recognizer with the GC, since Dispose() will cause freezing/crash
            ForceDestroy();
            yield return null;

            //Allow the recognizer to restart normally
            NewRecognizer();
        }
    }



}

