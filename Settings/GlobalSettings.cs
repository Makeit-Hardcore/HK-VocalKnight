using System.Linq;
using System.Collections.Generic;
using VocalKnight.Utils;

namespace VocalKnight.Settings
{
	public class GSets
	{
		public int difficulty = 0;
		public bool oneAtATime;
		public int wordMatching;
		public int potentialKWs;
		public Dictionary<string, bool> commandToggles;
		
		public GSets()
        {
			commandToggles = new Dictionary<string, bool>();
			foreach (string command in RecognizerUtil.GetCommands())
            {
				string baseCommand = command.Split(' ')[0];
				if (!commandToggles.ContainsKey(baseCommand))
					commandToggles.Add(baseCommand, true);
            }

			SetDiffPreset();
        }

		public void UpdateToggles()
        {
			List<string> newCommands = new List<string>();
			foreach (string command in RecognizerUtil.GetCommands())
			{
				string baseCommand = command.Split(' ')[0];
				if (!commandToggles.ContainsKey(baseCommand))
					commandToggles.Add(baseCommand, true);
				if (!newCommands.Contains(baseCommand))
					newCommands.Add(baseCommand);
			}
			foreach (string oldCommand in commandToggles.Keys)
				if (!newCommands.Contains(oldCommand))
					commandToggles.Remove(oldCommand);
		}

		public void ToCustom()
        {
			difficulty = 3;
        }

		public void SetDiffPreset()
        {
			string[] kws = {};
			switch (difficulty)
            {
				case 0: //ATTUNED (EASY)
					oneAtATime = true;
					wordMatching = 0;
					potentialKWs = 1;
					kws = RecognizerUtil.GetCommandsEasy();
					break;
				case 1: //ASCENDED (MEDIUM)
					oneAtATime = false;
					wordMatching = 1;
					potentialKWs = 2;
					kws = RecognizerUtil.GetCommandsEasy().Concat(RecognizerUtil.GetCommandsMed()).ToArray();
					break;
				case 2: //RADIANT (HARD)
					oneAtATime = false;
					wordMatching = 2;
					potentialKWs = 4;
					kws = commandToggles.Keys.ToArray();
					break;
				case 3: //CUSTOM
					break;
				default:
					Logger.LogError("Difficulty set to an unexpected value: " + difficulty);
					break;
            }

			List<string> commands = commandToggles.Keys.ToList();
			foreach (string kw in commands)
            {
				if (kws.Contains(kw)) commandToggles[kw] = true;
				else commandToggles[kw] = false;
            }
        }
	}
}
