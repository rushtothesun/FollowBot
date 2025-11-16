using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using log4net;
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

        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => "Letale";
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
            if (!FollowBotSettings.Instance.InteractQuest) return false;
            var areaId = LokiPoe.CurrentWorldArea.Id;

            foreach (var interactQuestObj in QuestInteractionObjects)
            {
                if (areaId != interactQuestObj.ActId) continue;

                if (!PlayerHasItem(interactQuestObj.IncludeQuestItem)) continue;

                NetworkObject interactTarget = LokiPoe.ObjectManager.Objects.FirstOrDefault(x => x.Name == interactQuestObj.ObjectName);

                if (interactTarget == null) continue;
                if (!interactTarget.IsTargetable) return false;
                if (!LokiPoe.CurrentWorldArea.IsTown && LokiPoe.Me.Position.Distance(interactTarget.Position) > 30)
                {
                    return false;
                }
                Log.Debug($"[{Name}: Find interact object [{interactQuestObj.ObjectName}]");
                await interactTarget.WalkablePosition().ComeAtOnce();
                await PlayerAction.Interact(interactTarget);

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
                if (!findObj.PathExists()) continue;

                if (!LokiPoe.CurrentWorldArea.IsTown && LokiPoe.Me.Position.Distance(findObj.Position) > 30) continue;
                Log.Debug($"[{Name}: Find Npc [{npcInteractInf.NpcName}]");

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
            new InteractQuestObject("2_6_14","Ignition Switch", new string[]{"The Black Flag"}),
            new InteractQuestObject("2_6_14", "The Beacon", new string[]{"The Black Flag"}),
            new InteractQuestObject("2_7_5_2","Secret Passage" ),
            new InteractQuestObject("2_7_9", "Firefly"),
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
            new InteractQuestNpc("1_4_3_3", "Lady Dialla",
                ()=> PlayerHasItem(new string[] {"The Eye of Fury", "The Eye of Desire"}),
                NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("1_4_6_2", "Piety", ()=> CheckQuestStateId("a4q1", 15),
                NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("1_4_6_3", "Piety",
                ()=> PlayerHasItem(new string[]{"Malachai's Heart", "Malachai's Entrails", "Malachai's Lungs" }),
                NpcHelper.TalkAndSkipDialog),
            // Act 5
            //new InteractQuestNpc("1_5_5", "Bannon", NpcHelper.TalkAndSkipDialog),
            // Act 7
            new InteractQuestNpc("2_7_5_1","Silk",()=> PlayerHasItem("Black Venom"),
                (obj)=> NpcHelper.TakeReward(obj, "Black Death Reward")),
            new InteractQuestNpc("2_7_11", "Yeena", ()=> CheckQuestStateId("a7q7", 3), NpcHelper.TalkAndSkipDialog),
            // Act 8
            new InteractQuestNpc("2_8_8", "Clarissa",()=> PlayerHasItem("Ankh of Eternity"),NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_8_8", "Clarissa",()=>{
                var quest = Dat.QuestStates.FirstOrDefault(x=> x.Quest.Id == "a8q6");
                if(quest == null) return false;
                if(quest.QuestProgressText == "Talk to Clarissa") return true;
                return false;
            }, NpcHelper.TalkAndSkipDialog),
            // Act 9
            new InteractQuestNpc("2_9_town", "Petarus and Vanja", () => PlayerHasItem("Storm Blade"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Sin", () => CheckQuestStateId("a9q1", 17), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_9_town", "Petarus and Vanja", () => CheckQuestStateId("a9q5", 7), (obj)=>NpcHelper.TakeReward(obj,"Take Bottled Storm")),
            new InteractQuestNpc("2_9_8", "Sin", ()=> PlayerHasItem("Trarthan Powder"), NpcHelper.TalkAndSkipDialog),
            // Act 10
            new InteractQuestNpc("2_10_town", "Bannon", ()=> PlayerHasItem("The Staff of Purity"), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_10_1", "Bannon", ()=>CheckQuestStateId("a10q1",4), NpcHelper.TalkAndSkipDialog),
            new InteractQuestNpc("2_10_2", "Innocence", () => CheckQuestStateId("a10q3", 10), NpcHelper.TalkAndSkipDialog)



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
