#ifndef MODULE_ACI_H
#define MODULE_ACI_H

#include "ACISocketClient.h"

#include "ScriptMgr.h"
#include "ChatCommand.h"
#include "ScriptMgr.h"

#include <boost/asio.hpp>
#include <memory>

using namespace Acore::ChatCommands;

enum ACIOpCodes
{
    ACI_CMSG_MSG = 0,
    ACI_SMSG_MSG = 1,
};

class ACISocketHandler
{
private:
    ACISocketHandler() { }

public:
    static ACISocketHandler* GetInstance()
    {
        static ACISocketHandler instance;

        return &instance;
    }

    std::string ReplaceEmojis(std::string msg);

public:
    ACISocketClient* Client;
};

#define sACISocketHandler ACISocketHandler::GetInstance()

class ACIWorldScript : public WorldScript
{
public:
    ACIWorldScript() : WorldScript("ACIWorldScript") { }

    void OnAfterConfigLoad(bool /*reload*/) override;
    void OnUpdate(uint32 /*diff*/) override;

private:
    TaskScheduler scheduler;
};

class ACICommandsScript : public CommandScript
{
public:
    ACICommandsScript() : CommandScript("ACICommandsScript") { }

    ChatCommandTable GetCommands() const override;
    static bool HandleACIChatCommand(ChatHandler* handler, Tail msg);
};

#endif // MODULE_ACI_H
