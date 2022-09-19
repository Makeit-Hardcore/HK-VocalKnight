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
			//{ "reset", new List<string>() {"reset reset reset"} },
			{ "zap", new List<string>() {"zap","shock","electic"} },
			{ "spikefloor", new List<string>() {"spike","grim","point"} }
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

