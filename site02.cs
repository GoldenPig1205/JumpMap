/* Jump Map (ver. Alpha 0.0.1) */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;
using UnityEngine;

namespace site02
{
    public class site02 : Plugin<Config>
    {
        public static site02 Instance;

        public List<string> Owner = new List<string>() { "76561198447505804@steam" };

        public override void OnEnabled()
        {
            Instance = this;

            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;

            Exiled.Events.Handlers.Player.Verified += OnVerified;
            Exiled.Events.Handlers.Player.Died += OnDied;
            Exiled.Events.Handlers.Player.FlippingCoin += OnFlippingCoin;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;

            Exiled.Events.Handlers.Player.Verified -= OnVerified;
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.FlippingCoin -= OnFlippingCoin;

            Instance = null;
        }

        public void OnWaitingForPlayers()
        {
            Server.ExecuteCommand($"/decontamination disable");
            Round.IsLocked = true;
            Round.Start();
        }

        public void OnRoundStarted()
        {
            MapEditorReborn.API.Features.Serializable.MapSchematic mapByName = MapEditorReborn.API.Features.MapUtils.GetMapByName("jm");
            MapEditorReborn.API.API.CurrentLoadedMap = mapByName;

            AudioPlayer.API.AudioController.SpawnDummy(1, "DJ GoldenPig1205", "yellow", "GoldenRadio");

        }

        public void OnVerified(Exiled.Events.EventArgs.Player.VerifiedEventArgs ev)
        {
            if (Owner.Contains(ev.Player.UserId))
            {
                UserGroup owner = new UserGroup() { Permissions = 9223372036854775807, KickPower = 255, RequiredKickPower = 255, Cover = false };
                ev.Player.Group = owner;
            }

            ev.Player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            ev.Player.Position = new Vector3(80.45463f, 1053.379f, -42.54824f);
        }

        public async void OnDied(Exiled.Events.EventArgs.Player.DiedEventArgs ev)
        {
            for (int i=1; i<5; i++)
            {
                ev.Player.ShowHint($"{5 - i}초 뒤 부활합니다.", 1);
                await Task.Delay(1000);
            }

            ev.Player.Role.Set(PlayerRoles.RoleTypeId.ClassD);
            ev.Player.Position = new Vector3(80.45463f, 1053.379f, -42.54824f);
        }

        public void OnFlippingCoin(Exiled.Events.EventArgs.Player.FlippingCoinEventArgs ev)
        {
            ServerConsole.AddLog($"{ev.Player.Nickname}의 위치 : {ev.Player.Position.x} {ev.Player.Position.y} {ev.Player.Position.z}", ConsoleColor.DarkMagenta);
        }

    }
}

