using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using VocalKnight.Entities.Attributes;
using VocalKnight.Precondition;
using VocalKnight.Utils;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UObject = UnityEngine.GameObject;
using URandom = UnityEngine.Random;
using Satchel;

namespace VocalKnight.Commands
{
    public class Area
    {
        [HKCommand("bees")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Hive Knight's bee storm attack")]
        public IEnumerator Bees()
        {
            Vector3 pos;
            RaycastHit2D floorHit;

            for (int i = 0; i < 25; i++)
            {
                pos = HeroController.instance.transform.position;
                floorHit = Physics2D.Raycast(pos, Vector2.down, 500, 1 << 8);
                if (floorHit && floorHit.point.y < pos.y)
                    pos = floorHit.point;
                if (HeroController.instance.cState.facingRight) pos.x += 10;
                else pos.x -= 10;

                GameObject bee = UObject.Instantiate
                (
                    ObjectLoader.InstantiableObjects["bee"],
                    Vector3.zero,
                    Quaternion.Euler(0, 0, 180)
                );

                bee.SetActive(true);

                PlayMakerFSM ctrl = bee.LocateMyFSM("Control");

                // Set reset vars so they recycle properly
                ctrl.Fsm.GetFsmFloat("X Left").Value = pos.x - 10;
                ctrl.Fsm.GetFsmFloat("X Right").Value = pos.x + 10;
                ctrl.Fsm.GetFsmFloat("Start Y").Value = pos.y + 17 + URandom.Range(-3f, 3f);

                // Despawn y
                ctrl.GetAction<FloatCompare>("Swarm", 3).float2.Value = pos.y - 5f;
                
                // Recycle after going oob
                //ctrl.ChangeTransition("Reset", "FINISHED", "Pause");
                
                // Start the swarming
                ctrl.SendEvent("SWARM");
                yield return CoroutineUtil.WaitWithCancel(0.2f);
            }
        }

        [HKCommand("belfly")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Spawns a couple of the boom bats")]
        public IEnumerator Belflies()
        {
            Vector3 pos;

            for (int i = 0; i < 5; i++)
            {
                pos = HeroController.instance.transform.position;
                if (HeroController.instance.cState.facingRight) pos.x += 5;
                else pos.x -= 5;
                pos.y += 5;

                if (!ObjectLoader.InstantiableObjects.TryGetValue("belfly", out GameObject go))
                {
                    Logger.LogError("Could not get GameObject " + "belfly");
                    yield break;
                }
                GameObject fly = UObject.Instantiate(go, pos, Quaternion.identity);
                GameObject.Destroy(fly.GetComponent<PersistentBoolItem>());
                fly.SetActive(true);

                PlayMakerFSM ctrl = fly.LocateMyFSM("Ceiling Dropper");
                ctrl.FsmVariables.GetFsmBool("Alert Range").Value = true;

                yield return CoroutineUtil.WaitWithCancel(0.8f);
            }
        }

        [HKCommand("lasers")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Summons Crystal Peak lasers")]
        public void Lasers()
        {
            Vector3 pos = HeroController.instance.transform.position;

            RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 500, 1 << 8);

            // Take the minimum so that we go from the floor
            if (hit && hit.point.y < pos.y)
            {
                pos = hit.point;
            }

            const float MAX_ADD = 10;

            for (int i = -10; i <= 10; i++)
            {
                Vector3 turret_pos = pos + new Vector3(i * 2, MAX_ADD, 0);

                RaycastHit2D up = Physics2D.Raycast(pos, (turret_pos - pos).normalized, 500, 1 << 8);

                // If the ceiling is above where we're going to spawn, put it right beneath the ceiling.
                if (up.point.y > pos.y + 10)
                {
                    turret_pos = up.point + new Vector2(0, -0.5f);
                }

                GameObject turret = UObject.Instantiate
                (
                    ObjectLoader.InstantiableObjects["Laser Turret"],
                    turret_pos,
                    Quaternion.Euler(0, 0, 180 + URandom.Range(-30f, 30f))
                );

                turret.LocateMyFSM("Laser Bug").GetState("Init").AddAction
                (
                    new WaitRandom
                    {
                        timeMax = .75f,
                        timeMin = 0
                    }
                );

                turret.SetActive(true);
            }
        }

        [HKCommand("jelly")]
        [Cooldown(CommonVars.cldn * 2)]
        [Summary("Fills the room with jellies")]
        public void SpawnJellies()
        {
            CameraController cam = GameCameras.instance.transform.Find("CameraParent/tk2dCamera").gameObject.GetComponent<CameraController>();
            float buffer = 5f;
            float jellyBuffer = 10f;
            bool badPosFlag = false;
            int maxJellies = (int)Math.Ceiling(cam.sceneWidth * cam.sceneHeight / 400);

            PolygonCollider2D[] polyTiles = ConvertEdge2Poly(cam);

            List<Vector2> jelliesPos = new List<Vector2>();

            Logger.Log("Spawning " + maxJellies + " jellies");
            for (int j = 0; j < maxJellies; j++)
            {
                Vector2 spawnPos = new Vector2();

                for (int spawnAttempt = 0; spawnAttempt <= 50; spawnAttempt++)
                {
                    //We tried too many times to place the jelly, just give up
                    if (spawnAttempt == 50)
                    {
                        Logger.LogWarn("Failed to place jelly " + j + " out of " + maxJellies);
                        return;
                    }

                    badPosFlag = false;
                    spawnPos = new Vector2(URandom.Range(buffer, cam.sceneWidth - buffer), URandom.Range(buffer, cam.sceneHeight - buffer));
                    
                    //Don't let jellies spawn too close to one another
                    foreach (Vector2 jellyPos in jelliesPos)
                    {
                        if (Vector2.Distance(spawnPos, jellyPos) < jellyBuffer)
                        {
                            badPosFlag = true;
                            break;
                        }
                    }

                    if (badPosFlag) continue;

                    //Don't let jellies spawn inside walls or platforms
                    foreach (PolygonCollider2D tile in polyTiles)
                    {
                        if (tile.OverlapPoint(spawnPos))
                        {
                            badPosFlag = true;
                            break;
                        }
                    }

                    if (badPosFlag) continue;

                    break;
                }
                
                jelliesPos.Add(spawnPos);
                GameObject jelly = Enemies.SpawnEnemyGeneric("jellyfish");
                jelly.transform.position = spawnPos;
                jelly.SetActive(true);
            }
        }

        [HKCommand("grub")]
        [Cooldown(CommonVars.cldn)]
        [Summary("Fireb0rn's worst nightmare")]
        public void Grubs()
        {
            CameraController cam = GameCameras.instance.transform.Find("CameraParent/tk2dCamera").gameObject.GetComponent<CameraController>();
            float xDelta = 5f;

            PolygonCollider2D[] polyTiles = ConvertEdge2Poly(cam);

            List<Vector2> grubsPos = new List<Vector2>();

            Logger.Log("Spawning grubs");

            float xCentral = 0f;
            while (xCentral < cam.sceneWidth)
            {
                float y = cam.sceneHeight;
                while (y > 0f)
                {
                    bool badPosFlag = false;
                    float x = xCentral + URandom.Range(-1.5f, 1.5f);

                    RaycastHit2D floor = Physics2D.Raycast(new Vector2(x, y), Vector2.down, y, 1 << 8);
                    if (floor.collider == null)
                        break;
                    
                    Vector2 spawnPos = floor.point + new Vector2(0f, 1.35f);

                    foreach (PolygonCollider2D tile in polyTiles)
                    {
                        if (tile.OverlapPoint(spawnPos))
                            badPosFlag = true;
                    }
                    if (!badPosFlag)
                    {
                        ObjectLoader.InstantiableObjects.TryGetValue("grub", out GameObject go);
                        GameObject grub = UObject.Instantiate(go, spawnPos, Quaternion.identity);
                        UObject.Destroy(grub.GetComponent<PersistentBoolItem>());
                        PlayMakerFSM grubFSM = grub.LocateMyFSM("Bottle Control");
                        SFCore.Utils.FsmUtil.RemoveFsmAction(grubFSM, "Shatter", 2);
                        SFCore.Utils.FsmUtil.RemoveFsmAction(grubFSM, "Shatter", 1);
                        grub.SetActive(true);
                    }

                    y = floor.point.y - 2f;
                }

                xCentral += URandom.Range(4f, 8f);
            }
        }

        private PolygonCollider2D[] ConvertEdge2Poly(CameraController cam)
        {
            EdgeCollider2D[] tiles = cam.tilemap.renderData.transform.GetChild(0).GetComponentsInChildren<EdgeCollider2D>();
            PolygonCollider2D[] polyTiles = new PolygonCollider2D[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                Vector2[] vertices = tiles[i].points;
                polyTiles[i] = tiles[i].gameObject.AddComponent<PolygonCollider2D>();
                polyTiles[i].points = vertices;
                polyTiles[i].pathCount = 1;
                polyTiles[i].SetPath(0, vertices);
                UObject.Destroy(tiles[i]);
            }
            return polyTiles;
        }

        [HKCommand("spikefloor")]
        [Summary("Spawns NKG spikes from the floor")]
        [Cooldown(CommonVars.cldn * 2)]
        public IEnumerator SpikeFloor()
        {
            Vector3 hero_pos = HeroController.instance.transform.position;

            var audio_player = new GameObject().AddComponent<AudioSource>();

            audio_player.volume = GameManager.instance.GetImplicitCinematicVolume();

            var spike_fsms = new List<PlayMakerFSM>();

            const float SPACING = 2.5f;
 
            for (int i = -8; i <= 8; i++)
            {
                GameObject spike = UObject.Instantiate(ObjectLoader.InstantiableObjects["nkgspike"]);

                spike.SetActive(true);

                Vector3 pos = hero_pos + new Vector3(i * SPACING, 0);
                
                RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.down, 500, 1 << 8);
                
                pos.y -= hit ? hit.distance : 0;
                
                spike.transform.position = pos;

                PlayMakerFSM ctrl = spike.LocateMyFSM("Control");
                
                spike_fsms.Add(ctrl);
                
                ctrl.SendEvent("SPIKES READY");
            }

            audio_player.PlayOneShot(Game.Clips.FirstOrDefault(x => x.name == "grimm_spikes_pt_1_grounded"));

            yield return new WaitForSeconds(0.55f);

            foreach (PlayMakerFSM spike in spike_fsms)
            {
                spike.SendEvent("SPIKES UP");
            }
            
            yield return new WaitForSeconds(0.15f);
            
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            
            audio_player.PlayOneShot(Game.Clips.FirstOrDefault(x => x.name == "grimm_spikes_pt_2_shoot_up"));
            
            yield return new WaitForSeconds(0.45f);
            
            foreach (PlayMakerFSM spike in spike_fsms)
            {
                spike.SendEvent("SPIKES DOWN");
            }
            
            audio_player.PlayOneShot(Game.Clips.FirstOrDefault(x => x.name == "grimm_spikes_pt_3_shrivel_back"));
            
            yield return new WaitForSeconds(0.5f);

            foreach (GameObject go in spike_fsms.Select(x => x.gameObject))
            {
                UObject.Destroy(go);
            }
        }

        [HKCommand("radiance")]
        [Cooldown(1)]
        [Summary("Spawns a set of Radiance orbs")]
        public IEnumerator SpawnAbsOrb()
        {
            if (HeroController.instance == null)
                yield break;

            GameObject orbgroup = ObjectLoader.InstantiableObjects["AbsOrb"]; // get an go contains orb and it's effect

            GameObject orbPre = orbgroup.transform.Find("Radiant Orb").gameObject;
            
            GameObject ShotCharge_Pre = orbgroup.transform.Find("Shot Charge").gameObject; //get charge effect
            GameObject ShotCharge2_Pre = orbgroup.transform.Find("Shot Charge 2").gameObject;
            
            GameObject ShotCharge = UObject.Instantiate(ShotCharge_Pre);
            GameObject ShotCharge2 = UObject.Instantiate(ShotCharge2_Pre);


            for (int i = 0; i < 2; i++)
            {
                float x = HeroController.instance.transform.position.x + URandom.Range(-7f, 8f);
                float y = HeroController.instance.transform.position.y + URandom.Range(4f, 8f);
                var spawnPoint = new Vector3(x, y);

                ShotCharge.transform.position = spawnPoint;
                ShotCharge2.transform.position = spawnPoint;

                ShotCharge.SetActive(true);
                ShotCharge2.SetActive(true);

                ParticleSystem.EmissionModule em = ShotCharge.GetComponent<ParticleSystem>().emission;
                ParticleSystem.EmissionModule em2 = ShotCharge2.GetComponent<ParticleSystem>().emission;

                em.enabled = true; // emit some effect 
                em2.enabled = true;

                yield return new WaitForSeconds(1);

                GameObject orb = orbPre.Spawn(spawnPoint); // Spawn Orb

                orb.GetComponent<Rigidbody2D>().isKinematic = false;
                orb.LocateMyFSM("Orb Control").SetState("Chase Hero");

                em.enabled = false;
                em2.enabled = false;
            }

            UObject.Destroy(ShotCharge);
            UObject.Destroy(ShotCharge2);
        }
    }
}