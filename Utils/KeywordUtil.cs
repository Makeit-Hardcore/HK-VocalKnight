using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Cryptography;
using VocalKnight.Components;
using VocalKnight.Entities;

namespace VocalKnight.Utils
{
    public static class KeywordUtil
    {
        /// <summary>
        /// Signal for Randomizer to communicate w/ menu screen GENERATE button
        /// RandomizeKeywords() sets true if randomization assignment was a success
        /// VocalKnight.cs "GenRand" checks for rando result and sets false afterward for new rando
        /// </summary>
        public static bool genSuccess = false;

        /// <summary>
        /// Official command list source + default keywords
        /// </summary>
        private static Dictionary<string, List<string>>[] _defaults = {
            // ATTUNED
            new Dictionary<string, List<string>>() {
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
            // ASCENDED
            }, new Dictionary<string, List<string>>() {
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
            // RADIANT
            }, new Dictionary<string, List<string>>() {
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
            }
        };

        /// <summary>
        /// Active commands and associated keywords
        /// </summary>
        private static Dictionary<string, List<string>> _active = new Dictionary<string, List<string>>();

        public static string[] GetActiveCommands()
        {
            return _active.Keys.ToArray();
        }

        /// <summary>
        /// Returns the list of all commands given a preset difficulty
        /// Custom difficulty returns the entire list
        /// </summary>
        /// <param name="difficulty"></param>
        /// <returns></returns>
        public static string[] GetCommands(int difficulty)
        {
            List<string> _commands = new List<string>();
            switch (difficulty)
            {
                case 0: // ATTUNED
                    _commands.AddRange(_defaults[0].Keys);
                    break;
                case 1: // ASCENDED
                    _commands.AddRange(_defaults[1].Keys);
                    goto case 0;
                case 2: // RADIANT
                    _commands.AddRange(_defaults[2].Keys);
                    goto case 1;
                case 3: // CUSTOM
                    goto case 2;
                default:
                    Logger.LogError("Requested difficulty is not [0,3]: " + difficulty);
                    return new string[0];
            }
            string[] commands = _commands.ToArray();
            Array.Sort(commands, StringComparer.InvariantCulture);
            return commands;
        }

        /// <summary>
        /// Returns the assigned keywords for a given command, regardless of if the command is active
        /// 
        /// USED BY: RecognizerUtil.cs, KeyIndexerUtil.cs
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static List<string> GetKeywords(string query)
        {
            Dictionary<string, List<string>>[] kwSrc = (VocalKnight.GS.kwSet != 1) ? _defaults : VocalKnight.GS.customKws;

            for (int i = 0; i < kwSrc.Length; i++)
            {
                foreach (string cmd in kwSrc[i].Keys)
                {
                    if (cmd != query) continue;
                    return kwSrc[i][cmd];
                }
            }
            
            Logger.Log("GetKeywords could not find command " + query);
            return null;
        }

        /// <summary>
        /// Loads commands & keywords into active dictionary according to current settings
        /// 
        /// USED BY: VocalKnight.cs
        /// </summary>
        public static void UpdateKeywords_All()
        {
            _active = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>>[] kwSrc = (VocalKnight.GS.kwSet != 1) ? _defaults : VocalKnight.GS.customKws;

            foreach (string base_cmd in VocalKnight.GS.commandToggles.Keys)
            {
                if (!VocalKnight.GS.commandToggles[base_cmd]) continue;
                for (int i = 0; i < kwSrc.Length; i++)
                {
                    foreach (string cmd in kwSrc[i].Keys)
                    {
                        if (cmd.Split(' ')[0] != base_cmd) continue;
                        _active.Add(cmd, new List<string>(kwSrc[i][cmd]));
                    }
                }
            }
        }

        /// <summary>
        /// Called by VocalKnight.cs menu script to generate a randomized keyword set
        ///  & save it to memory
        ///  
        /// Google Doc index creation is handled by the menu script
        /// </summary>
        public static void RandomizeKeywords()
        {
            string[] kw_buff = null;
            if (!WordBase.getWords(4*GetCommands(2).Length, out kw_buff)) return;
            Queue<string> kw_queue = new Queue<string>(kw_buff);

            VocalKnight.GS.customKws = new Dictionary<string, List<string>>[2];
            for (int i=0; i<2; i++)
            {
                foreach (string cmd in VocalKnight.GS.customKws[i].Keys)
                {
                    VocalKnight.GS.customKws[i].Add(cmd, new List<string>());
                    for (int n=0; n<4; n++)
                    {
                        VocalKnight.GS.customKws[i][cmd].Add(kw_queue.Dequeue());
                    }
                }
            }

            genSuccess = true;
        }
    }
}
