using Modding;
using System;
using UnityEngine;
using UObject = UnityEngine.Object;
using UnityEngine.Windows.Speech;
using System.Collections;
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
        private DictationRecognizer dictRecognizer;
        private Dictionary<string, Action> keywords = new Dictionary<string, Action>();
        private List<string> guessedKeys = new List<string>();
        private MonoBehaviour _coroutineRunner;
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

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public VocalKnight() : base()
        {
            _instance = this;
        }

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

            var go = new GameObject();
            UObject.DontDestroyOnLoad(go);
            _coroutineRunner = go.AddComponent<NonBouncer>();

            System.Random rand = new System.Random();

            ConfigureCooldowns();

            keywords.Add("reset reset reset", () => { CoroutineUtil.cancel = true; });

            keywords.Add("zap", () => { _coroutineRunner.StartCoroutine(Enemies.StartZapping()); });
            keywords.Add("shock", () => { _coroutineRunner.StartCoroutine(Enemies.StartZapping()); });
            keywords.Add("jar", () => { _coroutineRunner.StartCoroutine(Enemies.Jars()); });
            keywords.Add("attack", () => { _coroutineRunner.StartCoroutine(Enemies.Revek()); });
            keywords.Add("ghost", () => { _coroutineRunner.StartCoroutine(Enemies.Revek()); });

            keywords.Add("night", () => { Enemies.SpawnPureVessel(); });
            keywords.Add("shade", () => { Enemies.SpawnShade(); });
            keywords.Add("bug", () => { Enemies.SpawnEnemy("buzzer"); });
            keywords.Add("roll", () => { Enemies.SpawnEnemy("roller"); });
            keywords.Add("spit", () => { Enemies.SpawnEnemy("aspid"); });
            keywords.Add("who", () => { Enemies.SpawnEnemy("hu"); });
            keywords.Add("eye", () => { Enemies.SpawnEnemy("noeyes"); });
            keywords.Add("0", () => { Enemies.SpawnEnemy("xero"); });
            keywords.Add("nothing", () => { Enemies.SpawnEnemy("xero"); });
            keywords.Add("none", () => { Enemies.SpawnEnemy("xero"); });
            keywords.Add("nun", () => { Enemies.SpawnEnemy("xero"); });
            keywords.Add("mark", () => { Enemies.SpawnEnemy("markoth"); });
            keywords.Add("shield", () => { Enemies.SpawnEnemy("markoth"); });
            keywords.Add("ascend", () => { Enemies.SpawnEnemy("gorb"); });
            keywords.Add("rise", () => { Enemies.SpawnEnemy("gorb"); });
            keywords.Add("brain", () => { Enemies.SpawnEnemy("gorb"); });
            keywords.Add("gorb", () => { Enemies.SpawnEnemy("gorb"); });
            keywords.Add("ball", () => { Enemies.SpawnEnemy("marmu"); });
            keywords.Add("moo", () => { Enemies.SpawnEnemy("marmu"); });
            keywords.Add("alien", () => { Enemies.SpawnEnemy("galien"); });
            keywords.Add("fight", () => { Enemies.SpawnEnemy("galien"); });
            keywords.Add("spin", () => { Enemies.SpawnEnemy("galien"); });
            keywords.Add("maw", () => { Enemies.SpawnEnemy("mawlek"); });
            keywords.Add("lick", () => { Enemies.SpawnEnemy("mawlek"); });
            keywords.Add("sword", () => { Enemies.SpawnEnemy("sword"); });
            keywords.Add("throw", () => { Enemies.SpawnEnemy("javelin"); });
            keywords.Add("disc", () => { Enemies.SpawnEnemy("petra"); });
            keywords.Add("blade", () => { Enemies.SpawnEnemy("petra"); });
            keywords.Add("annoy", () => { Enemies.SpawnEnemy("belfly"); });
            keywords.Add("fly", () => { Enemies.SpawnEnemy("belfly"); });
            keywords.Add("boom", () => { Enemies.SpawnEnemy("belfly"); });
            keywords.Add("explode", () => { Enemies.SpawnEnemy("belfly"); });
            keywords.Add("crystal", () => { Enemies.SpawnEnemy("crystal"); });
            keywords.Add("shoot", () => { Enemies.SpawnEnemy("crystal"); });
            keywords.Add("rad", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("raid", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("boss", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("final", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("moth", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("god", () => { Enemies.SpawnEnemy("radiance"); });
            keywords.Add("big", () => { Enemies.SpawnEnemy("bigbee"); });
            keywords.Add("scary", () => { Enemies.SpawnEnemy("bigbee"); });
            keywords.Add("sting", () => { Enemies.SpawnEnemy("drillbee"); });
            keywords.Add("drill", () => { Enemies.SpawnEnemy("drillbee"); });
            keywords.Add("screw", () => { Enemies.SpawnEnemy("drillbee"); });
            keywords.Add("angry", () => { Enemies.SpawnEnemy("angrybuzzer"); });
            keywords.Add("mad", () => { Enemies.SpawnEnemy("angrybuzzer"); });

            keywords.Add("flip", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Flip")); });
            keywords.Add("switch", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Invert")); });
            keywords.Add("drunk", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Nausea")); });
            keywords.Add("blur", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Nausea")); });
            keywords.Add("reverse", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Backwards")); });
            keywords.Add("back", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Backwards")); });
            keywords.Add("two", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Mirror")); });
            keywords.Add("old", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Pixelate")); });
            keywords.Add("close", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Zoom")); });
            keywords.Add("zoom", () => { _coroutineRunner.StartCoroutine(Commands.Camera.AddEffect("Zoom")); });

            keywords.Add("dark", () => { _coroutineRunner.StartCoroutine(Player.Blind()); });
            keywords.Add("blind", () => { _coroutineRunner.StartCoroutine(Player.Blind()); });
            keywords.Add("dinilb", () => { _coroutineRunner.StartCoroutine(Player.Blind()); });
            keywords.Add("pogo", () => { _coroutineRunner.StartCoroutine(Player.PogoKnockback()); });
            keywords.Add("bounce", () => { _coroutineRunner.StartCoroutine(Player.PogoKnockback()); });
            keywords.Add("move", () => { _coroutineRunner.StartCoroutine(Player.Conveyor()); });
            keywords.Add("sleep", () => { _coroutineRunner.StartCoroutine(Player.Sleep()); });
            //keywords.Add("soul", () => { _coroutineRunner.StartCoroutine(Player.LimitSoul()); });
            keywords.Add("wind", () => { _coroutineRunner.StartCoroutine(Player.Wind()); });
            keywords.Add("blow", () => { _coroutineRunner.StartCoroutine(Player.Wind()); });
            keywords.Add("time", () => { _coroutineRunner.StartCoroutine(Player.ChangeTimescale((float)rand.NextDouble() * 1.75f + 0.25f)); });
            keywords.Add("slow", () => { _coroutineRunner.StartCoroutine(Player.ChangeTimescale(0.25f)); });
            keywords.Add("fast", () => { _coroutineRunner.StartCoroutine(Player.ChangeTimescale(2f)); });
            keywords.Add("heavy", () => { _coroutineRunner.StartCoroutine(Player.ChangeGravity(1.9f)); });
            keywords.Add("moon", () => { _coroutineRunner.StartCoroutine(Player.ChangeGravity(0.2f)); });
            keywords.Add("turn", () => { _coroutineRunner.StartCoroutine(Player.InvertControls()); });
            keywords.Add("slip", () => { _coroutineRunner.StartCoroutine(Player.Slippery()); });
            keywords.Add("wet", () => { _coroutineRunner.StartCoroutine(Player.Slippery()); });
            keywords.Add("water", () => { _coroutineRunner.StartCoroutine(Player.Slippery()); });
            keywords.Add("hydrate", () => { _coroutineRunner.StartCoroutine(Player.Slippery()); });
            keywords.Add("limit", () => { _coroutineRunner.StartCoroutine(Player.EnableBindings()); });
            keywords.Add("bind", () => { _coroutineRunner.StartCoroutine(Player.EnableBindings()); });
            keywords.Add("restrict", () => { _coroutineRunner.StartCoroutine(Player.EnableBindings()); });
            keywords.Add("small", () => { _coroutineRunner.StartCoroutine(Player.NailScale(0.3f)); });
            //keywords.Add("run", () => { _coroutineRunner.StartCoroutine(Player.WalkSpeed(5f)); });
            //keywords.Add("speed", () => { _coroutineRunner.StartCoroutine(Player.WalkSpeed(10f)); });
            //keywords.Add("walk", () => { _coroutineRunner.StartCoroutine(Player.WalkSpeed(0.3f)); });
            keywords.Add("ouch", () => { Player.HazardRespawn(); });
            keywords.Add("hurt", () => { Player.HazardRespawn(); });
            keywords.Add("die", () => { }); //ADD
            keywords.Add("death", () => { });
            keywords.Add("respawn", () => { });
            keywords.Add("bench", () => { });
            keywords.Add("dash", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("dash")); });
            keywords.Add("heart", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("superdash")); });
            keywords.Add("claw", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("claw")); });
            keywords.Add("wall", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("claw")); });
            keywords.Add("wing", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("wings")); });
            keywords.Add("jump", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("wings")); });
            keywords.Add("acid", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("tear")); });
            keywords.Add("isma", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("tear")); });
            keywords.Add("tear", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("tear")); });
            keywords.Add("swim", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("")); }); //ADD
            keywords.Add("dream", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("dnail")); });
            keywords.Add("nail", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("nail")); });
            keywords.Add("swing", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("nail")); });
            keywords.Add("slash", () => { _coroutineRunner.StartCoroutine(Player.ToggleAbility("nail")); });
            keywords.Add("geo", () => { Player.Geo(); });
            keywords.Add("coin", () => { Player.Geo(); });
            keywords.Add("money", () => { Player.Geo(); });
            keywords.Add("maggot", () => { _coroutineRunner.StartCoroutine(Player.EnableMaggotPrimeSkin()); });
            keywords.Add("baby", () => { _coroutineRunner.StartCoroutine(Player.EnableMaggotPrimeSkin()); });

            keywords.Add("bee", () => { _coroutineRunner.StartCoroutine(Area.Bees()); });
            keywords.Add("bea", () => { _coroutineRunner.StartCoroutine(Area.Bees()); });
            keywords.Add("laser", () => { Area.Lasers(); });
            keywords.Add("peak", () => { Area.Lasers(); });
            keywords.Add("spike", () => { _coroutineRunner.StartCoroutine(Area.SpikeFloor()); });
            keywords.Add("grim", () => { _coroutineRunner.StartCoroutine(Area.SpikeFloor()); });
            keywords.Add("point", () => { _coroutineRunner.StartCoroutine(Area.SpikeFloor()); });
            keywords.Add("orb", () => { _coroutineRunner.StartCoroutine(Area.SpawnAbsOrb()); });
            keywords.Add("sphere", () => { _coroutineRunner.StartCoroutine(Area.SpawnAbsOrb()); });

            //ModHooks.SavegameLoadHook += LaunchRecognizer;
            //TEMPORARY NEW GAME HOOK FOR TESTING
            ModHooks.NewGameHook += LaunchRecognizer;
            ModHooks.SavegameLoadHook += LaunchRecognizer;
            On.QuitToMenu.Start += KillRec_Standin;

            Log("Initialized");
        }

        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();

        public void LaunchRecognizer(int id)
        {
            dictRecognizer = new DictationRecognizer();
            dictRecognizer.DictationHypothesis += Hypothesis;
            dictRecognizer.DictationResult += Result;
            dictRecognizer.DictationComplete += Completion;
            dictRecognizer.DictationError += Error;
            dictRecognizer.Start();
        }

        public void LaunchRecognizer()
        {
            dictRecognizer = new DictationRecognizer();
            dictRecognizer.DictationHypothesis += Hypothesis;
            dictRecognizer.DictationResult += Result;
            dictRecognizer.DictationComplete += Completion;
            dictRecognizer.DictationError += Error;
            dictRecognizer.Start();
        }

        protected IEnumerator KillRec_Standin(On.QuitToMenu.orig_Start orig, QuitToMenu self)
        {
            Log("Returning to Menu, killing Recognizer");
            KillRecognizer();
            _coroutineRunner.StopAllCoroutines();
            yield return null;
            orig(self);
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
            }
        }

        public void Hypothesis(string text)
        {
            foreach (string key in keywords.Keys)
            {
                if (text.Contains(key))
                {
                    ExecuteAction(key);
                    guessedKeys.Add(key);
                }
            }
        }

        public void Result(string text, ConfidenceLevel confidence)
        {
            foreach (string key in keywords.Keys)
            {
                if (text.Contains(key) && !guessedKeys.Contains(key))
                {
                    ExecuteAction(key);
                }
            }
            guessedKeys.Clear();
        }

        public void Completion(DictationCompletionCause cause)
        {
            switch (cause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                case DictationCompletionCause.Canceled:
                case DictationCompletionCause.Complete:
                    // Restart required
                    KillRecognizer();
                    LaunchRecognizer();
                    break;
                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    // Error
                    Logger.LogError("Error killed Dictation Recognizer");
                    KillRecognizer();
                    break;
            }
        }

        public void Error(string error, int hresult)
        {
            Log("Dictation Error: " + error);
        }

        private void ExecuteAction(string key)
        {
            Action kwAction;
            if (keywords.TryGetValue(key, out kwAction))
            {
                kwAction.Invoke();
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
