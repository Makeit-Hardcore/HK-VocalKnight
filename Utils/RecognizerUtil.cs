using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine.Windows.Speech;

namespace VocalKnight.Utils
{
	public class RecognizerUtil
	{
		private DictationRecognizer dictRecognizer;

		public static ObservableCollection<string> foundCommands = new ObservableCollection<string>();
		// Dict{ command, (list of keywords) }
		private static Dictionary<string, List<string>> keywords = new Dictionary<string, List<string>>()
        {
			{ "reset", new List<string>() {"reset reset reset"} },
			{ "spikefloor", new List<string>() {"spike","grim","point"} },
            { "bees", new List<string>() {"bee","bea","hive"} },
            { "lasers", new List<string>() {"laser","peak","peek"} },
            { "orb", new List<string>() {"orb","sphere","light","lite"} },
            { "cameffect Invert", new List<string>() {"switch","invert"} },
            { "cameffect Flip", new List<string>() {"flip"} },
            { "cameffect Nausea", new List<string>() {"blur","wave","dizzy"} },
            //{ "cameffect Backwards", new List<string>() {"reverse","back"} }, //BREAKS THE GAME
            { "cameffect Mirror", new List<string>() {"mirror","two"} },
            { "cameffect Pixelate", new List<string>() {"old","retro","censor","pixel"} },
            { "cameffect Zoom", new List<string>() {"close","zoom"} },
            { "ax2uBlind", new List<string>() {"dark","blind","dinilb"} }, //figure out how it reads "blind" backwards
            { "nopogo", new List<string>() {"pogo","down"} },
            { "sleep", new List<string>() {"sleep","tired","drowsy"} },
            { "limitSoul", new List<string>() {"soul","limit"} }, //FIX
            { "wind", new List<string>() {"wind","blow","push"} },
            { "timescale 0.5", new List<string>() {"slow"} },
            { "timescale 2", new List<string>() {"fast","time"} },
            { "gravity 0.5", new List<string>() {"moon","space","float"} },
            { "gravity 1.9", new List<string>() {"heavy","weight","strong","fat"} },
            { "invertcontrols", new List<string>() {"turn","wrong","damn"} },
            { "slippery", new List<string>() {"slip","wet","water","hydrate"} },
            { "nailscale 0.5", new List<string>() {"small","tiny"} },
            { "bindings", new List<string>() {"bind","pantheon","chain"} },
            { "hwurmpU", new List<string>() {"cursed","ugly","pretty" } },
            { "walkspeed 4", new List<string>() {"run","sprint"} },
            { "walkspeed 0.5", new List<string>() {"walk","jog"} },
            { "geo", new List<string>() {"geo","money","coin"} }, //What does the rec thing "geo" sounds like in a sentence?
            { "respawn", new List<string>() {"ouch","hurt"} },
            { "bench", new List<string>() {"bench","rest","spawn"} }, //IMPLEMENT
            { "die", new List<string>() {"die","death","kill","dead"} }, //IMPLEMENT
            { "toggle dash", new List<string>() {"dash"} },
            { "toggle superdash", new List<string>() {"sea dash"} }, //VERIFY KEYWORD
            { "toggle claw", new List<string>() {"claw","wall","cling" } },
            { "toggle wings", new List<string>() {"wing","double"} },
            { "toggle tear", new List<string>() {"isma","tear","acid"} },
            { "toggle dnail", new List<string>() {"dream"} },
            { "toggle nail", new List<string>() {"nail","swing"} },
            { "doubledamage", new List<string>() {"damage","fragile","weak","week"} },
            { "zap", new List<string>() {"zap","shock"} },
            { "jars", new List<string>() {"jar","enemy","collect","trap"} }, //add more potential enemies
            { "purevessel", new List<string>() {"night","vessel","pure","white"} },
            { "revek", new List<string>() {"grave","attack","ninja","protect"} },
            { "shade", new List<string>() {"shade","ghost","regret"} },
            { "belfly", new List<string>() {"fly","boom","explode","annoy"} },
            { "enemy marmu", new List<string>() {"marm","cat","ball"} },
            { "enemy hu", new List<string>() {"who","pancake","flat"} }, //FIX
            { "enemy gorb", new List<string>() {"brain","ascend"} }, //FIX
            { "enemy noeyes", new List<string>() {"eye","baby"} }, //FIX
            { "enemy galien", new List<string>() {"alien","spin"} }, //FIX
            { "enemy xero", new List<string>() {"0","none","nothing"} }, //FIX
            { "enemy markoth", new List<string>() {"shield","mark"} }, //FIX
            { "enemy bigbee", new List<string>() {"big","large"} },
            { "enemy drillbee", new List<string>() {"drill","screw","sting"} },
            { "enemy crystal", new List<string>() {"shoot","crystal"} },
            { "enemy petra", new List<string>() {"petra","disc","blade"} },
            { "enemy sword", new List<string>() {"sword"} },
            { "enemy javelin", new List<string>() {"throw"} },
            { "enemy angrybuzzer", new List<string>() {"angry","mad","furious"} },
            { "enemy mawlek", new List<string>() {"maw","lick"} },
            { "enemy aspid", new List<string>() {"spit","primal","triple"} },
            { "enemy radiance", new List<string>() {"rad","raid","moth","god"} } //FIX
        };

		public RecognizerUtil()
		{
        }

        public void StartRecognizer()
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
                if (dictRecognizer.Status == SpeechSystemStatus.Running)
                    dictRecognizer.Stop();
                dictRecognizer.DictationHypothesis -= Hypothesis;
                dictRecognizer.DictationResult -= Result;
                dictRecognizer.DictationComplete -= Completion;
                dictRecognizer.DictationError -= Error;
                dictRecognizer.Dispose();
                dictRecognizer = null;
            }
            else Logger.LogWarn("Tried to kill Recognizer, but found NULL");
        }

        public void Hypothesis(string text)
        {
            foreach (string command in keywords.Keys)
                foreach (string keyword in keywords[command])
                    if (!foundCommands.Contains(command) && text.Contains(keyword))
                        foundCommands.Add(command);
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            Hypothesis(text);
            foundCommands.Clear();
        }

        public void Completion(DictationCompletionCause cause)
        {
            switch (cause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                case DictationCompletionCause.Canceled:
                case DictationCompletionCause.Complete:
                    // Nothing really wrong, just a restart required
                    KillRecognizer();
                    StartRecognizer();
                    break;

                //TODO: Add visual feedback for the player for errors listed
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    Logger.LogError("Error killed Dictation Recognizer");
                    KillRecognizer();
                    Logger.Log("Attempting to restart recognizer...");
                    StartRecognizer();
                    break;
            }
        }

        public void Error(string error, int hresult)
        {
            Logger.Log("Dictation Error: " + error);
        }
    }
}

