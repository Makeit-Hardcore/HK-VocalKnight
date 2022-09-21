using Modding;
using System;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEngine.Windows.Speech;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using VocalKnight.Commands;
using VocalKnight.Entities;
using VocalKnight.Precondition;
using VocalKnight.Utils;

namespace VocalKnight
{
    public class VocalKnight : Mod
    {
        private RecognizerUtil recognizer;
        public Dictionary<string, int> Cooldowns = new();
        internal CommandProcessor Processor { get; private set; }

        private static VocalKnight? _instance;

        public static VocalKnight Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(VocalKnight)} was never initialized");
                }
                return _instance;
            }
        }

        public override string GetVersion() => "v0.5.0";

        public VocalKnight() : base()
        {
            _instance = this;
        }

        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            ObjectLoader.Load(preloadedObjects);
            ObjectLoader.LoadAssets();

            Processor = new CommandProcessor();
            Processor.RegisterCommands<Player>();
            Processor.RegisterCommands<Enemies>();
            Processor.RegisterCommands<Area>();
            Processor.RegisterCommands<Commands.Camera>();
            Processor.RegisterCommands<Game>();

            ConfigureCooldowns();

            NewRecognizer();
            RecognizerUtil.foundCommands.CollectionChanged += ExecuteCommands;

            Log("Initialized");
        }

        public void NewRecognizer()
        {
            if (recognizer != null) DeleteRecognizer();

            recognizer = new RecognizerUtil();
            recognizer.StartRecognizer();
        }

        protected void DeleteRecognizer()
        {
            recognizer.KillRecognizer();
            recognizer = null;
        }

        private void ExecuteCommands(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.NewItems != null)
            {
                foreach (string command in args.NewItems)
                {
                    Logger.Log("Executing command: " + command);
                    Processor.Execute(command, null);
                }
            }
        }

        private void ConfigureCooldowns()
        {
            // No cooldowns configured, let's populate the dictionary.
            if (Cooldowns.Count == 0)
            {
                foreach (Command c in Processor.Commands)
                {
                    CooldownAttribute cd = c.Preconditions.OfType<CooldownAttribute>().FirstOrDefault();
                    if (cd == null)
                        continue;

                    Cooldowns[c.Name] = (int)cd.Cooldown.TotalSeconds;
                }
                return;
            }

            foreach (Command c in Processor.Commands)
            {
                if (!Cooldowns.TryGetValue(c.Name, out int time))
                    continue;
                CooldownAttribute cd = c.Preconditions.OfType<CooldownAttribute>().First();
                cd.Cooldown = TimeSpan.FromSeconds(time);
            }
        }
    }
}
