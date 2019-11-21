using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using AdescomWebService;
using Microsoft.Extensions.Configuration;
using Adescom_CTM_Billings.Models;

namespace Adescom_CTM_Billings
{
    class AdescomCTM
    {
        public IConfiguration _configuration { get; set; }

        public AdescomCTM(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<Client>> GetClientsAsyncWithTimeout(bool includeCLIDs, int? clientId)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(_configuration.GetValue<int>("WebServiceConnectionSettings:RequestTimeout")));

                Task<List<Client>> task;

                if (await Task.WhenAny(task = GetClientsAsync(includeCLIDs, clientId), timeoutTask) == timeoutTask)
                {
                    cancellationTokenSource.Cancel();
                    throw new TimeoutException();
                }

                return await task;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<Client>> GetClientsAsync(bool includeCLIDs, int? clientId)
        {
            try
            {
                string sessionId = "";
                AdescomCTMSoapWrapper atmanCTMWithoutProxyClassesClient = new AdescomCTMSoapWrapper(_configuration);
                ConcurrentBag<Client> clients = new ConcurrentBag<Client>();
                sessionId = await atmanCTMWithoutProxyClassesClient.LoginAsync(
                    _configuration.GetValue<string>("WebServiceConnectionSettings:ResellerLogin"), 
                    _configuration.GetValue<string>("WebServiceConnectionSettings:ResellerPassword"), 
                    _configuration.GetValue<int>("WebServiceConnectionSettings:SessionTimeout"));
                clientDetailsArray clientDetailsArray = await atmanCTMWithoutProxyClassesClient.GetClientsAsync();

                List<clientDetails> clientDetailsFiltered;

                if (clientId.HasValue)
                    clientDetailsFiltered = clientDetailsArray.clients.Where(c => c.clientID == clientId.Value).ToList();
                else
                    clientDetailsFiltered = clientDetailsArray.clients.ToList();

                if (includeCLIDs)
                {
                    if (clientDetailsArray != null && clientDetailsArray.count > 0)
                    {
                        Parallel.ForEach(clientDetailsFiltered, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (client) =>
                        {
                            GetCLIDsForClient(client, atmanCTMWithoutProxyClassesClient, clients);
                        });
                    }
                }
                else
                {
                    foreach (clientDetails clientDetails in clientDetailsFiltered)
                        clients.Add(new Client(clientDetails.clientID, clientDetails.billingDetails.name, new List<Clid>() { }));
                }

                await atmanCTMWithoutProxyClassesClient.LogoutAsync(sessionId);
                return clients.ToList();
            }
            catch (AggregateException ex)
            {
                throw new Exception(ex.InnerException.Message);
            }
        }


        private async void GetCLIDsForClient(clientDetails client, AdescomCTMSoapWrapper atmanCTMWithoutProxyClassesClient, ConcurrentBag<Client> clients)
        {
            List<Clid> clids = new List<Clid>();

            // Get CLIDs
            clidDetailsArray clidDetailsArray = atmanCTMWithoutProxyClassesClient.GetCLIDsForClientAsync(client.clientID).Result;
            if (clidDetailsArray != null && clidDetailsArray.count > 0)
                foreach (var clid in clidDetailsArray.clids)
                    if (clid.active)
                        clids.Add(new Clid(clid.number.countryCode, clid.number.areaCode, clid.number.shortCLID));

            // Get trunk groups CLIDs
            TrunkgroupsArray trunkgroupsArray = atmanCTMWithoutProxyClassesClient.GetTrunkGroupsForClientAsync(client.clientID).Result;
            if (trunkgroupsArray != null && trunkgroupsArray.count > 0)
                foreach (var trunk in trunkgroupsArray.items)
                    if (trunk.active)
                        foreach (var clid in trunk.numbers.items)
                            clids.Add(new Clid(clid.countryCode, clid.areaCode, clid.shortCLID));

            clients.Add(new Client(client.clientID, client.billingDetails.name, clids.Distinct().ToList()));
        }

        public async Task<List<ClientBilling>> GetBillingForClientsAsyncWithTimeout(List<Client> clients, DateTime startDate, DateTime endDate)
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(_configuration.GetValue<int>("WebServiceConnectionSettings:RequestTimeout")));

                Task<List<ClientBilling>> task;

                if (await Task.WhenAny(task = GetBillingForClientsAsync(clients, startDate, endDate), timeoutTask) == timeoutTask)
                {
                    cancellationTokenSource.Cancel();
                    throw new TimeoutException();
                }

                return await task;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<ClientBilling>> GetBillingForClientsAsync(List<Client> clients, DateTime startDate, DateTime endDate)
        {
            try
            {
                string sessionId = "";
                AdescomCTMSoapWrapper atmanCTMWithoutProxyClassesClient = new AdescomCTMSoapWrapper(_configuration);
                BillingQueryOptions billingQueryOptions = new BillingQueryOptions()
                {
                    incoming = false,
                    outgoing = true,
                    includeZeroDuration = false
                };

                ConcurrentBag<ClientBilling> clientsBillings = new ConcurrentBag<ClientBilling>();

                sessionId = await atmanCTMWithoutProxyClassesClient.LoginAsync(
                    _configuration.GetValue<string>("WebServiceConnectionSettings:ResellerLogin"), 
                    _configuration.GetValue<string>("WebServiceConnectionSettings:ResellerPassword"), 
                    _configuration.GetValue<int>("WebServiceConnectionSettings:SessionTimeout"));

                Parallel.ForEach(clients, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (client) =>
                {
                    GetBillingForClient(startDate, endDate, client, atmanCTMWithoutProxyClassesClient, billingQueryOptions, clientsBillings);
                });

                await atmanCTMWithoutProxyClassesClient.LogoutAsync(sessionId);
                return clientsBillings.ToList();
            }
            catch (AggregateException ex)
            {
                throw new Exception(ex.InnerException.Message);
            }
        }

        private async void GetBillingForClient(DateTime fromDate, DateTime toDate, Client client, AdescomCTMSoapWrapper atmanCTMWithoutProxyClassesClient, BillingQueryOptions billingQueryOptions, ConcurrentBag<ClientBilling> clientsBillings)
        {
            try
            {
                List<BillingRecord> billingRecords = new List<BillingRecord>();
                BillingRecordsArrayEx billingRecordsArrayEx = atmanCTMWithoutProxyClassesClient.GetBillingForClientAsync(client.Id, fromDate, toDate).Result;

                if (billingRecordsArrayEx != null && billingRecordsArrayEx.totalCount > 0)
                {
                    foreach (BillingRecordEx billingRecordEx in billingRecordsArrayEx.items)
                    {
                        double price = 0;
                        double priceInclTaxes = 0;

                        if (billingRecordEx.price.HasValue)
                            price = billingRecordEx.price.Value;

                        if (billingRecordEx.priceInclTaxes.HasValue)
                            priceInclTaxes = billingRecordEx.priceInclTaxes.Value;

                        billingRecords.Add(new BillingRecord(billingRecordEx.startDate, billingRecordEx.source, Utils.RemoveCountryCodeFromTelephoneNumber(
                            _configuration.GetValue<string>("ApplicationSettings:CountryCode"), 
                            _configuration.GetValue<int>("ApplicationSettings:NumberOfDigitsForAreaAndShortCLID"), 
                            billingRecordEx.destination), 
                            billingRecordEx.duration, 
                            price));
                    }
                }

                // Billing records filtering by destination
                clientsBillings.Add(new ClientBilling(client, fromDate, toDate, billingRecords.
                    Where(r => ((r.Price != 0) && !(r.Destination.Any(n => char.IsLetter(n)))))
                    .ToList()));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
