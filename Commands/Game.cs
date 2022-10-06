using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VocalKnight.Entities.Attributes;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using HutongGames.PlayMaker;
using JetBrains.Annotations;
using Modding;
using MonoMod.RuntimeDetour;
using On.HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using UObject = UnityEngine.Object;

namespace VocalKnight.Commands
{
    [UsedImplicitly]
    public class Game
    {
        internal static AudioClip[] Clips { get; private set; } = Resources.FindObjectsOfTypeAll<AudioClip>();

        private bool partyFlag;

        public Game()
        {
            // Just for the side effects.
            Resources.LoadAll("");

            //Clips = Resources.FindObjectsOfTypeAll<AudioClip>();


        }

        [HKCommand("setText")]
        [Summary("Sets all in-game text to a predetermined message")]
        [Cooldown(30)]
        public IEnumerator Text([RemainingText]string msg)
        {
            string OnLangGet(string key, string title, string orig) => msg;

            ModHooks.LanguageGetHook += OnLangGet;

            yield return new WaitForSeconds(30f);

            ModHooks.LanguageGetHook -= OnLangGet;
        }

        [HKCommand("reset")]
        [Cooldown(0)]
        [Summary("Undoes lasting effects. Must say \"Reset Reset Reset\" to activate")]
        public void Reset()
        {
            CoroutineUtil.cancel = true;
            partyFlag = true;
        }

        [HKCommand("heal")]
        [Cooldown(60)]
        public void Heal()
        {
            if (Random.Range(1, 3) == 1)
            {
                HeroController.instance.MaxHealth();
                
                return;
            }

            foreach (HealthManager hm in UObject.FindObjectsOfType<HealthManager>())
            {
                hm.hp *= 3;
                hm.hp /= 2;
            }
        }

        [HKCommand("rng")]
        [Summary("YUP RNG.")]
        [Cooldown(120)]
        public IEnumerator RNG()
        {
            static void OnWait(Wait.orig_OnEnter orig, HutongGames.PlayMaker.Actions.Wait self)
            {
                FsmFloat orig_time = self.time;

                self.time = Random.Range(0, 3f * self.time.Value);

                orig(self);

                self.time = orig_time;
            }

            static void OnWaitRandom(WaitRandom.orig_OnEnter orig, HutongGames.PlayMaker.Actions.WaitRandom self)
            {
                FsmFloat orig_time_min = self.timeMin;
                FsmFloat orig_time_max = self.timeMax;

                self.timeMin = Random.Range(0, self.timeMin.Value * 3f);
                self.timeMax = Random.Range(0, self.timeMax.Value * 3f);

                orig(self);

                self.timeMin = orig_time_min;
                self.timeMax = orig_time_max;
            }

            static void AnimPlay
            (
                On.tk2dSpriteAnimator.orig_Play_tk2dSpriteAnimationClip_float_float orig,
                tk2dSpriteAnimator self,
                tk2dSpriteAnimationClip clip,
                float start,
                float fps
            )
            {
                float orig_fps = clip.fps;

                clip.fps = Random.Range(clip.fps / 4, clip.fps * 4);
                
                orig(self, clip, start, fps);

                clip.fps = orig_fps;
            }

            Wait.OnEnter += OnWait;
            WaitRandom.OnEnter += OnWaitRandom;
            On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float += AnimPlay;

            for (int i = 0; i < 12; i++)
            {
                foreach (Rigidbody2D rb2d in UObject.FindObjectsOfType<Rigidbody2D>())
                {
                    Vector2 vel = rb2d.velocity;
                    
                    rb2d.velocity = new Vector2
                    (
                        Random.Range(-2 * vel.x, 2 * vel.y),
                        Random.Range(-2 * vel.y, 2 * vel.y)
                    );
                }

                yield return new WaitForSeconds(4f);
            }
            
            Wait.OnEnter -= OnWait;
            WaitRandom.OnEnter -= OnWaitRandom;
            On.tk2dSpriteAnimator.Play_tk2dSpriteAnimationClip_float_float -= AnimPlay;
        }


        [HKCommand("sfxRando")]
        [Cooldown(180)]
        [Summary("Randomizes sfx sounds.")]
        public IEnumerator SfxRando()
        {
            var oneShotHook = new Hook
            (
                typeof(AudioSource).GetMethod("PlayOneShot", new[] {typeof(AudioClip), typeof(float)}),
                new Action<Action<AudioSource, AudioClip, float>, AudioSource, AudioClip, float>(PlayOneShot)
            );

            var playHook = new Hook
            (
                typeof(AudioSource).GetMethod("Play", Type.EmptyTypes),
                new Action<Action<AudioSource>, AudioSource>(Play)
            );

            yield return new WaitForSecondsRealtime(60f);

            oneShotHook.Dispose();
            playHook.Dispose();
        }

        private static void PlayOneShot(Action<AudioSource, AudioClip, float> orig, AudioSource self, AudioClip clip, float volumeScale)
        {
            orig(self, Clips[Random.Range(0, Clips.Length - 1)], volumeScale);
        }

        private static void Play(Action<AudioSource> orig, AudioSource self)
        {
            AudioClip orig_clip = self.clip;

            self.clip = Clips[Random.Range(0, Clips.Length - 1)];

            orig(self);

            self.clip = orig_clip;
        }

        [HKCommand("party")]
        [Cooldown(15f)]
        [Summary("Initiates party time")]
        public IEnumerator PartyTime()
        {
            var go = new GameObject();
            MonoBehaviour runner = go.AddComponent<NonBouncer>();
            partyFlag = false;

            void SceneChanged(Scene oldScene, Scene newScene) => partyFlag = true;
            void PlayerDied() => partyFlag = true;
            int TookDamage(int damage)
            {
                partyFlag = true;
                return damage;
            }

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneChanged;
            ModHooks.AfterPlayerDeadHook += PlayerDied;
            ModHooks.TakeHealthHook += TookDamage;

            HueShifter.ZFrequency = 100f;
            HueShifter.TimeFrequency = 100f;
            HueShifter.SetAllTheShaders();
            AudioSource bgm = GameManager.instance.transform.Find("AudioManager/Music/Main").gameObject.GetComponent<AudioSource>();
            AudioClip bgm_orig = bgm.clip;
            bgm.PlayOneShot(VocalKnight.customAudio["Funky Dealer"]);

            runner.StartCoroutine(TimedAction(15f, () =>
            {
                bgm.clip = bgm_orig;
                bgm.Play();
                partyFlag = true;
            }));
            while (!partyFlag)
                yield return HeroController.instance.GetComponent<Emoter>().Emote();

            HueShifter.SetDefaults();
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= SceneChanged;
            ModHooks.AfterPlayerDeadHook -= PlayerDied;
            ModHooks.TakeHealthHook -= TookDamage;
        }

        private IEnumerator TimedAction(float waitTime, Action ThingtoDo)
        {
            yield return CoroutineUtil.WaitWithCancel(waitTime);
            ThingtoDo();
        }
    }

    public class Emoter : MonoBehaviour
    {
        private void Awake()
        {
            this._anim = HeroController.instance.gameObject.GetComponent<tk2dSpriteAnimator>();
        }

        private void Start()
        {
            tk2dSpriteCollectionData collection = HeroController.instance.GetComponent<tk2dSprite>().Collection;
            List<tk2dSpriteDefinition> list = collection.spriteDefinitions.ToList<tk2dSpriteDefinition>();
            foreach (tk2dSpriteDefinition tk2dSpriteDefinition in VocalKnight.EmotesBundle.LoadAsset<GameObject>("EmotesCollection").GetComponent<tk2dSpriteCollection>().spriteCollection.spriteDefinitions)
            {
                tk2dSpriteDefinition.material.shader = list[0].material.shader;
                list.Add(tk2dSpriteDefinition);
            }
            collection.spriteDefinitions = list.ToArray();
            List<tk2dSpriteAnimationClip> list2 = this._anim.Library.clips.ToList<tk2dSpriteAnimationClip>();
            foreach (tk2dSpriteAnimationClip item in VocalKnight.EmotesBundle.LoadAsset<GameObject>("EmotesAnim").GetComponent<tk2dSpriteAnimation>().clips)
            {
                list2.Add(item);
            }
            this._anim.Library.clips = list2.ToArray();
        }

        public IEnumerator Emote()
        {
            HeroController.instance.RelinquishControl();
            HeroController.instance.StopAnimationControl();
            this._anim.Play("Dab");
            yield return new WaitWhile(() => this._anim.IsPlaying("Dab"));
            HeroController.instance.RegainControl();
            HeroController.instance.StartAnimationControl();
            yield break;
        }

        private tk2dSpriteAnimator _anim;
    }
}