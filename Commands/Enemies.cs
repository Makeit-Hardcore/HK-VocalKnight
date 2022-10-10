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
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Vasi;

namespace VocalKnight.Commands
{
    public class Enemies
    {
        [HKCommand("enemy")]
        [Cooldown(2)]
        [Summary("Spawns a generic enemy from a preset list")]
        public void SpawnEnemy(string name)
        {
            GameObject enemy = SpawnEnemyGeneric(name);

            if (enemy != null)
                enemy.SetActive(true);
        }

        [HKCommand("marmu")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Spawns Ghost Warrior Marmu")]
        public void SpawnMarmu()
        {
            GameObject enemy = SpawnEnemyGeneric("marmu");

            if (enemy != null)
                enemy.SetActive(true);
        }

        [HKCommand("xero")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Spawns Ghost Warrior Xero")]
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
        [Cooldown(17)] //This number is specific such that Gorb is destroyed when he's naturally inactive
        [Summary("Spawns Ghost Warrior Gorb")]
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

        public static GameObject SpawnEnemyGeneric(string name)
        {
            if (!ObjectLoader.InstantiableObjects.TryGetValue(name, out GameObject go))
            {
                Logger.LogError("Could not get GameObject " + name);
                return null;
            }
            Vector3 position = HeroController.instance.gameObject.transform.position;
            position.y += 5;
            if (HeroController.instance.cState.facingRight)
                position.x += 5;
            else
                position.x -= 5;
            GameObject enemy = UObject.Instantiate(go, position, Quaternion.identity);

            //For enemies that do not typically respawn until bench
            UObject.Destroy(enemy.GetComponent<PersistentBoolItem>());

            //Reduce enemy health
            enemy.GetComponent<HealthManager>().hp /= 4;

            //Killing a spawned ghost warrior does not count as defeating the real deal
            if (enemy.name.Contains("Ghost Warrior"))
                enemy.LocateMyFSM("Set Ghost PD Int").GetState("Set").RemoveAllOfType<SetPlayerDataInt>();

            return enemy;
        }

        [HKCommand("jars")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Spawns Collector's jars that contain various generic enemies")]
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
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Spawns Pure Vessel")]
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
        [Summary("Revek attacks the player until he is hit")]
        [Cooldown(CommonVars.cldn * 2)]
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

        [HKCommand("zap")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Electric shocks follow the player")]
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

        [HKCommand("nightmare")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Summons NKG to perform a random attack")]
        public void SummonNKG()
        {
            string StartState;
            int attack = 3; //Currently only flame pillars is working properly
            Logger.Log("Performing attack: " + attack);
            switch (attack)
            {
                case 1: //Dive Dash
                    Logger.Log("Performing NKG: Dive Dash");
                    StartState = "AD Pos";
                    break;
                case 2: //Fire Bats
                    Logger.Log("Performing NKG: Fire Bats");
                    StartState = "FB Hero Pos";
                    break;
                case 3: //Flame Pillar
                    Logger.Log("Performing NKG: Flame Pillar");
                    StartState = "Pillar Pos";
                    break;
                case 4: //Uppercut
                    Logger.Log("Performing NKG: Uppercut");
                    StartState = "Slash Tele In";
                    break;
                default:
                    return;
            }

            GameObject Grimm = SpawnEnemyGeneric("NKG");
            Grimm.SetActive(true);
            PlayMakerFSM ctrl = Grimm.LocateMyFSM("Control");
            Grimm.gameObject.layer = 31; //set to a layer that isnt in GlobalEnums.PhysLayers to avoid collision

            //we will use this as an "idle" state
            FsmState WaitingForAttackState = SFCoreFSM.CopyFsmState(ctrl, "Dormant", "WaitingForAttack");
            SFCoreFSM.ChangeFsmTransition(WaitingForAttackState, "WAKE", StartState);
            SFCoreFSM.ChangeFsmTransition(ctrl.GetState("Tele Out"), "FINISHED", "WaitingForAttack");
            if (attack == 4)
            {
                SFCoreFSM.ChangeFsmTransition(ctrl.GetState("Explode Pause"), "FINISHED", "WaitingForAttacK");
                SFCoreFSM.ChangeFsmTransition(ctrl.GetState("After Evade"), "FIREBATS", "Slash Antic");
                SFCoreFSM.ChangeFsmTransition(ctrl.GetState("Auto Evade?"), "EVADE", "Slash Antic");
                SFCoreFSM.AddFsmTransition(ctrl.GetState("Dormant"), "FINISHED", "Tele Out");
                ctrl.GetAction<FloatCompare>("After Evade").greaterThan = ctrl.FsmEvents.First(trans => trans.Name == "SLASH");
            }

            UObject.DestroyImmediate(Grimm.LocateMyFSM("constrain_x"));
            UObject.DestroyImmediate(Grimm.LocateMyFSM("Constrain Y"));
            UObject.DestroyImmediate(Grimm.LocateMyFSM("Stun"));

            Grimm.GetComponent<HealthManager>().hp = Int32.MaxValue;
            Grimm.GetComponent<HealthManager>().hp = Int32.MaxValue;

            var pos = HeroController.instance.transform.position;

            switch (attack)
            {
                case 1:
                    ctrl.FsmVariables.FindFsmFloat("Ground Y").Value = pos.y + 2f;
                    ctrl.FsmVariables.FindFsmFloat("AD Min X").Value = pos.x - 100f;
                    ctrl.FsmVariables.FindFsmFloat("AD Max X").Value = pos.x + 100f;
                    ctrl.GetState("AD Tele In").GetAction<SetPosition>().y = pos.y + 10f;
                    break;
                case 2:
                    ctrl.GetState("FB Hero Pos").GetAction<FloatCompare>().float2.Value =
                        HeroController.instance.transform.position.x + (URandom.value <= 0.5 ? 10f : -10f);
                    ctrl.FsmVariables.FindFsmFloat("Ground Y").Value = pos.y + 2;
                    var teleL = ctrl.GetState("FB Tele L");
                    teleL.GetAction<RandomFloat>().min = pos.x - 7f;
                    teleL.GetAction<RandomFloat>().max = pos.x - 7f;
                    var teleR = ctrl.GetState("FB Tele R");
                    teleR.GetAction<RandomFloat>().min = pos.x + 7f;
                    teleR.GetAction<RandomFloat>().max = pos.x + 7f;
                    break;
                case 3:
                    ctrl.FsmVariables.FindFsmFloat("Ground Y").Value = pos.y + 2f;
                    ctrl.FsmVariables.FindFsmFloat("Min X").Value = pos.x - 999f;
                    ctrl.FsmVariables.FindFsmFloat("Max X").Value = pos.x + 999f;
                    ctrl.GetState("Pillar Tele In").GetAction<SetPosition>().y = pos.y + 7f;
                    ctrl.GetState("Pillar").GetAction<SetPosition>().y = pos.y - 1;
                    break;
                case 4:
                    float dir = HeroController.instance.transform.localScale.x;
                    float num = 7f;
                    pos += new Vector3(dir * num, 2f, 0f);
                    ctrl.FsmVariables.FindFsmFloat("Tele X").Value = pos.x;
                    ctrl.FsmVariables.FindFsmFloat("Ground Y").Value = pos.y;
                    ctrl.GetState("UP Explode").GetAction<SetPosition>().y.Value = pos.y + 7f;
                    ctrl.GetAction<FloatCompare>("Uppercut Up", 7).float2.Value = pos.y + 7f;
                    break;
                default:
                    break;
            }

            var go = new GameObject();
            MonoBehaviour runner = go.AddComponent<NonBouncer>();
            runner.StartCoroutine(NKGWaitToDestroy(Grimm, ctrl));

            ctrl.SetState(StartState);
        }

        private IEnumerator NKGWaitToDestroy(GameObject grimm, PlayMakerFSM ctrl)
        {
            yield return new WaitUntil(() => ctrl.ActiveStateName == "WaitingForAttack");
            UObject.Destroy(grimm);
        }

        [HKCommand("grimmchild")]
        [Summary("Spawns a Grimmchild that attacks The Knight")]
        [Cooldown(CommonVars.cldn * 2)]
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

            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn * 2);
            
            UObject.Destroy(grimmchildGO);
        }

        [HKCommand("aspidrancher")]
        [Summary("Spawns one primal aspid for every missed nailswing")]
        [Cooldown(CommonVars.cldn * 2)]
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
            yield return CoroutineUtil.WaitWithCancel(CommonVars.cldn * 2);
            ModHooks.AfterAttackHook -= AfterAttackHook;
            ModHooks.SlashHitHook -= SlashHitHook;
            runner.StopAllCoroutines();
            UObject.Destroy(go);
        }

        [HKCommand("sheo")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Summons Paintmaster Sheo to perform an attack")]
        public void Sheo(string color)
        {
            string StartState;
            switch (color)
            {
                case "red":
                    StartState = "JumpSlash1";
                    break;
                case "purple":
                    StartState = "GSlash Charge";
                    break;
                case "blue":
                    StartState = "Slash Antic";
                    break;
                case "yellow":
                    StartState = "Stab Antic";
                    break;
                default:
                    return;
            }

            GameObject Sheo = SpawnEnemyGeneric("sheo");
            Sheo.gameObject.layer = 31;
            Sheo.SetActive(true);

            PlayMakerFSM nailmaster_sheo = Sheo.LocateMyFSM(nameof(nailmaster_sheo));

            nailmaster_sheo.GetState("Set Paint HP").ClearTransitions();

            var Idle = nailmaster_sheo.GetState("Idle");
            Idle.Actions = new FsmStateAction[]
            {
            Idle.GetAction<Tk2dPlayAnimation>()
            };
            Idle.ClearTransitions();

            Idle.AddMethod(() =>
            {
                Sheo.GetComponent<MeshRenderer>().enabled = false;
                Sheo.GetComponent<BoxCollider2D>().enabled = false;
                Sheo.transform.position = new Vector2(Mathf.Infinity, Mathf.Infinity);
            });

            if (color == "red")
            {
                nailmaster_sheo.GetState(StartState).InsertMethod(0, () =>
                {
                    Sheo.SetActive(true);
                    Sheo.GetComponent<MeshRenderer>().enabled = true;
                    Sheo.GetComponent<BoxCollider2D>().enabled = true;
                    var pos = HeroController.instance.transform.position;
                    Sheo.transform.position = new Vector3(pos.x, pos.y + 10, Sheo.transform.position.z);
                    Sheo.GetComponent<Rigidbody2D>().gravityScale = 0;
                });

                nailmaster_sheo.GetState("Dstab").AddAction(new RunEveryFrame()
                {
                    MethodToRun = () =>
                    {
                        if (Sheo.transform.position.y < HeroController.instance.transform.position.y + 2f)
                        {
                            nailmaster_sheo.SetState("Dstab Land");
                            nailmaster_sheo.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                        }
                    }
                });

                nailmaster_sheo.GetState("Dstab").GetAction<SetVelocity2d>().y = -60f;
            } else
            {
                nailmaster_sheo.GetState(StartState).InsertMethod(0, () =>
                {
                    Sheo.SetActive(true);
                    Sheo.GetComponent<MeshRenderer>().enabled = true;
                    Sheo.GetComponent<BoxCollider2D>().enabled = true;
                    float moveAmount = 12;
                    var pos = HeroController.instance.transform.position;
                    float posadder = 0;
                    if (HeroController.instance.move_input == 0)
                        posadder += URandom.value < 0.5 ? -moveAmount : moveAmount;
                    else if (Math.Abs(HeroController.instance.move_input - 1) < Mathf.Epsilon)
                        posadder += moveAmount;
                    else if (Math.Abs(HeroController.instance.move_input + 1) < Mathf.Epsilon)
                        posadder += -moveAmount;
                    Sheo.transform.position = pos + new Vector3(posadder, 1, 0);
                });
            }

            Sheo.GetComponent<MeshRenderer>().enabled = false;
            Sheo.GetComponent<BoxCollider2D>().enabled = false;
            Sheo.GetComponent<HealthManager>().hp = Int32.MaxValue;
            nailmaster_sheo.SetState("Init");

            if (color == "red")
            {
                nailmaster_sheo.GetState("JumpSlash1").GetAction<SetPosition>().y = HeroController.instance.transform.position.y + 10;
                nailmaster_sheo.FsmVariables.FindFsmFloat("Topslash Y").Value =
                    HeroController.instance.transform.position.y + 10;
            }

            nailmaster_sheo.SetState(StartState);
        }
    }

    public class RunEveryFrame : FsmStateAction
    {
        public Action MethodToRun;

        public override void OnUpdate()
        {
            MethodToRun();
        }
    }
}