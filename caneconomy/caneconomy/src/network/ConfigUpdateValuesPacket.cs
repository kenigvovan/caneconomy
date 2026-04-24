using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Datastructures;
using static caneconomy.src.Config;

namespace caneconomy.src.network
{
    [ProtoContract]
    public class ConfigUpdateValuesPacket
    {
        [ProtoMember(1)]
        public string EXTENDED_COINS_VALUES_TO_CODE_PRIVATE;
    }
}
