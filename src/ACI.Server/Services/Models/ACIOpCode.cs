namespace ACI.Server.Services.Models
{
    public enum ACIOpCode : uint
    {
        CMSG_MSG = 0,
        SMSG_MSG = 1,
        SMSG_AUTH = 2,
        CMSG_AUTH = 3
    }
}
