using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using VocalKnight.Entities.Attributes;
using VocalKnight.ModHelpers;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.Networking;
using VasiFSM = Vasi.FsmUtil;
using UObject = UnityEngine.Object;

namespace VocalKnight.Commands
{
    public class Player
    {
        // Most (all) of the commands stolen from Chaos Mod by Seanpr
        private  GameObject _maggot;

        public Player()
        {
            IEnumerator GetMaggotPrime()
            {
                const string hwurmpURL = "https://cdn.discordapp.com/attachments/410556297046523905/716824653280313364/hwurmpU.png";

                UnityWebRequest www = UnityWebRequestTexture.GetTexture(hwurmpURL);

                yield return www.SendWebRequest();

                Texture texture = DownloadHandlerTexture.GetContent(www);

                Sprite maggotPrime = Sprite.Create
                (
                    (Texture2D) texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                _maggot = new GameObject("maggot");
                _maggot.AddComponent<SpriteRenderer>().sprite = maggotPrime;
                _maggot.SetActive(false);

                UObject.DontDestroyOnLoad(_maggot);
            }

            GameManager.instance.StartCoroutine(GetMaggotPrime());
        }

        [HKCommand("ax2uBlind")]
        [Summary("Makes the room a dark room")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator Blind()
        {
            void OnSceneLoad(On.GameManager.orig_EnterHero orig, GameManager self, bool additiveGateSearch)
            {
                orig(self, additiveGateSearch);
                
                DarknessHelper.Darken();
            }

            DarknessHelper.Darken();
            On.GameManager.EnterHero += OnSceneLoad;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            On.GameManager.EnterHero -= OnSceneLoad;
            DarknessHelper.Lighten();
        }

        [HKCommand("nopogo")]
        [Summary("Disables pogo bouncing")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator PogoKnockback()
        {
            void NoBounce(On.HeroController.orig_Bounce orig, HeroController self) { }

            On.HeroController.Bounce += NoBounce;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            On.HeroController.Bounce -= NoBounce;
        }

        [HKCommand("conveyor")]
        [Summary("Floors or walls act like conveyors")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator Conveyor()
        {
            bool vert = Random.Range(0, 2) == 0;

            float speed = Random.Range(-30f, 30f);

            HeroController hc = HeroController.instance;

            if (vert)
            {
                hc.cState.onConveyorV = true;
                hc.GetComponent<ConveyorMovementHero>().StartConveyorMove(0f, speed);
            }
            else
            {
                hc.cState.onConveyor = true;
                hc.SetConveyorSpeed(speed);
            }

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            if (vert)
                hc.cState.onConveyorV = false;
            else
                hc.cState.onConveyor = false;

            hc.GetComponent<ConveyorMovementHero>().StopConveyorMove();
        }

        [HKCommand("jumplength")]
        [Summary("Gives a random jump length")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator JumpLength()
        {
            HeroController hc = HeroController.instance;

            int prev_steps = hc.JUMP_STEPS;

            hc.JUMP_STEPS = Random.Range(hc.JUMP_STEPS / 2, hc.JUMP_STEPS * 8);

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            hc.JUMP_STEPS = prev_steps;
        }

        [HKCommand("sleep")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Puts The Knight to sleep")]
        public  IEnumerator Sleep()
        {
            const string SLEEP_CLIP = "Wake Up Ground";

            HeroController hc = HeroController.instance;

            var anim = hc.GetComponent<HeroAnimationController>();

            anim.PlayClip(SLEEP_CLIP);

            hc.StopAnimationControl();
            hc.RelinquishControl();

            yield return CoroutineUtil.WaitWithCancel(anim.GetClipDuration(SLEEP_CLIP));

            hc.StartAnimationControl();
            hc.RegainControl();
        }

        [HKCommand("jumpspeed")]
        [Summary("Gives a random jump speed")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator JumpSpeed()
        {
            HeroController hc = HeroController.instance;

            float prev_speed = hc.JUMP_SPEED;

            hc.JUMP_SPEED = Random.Range(hc.JUMP_SPEED / 4f, hc.JUMP_SPEED * 4f);

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            hc.JUMP_SPEED = prev_speed;
        }

        [HKCommand("wind")]
        [Summary("Makes it a windy day")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator Wind()
        {
            float speed = Random.Range(-6f, 6f);

            float prev_s = HeroController.instance.conveyorSpeed;
            
            HeroController.instance.cState.inConveyorZone = true;
            HeroController.instance.conveyorSpeed = speed;
            
            void BeforePlayerDead()
            {
                HeroController.instance.cState.inConveyorZone = false;
                HeroController.instance.conveyorSpeed = prev_s;
                
                ModHooks.BeforePlayerDeadHook -= BeforePlayerDead;
            }

            // Prevent wind from pushing you OOB on respawn.
            ModHooks.BeforePlayerDeadHook += BeforePlayerDead;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            ModHooks.BeforePlayerDeadHook -= BeforePlayerDead;

            HeroController.instance.cState.inConveyorZone = false;
            HeroController.instance.conveyorSpeed = prev_s;
        }

        [HKCommand("dashSpeed")]
        [Summary("Changes dash speed")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator DashSpeed()
        {
            HeroController hc = HeroController.instance;

            float len = Random.Range(.25f * hc.DASH_SPEED, hc.DASH_SPEED * 12f);
            float orig_dash = hc.DASH_SPEED;

            hc.DASH_SPEED = len;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            hc.DASH_SPEED = orig_dash;
        }

        [HKCommand("dashLength")]
        [Summary("Changes dash length")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator DashLength()
        {
            HeroController hc = HeroController.instance;

            float len = Random.Range(.25f * hc.DASH_TIME, hc.DASH_TIME * 12f);
            float orig_dash = hc.DASH_TIME;

            hc.DASH_TIME = len;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            hc.DASH_TIME = orig_dash;
        }

        [HKCommand("dashVector")]
        [Summary("Changes dash vector")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator DashVector()
        {
            Vector2? vec = null;
            Vector2? orig = null;

            Vector2 VectorHook(Vector2 change)
            {
                if (
                    orig == change
                    && vec is Vector2 v
                )
                    return v;

                const float factor = 4f;

                orig = change;

                float mag = change.magnitude;

                float x = factor * Random.Range(-mag, mag);
                float y = factor * Random.Range(-mag, mag);

                return (Vector2) (vec = new Vector2(x, y));
            }

            ModHooks.DashVectorHook += VectorHook;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            ModHooks.DashVectorHook -= VectorHook;
        }

        [HKCommand("timescale")]
        [Summary("Makes the game run faster or slower")]
        [Cooldown(CommonVars.cldn * 2)]
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public  IEnumerator ChangeTimescale([EnsureFloat(0.01f, 2f)] float scale)
        {
            SanicHelper.TimeScale = scale;

            Time.timeScale = Time.timeScale == 0 ? 0 : scale;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn * scale);

            Time.timeScale = Time.timeScale == 0 ? 0 : 1;

            SanicHelper.TimeScale = 1;
        }

        [HKCommand("weight")]
        [Summary("Changes the weight of the player")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator ChangeGravity([EnsureFloat(0.2f, 1.90f)] float scale)
        {
            var rigidBody = HeroController.instance.gameObject.GetComponent<Rigidbody2D>();

            float def = rigidBody.gravityScale;

            rigidBody.gravityScale = scale;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            //Fixes a glitch where ending low gravity while the player is airborne causes them to float up infinitely
            yield return new WaitUntil(() => HeroController.instance.cState.onGround);

            rigidBody.gravityScale = def;
        }


        [HKCommand("invertcontrols")]
        [Summary("Inverts movement controls")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator InvertControls()
        {
            void Invert(On.HeroController.orig_Move orig, HeroController self, float dir)
            {
                if (HeroController.instance.transitionState != HeroTransitionState.WAITING_TO_TRANSITION)
                {
                    orig(self, dir);
                    
                    return;
                }

                orig(self, -dir);
            }
            
            Vector2 InvertDash(Vector2 change)
            {
                return -change;
            }

            void FaceLeft(On.HeroController.orig_FaceLeft orig, HeroController self)
            {
                HeroController.instance.cState.facingRight = true;
                Vector3 localScale = HeroController.instance.transform.localScale;
                localScale.x = -1f;
                HeroController.instance.transform.localScale = localScale;
            }

            void FaceRight(On.HeroController.orig_FaceRight orig, HeroController self)
            {
                HeroController.instance.cState.facingRight = false;
                Vector3 localScale = HeroController.instance.transform.localScale;
                localScale.x = 1f;
                HeroController.instance.transform.localScale = localScale;
            }

            On.HeroController.Move += Invert;
            On.HeroController.FaceLeft += FaceLeft;
            On.HeroController.FaceRight += FaceRight;
            //ModHooks.DashVectorHook += InvertDash;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            On.HeroController.Move -= Invert;
            On.HeroController.FaceLeft -= FaceLeft;
            On.HeroController.FaceRight -= FaceRight;
            //ModHooks.DashVectorHook -= InvertDash;
        }

        [HKCommand("bounce")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Makes the floor bouncy")]
        public IEnumerator Bouncy()
        {
            void CauseBounce()
            {
                if (HeroController.instance.cState.onGround)
                    HeroController.instance.ShroomBounce();
            }

            ModHooks.HeroUpdateHook += CauseBounce;
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);
            ModHooks.HeroUpdateHook -= CauseBounce;
        }

        [HKCommand("slippery")]
        [Summary("Makes the floor slippery")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator Slippery()
        {
            float last_move_dir = 0;

            void Slip(On.HeroController.orig_Move orig, HeroController self, float move_direction)
            {
                if (HeroController.instance.transitionState != HeroTransitionState.WAITING_TO_TRANSITION)
                {
                    orig(self, move_direction);
                    
                    return;
                }

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (move_direction == 0f)
                {
                    move_direction = last_move_dir;
                }

                orig(self, move_direction);

                last_move_dir = move_direction;
            }
            
            On.HeroController.Move += Slip;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            On.HeroController.Move -= Slip;
        }

        [HKCommand("nailscale")]
        [Summary("Changes nail size for The Knight")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator NailScale([EnsureFloat(.3f, 5f)] float nailScale)
        {
            void ChangeNailScale(On.NailSlash.orig_StartSlash orig, NailSlash self)
            {
                orig(self);
                
                self.transform.localScale *= nailScale;
            }

            On.NailSlash.StartSlash += ChangeNailScale;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            On.NailSlash.StartSlash -= ChangeNailScale;
        }

        [HKCommand("bindings")]
        [Summary("Enables bindings")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator EnableBindings()
        {
            BindingsHelper.AddDetours();

            On.BossSceneController.RestoreBindings += BindingsHelper.NoOp;
            On.GGCheckBoundSoul.OnEnter += BindingsHelper.CheckBoundSoulEnter;

            BindingsHelper.ShowIcons();

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            BindingsHelper.Unload();
        }

        [HKCommand("hwurmpU")]
        [Summary("I don't even know honestly...")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator EnableMaggotPrimeSkin()
        {
            GameObject go = UObject.Instantiate(_maggot, HeroController.instance.transform.position + new Vector3(0, 0, -1f), Quaternion.identity);

            go.transform.parent = HeroController.instance.transform;
            
            go.SetActive(true);

            var renderer = HeroController.instance.GetComponent<MeshRenderer>();
            renderer.enabled = false;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            UObject.DestroyImmediate(go);

            renderer.enabled = true;
        }

        [HKCommand("walkspeed")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Changes The Knight's walk speed")]
        public  IEnumerator WalkSpeed([EnsureFloat(0.3f, 10f)] float speed)
        {
            float prev_speed = HeroController.instance.RUN_SPEED;

            HeroController.instance.RUN_SPEED *= speed;

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);

            HeroController.instance.RUN_SPEED = prev_speed;
        }

        [HKCommand("geo")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("The Knight explodes with geo")]
        public  void Geo()
        {
            GameObject[] geos = Resources.FindObjectsOfTypeAll<GameObject>().Where(x => x.name.StartsWith("Geo")).ToArray();
            
            GameObject large = geos.First(x => x.name.Contains("Large"));
            GameObject medium = geos.First(x => x.name.Contains("Med"));
            GameObject small = geos.First(x => x.name.Contains("Small"));
            
            small.SetActive(true);
            medium.SetActive(true);
            large.SetActive(true);
            
            HeroController.instance.proxyFSM.SendEvent("HeroCtrl-HeroDamaged");
            HeroController.instance.StartCoroutine(
                (IEnumerator) typeof(HeroController)
                              .GetMethod("StartRecoil", BindingFlags.NonPublic | BindingFlags.Instance)
                              ?.Invoke(HeroController.instance, new object[] { CollisionSide.left, true, 0 })
            );

            int amount = Random.Range(100, 500);
            HeroController.instance.TakeGeo(amount);
            SpawnGeo(amount, small, medium, large);
        }

        [HKCommand("respawn")]
        [Cooldown(CommonVars.cldn)]
        [Summary("Causes a hazard respawn like falling into spikes")]
        public IEnumerator HazardRespawn()
        {
            // Don't trigger during transitions or anything
            if (HeroController.instance.transitionState != HeroTransitionState.WAITING_TO_TRANSITION)
                yield break;

            HeroController.instance.TakeDamage(null, CollisionSide.bottom, 1, 2);
        }

        [HKCommand("die")]
        [Cooldown(CommonVars.cldn * 4)]
        [Summary("Kills The Knight on the spot")]
        public void KillPlayer()
        {
            // Don't trigger during transitions or anything
            if (HeroController.instance.transitionState != HeroTransitionState.WAITING_TO_TRANSITION)
                return;

            HeroController.instance.TakeDamage(null, CollisionSide.bottom, PlayerData.instance.health, 1);
        }

        //Following code taken from Benchwarp mod
        [HKCommand("bench")]
        [Cooldown(CommonVars.cldn * 3)]
        [Summary("Warps The Knight to the last bench they sat at")]
        public IEnumerator BenchWarp()
        {
            // Don't trigger during transitions or anything
            if (HeroController.instance.transitionState != HeroTransitionState.WAITING_TO_TRANSITION)
                yield break;

            GameManager.instance.SaveGame();
            GameManager.instance.TimePasses();
            UIManager.instance.UIClosePauseMenu();

            // Collection of various redundant attempts to fix the infamous soul orb bug
            HeroController.instance.TakeMPQuick(PlayerData.instance.MPCharge); // actually broadcasts the event
            HeroController.instance.SetMPCharge(0);
            PlayerData.instance.MPReserve = 0;
            HeroController.instance.ClearMP(); // useless
            PlayMakerFSM.BroadcastEvent("MP DRAIN"); // This is the main fsm path for removing soul from the orb
            PlayMakerFSM.BroadcastEvent("MP LOSE"); // This is an alternate path (used for bindings and other things) that actually plays an animation?
            PlayMakerFSM.BroadcastEvent("MP RESERVE DOWN");

            // Set some stuff which would normally be set by LoadSave
            HeroController.instance.AffectedByGravity(false);
            HeroController.instance.transitionState = HeroTransitionState.EXITING_SCENE;
            if (HeroController.SilentInstance != null)
            {
                if (HeroController.instance.cState.onConveyor || HeroController.instance.cState.onConveyorV || HeroController.instance.cState.inConveyorZone)
                {
                    HeroController.instance.GetComponent<ConveyorMovementHero>()?.StopConveyorMove();
                    HeroController.instance.cState.inConveyorZone = false;
                    HeroController.instance.cState.onConveyor = false;
                    HeroController.instance.cState.onConveyorV = false;
                }

                HeroController.instance.cState.nearBench = false;
            }
            GameManager.instance.cameraCtrl.FadeOut(CameraFadeType.LEVEL_TRANSITION);

            yield return new WaitForSecondsRealtime(0.5f);

            // Actually respawn the character
            GameManager.instance.SetPlayerDataBool(nameof(PlayerData.atBench), false);
            // Allow the player to have control if they warp to a non-bench while diving or cdashing
            if (HeroController.SilentInstance != null)
            {
                HeroController.instance.cState.superDashing = false;
                HeroController.instance.cState.spellQuake = false;
            }
            GameManager.instance.ReadyForRespawn(false);

            yield return new WaitWhile(() => GameManager.instance.IsInSceneTransition);

            // Revert pause menu timescale
            Time.timeScale = 1f;
            GameManager.instance.FadeSceneIn();

            // We have to set the game non-paused because TogglePauseMenu sucks and UIClosePauseMenu doesn't do it for us.
            GameManager.instance.isPaused = false;

            // Restore various things normally handled by exiting the pause menu. None of these are necessary afaik
            GameCameras.instance.ResumeCameraShake();
            if (HeroController.SilentInstance != null)
            {
                HeroController.instance.UnPause();
            }
            MenuButtonList.ClearAllLastSelected();

            //This allows the next pause to stop the game correctly
            TimeController.GenericTimeScale = 1f;

            // Restores audio to normal levels. Unfortunately, some warps pop atm when music changes over
            GameManager.instance.actorSnapshotUnpaused.TransitionTo(0f);
            GameManager.instance.ui.AudioGoToGameplay(.2f);

            bool IsDreamRoom()
            {
                return GameManager.instance.sm.mapZone switch
                {
                    GlobalEnums.MapZone.DREAM_WORLD
                    or GlobalEnums.MapZone.GODS_GLORY
                    or GlobalEnums.MapZone.GODSEEKER_WASTE
                    or GlobalEnums.MapZone.WHITE_PALACE => true,
                    _ => false,
                };
            }

            // reset some things not cleaned up when exiting dream sequences, etc
            if (HeroController.SilentInstance != null)
            {
                HeroController.SilentInstance.takeNoDamage = false;
                if (!IsDreamRoom() && HeroController.SilentInstance.proxyFSM?.FsmVariables?.FindFsmBool("No Charms") is HutongGames.PlayMaker.FsmBool noCharms) noCharms.Value = false;
            }
            if (HutongGames.PlayMaker.FsmVariables.GlobalVariables.FindFsmBool("Is HUD Out") is HutongGames.PlayMaker.FsmBool hudOut && hudOut.Value)
            {
                PlayMakerFSM.BroadcastEvent("IN");
            }
        }

        [HKCommand("gravup")]
        [Cooldown(CommonVars.cldn / 2)]
        [Summary("Flips gravity upside down")]
        public IEnumerator FlipGravity()
        {
            HeroController _mHc = HeroController.instance;
            
            void Flip()
            {
                _mHc.SetAttr("BUMP_VELOCITY", _mHc.GetAttr<HeroController, float>("BUMP_VELOCITY") * -1);
                _mHc.BOUNCE_VELOCITY *= -1;
                _mHc.WALLSLIDE_DECEL *= -1;
                _mHc.WALLSLIDE_SPEED *= -1;
                _mHc.SWIM_ACCEL *= -1;
                _mHc.SWIM_MAX_SPEED *= -1;
                _mHc.JUMP_SPEED_UNDERWATER *= -1;
                _mHc.JUMP_SPEED *= -1;
                _mHc.SHROOM_BOUNCE_VELOCITY *= -1;
                _mHc.RECOIL_DOWN_VELOCITY *= -1;
                _mHc.MAX_FALL_VELOCITY *= -1;
                _mHc.MAX_FALL_VELOCITY_UNDERWATER *= -1;
                //m_rb2d.gravityScale *= -1;
                Physics2D.gravity *= -1;
                Vector3 tmpVec3 = _mHc.gameObject.transform.localScale;
                tmpVec3.y *= -1;
                _mHc.gameObject.transform.localScale = tmpVec3;
            }

            Flip();
            yield return CoroutineUtil.WaitWithCancel(0.75f);
            Flip();
        }

        private void SpawnGeo(int amount, GameObject smallGeoPrefab, GameObject mediumGeoPrefab, GameObject largeGeoPrefab)
        {
            if (amount <= 0) return;

            if (smallGeoPrefab == null || mediumGeoPrefab == null || largeGeoPrefab == null)
            {
                HeroController.instance.AddGeo(amount);
                
                return;
            }

            var random = new System.Random();

            int smallNum = random.Next(0, amount / 10);
            amount -= smallNum;

            int largeNum = random.Next(amount / (25 * 2), amount / 25 + 1);
            amount -= largeNum * 25;

            int medNum = amount / 5;
            amount -= medNum * 5;

            smallNum += amount;

            FlingUtils.SpawnAndFling
            (
                new FlingUtils.Config
                {
                    Prefab = smallGeoPrefab,
                    AmountMin = smallNum,
                    AmountMax = smallNum,
                    SpeedMin = 15f,
                    SpeedMax = 30f,
                    AngleMin = 80f,
                    AngleMax = 115f
                },
                HeroController.instance.transform,
                new Vector3(0f, 0f, 0f)
            );
            
            FlingUtils.SpawnAndFling
            (
                new FlingUtils.Config
                {
                    Prefab = mediumGeoPrefab,
                    AmountMin = medNum,
                    AmountMax = medNum,
                    SpeedMin = 15f,
                    SpeedMax = 30f,
                    AngleMin = 80f,
                    AngleMax = 115f
                },
                HeroController.instance.transform,
                new Vector3(0f, 0f, 0f)
            );
            
            FlingUtils.SpawnAndFling
            (
                new FlingUtils.Config
                {
                    Prefab = largeGeoPrefab,
                    AmountMin = largeNum,
                    AmountMax = largeNum,
                    SpeedMin = 15f,
                    SpeedMax = 30f,
                    AngleMin = 80f,
                    AngleMax = 115f
                },
                HeroController.instance.transform,
                new Vector3(0f, 0f, 0f)
            );
        }

        [HKCommand("disable")]
        [Summary("Disables an ability, such as dash or claw")]
        [Cooldown(CommonVars.cldn * 2)]
        public IEnumerator ToggleAbility(string ability)
        {
            const float time = CommonVars.cldn;

            PlayerData pd = PlayerData.instance;

            switch (ability)
            {
                case "dash":
                    yield return PlayerDataUtil.FakeSet(nameof(PlayerData.canDash), false, time);
                    break;
                case "superdash":
                    yield return PlayerDataUtil.FakeSet(nameof(PlayerData.hasSuperDash), false, time);
                    break;
                case "claw":
                    yield return PlayerDataUtil.FakeSet(nameof(PlayerData.hasWalljump), false, time);
                    break;
                case "wings":
                    yield return PlayerDataUtil.FakeSet(nameof(PlayerData.hasDoubleJump), false, time);
                    break;
                case "dnail":
                    yield return PlayerDataUtil.FakeSet(nameof(PlayerData.hasDreamNail), false, time);
                    break;
            }
        }

        [HKCommand("nailonly")]
        [Summary("Disables the usage of spells")]
        [Cooldown(CommonVars.cldn * 2)]
        public IEnumerator DisableSpells()
        {
            bool OnCanCast(On.HeroController.orig_CanCast orig, HeroController self)
            {
                return false;
            }

            On.HeroController.CanCast += OnCanCast;
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);
            On.HeroController.CanCast -= OnCanCast;
        }

        [HKCommand("nonail")]
        [Summary("Prevents The Knight from swinging the nail")]
        [Cooldown(CommonVars.cldn * 2)]
        public IEnumerator DisableNail()
        {
            bool OnCanAttack(On.HeroController.orig_CanAttack orig, HeroController self)
            {
                return false;
            }

            On.HeroController.CanAttack += OnCanAttack;
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);
            On.HeroController.CanAttack -= OnCanAttack;
        }

        [HKCommand("noheal")]
        [Summary("Prevents The Knight from focusing")]
        [Cooldown(CommonVars.cldn * 2)]
        public IEnumerator DisableFocus()
        {
            bool OnCanFocus(On.HeroController.orig_CanFocus orig, HeroController self)
            {
                return false;
            }

            On.HeroController.CanFocus += OnCanFocus;
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);
            On.HeroController.CanFocus -= OnCanFocus;
        }

        [HKCommand("doubledamage")]
        [Summary("Makes The Knight take double damage")]
        [Cooldown(CommonVars.cldn * 2)]
        public  IEnumerator DoubleDamage()
        {
            static int InstanceOnTakeDamageHook(ref int hazardtype, int damage) => damage * 2;
            ModHooks.TakeDamageHook += InstanceOnTakeDamageHook;
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn);
            ModHooks.TakeDamageHook -= InstanceOnTakeDamageHook;
        }

        [HKCommand("hungry")]
        [Summary("Drains soul constantly. When soul reaches 0, the player takes damage")]
        [Cooldown(CommonVars.cldn * 4)]
        public IEnumerator HungryKnight()
        {
            var go = new GameObject();
            UObject.DontDestroyOnLoad(go);
            MonoBehaviour runner = go.AddComponent<NonBouncer>();
            bool flag = true;

            IEnumerator HandleSoul()
            {
                while (flag)
                {
                    if (!HeroController.instance.controlReqlinquished)
                    {
                        if (BossSequenceController.BoundSoul)
                        {
                            HeroController.instance.TakeMP(5);
                        }
                        else
                        {
                            HeroController.instance.TakeMP(11);
                        }
                    }
                    yield return new WaitForSeconds(3f);
                    if (PlayerData.instance.GetInt("MPCharge") == 0)
                    {
                        if (!HeroController.instance.controlReqlinquished)
                        {
                            HeroController.instance.TakeDamage(HeroController.instance.gameObject, GlobalEnums.CollisionSide.other, 1, 1);
                        }
                        yield return new WaitForSeconds(3f);
                    }
                }
                yield break;
            }

            runner.StartCoroutine(HandleSoul());
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn * 3);
            runner.StopAllCoroutines();
            UObject.Destroy(go);
        }

        [HKCommand("charmcurse")]
        [Cooldown(CommonVars.cldn * 4)]
        [Summary("Unequips all the player's charms")]
        public void UnequipCharms()
        {
            void CharmUpdate()
            {
                //Custom charm update method to prevent healing the player
                HeroController hc = HeroController.instance;
                if (hc.playerData.GetBool("equippedCharm_26"))
                {
                    ReflectionHelper.SetField(hc, "nailChargeTime", hc.NAIL_CHARGE_TIME_CHARM);
                }
                else
                {
                    ReflectionHelper.SetField(hc, "nailChargeTime", hc.NAIL_CHARGE_TIME_DEFAULT);
                }
                if (hc.playerData.GetBool("equippedCharm_23") && !hc.playerData.GetBool("brokenCharm_23"))
                {
                    hc.playerData.SetInt("maxHealth", hc.playerData.GetInt("maxHealthBase") + 2);
                }
                else
                {
                    hc.playerData.SetInt("maxHealth", hc.playerData.GetInt("maxHealthBase"));
                }
                if (hc.playerData.GetBool("equippedCharm_27"))
                {
                    hc.playerData.SetInt("joniHealthBlue", (int)(hc.playerData.GetInt("maxHealth") * 1.4f));
                    hc.playerData.SetInt("maxHealth", 1);
                    ReflectionHelper.SetField(hc, "joniBeam", true);
                }
                else
                {
                    hc.playerData.SetInt("joniHealthBlue", 0);
                }
                if (hc.playerData.GetBool("equippedCharm_40") && hc.playerData.GetInt("grimmChildLevel") == 5)
                {
                    hc.carefreeShieldEquipped = true;
                }
                else
                {
                    hc.carefreeShieldEquipped = false;
                }
            }

            foreach (int num in PlayerData.instance.GetVariable<List<int>>(nameof(PlayerData.equippedCharms)).ToArray())
            {
                GameManager.instance.UnequipCharm(num);
                PlayerData.instance.SetBool("equippedCharm_" + num, false);
            }

            //Extra stuff to make sure
            PlayerData.instance.CalculateNotchesUsed();
            GameManager.instance.RefreshOvercharm();

            CharmUpdate();
            PlayMakerFSM.BroadcastEvent("CHARM INDICATOR CHECK");
        }

        [HKCommand("timewarp")]
        [Summary("Warps The Knight back to where they were X seconds ago")]
        [Cooldown(CommonVars.cldn)]
        public IEnumerator Timewarp()
        {
            PlayMakerFSM dreamnailFSM = HeroController.instance.gameObject.LocateMyFSM("Dream Nail");
            GameObject flash = VasiFSM.GetAction<SpawnObjectFromGlobalPool>(dreamnailFSM, "Set", 12).gameObject.Value;

            AudioClip audioSet = VasiFSM.GetAction<AudioPlayerOneShotSingle>(dreamnailFSM, "Set", 11).audioClip.Value as AudioClip;
            AudioClip audioWarp = VasiFSM.GetAction<AudioPlayerOneShotSingle>(dreamnailFSM, "Warp End", 9).audioClip.Value as AudioClip;
            AudioSource audioSource = HeroController.instance.GetComponent<AudioSource>();

            //Set gate
            Vector3 position = HeroController.instance.transform.position;
            string warpScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            audioSource.PlayOneShot(audioSet);

            //Wait
            yield return new WaitForSeconds(3f);

            //Warp to gate (unless in another scene & wait until player is in control)
            HeroController.instance.parryInvulnTimer = 0.6f;
            yield return new WaitUntil(() => !HeroController.instance.cState.transitioning);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != warpScene)
                yield break;
            yield return new WaitUntil(() => HeroController.instance.CanInput());
            HeroController.instance.transform.position = position;
            UObject.Instantiate(flash, position, Quaternion.identity);
            audioSource.PlayOneShot(audioWarp);
        }
    }
}