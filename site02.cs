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
            Exiled.Events.Handlers.Player.SearchingPickup += OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppedItem += OnDroppedItem;
            Exiled.Events.Handlers.Player.DroppingAmmo += OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SpawnedRagdoll += OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.Hurt += OnHurt;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Left -= OnLeft;
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.SearchingPickup -= OnSearchingPickup;
            Exiled.Events.Handlers.Player.DroppedItem -= OnDroppedItem;
            Exiled.Events.Handlers.Player.DroppingAmmo -= OnDroppingAmmo;
            Exiled.Events.Handlers.Player.SpawnedRagdoll -= OnSpawnedRagdoll;
            Exiled.Events.Handlers.Player.Hurt -= OnHurt;

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
                    if (!player.IsNPC)
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
                }

                await Task.Delay(1000);
            }
        }

        public void OnVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            Verified(ev.Player);
        }

        public async void Verified(Player player)
        {
            gtool.hits.Add(new Hit { player = player });

            Server.ExecuteCommand($"/speak {player.Id} enable");
            player.Role.Set(PlayerRoles.RoleTypeId.Tutorial);
            player.Position = new Vector3(80.45463f, 1053.379f, -42.54824f);

            if (!Stage.Keys.Contains(player.UserId))
                Stage.Add(player.UserId, "1");

            if (!HealingCooldown.Keys.Contains(player.UserId))
                HealingCooldown.Add(player.UserId, 0);

            while (player != null)
            {
                if (!player.IsDead)
                    player.ShowHint($"<b>Stage {Stage[player.UserId]}</b>", 1.2f);
                await Task.Delay(1000);
            }
        }

        public void OnLeft(Exiled.Events.EventArgs.Player.LeftEventArgs ev)
        {
            if (Stage.Keys.Contains(ev.Player.UserId))
            {
                Stage.Remove(ev.Player.UserId);
                HealingCooldown.Remove(ev.Player.UserId);
            }
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

            int ul = int.Parse(Stage[ev.Player.UserId]);

            Vector3 position()
            {
                switch (ul)
                {
                    case 2:
                        return new Vector3(80.3074f, 1054.351f, -7.996504f);

                    case 3:
                        return new Vector3(90.88943f, 1055.676f, -14.25041f);

                    case 4:
                        return new Vector3(89.58475f, 1054.094f, -42.57072f);

                    case 5:
                        return new Vector3(98.65897f, 1065.133f, -18.92229f);

                    case 6:
                        return new Vector3(73.04178f, 1055.922f, -23.73088f);

                    case 7:
                        return new Vector3(77.54218f, 1058.104f, -53.67687f);

                    case 8:
                        return new Vector3(68.44922f, 1089.934f, -8.289063f);

                    case 9:
                        return new Vector3(62.8125f, 1048.953f, 10.29688f);

                    case 10:
                        return new Vector3(68.34256f, 1073.64f, -61.43791f);

                    case 11:
                        return new Vector3(98.6785f, 1082.613f, -60.84807f);

                    default:
                        return new Vector3(80.24559f, 1053.379f, -43.26994f);
                }
            }

            ev.Player.Position = position();
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
    }
}

