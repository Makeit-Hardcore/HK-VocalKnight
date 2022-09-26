using System;
using System.Collections.Generic;
using VocalKnight.Utils;

namespace VocalKnight.Settings
{
	public class GSets
	{
		public int potentialKWs = 2;
		public Dictionary<string, bool> commandToggles;
		
		public GSets()
        {
			commandToggles = new Dictionary<string, bool>();
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
	}
}
