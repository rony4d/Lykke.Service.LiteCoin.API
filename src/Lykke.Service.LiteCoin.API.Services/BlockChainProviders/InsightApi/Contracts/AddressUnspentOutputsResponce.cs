﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Lykke.Service.LiteCoin.API.Services.BlockChainProviders.InsightApi.Contracts
{
    public class AddressUnspentOutputsResponce
    {
        [JsonProperty("satoshis")]
        public long Satoshi { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmation { get; set; }

        [JsonProperty("scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [JsonProperty("vout")]
        public int N { get; set; }

        [JsonProperty("txid")]
        public string TxHash { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }
}
