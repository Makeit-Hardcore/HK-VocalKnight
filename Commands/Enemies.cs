using System.Linq;
using System.Collections;
using VocalKnight.Components;
using VocalKnight.Entities.Attributes;
using VocalKnight.Extensions;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Vasi;

namespace VocalKnight.Commands
{
    public class Enemies
    {
        [HKCommand("enemy")]
        [Cooldown(2)]
        public  void SpawnEnemy(string name)
        {
            Logger.Log($"Trying to spawn enemy {name}");
            if (!ObjectLoader.InstantiableObjects.TryGetValue(name, out GameObject go))
            {
                Logger.LogError("Could not get GameObject " + name);
                return;
            }
            Vector3 position = HeroController.instance.gameObject.transform.position;
            position.y += 5;
            GameObject enemy = Object.Instantiate(go, position, Quaternion.identity);
            GameObject.Destroy(enemy.GetComponent<PersistentBoolItem>());
            enemy.SetActive(true);
        }

        [HKCommand("jars")]
        [Cooldown(5)]
        public  IEnumerator Jars()
        {
            const string path = "_GameCameras/CameraParent/tk2dCamera/SceneParticlesController/town_particle_set/Particle System";
            string[] enemies = {"roller", "aspid", "buzzer", "crystal", "petra", "drillbee", "angrybuzzer", "sword", "javelin"};
            AudioClip shatter_clip = Game.Clips.First(x => x.name == "globe_break_larger");
            Vector3 pos = HeroController.instance.transform.position;
            GameObject break_jar = ObjectLoader.InstantiableObjects["prefab_jar"];
            for (int i = -2; i <= 2; i++)
            {
                // Spawn the jar
                GameObject go = Object.Instantiate
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
                ctrl.EnemyPrefab = ObjectLoader.InstantiableObjects[enemies[Random.Range(0, enemies.Length)]];
                ctrl.EnemyHP = 10;
                yield return new WaitForSeconds(0.1f);
                go.SetActive(true);
            }
        }

        [HKCommand("purevessel")]
        [Cooldown(5)]        
        public  void SpawnPureVessel()
        {
            // stolen from https://github.com/SalehAce1/PathOfPureVessel
            var (x, y, _) = HeroController.instance.gameObject.transform.position;
            GameObject pv = Object.Instantiate
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
        [Cooldown(30)]
        public  IEnumerator Revek()
        {
            GameObject revek = Object.Instantiate
            (
                ObjectLoader.InstantiableObjects["Revek"],
                HeroController.instance.gameObject.transform.position,
                Quaternion.identity
            );
            yield return new WaitForSecondsRealtime(1);
            Object.DontDestroyOnLoad(revek);
            revek.SetActive(true);
            PlayMakerFSM ctrl = revek.LocateMyFSM("Control");
            // Make sure init gets to run.
            yield return null;
            // Actually spawn.
            ctrl.SetState("Appear Pause");
            // ReSharper disable once ImplicitlyCapturedClosure (ctrl)
            ctrl.GetState("Hit").AddMethod(() => Object.Destroy(revek));
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
                    Object.Destroy(revek);
                }
            }
            GameManager.instance.UnloadingLevel += OnUnload;
            USceneManager.activeSceneChanged += OnLoad;
            yield return CoroutineUtil.WaitWithCancel(30);
            Object.Destroy(revek);
            GameManager.instance.UnloadingLevel -= OnUnload;
            USceneManager.activeSceneChanged -= OnLoad;
        }

        [HKCommand("shade")]
        [Cooldown(5)]
        public  void SpawnShade()
        {
            Object.Instantiate(GameManager.instance.sm.hollowShadeObject, HeroController.instance.transform.position, Quaternion.identity);
        }

        [HKCommand("zap")]
        [Cooldown(15)]
        public IEnumerator StartZapping()
        {
            GameObject prefab = ObjectLoader.InstantiableObjects["zap"];
            for (int i = 0; i < 12; i++)
            {
                GameObject zap = Object.Instantiate(prefab, HeroController.instance.transform.position, Quaternion.identity);
                zap.SetActive(true);
                yield return new WaitForSeconds(0.5f);
            }
        }
        
    }
}