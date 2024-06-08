/* Jump Map (ver. Alpha 0.0.1) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using UnityEngine;
using PlayerRoles.FirstPersonControl;

namespace site02
{
    public class Hit
    {
        public Player player;
        public PlayerMovementState Movestate;
        bool CanHit = true;
        PlayerMovementState _Movestate;

        public void Update()
        {
            Movestate = (player.Role.Base as FpcStandardRoleBase).FpcModule.CurrentMovementState;

            if (Movestate != _Movestate)
            {
                _Movestate = Movestate;

                if (Movestate == PlayerMovementState.Sneaking && CanHit)
                {
                    site02.Instance.OnMelee(player);
                    CanHit = false;
                    MEC.Timing.CallDelayed(1, () => { CanHit = true; });
                }
            }
        }
    }

    public class Gtool : MonoBehaviour
    {
        public List<Hit> hits = new List<Hit>();

        void Check(Player player)
        {
            if (player.IsDead || player.IsCuffed)
                return;

            if (Physics.Raycast(player.Position, Vector3.down, out RaycastHit hit, 1, (LayerMask)1))
            {
                string pos = hit.collider.name;

                if (pos.StartsWith("Stage "))
                {
                    int level = int.Parse(pos.Replace("Stage ", ""));
                    int ul = int.Parse(site02.Instance.Stage[player.UserId]);

                    if (level > ul)
                        site02.Instance.Stage[player.UserId] = level.ToString();
                }

                else if (pos == "Question 1")
                    player.ShowHint($"이곳에 가장 처음으로 도달한 유저의 이름은 '은별'이다. (O/X)", 100);

                else if (pos == "Believe")
                    player.ShowHint($"believe, yourself, trust constantly.", 100);
            }
        }

        void Update()
        {
            hits.RemoveAll(x => x.player == null);

            foreach (var p in hits)
            {
                try
                {
                    p.Update();
                    Check(p.player);
                }
                catch (Exception ex)
                {

                }
            }
        }

    }

    public class site02 : Plugin<Config>
    {
        public static site02 Instance;
        public Gtool gtool;

        public List<string> Owner = new List<string>() { "76561198447505804@steam" };
        public Dictionary<string, string> Stage = new Dictionary<string, string>(); // ID, 스테이지
        public Dictionary<string, int> HealingCooldown = new Dictionary<string, int>(); // ID, 힐 쿨타임

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;

            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Left += OnLeft;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.Spawned += OnSpawned;
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppedItem += OnDroppedItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.Hurt += OnHurt;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppedItem -= OnDroppedItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SpawnedRagdoll -= OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.Hurt -= OnHurt;
            Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;

            Instance = null;
        }

        public void OnMelee(Player player)
        {
            if (player.IsDead || player.IsCuffed || player.IsScp)
                return;

            if (Physics.Raycast(player.ReferenceHub.PlayerCameraReference.position + player.ReferenceHub.PlayerCameraReference.forward * 0.2f, player.ReferenceHub.PlayerCameraReference.forward, out RaycastHit hit, 1, InventorySystem.Items.Firearms.Modules.StandardHitregBase.HitregMask) &&
                hit.collider.TryGetComponent<IDestructible>(out IDestructible destructible))
            {
                Hitmarker.SendHitmarkerDirectly(player.ReferenceHub, 1f);
                destructible.Damage(1, new PlayerStatsSystem.CustomReasonDamageHandler("무지성으로 뚜드려 맞아 죽었습니다.", 12), hit.point);
            }
        }

        public void OnWaitingForPlayers()
        {
            Server.ExecuteCommand($"/decontamination disable");
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Round.Start();
        }

        public async void OnRoundStarted()
        {
            GameObject gameobject = GameObject.Instantiate(new GameObject());
            gtool = gameobject.AddComponent<Gtool>();

            MapEditorReborn.API.Features.Serializable.MapSchematic mapByName = MapEditorReborn.API.Features.MapUtils.GetMapByName("jm");
            MapEditorReborn.API.API.CurrentLoadedMap = mapByName; 
            
            while (true)
            {
                foreach (var player in Player.List)
                {
                    if (HealingCooldown[player.UserId] <= 0)
                    {
                        player.Heal(10);
                    }
                    else
                    {
                        HealingCooldown[player.UserId] -= 1;
                    }
                }

                await Task.Delay(1000);
            }
        }

        public async void OnVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            gtool.hits.Add(new Hit { player = ev.Player });

            Server.ExecuteCommand($"/speak {ev.Player.Id} enable");
            ev.Player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
            ev.Player.Position = new Vector3(80.45463f, 1053.379f, -42.54824f);

            if (!Stage.Keys.Contains(ev.Player.UserId))
                Stage.Add(ev.Player.UserId, "1");

            if (!HealingCooldown.Keys.Contains(ev.Player.UserId))
                HealingCooldown.Add(ev.Player.UserId, 0);

            while (ev.Player != null)
            {
                if (!ev.Player.IsDead)
                    ev.Player.ShowHint($"<b>Stage {Stage[ev.Player.UserId]}</b>", 1);
                await Task.Delay(1000);
            }
        }

        public void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            Stage.Remove(ev.Player.UserId);
            HealingCooldown.Remove(ev.Player.UserId);
        }

        public void OnDying(Exiled.Events.EventArgs.Player.DyingEventArgs ev)
        {
            ev.Player.ClearInventory();
        }

        public async void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            for (int i=1; i<5; i++)
            {
                ev.Player.ShowHint($"{5 - i}초 뒤 부활합니다.", 1);
                await Task.Delay(1000);
            }

            ev.Player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);

            Vector3 position()
            {
                int ul = int.Parse(Stage[ev.Player.UserId]);

                if (ul >= 10)
                    return new Vector3(68.40506f, 1073.64f, -61.16057f);

                else if (ul >= 5)
                    return new Vector3(98.48161f, 1065.135f, -19.03773f);

                else
                    return new Vector3(80.45463f, 1053.379f, -42.54824f);
            }
            ev.Player.Position = position();
        }

        public async void OnSpawned(Exiled.Events.EventArgs.Player.SpawnedEventArgs ev)
        {
            Player.List.ToList().ForEach(x => x.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 0));
            await Task.Delay(10);
            Player.List.ToList().ForEach(x => x.EnableEffect(Exiled.API.Enums.EffectType.FogControl, 1));
        }

        public void OnSearchingPickup(Exiled.Events.EventArgs.Player.SearchingPickupEventArgs ev)
        {
            List<ItemType> BlackList = new List<ItemType>() { ItemType.KeycardMTFCaptain, ItemType.KeycardFacilityManager };

            if (!BlackList.Contains(ev.Pickup.Type))
                ev.Player.AddItem(ev.Pickup);

            else
            {
                if (ev.Pickup.Type == ItemType.KeycardMTFCaptain)
                    ev.Player.Position = new Vector3(68.34256f, 1073.644f, -61.43791f);

                else if (ev.Pickup.Type == ItemType.KeycardFacilityManager)
                    Server.ExecuteCommand($"/rocket {ev.Player.Id} 1");
            }
        }

        public async void OnDroppedItem(Exiled.Events.EventArgs.Player.DroppedItemEventArgs ev)
        {
            await Task.Delay(10000);
            ev.Pickup.Destroy();
        }

        public void OnDroppingAmmo(Exiled.Events.EventArgs.Player.DroppingAmmoEventArgs ev)
        {
            ev.IsAllowed = false;
        }

        public async void OnSpawnedRagdoll(Exiled.Events.EventArgs.Player.SpawnedRagdollEventArgs ev)
        {
            await Task.Delay(10000);
            ev.Ragdoll.Destroy();
        }

        public void OnHurt(Exiled.Events.EventArgs.Player.HurtEventArgs ev)
        {
            HealingCooldown[ev.Player.UserId] = 5;
        }

        public void OnFlippingCoin(Exiled.Events.EventArgs.Player.FlippingCoinEventArgs ev)
        {
            ServerConsole.AddLog($"{ev.Player.Nickname}의 위치 : {ev.Player.Position.x} {ev.Player.Position.y} {ev.Player.Position.z}", ConsoleColor.DarkMagenta);
        }

    }
}

