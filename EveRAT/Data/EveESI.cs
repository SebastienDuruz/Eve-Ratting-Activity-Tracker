/// Author : Sébastien Duruz
/// Date : 03.05.2023

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ESI.NET;
using ESI.NET.Enumerations;
using ESI.NET.Models.Universe;
using Microsoft.Extensions.Options;
using Structure = ESI.NET.Models.Sovereignty.Structure;

namespace EveContractsFetcher.Data
{
    /// <summary>
    /// Class EveESI
    /// </summary>
    public class EveESI
    {
        /// <summary>
        /// ESI Client
        /// </summary>
        public EsiClient Client { get; set; }

        /// <summary>
        /// Expiration of the current SystemKills Data
        /// </summary>
        public DateTime? CurrentExpires { get; set; }

        /// <summary>
        /// Start of the current SystemKills Data
        /// </summary>
        public DateTime? CurrentLastModified { get; set; }

        /// <summary>
        /// Custom Constructor
        /// </summary>
        /// <param name="clientId">ClientId</param>
        /// <param name="secretKey">SecretKey</param>
        /// <param name="callbackUrl">callbackUrl</param>
        /// <param name="userAgent">userAgent</param>
        public EveESI(string clientId, string secretKey, string callbackUrl, string userAgent)
        {
            IOptions<EsiConfig> config = Options.Create(new EsiConfig()
            {
                EsiUrl = "https://esi.evetech.net/",
                DataSource = DataSource.Tranquility,
                ClientId = clientId,
                SecretKey = secretKey,
                CallbackUrl = callbackUrl,
                UserAgent = userAgent
            });
            Client = new EsiClient(config);
        }

        /// <summary>
        /// Fetch the current kills for systems
        /// </summary>
        public async Task<List<Kills>> FetchCurrentKills()
        {
            CurrentExpires = (await Client.Universe.Kills()).Expires;
            CurrentLastModified = (await Client.Universe.Kills()).LastModified;

            return (await Client.Universe.Kills()).Data;
        }

        /// <summary>
        /// Fetch the current Sovereignty for systems
        /// </summary>
        public async Task<List<Structure>> FetchCurrentSov()
        {
            return (await Client.Sovereignty.Structures()).Data;
        }
    }
}