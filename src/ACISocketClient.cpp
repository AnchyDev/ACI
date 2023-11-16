#include "ACI.h"
#include "ACISocketClient.h"

#include <json.hpp>

uint32 ACISocketClient::ReadUInt32()
{
    std::array<char, sizeof(uint32_t)> dataBuf;
    socket.read_some(boost::asio::buffer(dataBuf));

    uint32_t data;
    std::memcpy(&data, dataBuf.data(), sizeof(uint32_t));

    return data;
}

std::string ACISocketClient::ReadString()
{
    uint32_t msgSize = ReadUInt32();

    std::array<char, 1024> dataBuf;
    socket.read_some(boost::asio::buffer(dataBuf, msgSize));

    return std::string(dataBuf.data(), msgSize);
}

void ACISocketClient::ReceiveHandler()
{
    uint32_t opCode = ReadUInt32();

    LOG_INFO("module", "Got OpCode: {}", opCode);

    auto it = handlers.find(opCode);
    if (it == handlers.end())
    {
        LOG_ERROR("module", "Received invalid OpCode: {}", opCode);
        return;
    }

    // Run handler
    handlers[opCode]();
}

void ACISocketClient::RegisterHandler(uint32_t opCode, const std::function<void()>& operation)
{
    handlers.emplace(opCode, operation);
}

void ACISocketClient::Connect()
{
    if (retryCounter >= 5)
    {
        LOG_WARN("module", "Reached maximum retries (5), reload config to manually reconnect to server.");
        return;
    }

    LOG_INFO("module", "Connecting to ACI server (attempt {})..", retryCounter + 1);
    //LOG_INFO("module", "Currently inside thread: {}", std::hash<std::thread::id>{}(std::this_thread::get_id()));

    try
    {
        socket.connect(endpoint);

        LOG_INFO("module", "Connected!");
        shouldReconnect = false;
        retryCounter = 0;

        LOG_INFO("module", "Starting listen thread..");

        listening = true;
        io_context_thread = std::thread([this]()
            {
                while (listening)
                {
                    try
                    {
                        ReceiveHandler();
                    }
                    catch (std::exception ex)
                    {
                        LOG_ERROR("module", "An error occured while trying to read data from the socket: {}", ex.what());
                        break;
                    }
                }

                LOG_INFO("module", "Lost connection to the server, starting reconnect..");
                shouldReconnect = true;
            });
        io_context_thread.detach();
    }
    catch (std::exception ex)
    {
        LOG_ERROR("module", ex.what());
        shouldReconnect = true;
        retryCounter++;
    }
}

void ACISocketClient::Disconnect()
{
    if (!socket.is_open())
    {
        return;
    }

    listening = false;
    socket.close();
}

bool ACISocketClient::IsConnected()
{
    return (socket.is_open());
}

void ACISocketClient::ResetRetries()
{
    retryCounter = 0;
}

void ACISocketClient::SendPacketMsg(std::string realm, std::string name, std::string msg)
{
    try
    {
        uint32 opCode = (uint32)ACI_CMSG_MSG;

        nlohmann::json json;
        json["origin"] = realm;
        json["author"] = name;
        json["message"] = msg;

        std::string data = json.dump();
        uint32 size = data.size();

        socket.send(boost::asio::buffer(&opCode, sizeof(uint32)));
        socket.send(boost::asio::buffer(&size, sizeof(uint32)));
        socket.send(boost::asio::buffer(data));
    }
    catch (std::exception ex)
    {
        LOG_ERROR("module", ex.what());
    }
}
