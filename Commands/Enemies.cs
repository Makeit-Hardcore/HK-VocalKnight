using System;
using System.Linq;
using System.Collections;
using VocalKnight.Components;
using VocalKnight.Entities.Attributes;
using VocalKnight.Extensions;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Modding;
using UObject = UnityEngine.Object;
using URandom = UnityEngine.Random;
using SFCoreFSM = SFCore.Utils.FsmUtil;
using Satch = Satchel.FsmUtil;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Vasi;

namespace VocalKnight.Commands
{
    public class Enemies
    {
        [HKCommand("enemy")]
        [Cooldown(2)]
        public void SpawnEnemy(string name)
        {
            GameObject enemy = SpawnEnemyGeneric(name);

            if (enemy != null)
                enemy.SetActive(true);
        }

        [HKCommand("xero")]
        [Cooldown(15)]
        public void SpawnXero()
        {
            GameObject xero = SpawnEnemyGeneric("xero");
            if (xero == null)
                return;

            GameObject.Destroy(xero.LocateMyFSM("Y Limit"));
            PlayMakerFSM ctrlMov = xero.LocateMyFSM("Movement");
            PlayMakerFSM ctrlAttack = xero.LocateMyFSM("Attacking");

            //Some breaking error with these actions, remove for now
            //(Xero still spawns just fine, but with less fanfare)
            ctrlMov.GetState("Warp In").RemoveAllOfType<ActivateGameObject>();

            //Prevent Xero from cancelling attacks based on player position
            SFCoreFSM.ChangeFsmTransition(ctrlAttack, "Wait Rage", "FINISHED", "Antic");
            SFCoreFSM.ChangeFsmTransition(ctrlAttack, "Antic Rage", "CANCEL", "Antic");
            SFCoreFSM.ChangeFsmTransition(ctrlAttack, "Wait", "FINISHED", "Antic");

            //Rewrite his movement code to constantly follow the player

            SFCoreFSM.AddFsmFloatVariable(ctrlMov, "Hero Y");

            FsmState getPosState = SFCoreFSM.AddFsmState(ctrlMov, "Get Hero Pos");
            SFCoreFSM.AddFsmAction(getPosState, new GetPosition()
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = HeroController.instance.gameObject
                },
                vector = new Vector3(0, 0, 0),
                x = SFCoreFSM.GetFloatVariable(ctrlMov, "Hero X"),
                y = SFCoreFSM.GetFloatVariable(ctrlMov, "Hero Y"),
                z = 0f,
                space = 0,
                everyFrame = false
            });
            SFCoreFSM.AddFsmAction(getPosState, new FloatAddV2()
            {
                floatVariable = SFCoreFSM.GetFloatVariable(ctrlMov, "Hero Y"),
                add = 5f,
                everyFrame = false,
                perSecond = false,
                fixedUpdate = false,
                activeBool = true
            });

            FsmState setState = SFCoreFSM.AddFsmState(ctrlMov, "Set");
            SFCoreFSM.AddFsmAction(setState, new SetPosition()
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = SFCoreFSM.GetFsmGameObjectVariable(ctrlMov, "Target")
                },
                vector = new Vector3(0, 0, 0),
                x = SFCoreFSM.GetFsmFloatVariable(ctrlMov, "Hero X"),
                y = SFCoreFSM.GetFsmFloatVariable(ctrlMov, "Hero Y"),
                z = 0f,
                space = 0,
                everyFrame = false,
                lateUpdate = false
            });

            FsmState moveState = SFCoreFSM.AddFsmState(ctrlMov, "Move");
            SFCoreFSM.AddFsmAction(moveState, new ChaseObjectV2()
            {
                gameObject = new FsmOwnerDefault()
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = ctrlMov.gameObject
                },
                target = SFCoreFSM.FindFsmGameObjectVariable(ctrlMov, "Target"),
                speedMax = 15f,
                accelerationForce = 50f,
                offsetX = 0f,
                offsetY = 0f
            });

            SFCoreFSM.AddFsmTransition(ctrlMov, "Get Hero Pos", "FINISHED", "Set");
            SFCoreFSM.AddFsmTransition(ctrlMov, "Set", "FINISHED", "Move");
            SFCoreFSM.AddFsmTransition(ctrlMov, "Move", "FINISHED", "Get Hero Pos");

            SFCoreFSM.ChangeFsmTransition(ctrlMov, "Ready", "FINISHED", "Get Hero Pos");
            SFCoreFSM.ChangeFsmTransition(ctrlMov, "Attacking", "ATTACK END", "Get Hero Pos");
            SFCoreFSM.ChangeFsmTransition(ctrlMov, "Return", "FINISHED", "Get Hero Pos");

            xero.SetActive(true);
        }

        [HKCommand("gorb")]
        [Cooldown(17)]
        [Summary("Gorb spawns and throws a circle of spikes 3 times")]
        public IEnumerator SpawnGorb()
        {
            GameObject gorbGO = SpawnEnemyGeneric("gorb");
            gorbGO.transform.position = HeroController.instance.transform.position + new Vector3(0f, 5f, 0f);
            UObject.Destroy(gorbGO.GetComponent<DamageHero>());
            UObject.Destroy(gorbGO.GetComponent<Collider2D>());
            gorbGO.SetActive(true);

            PlayMakerFSM gorbMovementFSM = gorbGO.LocateMyFSM("Movement");
            PlayMakerFSM gorbAttackFSM = gorbGO.LocateMyFSM("Attacking");
            PlayMakerFSM gorbDistanceAttackFSM = gorbGO.LocateMyFSM("Distance Attack");

            //Make Gorb teleport out, wait, then teleport to player
            gorbMovementFSM.GetAction<Wait>("Warp Out", 2).time = 1f;
            SFCoreFSM.RemoveFsmAction(gorbMovementFSM, "Return", 4);
            SFCoreFSM.RemoveFsmAction(gorbMovementFSM, "Return", 0);
            SFCoreFSM.InsertFsmAction(gorbMovementFSM, "Return", gorbMovementFSM.GetAction<SetPosition>("Warp", 1), 0);
            SFCoreFSM.InsertMethod(gorbMovementFSM, "Return", () =>
            {
                gorbMovementFSM.FsmVariables.FindFsmVector3("Warp Pos").Value = HeroController.instance.transform.position;
            }, 0);

            //Prevent movement FSM from leaving Return state
            SFCoreFSM.RemoveFsmTransition(gorbMovementFSM, "Return", "FINISHED");

            //Reset isAttacking
            SFCoreFSM.InsertMethod(gorbAttackFSM, "Recover", () =>
            {
                gorbMovementFSM.SendEvent("RETURN");
            }, 0);

            //Set attack timer
            gorbAttackFSM.GetAction<WaitRandom>("Wait", 0).timeMin.Value = 0.75f;
            gorbAttackFSM.GetAction<WaitRandom>("Wait", 0).timeMax.Value = 0.75f;

            //Make attack FSM skip check for player
            SFCoreFSM.ChangeFsmTransition(gorbAttackFSM, "Wait", "FINISHED", "Antic");

            //Wait until player has control
            SFCoreFSM.InsertMethod(gorbAttackFSM, "Antic", () =>
            {
                if (HeroController.instance.controlReqlinquished)
                    gorbAttackFSM.SendEvent("ATTACK OK");
            }, 0);

            //Disable Gorb's distance attack
            SFCoreFSM.RemoveFsmTransition(gorbDistanceAttackFSM, "Init", "FINISHED");

            yield return CoroutineUtil.WaitWithCancel(17f);

            UObject.Destroy(gorbGO);
        }

        private GameObject SpawnEnemyGeneric(string name)
        {
            Logger.Log($"Trying to spawn enemy {name}");
            if (!ObjectLoader.InstantiableObjects.TryGetValue(name, out GameObject go))
            {
                Logger.LogError("Could not get GameObject " + name);
                return null;
            }
            Vector3 position = HeroController.instance.gameObject.transform.position;
            position.y += 5;
            GameObject enemy = UObject.Instantiate(go, position, Quaternion.identity);

            //For enemies that do not typically respawn until bench
            UObject.Destroy(enemy.GetComponent<PersistentBoolItem>());

            //Killing a spawned ghost warrior does not count as defeating the real deal
            if (enemy.name.Contains("Ghost Warrior"))
                enemy.LocateMyFSM("Set Ghost PD Int").GetState("Set").RemoveAllOfType<SetPlayerDataInt>();

            return enemy;
        }

        [HKCommand("jars")]
        [Cooldown(5)]
        public IEnumerator Jars()
        {
            const string path = "_GameCameras/CameraParent/tk2dCamera/SceneParticlesController/town_particle_set/Particle System";
            string[] enemies = { "roller", "aspid", "buzzer", "crystal", "petra", "drillbee", "angrybuzzer", "sword", "javelin" };
            AudioClip shatter_clip = Game.Clips.First(x => x.name == "globe_break_larger");
            Vector3 pos = HeroController.instance.transform.position;
            GameObject break_jar = ObjectLoader.InstantiableObjects["prefab_jar"];
            for (int i = -2; i <= 2; i++)
            {
                // Spawn the jar
                GameObject go = UObject.Instantiate
                (
                    ObjectLoader.InstantiableObjects["jar"],
                    pos + new Vector3(i * 7, 10, 0),
                    Quaternion.identity
                );
                go.AddComponent<CircleCollider2D>().radius = .3f;
                go.AddComponent<NonThunker>();
                go.AddComponent<Rigidbody2D>();
                go.AddComponent<DamageHero>().damageDealt = 1;
                go.AddComponent<AudioSource>();
                var ctrl = go.AddComponent<BetterSpawnJarControl>();
                var ps = GameObject.Find(path).GetComponent<ParticleSystem>();
                ctrl.Clip = shatter_clip;
                ctrl.ParticleBreak = break_jar.GetChild("Pt Glass L").GetComponent<ParticleSystem>();
                ctrl.ParticleBreakSouth = break_jar.GetChild("Pt Glass S").GetComponent<ParticleSystem>();
                ctrl.ReadyDust = ctrl.Trail = ps;
                ctrl.StrikeNailReaction = new GameObject();
                ctrl.EnemyPrefab = ObjectLoader.InstantiableObjects[enemies[URandom.Range(0, enemies.Length)]];
                ctrl.EnemyHP = 10;
                yield return new WaitForSeconds(0.1f);
                go.SetActive(true);
            }
        }

        [HKCommand("purevessel")]
        [Cooldown(5)]
        //Adapted from: HollowTwitch mod by Sid-003, fifty-six, and a2659802
        public void SpawnPureVessel()
        {
            // stolen from https://github.com/SalehAce1/PathOfPureVessel
            var (x, y, _) = HeroController.instance.gameObject.transform.position;
            GameObject pv = UObject.Instantiate
            (
                ObjectLoader.InstantiableObjects["pv"],
                HeroController.instance.gameObject.transform.position + new Vector3(0, 2.6f),
                Quaternion.identity
            );
            pv.GetComponent<HealthManager>().hp /= 4;
            pv.SetActive(true);
            RaycastHit2D castLeft = Physics2D.Raycast(new Vector2(x, y), Vector2.left, 1000, 1 << 8);
            RaycastHit2D castRight = Physics2D.Raycast(new Vector2(x, y), Vector2.right, 1000, 1 << 8);
            if (!castLeft)
                castLeft.distance = 30f;
            if (!castRight)
                castRight.distance = 30f;
            PlayMakerFSM control = pv.LocateMyFSM("Control");
            control.FsmVariables.FindFsmFloat("Left X").Value = x - castLeft.distance;
            control.FsmVariables.FindFsmFloat("Right X").Value = x + castRight.distance;
            control.FsmVariables.FindFsmFloat("TeleRange Max").Value = x - castLeft.distance;
            control.FsmVariables.FindFsmFloat("TeleRange Min").Value = x + castRight.distance;
            control.FsmVariables.FindFsmFloat("Plume Y").Value = y - 3.2f;
            control.FsmVariables.FindFsmFloat("Stun Land Y").Value = y + 3f;
            var plume_gen = control.GetState("Plume Gen");
            plume_gen.InsertMethod
            (
                3,
                () =>
                {
                    GameObject go = control.GetAction<SpawnObjectFromGlobalPool>("Plume Gen", 0).storeObject.Value;
                    PlayMakerFSM fsm = go.LocateMyFSM("FSM");
                    fsm.GetAction<FloatCompare>("Outside Arena?", 2).float2.Value = Mathf.Infinity;
                    fsm.GetAction<FloatCompare>("Outside Arena?", 3).float2.Value = -Mathf.Infinity;
                }
            );
            plume_gen.InsertMethod
            (
                5,
                () =>
                {
                    GameObject go = control.GetAction<SpawnObjectFromGlobalPool>("Plume Gen", 4).storeObject.Value;
                    PlayMakerFSM fsm = go.LocateMyFSM("FSM");
                    fsm.GetAction<FloatCompare>("Outside Arena?", 2).float2.Value = Mathf.Infinity;
                    fsm.GetAction<FloatCompare>("Outside Arena?", 3).float2.Value = -Mathf.Infinity;
                }
            );
            control.GetState("HUD Out").RemoveAction(0);
            var cp = pv.GetComponent<ConstrainPosition>();
            cp.xMax = x + castRight.distance;
            cp.xMin = x - castLeft.distance;
        }

        [HKCommand("revek")]
        [Summary("Revek attacks the player until hit once")]
        [Cooldown(30)]
        //Adapted from: HollowTwitch mod by Sid-003, fifty-six, and a2659802
        public IEnumerator Revek()
        {
            GameObject revek = UObject.Instantiate
            (
                ObjectLoader.InstantiableObjects["Revek"],
                HeroController.instance.gameObject.transform.position,
                Quaternion.identity
            );
            yield return new WaitForSecondsRealtime(1);
            UObject.DontDestroyOnLoad(revek);
            revek.SetActive(true);
            PlayMakerFSM ctrl = revek.LocateMyFSM("Control");
            // Make sure init gets to run.
            yield return null;
            // Actually spawn.
            ctrl.SetState("Appear Pause");
            // ReSharper disable once ImplicitlyCapturedClosure (ctrl)
            ctrl.GetState("Hit").AddMethod(() => UObject.Destroy(revek));
            // ReSharper disable once ImplicitlyCapturedClosure (ctrl)
            void OnUnload()
            {
                if (revek == null)
                    return;
                revek.SetActive(false);
            }
            void OnLoad(Scene a, Scene b)
            {
                try
                {
                    if (revek == null)
                        return;
                    revek.SetActive(true);
                    ctrl.SetState("Appear Pause");
                }
                catch
                {
                    UObject.Destroy(revek);
                }
            }
            GameManager.instance.UnloadingLevel += OnUnload;
            USceneManager.activeSceneChanged += OnLoad;
            yield return CoroutineUtil.WaitWithCancel(30);
            UObject.Destroy(revek);
            GameManager.instance.UnloadingLevel -= OnUnload;
            USceneManager.activeSceneChanged -= OnLoad;
        }

        //TODO: Remove collider so the player can't pogo off the shade
        //TODO: Fix spell control (shade currently does not use spells)
        //TODO: Speed up shade
        //TODO: Remove shade boundaries(?)
        //TODO: Honestly, just write a Nemesis mod and incorporate that
        [HKCommand("shadeBROKEN")]
        [Cooldown(5)]
        public void SpawnShade()
        {
            var go = new GameObject();
            MonoBehaviour runner = go.AddComponent<NonBouncer>();

            GameObject shadeGO = UObject.Instantiate(GameManager.instance.sm.hollowShadeObject);
            shadeGO.transform.position = HeroController.instance.transform.position + 3 * Vector3.left;
            PlayMakerFSM shadeFSM = shadeGO.LocateMyFSM("Shade Control");
            shadeGO.SetActive(true);

            bool usingFireball = false;
            bool usingQuake = false;
            bool usingScream = false;
            bool warping = false;
            bool fireballFacingRight = false;

            IEnumerator SpellControl()
            {
                shadeFSM.SetState("Retreat Start");
                yield return new WaitWhile(() => warping);
                if (usingFireball) shadeFSM.SetState("Cast Antic");
                if (usingQuake) shadeFSM.SetState("Quake Antic");
                if (usingScream) shadeFSM.SetState("Scream Antic");
                yield break;
            }

            //Remove jingle
            SFCoreFSM.RemoveFsmTransition(shadeGO.LocateMyFSM("Play Audio"), "Pause", "FINISHED");
            SFCoreFSM.RemoveFsmTransition(shadeGO.transform.Find("Music Control").gameObject.LocateMyFSM("Music Control"), "Init", "FINISHED");

            //Remove dreamnail cheese
            SFCoreFSM.RemoveFsmTransition(shadeGO.LocateMyFSM("Dreamnail Kill"), "Idle", "DREAM IMPACT");

            //Remove spell limit, increase hp, and set spell levels to max
            SFCoreFSM.InsertMethod(shadeFSM, "Init", () =>
            {
                shadeFSM.FsmVariables.FindFsmInt("SP").Value = int.MaxValue;
                shadeGO.GetComponent<HealthManager>().hp = int.MaxValue;
                shadeFSM.FsmVariables.FindFsmInt("Fireball Level").Value = 2;
                shadeFSM.FsmVariables.FindFsmInt("Quake Level").Value = 2;
                shadeFSM.FsmVariables.FindFsmInt("Scream Level").Value = 2;

            }, 25);

            //Remove roam limit
            shadeFSM.FsmVariables.FindFsmFloat("Max Roam").Value = 999f;

            //Make shade unfriendly
            SFCoreFSM.RemoveFsmAction(shadeFSM, "Friendly?", 2);

            //Decrease frequency of random attacks and make them slashes with a delay
            shadeFSM.GetAction<WaitRandom>("Fly", 5).timeMin = 5f;
            shadeFSM.GetAction<WaitRandom>("Fly", 5).timeMax = 5f;
            SFCoreFSM.InsertMethod(shadeFSM, "Quake?", () =>
            {
                shadeFSM.SetState("Slash Antic");
            }, 0);

            //Decrease flight speed
            shadeFSM.GetAction<ChaseObject>("Fly", 4).speedMax = 1.5f;
            shadeFSM.GetAction<ChaseObjectV2>("Fly", 6).speedMax = 1.5f;

            //Teleport to different location depending on spell
            SFCoreFSM.InsertMethod(shadeFSM, "Retreat Start", () =>
            {
                warping = true;
            }, 0);
            SFCoreFSM.InsertMethod(shadeFSM, "Retreat", () =>
            {
                Vector3 position = HeroController.instance.transform.position;
                if (usingFireball)
                {
                    fireballFacingRight = HeroController.instance.cState.facingRight;
                    if (fireballFacingRight) position += 3f * Vector3.left;
                    else position += 3f * Vector3.right;

                    if (!HeroController.instance.CheckTouchingGround()) position += 3f * Vector3.down;
                }
                else if (usingQuake)
                {
                    position += 6f * Vector3.up;
                }
                else if (usingScream)
                {
                    position += 6f * Vector3.down;
                }
                shadeFSM.FsmVariables.FindFsmVector3("Start Pos").Value = position;
            }, 1);
            SFCoreFSM.InsertMethod(shadeFSM, "Retreat Reset", () =>
            {
                warping = false;
            }, 3);

            //Make shade face player before using fireball
            SFCoreFSM.InsertFsmAction(shadeFSM, "Cast Antic", shadeFSM.GetAction<FaceObject>("Fireball Pos", 3), 0);

            //Increase warp speed
            shadeFSM.GetAction<iTweenMoveTo>("Retreat", 2).time = 0.05f;

            //Increase attack speed
            shadeFSM.GetAction<Wait>("Cast Antic", 8).time = 0f;
            shadeFSM.GetAction<Wait>("Quake Antic", 7).time = 0.45f;
            shadeFSM.GetAction<Wait>("Scream Antic", 6).time = 0f;

            //Reset variables and position after using a spell
            SFCoreFSM.InsertMethod(shadeFSM, "Cast", () =>
            {
                usingFireball = false;
                RaycastHit2D raycastHit2D = Physics2D.Raycast(shadeGO.transform.position,
                    fireballFacingRight ? Vector3.right : Vector3.left, 3f, 256);
                if (raycastHit2D.collider != null)
                {
                    shadeFSM.SetState("Retreat Start");
                }
            }, 4);
            SFCoreFSM.InsertMethod(shadeFSM, "Land", () =>
            {
                usingQuake = false;
                shadeFSM.SetState("Retreat Start");
            }, 11);
            SFCoreFSM.InsertMethod(shadeFSM, "Scream Recover", () =>
            {
                usingScream = false;
                RaycastHit2D raycastHit2D = Physics2D.Raycast(shadeGO.transform.position, Vector3.up, 6f, 256);
                if (raycastHit2D.collider != null)
                {
                    shadeFSM.SetState("Retreat Start");
                }
            }, 2);

            //Detect when player uses a spell
            PlayMakerFSM spellFSM = HeroController.instance.spellControl;
            SFCoreFSM.InsertMethod(spellFSM, "Wallside?", () =>
            {
                if (usingFireball || usingQuake || usingScream || warping) return;
                usingFireball = true;
                runner.StartCoroutine(SpellControl());
            }, 0);
            SFCoreFSM.InsertMethod(spellFSM, "On Ground?", () =>
            {
                if (usingFireball || usingQuake || usingScream || warping) return;
                usingQuake = true;
                runner.StartCoroutine(SpellControl());
            }, 0);
            SFCoreFSM.InsertMethod(spellFSM, "Scream Get?", () =>
            {
                if (usingFireball || usingQuake || usingScream || warping) return;
                usingScream = true;
                runner.StartCoroutine(SpellControl());
            }, 0);

            runner.StopAllCoroutines();

            SFCoreFSM.RemoveFsmAction(spellFSM, "Wallside?", 0);
            SFCoreFSM.RemoveFsmAction(spellFSM, "On Ground?", 0);
            SFCoreFSM.RemoveFsmAction(spellFSM, "Scream Get?", 0);
        }

        /*{
        GameObject shade = UObject.Instantiate(GameManager.instance.sm.hollowShadeObject, HeroController.instance.transform.position, Quaternion.identity);
        shade.GetComponent<HealthManager>().IsInvincible = true;
    }*/

        [HKCommand("zap")]
        [Cooldown(15)]
        public IEnumerator StartZapping()
        {
            GameObject prefab = ObjectLoader.InstantiableObjects["zap"];
            for (int i = 0; i < 12; i++)
            {
                GameObject zap = UObject.Instantiate(prefab, HeroController.instance.transform.position, Quaternion.identity);
                zap.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }

        [HKCommand("grimmchild")]
        [Summary("Spawns a Grimmchild that attacks the player")]
        [Cooldown(15)]
        //Adapted from: Challenge Mode mod by Hoo-Knows
        public IEnumerator SpawnGrimmchild()
        {
            PlayMakerFSM spawnFSM = HeroController.instance.transform.Find("Charm Effects").gameObject.LocateMyFSM("Spawn Grimmchild");
            if (spawnFSM.FsmVariables.FindFsmGameObject("Child").Value == null) spawnFSM.SetState("Spawn");

            GameObject grimmchildGO = spawnFSM.FsmVariables.FindFsmGameObject("Child").Value;
            PlayMakerFSM grimmchildFSM = grimmchildGO.LocateMyFSM("Control");

            //Set level to 4
            grimmchildFSM.SetState("4");

            //Decrease flameball speed
            grimmchildFSM.FsmVariables.FindFsmFloat("Flameball Speed").Value = 10f;

            //Stay farther away from player
            grimmchildFSM.GetAction<DistanceFlySmooth>("Follow", 11).targetRadius = 6f;

            //Make teleports less frequent
            grimmchildFSM.GetAction<FloatCompare>("Follow", 17).float2 = 10f;

            //Target player
            SFCoreFSM.InsertMethod(grimmchildFSM, "Check For Target", () =>
            {
                grimmchildFSM.FsmVariables.FindFsmGameObject("Target").Value = HeroController.instance.gameObject;
            }, 2);

            //Prevent shooting if player is stunned
            SFCoreFSM.InsertMethod(grimmchildFSM, "Check For Target", () =>
            {
                if (HeroController.instance.controlReqlinquished)
                    grimmchildFSM.SendEvent("NO TARGET");
            }, 0);

            //Make grimmball do damage
            SFCoreFSM.InsertMethod(grimmchildFSM, "Shoot", () =>
            {
                GameObject grimmballGO = grimmchildFSM.FsmVariables.FindFsmGameObject("Flameball").Value;
                grimmballGO.layer = (int)PhysLayers.ENEMY_ATTACK;
                grimmballGO.AddComponent<DamageHero>();
                grimmballGO.GetComponent<DamageHero>().damageDealt = 1;
                grimmballGO.GetComponent<DamageHero>().hazardType = 1;
                grimmballGO.GetComponent<Rigidbody2D>().gravityScale = 0f;

                //Prevent grimmball from doing damage after impact
                PlayMakerFSM grimmballFSM = grimmballGO.LocateMyFSM("Control");
                SFCoreFSM.InsertFsmAction(grimmballFSM, "Impact", grimmballFSM.GetAction<SetCircleCollider>("Shrink", 3), 0);
            }, 10);

            yield return CoroutineUtil.WaitWithCancel(15f);
            
            UObject.Destroy(grimmchildGO);
        }

        [HKCommand("aspidrancher")]
        [Summary("Spawn a primal aspid for every missed nailswing")]
        [Cooldown(15)]
        //Adapted from: Challenge Mode mod by Hoo-Knows
        public IEnumerator AspidRancher()
        {
            var go = new GameObject();
            UObject.DontDestroyOnLoad(go);
            MonoBehaviour runner = go.AddComponent<NonBouncer>();

            GameObject aspidGO = SpawnEnemyGeneric("aspid");
            HealthManager hm = aspidGO.GetComponent<HealthManager>();
            hm.SetGeoLarge(0);
            hm.SetGeoMedium(0);
            hm.SetGeoSmall(0);
            hm.hp = 1;
            //Disable soul gain
            ReflectionHelper.SetField(hm, "enemyType", 6);

            bool spawnFlag = true;
            bool waiting = false;

            void TrySpawnAspid()
            {
                if (!spawnFlag || HeroController.instance.cState.nailCharging) return;

                GameObject aspid = UObject.Instantiate(aspidGO);
                foreach (PlayMakerFSM fsm in aspid.GetComponentsInChildren<PlayMakerFSM>())
                {
                    fsm.SetState(fsm.Fsm.StartState);
                }
                aspid.transform.position = HeroController.instance.transform.position;
                aspid.transform.position += 4 * (HeroController.instance.cState.facingRight ? Vector3.left : Vector3.right);
                aspid.SetActive(true);
            }

            IEnumerator WaitToSpawn()
            {
                waiting = true;
                yield return new WaitWhile(() => HeroController.instance.cState.attacking);
                waiting = false;

                TrySpawnAspid();
                yield break;
            }

            void AfterAttackHook(AttackDirection dir)
            {
                if (waiting)
                {
                    runner.StopAllCoroutines();
                    TrySpawnAspid();
                }
                spawnFlag = true;
                runner.StartCoroutine(WaitToSpawn());
            }

            void SlashHitHook(Collider2D otherCollider, GameObject slash)
            {
                GameObject go = otherCollider.gameObject;
                if (go.GetComponent<HealthManager>() != null || go.GetComponent<DamageHero>() != null)
                {
                    spawnFlag = false;
                }
            }

            ModHooks.AfterAttackHook += AfterAttackHook;
            ModHooks.SlashHitHook += SlashHitHook;
            yield return CoroutineUtil.WaitWithCancel(15f);
            ModHooks.AfterAttackHook -= AfterAttackHook;
            ModHooks.SlashHitHook -= SlashHitHook;
            runner.StopAllCoroutines();
            UObject.Destroy(go);
        }
    }
}