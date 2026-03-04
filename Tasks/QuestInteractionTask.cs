using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    struct InteractQuestObject
    {
        public string ActId { get; }
        public string ObjectName { get; }
        public string[] IncludeQuestItem { get; }
        public InteractQuestObject(string actId, string objectName, string[] includeQuestItem)
        {
            ActId = actId;
            ObjectName = objectName;
            IncludeQuestItem = includeQuestItem;
        }
        public InteractQuestObject(string actId, string objectName)
        {
            ActId = actId;
            ObjectName = objectName;
            IncludeQuestItem = new string[] { };
        }
    }

    class QuestInteractionTask : ITask
    {

        public string Author => "Letale, Rushtothesun";
        public string Description => "Quest interact";
        public string Name => "QuestInteract";
        public string Version => "0.0.0.1";

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.Follow.InteractQuest) return false;
            if (LokiPoe.CurrentWorldArea.IsMap || LokiPoe.CurrentWorldArea.IsHideoutArea) return false;

            var areaId = LokiPoe.CurrentWorldArea.Id;

            foreach (var interactQuestObj in QuestInteractionObjects)
            {
                if (areaId != interactQuestObj.ActId) continue;

                if (!PlayerHasItem(interactQuestObj.IncludeQuestItem)) continue;

                // Special handling for Ascendancy Plaque - check quest state
                if (interactQuestObj.ObjectName == "Ascendancy Plaque")
                {
                    if (!CheckQuestStateId("labyrinth_a3", 9)) continue;
                }

                NetworkObject interactTarget = LokiPoe.ObjectManager.Objects.FirstOrDefault(x => x.Name == interactQuestObj.ObjectName);

                if (interactTarget == null) continue;
                if (!interactTarget.IsTargetable) return false;
                if (!LokiPoe.CurrentWorldArea.IsTown && LokiPoe.Me.Position.Distance(interactTarget.Position) > 30)
                {
                    return false;
                }
                GlobalLog.Debug($"[{Name}: Find interact object [{interactQuestObj.ObjectName}]");
                await interactTarget.WalkablePosition().ComeAtOnce();
                await PlayerAction.Interact(interactTarget);

                // Special handling for Ascendancy Plaque - take transition after clicking
                if (interactQuestObj.ObjectName == "Ascendancy Plaque")
                {
                    GlobalLog.Debug($"[{Name}]: Ascendancy Plaque clicked, waiting before taking transition");
                    await Wait.SleepSafe(500, 1000);

                    var transition = LokiPoe.ObjectManager.Objects.FirstOrDefault<AreaTransition>(x => x.Name == "Aspirants' Plaza");
                    if (transition != null)
                    {
                        GlobalLog.Debug($"[{Name}]: Taking transition to Aspirants' Plaza");
                        await PlayerAction.TakeTransition(transition);
                    }
                    else
                    {
                        GlobalLog.Warn($"[{Name}]: Could not find transition to Aspirants' Plaza");
                    }
                }

                return true;
            }
            foreach (var npcInteractInf in InteractQuestNpcs)
            {
                if (areaId != npcInteractInf.ActId) continue;

                if (!npcInteractInf.TriggerAction()) continue;

                NetworkObject findObj = LokiPoe.ObjectManager.Objects.FirstOrDefault(x => x.Name == npcInteractInf.NpcName);

                if (findObj == null) continue;
                if (findObj.Reaction != Reaction.Npc) continue;
                if (!findObj.IsTargetable) continue;

                // Only check PathExists for non-town areas - in towns it can incorrectly return false
                if (!LokiPoe.CurrentWorldArea.IsTown && !findObj.PathExists()) continue;

                if (!LokiPoe.CurrentWorldArea.IsTown && LokiPoe.Me.Position.Distance(findObj.Position) > 30) continue;
                GlobalLog.Debug($"[{Name}: Find Npc [{npcInteractInf.NpcName}]");

                await npcInteractInf.Action(findObj);

                return true;

            }

            return false;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }
        static bool PlayerHasItem(string questItemName)
        {
            return Inventories.InventoryItems.Exists(x => x.Name == questItemName);
        }
        static bool PlayerHasItem(string[] questItemNamesList)
        {
            foreach (var questItemName in questItemNamesList)
            {
                if (!PlayerHasItem(questItemName))
                {
                    return false;
                }
            }
            return true;
        }
        private List<InteractQuestObject> QuestInteractionObjects { get; set; } = new List<InteractQuestObject>
        {
            new InteractQuestObject("1_1_3", "Strange Glyph Wall", new string[]{"Haliotis Glyph", "Roseus Glyph","Ammonite Glyph" }),
            new InteractQuestObject("1_2_12", "Tree Roots", new string[] {"Maligaro's Spike"}),
            new InteractQuestObject("1_2_9", "Thaumetic Seal", new string[] {"Thaumetic Emblem"}),
            new InteractQuestObject("1_2_11", "Ancient Seal"),
            new InteractQuestObject("1_3_2", "Sewer Grating", new string[] {"Sewer Keys"}),
            new InteractQuestObject("1_3_10_1", "Undying Blockage", new string[] {"Infernal Talc"}),
            new InteractQuestObject("1_3_15","Locked Door" , new string[] {"Tower Key"}),
            new InteractQuestObject("1_4_town", "Deshret's Seal", new string []{"Deshret's Banner"}),
            new InteractQuestObject("1_4_3_2", "Deshret's Spirit"),
            new InteractQuestObject("1_5_3", "Templar Courts Entrance", new string[] {"Eyes of Zeal"}),
            new InteractQuestObject("2_6_4","Fortress Gate", new string[] {"Eye of Conquest"}),
            new InteractQuestObject("2_6_14", "The Beacon", new string[]{"The Black Flag"}),
            new InteractQuestObject("2_7_5_2","Secret Passage" ),
            new InteractQuestObject("2_7_9", "Firefly"),
            new InteractQuestObject("1_3_town", "Ascendancy Plaque"),
        };
        private List<InteractQuestNpc> InteractQuestNpcs { get; set; } = new List<InteractQuestNpc>
        {
            // Act 1
            new InteractQuestNpc("1_1_town", "Nessa", () => CheckQuestStateId("a1q5", 2) && PlayerHasItem("Medicine Chest"), (obj) => NpcHelper.TakeReward(obj, "Medicine Chest Reward", "Quicksilver Flask")),
            new InteractQuestNpc("1_1_town", "Tarkleigh", () => CheckQuestStateId("a1q7", 3), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Dweller Reward")),
            new InteractQuestNpc("1_1_town", "Bestel", () => CheckQuestStateId("a1q6", 3), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Fairgraves Reward")),
            new InteractQuestNpc("1_1_9", "Captain Fairgraves",()=> PlayerHasItem("Allflame"), NpcHelper.TalkAndSkipDialog),
            // Act 2
            new InteractQuestNpc("1_1_town", "Bestel", () => CheckQuestStateId("a2q11", 0), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Road Reward")),
            new InteractQuestNpc("1_2_town", "Eramir", ()=>  CheckQuestStateId("a2q7",new int[] {0,2,10}) && PlayerHasItem(new string[]{"Alira's Amulet","Kraityn's Amulet","Oak's Amulet"}),
               (obj)=>NpcHelper.TakeReward(obj,"Take the Apex")),
            new InteractQuestNpc("1_2_town", "Yeena", () => PlayerHasItem("Golden Hand"), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Fellshrine Reward")),
            new InteractQuestNpc("1_2_4", "Kraityn, Scarbearer", () => Dat.QuestStates.Any(q => q.Quest.Id == "a2q7" && q.Id < 100), NpcHelper.BanditKillSelect),
            new InteractQuestNpc("1_2_9", "Alira Darktongue", () => Dat.QuestStates.Any(q => q.Quest.Id == "a2q7" && q.Id < 100), NpcHelper.BanditKillSelect),
            new InteractQuestNpc("1_2_12", "Oak, Skullbreaker", () => Dat.QuestStates.Any(q => q.Quest.Id == "a2q7" && q.Id < 100), NpcHelper.BanditKillSelect),
            // Act 3
            new InteractQuestNpc("1_3_town", "Hargan", () => CheckQuestStateId("a3q11", 21) && PlayerHasItem(new string[] { "Bust of Marceus Lioneye", "Bust of Hector Titucius", "Bust of Gaius Sentari" }),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Platinum Bust Reward")),
            new InteractQuestNpc("1_3_town", "Clarissa", ()=> PlayerHasItem("Tolman's Bracelet"),
               (obj)=>NpcHelper.TakeReward(obj, "Take Sewer Keys")),
            new InteractQuestNpc("1_3_1", "Clarissa", () => CheckQuestStateId("a3q1", new int[] {10, 12, 17}),
               NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("1_3_8_2", "Lady Dialla",
                ()=> PlayerHasItem(new string [] {"Ribbon Spool", "Thaumetic Sulphite" }),
                (obj)=> NpcHelper.TakeReward(obj, "Take Infernal Talc")),
            new InteractQuestNpc("1_3_town", "Grigor", () => CheckQuestStateId("a3q9", 3), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Piety Reward")),
            // Act 4
            new InteractQuestNpc("1_4_town", "Tasuni", () => CheckQuestStateId("a4q6", 2),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Deshret Reward")),
            new InteractQuestNpc("1_4_3_3", "Lady Dialla",
                ()=> PlayerHasItem(new string[] {"The Eye of Fury", "The Eye of Desire"}),
                NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("1_4_6_2", "Piety", ()=> CheckQuestStateId("a4q1", 15),
                NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("1_4_6_3", "Piety",
                ()=> PlayerHasItem(new string[]{"Malachai's Heart", "Malachai's Entrails", "Malachai's Lungs" }),
                NpcHelper.TalkAndSkipDialog),
            // Act 5
            new InteractQuestNpc("1_5_town", "Vilenta", () => PlayerHasItem("Miasmeter"), (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Miasmeter Reward")),
            new InteractQuestNpc("1_5_town", "Lani", () => CheckQuestStateId("a5q4", 1), (obj) => NpcHelper.TakeReward(obj, "Avarius Reward")),
            new InteractQuestNpc("1_5_town", "Lani", () => CheckQuestStateId("a5q7", new int[] {1, 2, 3}) && PlayerHasItem(new string[] {"Valako's Jaw", "Tukohama's Tooth", "Hinekora's Hair"}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Torments Reward")),
            //new InteractQuestNpc("1_5_5", "Bannon", NpcHelper.TalkAndSkipDialog),
            // Act 6
            new InteractQuestNpc("2_6_town", "Lilly Roth", () => CheckQuestStateId("a6q4", 2),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Twilight Strand Reward")),
            new InteractQuestNpc("2_6_town", "Bestel", () => CheckQuestStateId("a6q7", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Abberath Reward")),
            new InteractQuestNpc("2_6_town", "Tarkleigh", () => CheckQuestStateId("a6q3", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Tukohama Reward")),
            new InteractQuestNpc("2_6_town", "Tarkleigh", () => CheckQuestStateId("a6q6", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Puppet Mistress Reward")),
            // Act 7
            new InteractQuestNpc("2_7_5_1","Silk",()=> PlayerHasItem("Black Venom"),
                (obj)=> NpcHelper.TakeReward(obj, "Black Death Reward")),
            new InteractQuestNpc("2_7_11", "Yeena", ()=> PlayerHasItem("Firefly"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_7_town", "Yeena", ()=> CheckQuestStateId("a7q7", 3), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_7_town", "Weylam Roth", () => CheckQuestStateId("a7q6", new int[] {1, 2, 3}) && PlayerHasItem("Kishara's Star"),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Kishara's Star Reward")),
            new InteractQuestNpc("2_7_town", "Eramir", () => CheckQuestStateId("a7q9", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Gruthkul Reward")),
            new InteractQuestNpc("2_7_town", "Eramir", () => CheckQuestStateId("a7q1", 15),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Ralakesh Reward")),
            // Act 8
            new InteractQuestNpc("2_8_8", "Clarissa",()=> PlayerHasItem("Ankh of Eternity"),NpcHelper.TalkAndSkipDialog),
            /*new InteractQuestNpc("2_8_8", "Clarissa",()=>{
                var quest = Dat.QuestStates.FirstOrDefault(x=> x.Quest.Id == "a8q6");
                if(quest == null) return false;
                if(quest.QuestProgressText == "Talk to Clarissa") return true;
                return false;
            }, NpcHelper.TalkAndSkipDialog),*/
            new InteractQuestNpc("2_8_town", "Clarissa", () => CheckQuestStateId("a8q6", new int[] {1, 2, 3}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Tolman Reward")),
            new InteractQuestNpc("2_8_town", "Hargan", () => CheckQuestStateId("a8q4", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Yugul Reward")),
            new InteractQuestNpc("2_8_town", "Maramoa", () => CheckQuestStateId("a8q7", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Gemling Legion Reward")),
            // Act 9
            new InteractQuestNpc("2_9_town", "Petarus and Vanja", () => PlayerHasItem("Storm Blade"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Sin", () => CheckQuestStateId("a9q1", 17), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Sin", () => CheckQuestStateId("a9q5", 10), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Petarus and Vanja", () => CheckQuestStateId("a9q5", 7), (obj)=>NpcHelper.TakeReward(obj,"Take Bottled Storm")),
            new InteractQuestNpc("2_9_8", "Sin", ()=> PlayerHasItem("Trarthan Powder"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Petarus and Vanja", () => CheckQuestStateId("a9q4", new int[] {1, 2, 3}) && PlayerHasItem("Calendar of Fortune"),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Maraketh Calendar Reward")),
            new InteractQuestNpc("2_9_town", "Irasha", () => CheckQuestStateId("a9q5", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Shakari Reward")),
            new InteractQuestNpc("2_9_town", "Irasha", () => CheckQuestStateId("a9q2", new int[] {2, 3}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Feather Reward")),
            // Act 10
            new InteractQuestNpc("2_10_town", "Bannon", ()=> PlayerHasItem("The Staff of Purity"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_10_1", "Bannon", ()=>CheckQuestStateId("a10q1",4), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_10_2", "Innocence", () => CheckQuestStateId("a10q3", 10), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_10_town", "Weylam Roth", () => CheckQuestStateId("a10q4", new int[] {1, 2, 3}) && PlayerHasItem("Elixir of Allure"),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Elixir of Allure Reward")),
            new InteractQuestNpc("2_10_town", "Lani", () => CheckQuestStateId("a10q3", new int[] {2, 5}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Kitava Reward")),
            new InteractQuestNpc("2_10_town", "Lani", () => CheckQuestStateId("a10q6", new int[] {1, 2}),
                (obj) => NpcHelper.TakeRewardAndUseBook(obj, "Vilenta Reward"))
        };

        private static bool CheckQuestStateId(string questId, int stateId)
        {
            var quest = Dat.QuestStates.FirstOrDefault(q => q.Quest.Id == questId);
            if (quest == null) return false;
            if (quest.Id == stateId) return true;
            return false;
        }
        private static bool CheckQuestStateId(string questId, int[] stateId)
        {
            var quest = Dat.QuestStates.FirstOrDefault(q => q.Quest.Id == questId);
            if (quest == null) return false;
            return stateId.Contains(quest.Id);
        }

        struct InteractQuestNpc
        {
            public string ActId { get; }
            public string NpcName { get; }
            public Func<bool> TriggerAction { get; }
            public Func<NetworkObject, Task<bool>> Action { get; }

            public InteractQuestNpc(string actId, string npcName, Func<bool> triggerAction, Func<NetworkObject, Task<bool>> action)
            {
                ActId = actId;
                NpcName = npcName;
                TriggerAction = triggerAction;
                Action = action;
            }
            public InteractQuestNpc(string actId, string npcName, Func<NetworkObject, Task<bool>> action)
            {
                ActId = actId;
                NpcName = npcName;
                TriggerAction = () => true; ;
                Action = action;
            }

        }
    }
}
