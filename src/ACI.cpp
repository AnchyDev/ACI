#include "ACI.h"
#include "ACISocketClient.h"

#include "Chat.h"
#include "Config.h"
#include "Player.h"

#include "Realm.h"
#include "World.h"

#include <limits>
#include <thread>

#include <json.hpp>

std::string ACISocketHandler::ReplaceEmojis(std::string msg)
{
    return msg;
}

void SendIChatMessageToAll(uint32 faction, std::string origin, std::string author, std::string message)
{
    //worldMsg = ReplaceEmojis(worldMsg);
    std::string prefix = Acore::StringFormatFmt("|TInterface\\CHATFRAME\\UI-ChatWhisperIcon:16:16|t|cffFFFFFF[|cff5662F6Discord|cffFFFFFF]|r");

    if (origin != "Discord")
    {
        if (faction == TEAM_ALLIANCE)
        {
            prefix = Acore::StringFormatFmt("|TInterface\\GROUPFRAME\\UI-Group-PVP-Alliance:16:16|t|cffFFFFFF[|cff00FF00{}|cffFFFFFF]|r", origin);
        }
        else if (faction == TEAM_HORDE)
        {
            prefix = Acore::StringFormatFmt("|TInterface\\GROUPFRAME\\UI-Group-PVP-Horde:16:16|t|cffFFFFFF[|cff00FF00{}|cffFFFFFF]|r", origin);
        }
    }

    std::string msg = Acore::StringFormatFmt("{}|cffFFFFFF[|cffB0A9B2{}|cffFFFFFF]: |cffFFFFFF{}|r", prefix, author, message);
    msg = sACISocketHandler->ReplaceEmojis(msg);

    sWorld->SendServerMessage(SERVER_MSG_STRING, msg);
}

void ACIWorldScript::OnAfterConfigLoad(bool reload)
{
    if (!sConfigMgr->GetOption<bool>("ACI.Client.Enable", false))
    {
        return;
    }

    std::string address = sConfigMgr->GetOption<std::string>("ACI.Server.Address", "127.0.0.1");
    uint32 port = sConfigMgr->GetOption<uint32>("ACI.Server.Port", 4411);

    sACISocketHandler->Client = new ACISocketClient(address, port);

    if (reload)
    {
        sACISocketHandler->Client->Disconnect();
    }

    sACISocketHandler->Client->ResetRetries();
    sACISocketHandler->Client->Connect();
    sACISocketHandler->Client->RegisterHandler(ACI_SMSG_MSG, [this]()
    {
        std::string json = sACISocketHandler->Client->ReadString();

        nlohmann::json data = nlohmann::json::parse(json);
        std::string origin = data.at("origin");
        std::string author = data.at("author");
        std::string message = data.at("message");

        SendIChatMessageToAll(99, origin, author, message);
    });
}

void ACIWorldScript::OnUpdate(uint32 diff)
{
    if (!sConfigMgr->GetOption<bool>("ACI.Client.Enable", false))
    {
        return;
    }

    scheduler.Update(diff);

    if (sACISocketHandler->Client->shouldReconnect)
    {
        sACISocketHandler->Client->shouldReconnect = false;
        scheduler.Schedule(5s, [this](TaskContext)
        {
            sACISocketHandler->Client->Disconnect();
            sACISocketHandler->Client->Connect();
        });
    }
}

ChatCommandTable ACICommandsScript::GetCommands() const
{
    static ChatCommandTable commandTable =
    {
        { "ichat", HandleACIChatCommand, SEC_PLAYER, Console::No }
    };

    return commandTable;
}

bool ACICommandsScript::HandleACIChatCommand(ChatHandler* handler, Tail msg)
{
    auto player = handler->GetPlayer();
    if (!player)
    {
        LOG_ERROR("module", "Failed to find player when executing .achat command.");
        return false;
    }

    if (msg.empty())
    {
        ChatHandler(player->GetSession()).SendSysMessage("|cffFF0000You must supply a message.|r");
        return false;
    }

    if (!sACISocketHandler->Client->IsConnected())
    {
        ChatHandler(player->GetSession()).SendSysMessage("|cffFF0000Chat server is not currently connected.|r");
        return false;
    }

    std::string realmName = realm.Name;
    uint32 faction = player->GetTeamId();

    LOG_INFO("module", "Sending player message to server.");
    sACISocketHandler->Client->SendPacketMsg(faction, realmName, player->GetName(), msg.data());

    SendIChatMessageToAll(faction, realmName, player->GetName(), msg.data());

    return true;
}

void SC_AddACIScripts()
{
    new ACICommandsScript();
    new ACIWorldScript();
}
