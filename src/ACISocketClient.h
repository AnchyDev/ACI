#ifndef MODULE_ACI_SOCKET_CLIENT_H
#define MODULE_ACI_SOCKET_CLIENT_H

#include <boost/asio.hpp>
#include <random>

class ACISocketClient
{
public:
    ACISocketClient(std::string address, uint32_t port) : resolver(io_context), socket(io_context), buffer(), listening(false), shouldReconnect(false), retryCounter(0)
    {
        endpoint = boost::asio::ip::tcp::endpoint(boost::asio::ip::make_address(address), port);
    }

    void ReceiveHandler();
    void RegisterHandler(uint32_t opCode, const std::function<void()>& operation);

    void Connect();
    void Disconnect();

    bool IsConnected();
    void ResetRetries();

    void SendPacketMsg(std::string realm, std::string name, std::string msg);

    uint32_t ReadUInt32();
    std::string ReadString();

public:
    boost::asio::io_context io_context;
    boost::asio::ip::tcp::resolver resolver;
    boost::asio::ip::tcp::socket socket;
    boost::asio::ip::tcp::endpoint endpoint;

    std::thread io_context_thread;

    bool shouldReconnect;
private:
    boost::asio::mutable_buffer buffer;
    std::unordered_map<uint32_t, std::function<void()>> handlers;

    bool listening;
    uint32_t retryCounter;
};

#endif // MODULE_ACI_SOCKET_CLIENT_H
