using AdescomWebService;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Adescom_CTM_Billings
{
    class AdescomCTMSoapWrapper
    {
        public IConfiguration _configuration { get; set; }
        HttpClient _httpClient;
        Uri _webServiceUri;
      
        const string _loginRequest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ns1=""http://www.example.org/ResellerAPI/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:enc=""http://www.w3.org/2003/05/soap-encoding"">
                                <env:Body>
                                    <ns1:login env:encodingStyle=""http://www.w3.org/2003/05/soap-encoding"">
                                        <username xsi:type=""xsd:string"">%USERNAME%</username>
                                        <password xsi:type=""xsd:string"">%PASSWORD%</password>
                                        <timeout xsi:type=""xsd:int"">%TIMEOUT%</timeout>
                                    </ns1:login>
                                </env:Body>
                            </env:Envelope>";


        const string _clientsRequest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
	                            <s:Body s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
		                            <q1:getClients xmlns:q1=""http://www.example.org/ResellerAPI/"">
			                            <params href=""#id1""/>
		                            </q1:getClients>
		                            <q2:clientSearchParams id=""id1"" xsi:type=""q2:clientSearchParams"" xmlns:q2=""http://www.example.org/ResellerAPI/"">
			                            <resellerID xsi:type=""xsd:int"">0</resellerID>
		                            </q2:clientSearchParams>
	                            </s:Body>
                            </s:Envelope>";

        const string _CLIDsRequest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
                                <s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                                    <q1:getCLIDsForClient xmlns:q1=""http://www.example.org/ResellerAPI/"">
                                        <clientID xsi:type=""xsd:int"">%CLIENTID%</clientID>
                                    </q1:getCLIDsForClient>
                                </s:Body>
                            </s:Envelope>";

        const string _trunkGroupsRequest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ns1=""http://www.example.org/ResellerAPI/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:enc=""http://www.w3.org/2003/05/soap-encoding"">
                                <env:Body>
                                    <ns1:getTrunkgroupsForClient env:encodingStyle=""http://www.w3.org/2003/05/soap-encoding"">
                                        <clientID xsi:type=""xsd:int"">%CLIENTID%</clientID>
                                    </ns1:getTrunkgroupsForClient>
                                </env:Body>
                            </env:Envelope>";

        const string _billingRequest = @"<env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ns1=""http://www.example.org/ResellerAPI/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:enc=""http://www.w3.org/2003/05/soap-encoding"">
                                        <env:Body>
                                            <ns1:getBillingForClient env:encodingStyle=""http://www.w3.org/2003/05/soap-encoding"">
                                                <clientID xsi:type=""xsd:int"">%CLIENTID%</clientID>
                                                <fromDate xsi:type=""xsd:dateTime"">%FROMDATE%</fromDate>
                                                <toDate xsi:type=""xsd:dateTime"">%TODATE%</toDate>
                                                <options xsi:type=""ns1:BillingQueryOptions"">
                                                    <incoming xsi:type=""xsd:boolean"">false</incoming>
                                                    <outgoing xsi:type=""xsd:boolean"">true</outgoing>
                                                    <includeZeroDuration xsi:type=""xsd:boolean"">false</includeZeroDuration>
                                                    <countCDRs xsi:type=""xsd:boolean"">true</countCDRs>
                                                    <sumCDRs xsi:type=""xsd:boolean"">true</sumCDRs>
                                                    <strict xsi:type=""xsd:boolean"">true</strict>
                                                </options>
                                            </ns1:getBillingForClient>
                                        </env:Body>
                                    </env:Envelope>";

        const string logoutRequest = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                            <env:Envelope xmlns:env=""http://www.w3.org/2003/05/soap-envelope"" xmlns:ns1=""http://www.example.org/ResellerAPI/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:enc=""http://www.w3.org/2003/05/soap-encoding"">
                                <env:Body>
                                    <ns1:logout env:encodingStyle=""http://www.w3.org/2003/05/soap-encoding"">
                                        <sessionID xsi:type=""xsd:string"">%SESSIONID%</sessionID>
                                    </ns1:logout>
                                </env:Body>
                            </env:Envelope>";


        public AdescomCTMSoapWrapper(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _webServiceUri = new Uri(_configuration.GetValue<string>("WebServiceConnectionSettings:Url"));
        }

        public async Task<string> LoginAsync(string username, string password, int timeout)
        {
            try
            {
                return await GetSoapReponseForLogin(username, password, timeout);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<clientDetailsArray> GetClientsAsync()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(clientDetailsArray));
            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((await GetSoapReponseForGetClientsAsync()).Replace("getClientsResponse", "clientDetailsArray")));
            return xmlSerializer.Deserialize(memoryStream) as clientDetailsArray;
        }

        public async Task<clidDetailsArray> GetCLIDsForClientAsync(int clinetId)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(clidDetailsArray));
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((await GetSoapReponseForGetCLIDsForClientAsync(clinetId)).Replace("getCLIDsForClientResponse", "clidDetailsArray")));
                return xmlSerializer.Deserialize(memoryStream) as clidDetailsArray;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<TrunkgroupsArray> GetTrunkGroupsForClientAsync(int clinetId)
        {
            try
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(TrunkgroupsArray));
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes((await GetSoapReponseForGetTrunkGroupsForClientAsync(clinetId)).Replace("getTrunkgroupsForClientResponse", "TrunkgroupsArray")));
                return xmlSerializer.Deserialize(memoryStream) as TrunkgroupsArray;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<BillingRecordsArrayEx> GetBillingForClientAsync(int clientId, DateTime fromDate, DateTime toDate)
        {
            try
            { 
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(BillingRecordsArrayEx));
                var aa = (await GetSoapReponseForGetBillingForClientAsync(clientId, fromDate, toDate)).Replace("records", "BillingRecordsArrayEx");
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(aa));
                return xmlSerializer.Deserialize(memoryStream) as BillingRecordsArrayEx;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> LogoutAsync(string sessionID)
        {
            try
            {
                return await GetSoapReponseForLogoutAsync(sessionID);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForLogin(string username, string password, int timeout)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(_loginRequest.Replace("%USERNAME%", username.ToString()).Replace("%PASSWORD%", password.ToString()).Replace("%TIMEOUT%", timeout.ToString())));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                    .Descendants((XNamespace)"http://www.w3.org/2003/05/soap-envelope" + "Body")
                    .First()
                    .Descendants("loginResponse")
                    .Single()
                    .Value;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForGetClientsAsync()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(_clientsRequest));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                    .Descendants((XNamespace)"http://schemas.xmlsoap.org/soap/envelope/" + "Body")
                    .First()
                    .DescendantNodes()
                    .Skip(1)
                    .First().ToString().Replace("ns1:", "");
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForGetCLIDsForClientAsync(int clientId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(_CLIDsRequest.Replace("%CLIENTID%", clientId.ToString())));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                .Descendants((XNamespace)"http://schemas.xmlsoap.org/soap/envelope/" + "Body")
                .First()
                .DescendantNodes()
                .Skip(1)
                .First()
                .ToString().Replace("ns1:", "");
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForGetTrunkGroupsForClientAsync(int clientId)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(_trunkGroupsRequest.Replace("%CLIENTID%", clientId.ToString())));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                    .Descendants((XNamespace)"http://www.w3.org/2003/05/soap-envelope" + "Body")
                    .First()
                    .DescendantNodes()
                    .Skip(3)
                    .First()
                    .ToString().Replace("ns1:", "");
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForGetBillingForClientAsync(int clientId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(_billingRequest.Replace("%CLIENTID%", clientId.ToString()).Replace("%FROMDATE%", fromDate.ToString("o")).Replace("%TODATE%", toDate.ToString("o"))));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                    .Descendants((XNamespace)"http://schemas.xmlsoap.org/soap/envelope/" + "Body")
                    .First()
                    .DescendantNodes()
                    .Skip(1)
                    .First()
                    .ToString().Replace("ns1:", "");    // Date format 2018-02-03T00:00:00.0000000+01:00
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        public async Task<string> GetSoapReponseForLogoutAsync(string sessionID)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync(_webServiceUri, new StringContent(logoutRequest.Replace("%SESSIONID%", sessionID)));
                response.EnsureSuccessStatusCode();

                return XDocument.Parse(await response.Content.ReadAsStringAsync())
                    .Descendants((XNamespace)"http://www.w3.org/2003/05/soap-envelope" + "Body")
                    .First()
                    .Descendants("logoutResponse")
                    .Single()
                    .Value;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
    }
}
