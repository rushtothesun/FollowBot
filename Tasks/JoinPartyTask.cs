﻿using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using FollowBot.Helpers;
using log4net;
using System.Linq;
using System.Threading.Tasks;


namespace FollowBot.Tasks
{
    class JoinPartyTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();

        public string Name { get { return "JoinPartyTask"; } }
        public string Description { get { return "This task will ask for party."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }


        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
			
            if (LokiPoe.InstanceInfo.PartyStatus == DreamPoeBot.Loki.Game.GameData.PartyStatus.PartyMember)
			{
				if (FollowBot._leaderPartyEntry?.PlayerEntry != null)
				{
					var leadername = FollowBot._leaderPartyEntry.PlayerEntry.Name;
					if (leadername != "NoPoeContent")
					{
						await PartyHelper.LeaveParty();
					}
				}					        
				return false;
			}
			
            if (LokiPoe.InstanceInfo.PartyStatus == DreamPoeBot.Loki.Game.GameData.PartyStatus.PartyLeader)
            {
                await PartyHelper.LeaveParty();
                return true;
            }

            var invite = LokiPoe.InstanceInfo.PendingPartyInvites;
            if (invite.Any())
            {
                await PartyHelper.HandlePartyInvite();
            }
            else if (LokiPoe.InstanceInfo.PartyStatus == DreamPoeBot.Loki.Game.GameData.PartyStatus.None)
            {
                return true;
            }

            return true;
        }
        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}