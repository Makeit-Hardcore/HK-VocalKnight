using Modding;
using System;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        public RecognizerUtil recognizer;
        //public WhisperUtil recognizerW;
        public GameObject dictText;
        private GameObject kp;
        private static bool preloadFlag = false;
        public Dictionary<string, int> Cooldowns = new();
        internal CommandProcessor Processor { get; private set; }
        public static AssetBundle EmotesBundle;
        public static AssetBundle HueShiftBundle;
        public static Dictionary<string, AudioClip> customAudio = new Dictionary<string, AudioClip>();
        public MonoBehaviour _coroutineRunner;
        private Menu MenuRef, ToggleMenuRef;
        public static GSets GS { get; set; } = new GSets();

        public bool ToggleButtonInsideMenu => true;

        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? modToggleDelegates)
        {
            bool generated = false;

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

                            KeywordUtil.UpdateKeywords_All();
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
                        () => ToggleMenuRef.GetMenuScreen(MenuRef.menuScreen)),
                    new HorizontalOption(
                        "Keyword Set",
                        "\'Randomized\' uses the most recent set until you GENERATE a new one",
                        new string[] {"Default","Randomized"},
                        (setting) =>
                        {
                            GS.kwSet = setting;
                            if (setting == 1 && GS.customKws == null)
                            {
                                KeywordUtil.RandomizeKeywords();
                            }

                            if (setting == 1)
                            {
                                MenuRef.Find("GenRand").Show();
                            }
                            else
                            {
                                MenuRef.Find("GenRand").Hide();
                            }
                            KeywordUtil.UpdateKeywords_All();
                        },
                        () => GS.kwSet),
                    new MenuButton(
                        "GENERATE RANDOMIZED KEYWORD SET",
                        "Selects 4 random keywords for each effect & creates Google Doc index",
                        (Mbutton) =>
                        {
                            KeywordUtil.RandomizeKeywords();
                            KeywordUtil.UpdateKeywords_All();
                            KeyIndexerUtil.WriteToFile();
                            MenuRef.Find("GenRand").Hide();
                            generated = true;
                            MenuRef.Find("GenText").Show();
                            _coroutineRunner.StartCoroutine(TimedMenuText(MenuRef.Find("GenText"),MenuRef.Find("GenRand")));
                        },
                        Id: "GenRand")
                    {
                        isVisible = Convert.ToBoolean(GS.kwSet),
                    },
                    new TextPanel(
                        "Randomized keyword set generated",
                        Id: "GenText")
                    {
                        isVisible = false,
                    },
                    new MenuButton(
                        "Create Google Doc Index",
                        "",
                        (Mbutton) =>
                        {
                            if (!KeyIndexerUtil.WriteToFile())
                            {
                                MenuRef.Find("CreateIndex").Hide();
                                MenuRef.Find("LinkReminder").Show();
                                _coroutineRunner.StartCoroutine(TimedMenuText(MenuRef.Find("LinkReminder"), MenuRef.Find("CreateIndex")));
                            }
                        },
                        Id: "CreateIndex"),
                    new TextPanel(
                        "Google account must be linked",
                        Id: "LinkReminder")
                    {
                        isVisible = false,
                    },
                    new MenuButton(
                        "Link Google Account",
                        "",
                        (Mbutton) =>
                        {
                            KeyIndexerUtil.GetCredentials().Wait(1);
                        })
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

                                    KeywordUtil.UpdateKeywords_All();
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

                                    KeywordUtil.UpdateKeywords_All();
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

                            KeywordUtil.UpdateKeywords_All();
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

        public override string GetVersion() => "1.9.5";

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

                KeywordUtil.UpdateKeywords_All();

                var go = new GameObject();
                UObject.DontDestroyOnLoad(go);
                _coroutineRunner = go.AddComponent<NonBouncer>();

                ConfigureCooldowns();
                LoadAssets();
                HueShifter.LoadAssets();

                preloadFlag = true;

                //recognizerW = new WhisperUtil();
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

            kp = new GameObject();
            kp.name = "KeyPressDetector";
            UObject.DontDestroyOnLoad(kp);
            kp.AddComponent<KeyPress>();

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
                        Logger.Log(text);
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
                            KeyIndexerUtil.GetSecrets(manifestResourceStream);
                        }
                        else if (key == "bin")
                        {
                            //recognizerW = new WhisperUtil(manifestResourceStream);
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
        }

        private IEnumerator TimedMenuText(Element text, Element button)
        {
            yield return WaitFor(3f);
            text.Hide();
            button.Show();
        }

        public IEnumerator WaitFor(float waittime)
        {
            for (float timer = waittime; timer > 0; timer -= Time.deltaTime)
            {
                yield return null;
            }
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
        public bool holdit = false;

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.L))
            {
                VocalKnight.Instance.recognizer.runner.StartCoroutine(VocalKnight.Instance.recognizer.ForceReset());
                VocalKnight.Instance._coroutineRunner.StartCoroutine(HoldKeyPress());
            }
        }

        private IEnumerator HoldKeyPress()
        {
            holdit = true;
            yield return VocalKnight.Instance.WaitFor(2f);
            holdit = false;
        }
    }
}
