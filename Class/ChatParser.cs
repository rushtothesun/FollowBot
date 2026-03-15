using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using FollowBot.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatPanel = DreamPoeBot.Loki.Game.LokiPoe.InGameState.ChatPanel;

namespace FollowBot.Class
{
    public class ChatParser
    {
        public bool shouldCleanMessages;
        // MD5 deduplication disabled - was unreliable across area transitions.
        // Currently relying on /cls after command processing instead.
        // private volatile string _lastMd5;
        // private volatile Dictionary<string, bool> _treatedMd5List = new Dictionary<string, bool>();

        public ChatParser()
        {
            // _treatedMd5List.Clear();
            shouldCleanMessages = true;
            // _lastMd5 = "";
        }

        public static LokiPoe.InGameState.ChatResult SendChatMsg(string msg, bool closeChatUi = true)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return LokiPoe.InGameState.ChatResult.None;
            }
            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
            {
                LokiPoe.InGameState.ChatPanel.ToggleChat();
            }

            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
            {
                return LokiPoe.InGameState.ChatResult.UiNotOpen;
            }

            LokiPoe.InGameState.ChatResult result = LokiPoe.InGameState.ChatPanel.Chat(msg);

            if (closeChatUi)
            {
                if (LokiPoe.InGameState.ChatPanel.IsOpened)
                {
                    LokiPoe.InGameState.ChatPanel.ToggleChat();
                }
            }

            return result;
        }

        private void CleanMessage()
        {
            if (!LokiPoe.IsInGame) return;
            SendChatMsg("/cls");
            // MD5 deduplication disabled - was unreliable across area transitions.
            // List<chatPanel.ChatEntry> msgs = LokiPoe.InGameState.ChatPanel.Messages.ToList();
            // if (msgs.Count <= 0)
            // {
            //     return;
            // }
            //
            // for (int i = 0; i < msgs.Count; i++)
            // {
            //     var chatEntry = msgs[i];
            //     _lastMd5 = chatEntry.MD5;
            //     if (_treatedMd5List.TryGetValue(chatEntry.MD5, out bool alreadyTreated))
            //     {
            //         if (alreadyTreated)
            //         {
            //             continue;
            //         }
            //
            //         _treatedMd5List[chatEntry.MD5] = true;
            //     }
            //     else
            //     {
            //         _treatedMd5List.Add(chatEntry.MD5, true);
            //     }
            // }
        }
        public void Update()
        {
            if (!LokiPoe.IsInGame) return;
            if (shouldCleanMessages)
            {
                CleanMessage();
                shouldCleanMessages = false;
            }
            List<chatPanel.ChatEntry> msgs = LokiPoe.InGameState.ChatPanel.Messages;

            if (msgs.Count <= 0)
            {
                return;
            }

            // MD5 deduplication disabled - was unreliable across area transitions.
            // Now we process all messages and rely on /cls after command processing.
            // string lastMd5 = msgs.Last().MD5;
            //
            // if (lastMd5 == _lastMd5)
            // {
            //     return;
            // }
            //
            // _lastMd5 = lastMd5;

            for (int i = 0; i < msgs.Count; i++)
            {
                var chatEntry = msgs[i];
                // MD5 deduplication disabled
                // if (_treatedMd5List.TryGetValue(chatEntry.MD5, out bool alreadyTreated))
                // {
                //     if (alreadyTreated)
                //     {
                //         continue;
                //     }
                //
                //     _treatedMd5List[chatEntry.MD5] = true;
                // }
                // else
                // {
                //     _treatedMd5List.Add(chatEntry.MD5, true);
                // }

                try
                {
                    ProcessNewMessage(chatEntry);
                }
                catch (Exception)
                {
                    // Suppressing exceptions - if command processing fails, we don't want to crash the bot.
                    // Consider uncommenting for debugging: GlobalLog.Error($"{e}");
                }
            }
        }

        private void ProcessNewMessage(chatPanel.ChatEntry newmessage)
        {
            if (newmessage == null)
            {
                return;
            }

            switch (newmessage.MessageType)
            {
                case chatPanel.MessageType.Local:
                    break;
                case chatPanel.MessageType.Global:
                    break;
                case chatPanel.MessageType.Party:
                    ProcessPartyMessage(newmessage);
                    break;
                case chatPanel.MessageType.Whisper:
                    ProcessPartyMessage(newmessage);
                    break;
                case chatPanel.MessageType.Trade:
                    break;
                case chatPanel.MessageType.Guild:
                    break;
            }
        }

        private static bool TryCommand(string command, string expected, Action action)
        {
            if (command != expected) return false;
            action();
            return true;
        }

        private static void ProcessPartyMessage(LokiPoe.InGameState.ChatPanel.ChatEntry newmessage)
        {
            if (FollowBot._leaderPartyEntry == null || FollowBot._leaderPartyEntry.PlayerEntry == null)
                return;
            var leadername = FollowBot._leaderPartyEntry.PlayerEntry.Name;
            if (string.IsNullOrEmpty(leadername))
                return;
            if (newmessage.RemoteName != leadername)
                return;
            var start = newmessage.Message.IndexOf($"{leadername}:", StringComparison.InvariantCulture) + $"{leadername}:".Length + 1;
            var end = newmessage.Message.Length - start;
            var command = newmessage.Message.Substring(start, end);

            GlobalLog.Warn($"Received Message: {newmessage.Message}, Command: {command}");

            var chatCommands = FollowBotSettings.Instance.ChatCommands;
            var follow = FollowBotSettings.Instance.Follow;
            var combat = FollowBotSettings.Instance.Combat;
            var loot = FollowBotSettings.Instance.Loot;

            bool commandProcessed =
                TryCommand(command, chatCommands.OpenTownPortalChatCommand, () => DefenseAndFlaskTask.ShouldOpenPortal = true) ||
                TryCommand(command, chatCommands.TeleportToLeaderChatCommand, () => DefenseAndFlaskTask.ShouldTeleport = true) ||
                TryCommand(command, chatCommands.StartFollowChatCommand, () => follow.ShouldFollow = true) ||
                TryCommand(command, chatCommands.StopFollowChatCommand, () => follow.ShouldFollow = false) ||
                TryCommand(command, chatCommands.StartAttackChatCommand, () => combat.ShouldKill = true) ||
                TryCommand(command, chatCommands.StopAttackChatCommand, () => combat.ShouldKill = false) ||
                TryCommand(command, chatCommands.StartLootChatCommand, () => loot.ShouldLoot = true) ||
                TryCommand(command, chatCommands.StopLootChatCommand, () => loot.ShouldLoot = false) ||
                TryCommand(command, chatCommands.StartAutoTeleportChatCommand, () => follow.DontPortOutofMap = false) ||
                TryCommand(command, chatCommands.StopAutoTeleportChatCommand, () => follow.DontPortOutofMap = true) ||
                TryCommand(command, chatCommands.StartSentinelChatCommand, () => combat.UseStalkerSentinel = true) ||
                TryCommand(command, chatCommands.StopSentinelChatCommand, () => combat.UseStalkerSentinel = false) ||
                TryCommand(command, chatCommands.EnterPortalChatCommand, () => UltimatumTask.ShouldEnterPortal = true) ||
                TryCommand(command, chatCommands.DepositStashChatCommand, () => StashTask.ShouldDepositFromChat = true) ||
                TryCommand(command, chatCommands.AllocateChatCommand, () => AutoAllocatePassiveTask.ForceTrigger()) ||
                TryCommand(command, chatCommands.NewInstanceChatCommand, () => FollowTask.ShouldCreateNewInstance = true);

            if (commandProcessed)
            {
                SendChatMsg("/cls");
            }
        }
    }
}
