using Modding;
using System;
using UnityEngine;
using UObject = UnityEngine.Object;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using Satchel.BetterMenus;
using VocalKnight.Commands;
using VocalKnight.Entities;
using VocalKnight.Entities.Attributes;
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
                        "Maximum Potential Keywords",
                        "The max number of potential keywords that can set off an effect",
                        new string[] {"1","2","3","4"},
                        (setting) =>
                        {
                            GS.potentialKWs = setting + 1;
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
                ToggleMenuRef = new Menu("VocalKnight Effects", new Element[] { });
                foreach (string command in GS.commandToggles.Keys)
                {
                    string summary = "";
                    try {
                        summary = ((SummaryAttribute[])Processor.Commands.Find(x => x.Name.Contains(command))
                                    .MethodInfo.GetCustomAttributes(typeof(SummaryAttribute), true))[0].Summary;
                    } catch { }
                    
                    ToggleMenuRef.AddElement(new HorizontalOption(
                        command,
                        summary,
                        new string[] { "OFF", "ON" },
                        (setting) =>
                        {
                            GS.commandToggles[command] = Convert.ToBoolean(setting);
                        },
                        () => Convert.ToInt16(GS.commandToggles[command])));
                }
            }
            return MenuRef.GetMenuScreen(modListMenu);
        }

        public static VocalKnight? _instance;

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

        public override string GetVersion() => "0.9.0";

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

                preloadFlag = true;
            }

            //Initialize captions GameObject
            dictText = UObject.Instantiate(GameCameras.instance.gameObject.transform.
                               Find("HudCamera/Hud Canvas/Geo Counter/Geo Text").gameObject);
            UObject.DontDestroyOnLoad(dictText);
            dictText.name = "dictationTextDisplay";
            dictText.transform.SetPosition3D(-11.3f, -7.5f, 0.21f);
            UObject.Destroy(dictText.GetComponent<PlayMakerFSM>());
            dictText.GetComponent<TextMesh>().fontSize = 35;
            dictText.GetComponent<TextMesh>().text = "";
            dictText.SetActive(true);

            NewRecognizer();
            RecognizerUtil.foundCommands.CollectionChanged += ExecuteCommands;

            /*kp = new GameObject();
            kp.name = "KeyPressDetector";
            UObject.DontDestroyOnLoad(kp);
            kp.AddComponent<KeyPress>();
            */
            Log("Initialized");
        }

        public void NewRecognizer()
        {
            recognizer = new RecognizerUtil();
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

        public void Unload()
        {
            UObject.Destroy(kp);
            UObject.Destroy(dictText);
            recognizer = null;
            RecognizerUtil.foundCommands.CollectionChanged -= ExecuteCommands;
        }

        public void OnLoadGlobal(GSets s) => GS = s;

        public GSets OnSaveGlobal() => GS;
    }

    public class KeyPress : MonoBehaviour
    {
        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.R))
            {
                Logger.Log("Abandoning old Recognizer...");
                VocalKnight._instance.NewRecognizer();
            }
        }
    }
}
