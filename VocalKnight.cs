using Modding;
using System;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Satchel.BetterMenus;
using VocalKnight.Commands;
using VocalKnight.Entities;
using VocalKnight.Entities.Attributes;
using VocalKnight.Extensions;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using VocalKnight.Settings;

namespace VocalKnight
{
    public class VocalKnight : Mod, IGlobalSettings<GSets>, ICustomMenuMod, ITogglableMod
    {
        private RecognizerUtil recognizer;
        public GameObject dictText;
        private GameObject kp;
        private static bool preloadFlag = false;
        public Dictionary<string, int> Cooldowns = new();
        internal CommandProcessor Processor { get; private set; }
        public static AssetBundle EmotesBundle;
        public static AssetBundle HueShiftBundle;
        public static Dictionary<string, AudioClip> customAudio = new Dictionary<string, AudioClip>();
        private Menu MenuRef, ToggleMenuRef;
        public static GSets GS { get; set; } = new GSets();

        public bool ToggleButtonInsideMenu => true;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? modToggleDelegates)
        {
            if (MenuRef == null)
            {
                MenuRef = new Menu("VocalKnight", new Element[]
                {
                    Blueprints.CreateToggle(
                        modToggleDelegates.Value,
                        "Mod Enabled",
                        ""),
                    new HorizontalOption(
                        "Difficulty",
                        "Adjusts the settings below to defined presets",
                        new string[] {"ATTUNED","ASCENDED","RADIANT","CUSTOM"},
                        (setting) =>
                        {
                            GS.difficulty = setting;
                            GS.SetDiffPreset();

                            MenuRef.Find("One At A Time").Update();
                            MenuRef.Find("Word Matching").Update();
                            MenuRef.Find("Maximum Potential Keywords").Update();

                            RecognizerUtil.UpdateKeywords_All();
                        },
                        () => GS.difficulty),
                    new HorizontalOption(
                        "One At A Time",
                        "Only one effect can be active at any given time",
                        new string[] {"OFF", "ON"},
                        (setting) =>
                        {
                            GS.oneAtATime = Convert.ToBoolean(setting);
                            GS.difficulty = 3;
                            MenuRef.Find("Difficulty").Update();
                        },
                        () => Convert.ToInt16(GS.oneAtATime)),
                    new HorizontalOption(
                        "Word Matching",
                        "How deeply will the mod search for keywords?",
                        new string[] {"LET'S <color=red>GO</color>","THE <color=red>GO</color>AT","WRON<color=red>G O</color>RDER"},
                        (setting) =>
                        {
                            GS.wordMatching = setting;
                            GS.difficulty = 3;
                            MenuRef.Find("Difficulty").Update();
                        },
                        () => GS.wordMatching),
                    new HorizontalOption(
                        "Maximum Potential Keywords",
                        "The max number of potential keywords that can set off an effect",
                        new string[] {"1","2","3","4"},
                        (setting) =>
                        {
                            GS.potentialKWs = setting + 1;
                            GS.difficulty = 3;
                            MenuRef.Find("Difficulty").Update();
                        },
                        () => GS.potentialKWs - 1),
                    Blueprints.NavigateToMenu(
                        "Toggle Individual Effects",
                        "",
                        () => ToggleMenuRef.GetMenuScreen(MenuRef.menuScreen))
                });
            }
            if (ToggleMenuRef == null)
            {
                ToggleMenuRef = new Menu("VocalKnight Effects", new Element[]
                { 
                    new MenuRow(
                        new List<Element>()
                        {
                            new MenuButton(
                                "TURN ALL ON",
                                "",
                                (_) =>
                                {
                                    List<string> keys = GS.commandToggles.Keys.ToList();
                                    foreach (string key in keys)
                                    {
                                        GS.commandToggles[key] = true;
                                        ToggleMenuRef.Find(key).Update();
                                    }
                                    GS.difficulty = 3;

                                    RecognizerUtil.UpdateKeywords_All();
                                }),
                            new MenuButton(
                                "TURN ALL OFF",
                                "",
                                (_) =>
                                {
                                    List<string> keys = GS.commandToggles.Keys.ToList();
                                    foreach (string key in keys)
                                    {
                                        GS.commandToggles[key] = false;
                                        ToggleMenuRef.Find(key).Update();
                                    }
                                    GS.difficulty = 3;

                                    RecognizerUtil.UpdateKeywords_All();
                                })
                        },
                        "Enable Disable")
                });
                foreach (string command in GS.commandToggles.Keys)
                {
                    string summary = "";
                    try {
                        summary = ((SummaryAttribute[])CommandProcessor.Commands.Find(x => x.Name.Contains(command))
                                    .MethodInfo.GetCustomAttributes(typeof(SummaryAttribute), true))[0].Summary;
                    } catch { }
                    
                    ToggleMenuRef.AddElement(new HorizontalOption(
                        command,
                        summary,
                        new string[] { "OFF", "ON" },
                        (setting) =>
                        {
                            GS.commandToggles[command] = Convert.ToBoolean(setting);
                            GS.difficulty = 3;
                            MenuRef.Find("Difficulty").Update();

                            RecognizerUtil.UpdateKeywords_All();
                        },
                        () => Convert.ToInt16(GS.commandToggles[command])
                    ));
                }
            }
            return MenuRef.GetMenuScreen(modListMenu);
        }

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

        public override string GetVersion() => "0.9.1";

        public VocalKnight() : base()
        {
            _instance = this;
        }

        public override List<(string, string)> GetPreloadNames() => ObjectLoader.ObjectList.Values.ToList();

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            if (!preloadFlag)
            {
                ObjectLoader.Load(preloadedObjects);
                ObjectLoader.LoadAssets();

                Processor = new CommandProcessor();
                Processor.RegisterCommands<Player>();
                Processor.RegisterCommands<Enemies>();
                Processor.RegisterCommands<Area>();
                Processor.RegisterCommands<Commands.Camera>();
                Processor.RegisterCommands<Game>();

                ConfigureCooldowns();
                LoadAssets();
                HueShifter.LoadAssets();

                preloadFlag = true;
            }

            //Initialize captions GameObject
            dictText = UObject.Instantiate(GameCameras.instance.gameObject.transform.
                               Find("HudCamera/Hud Canvas/Geo Counter/Geo Text").gameObject);
            UObject.DontDestroyOnLoad(dictText);
            dictText.name = "dictationTextDisplay";
            dictText.transform.SetPosition3D(-13.5f, -8f, 0.21f);
            UObject.Destroy(dictText.GetComponent<PlayMakerFSM>());
            dictText.GetComponent<TextMesh>().fontSize = 35;
            dictText.GetComponent<TextMesh>().text = "";
            dictText.SetActive(true);

            ModHooks.AfterPlayerDeadHook += CancelEffects;
            On.HeroController.Awake += OnHeroControllerAwake;
            RecognizerUtil.foundCommands.CollectionChanged += ExecuteCommands;

            recognizer = new RecognizerUtil();

            /*kp = new GameObject();
            kp.name = "KeyPressDetector";
            UObject.DontDestroyOnLoad(kp);
            kp.AddComponent<KeyPress>();
            */
            Log("Initialized");
        }

        private void CancelEffects()
        {
            CoroutineUtil.cancel = true;
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
                foreach (Command c in CommandProcessor.Commands)
                {
                    CooldownAttribute cd = c.Preconditions.OfType<CooldownAttribute>().FirstOrDefault();
                    if (cd == null)
                        continue;

                    Cooldowns[c.Name] = (int)cd.Cooldown.TotalSeconds;
                }
                return;
            }

            foreach (Command c in CommandProcessor.Commands)
            {
                if (!Cooldowns.TryGetValue(c.Name, out int time))
                    continue;
                CooldownAttribute cd = c.Preconditions.OfType<CooldownAttribute>().First();
                cd.Cooldown = TimeSpan.FromSeconds(time);
            }
        }

        private void LoadAssets()
        {
            Dictionary<string, AssetBundle> _emoteBundles = new Dictionary<string, AssetBundle>();
            Dictionary<string, AssetBundle> _hueBundles = new Dictionary<string, AssetBundle>();

            var platform = Application.platform switch
            {
                RuntimePlatform.LinuxPlayer => "linux",
                RuntimePlatform.WindowsPlayer => "windows",
                RuntimePlatform.OSXPlayer => "osx",
                _ => throw new PlatformNotSupportedException("What platform are you even on??")
            };

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            foreach (string text in executingAssembly.GetManifestResourceNames())
            {
                using (Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(text))
                {
                    bool flag = manifestResourceStream != null;
                    if (flag)
                    {
                        string key = Path.GetExtension(text).Substring(1);
                        if (key.Contains("emotes"))
                            _emoteBundles[key] = AssetBundle.LoadFromStream(manifestResourceStream);
                        else if (key == "wav")
                        {
                            AudioClip audio = WavUtility.ToAudioClip(manifestResourceStream.ReadAllBytes(), text.Split('.')[2]);
                            customAudio.Add(audio.name, audio);
                        }
                        else if (key == "json")
                        {
                            KeyIndexerUtil.GetCredentials(manifestResourceStream).Wait();
                        }
                        Logger.Log("Loaded new resource: " + text);
                    }
                }
            }

            HueShiftBundle = AssetBundle.LoadFromStream(
                typeof(HueShifter).Assembly.GetManifestResourceStream(
                $"VocalKnight.Resources.AssetBundles.hueshiftshaders-{platform}"));

            switch (SystemInfo.operatingSystemFamily)
            {
                case (OperatingSystemFamily)1:
                    EmotesBundle = _emoteBundles["emotesmac"];
                    break;
                case (OperatingSystemFamily)2:
                    EmotesBundle = _emoteBundles["emoteswin"];
                    break;
                case (OperatingSystemFamily)3:
                    EmotesBundle = _emoteBundles["emoteslin"];
                    break;
            }
        }

        private void OnHeroControllerAwake(On.HeroController.orig_Awake orig, HeroController self)
        {
            orig.Invoke(self);
            self.gameObject.AddComponent<Emoter>();
            RecognizerUtil.UpdateKeywords_All();
        }

        public void Unload()
        {
            UObject.Destroy(kp);
            UObject.Destroy(dictText);

            if (HeroController.instance != null)
            {
                Emoter emoter;
                HeroController.instance.TryGetComponent(out emoter);
                UObject.Destroy(emoter);
            }

            recognizer.ForceDestroy();
            recognizer.runner.StopAllCoroutines();
            recognizer = null;
            GC.Collect();

            ModHooks.AfterPlayerDeadHook -= CancelEffects;
            On.HeroController.Awake -= OnHeroControllerAwake;
            RecognizerUtil.foundCommands.CollectionChanged -= ExecuteCommands;
        }

        public void OnLoadGlobal(GSets s)
        {
            GS = s;
            GS.UpdateToggles();
        }

        public GSets OnSaveGlobal() => GS;
    }

    public class KeyPress : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                Logger.Log("HeroController acceptingInput: " + HeroController.instance.CanInput());
                //VocalKnight.Instance.NewRecognizer();
            }
        }
    }
}
