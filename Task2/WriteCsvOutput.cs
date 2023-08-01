using CRMiddleware.CustomReport;
using HedgeMark.Stats.DataContracts;
using HedgeMark.Stats.DataContracts.DataModel;
using Kent.Boogaart.KBCsv;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPOPQRRegulatoryReport
{
    public class WriteCsvOutput
    {
        internal void GenerateCsvFromAPIData(List<ResponseEntities> responseEntitiesList, string csvFileName, ViewInput input)
        {
            try
            {
                var someRequest = responseEntitiesList.First().AggregationRequest;
                var groupBys = someRequest.GroupFilter;
                var isSecurityLevel = someRequest.IsSecurityRequired;
                var dictOnboardingCustodianCompanyId = (input.TemplateName.Equals(Constants.FundHoldings))
                                                    ? GetFundHoldingsData(string.Join(",", Program._manager.FundDetails.Select(x => x.Id).ToList()))
                                                    : new Dictionary<string, string>();
                var dictOnboardingCustodianCompanyId = (input.TemplateName.Equals(Constants.FundHoldings))
                                                    ? GetFundHoldingsData(string.Join(",", Program._manager.FundDetails.Select(x => x.Id).ToList()))
                                                    : new Dictionary<string, string>();
                
                var securityIDUnique = new List<string>();
                using (var stream = File.OpenWrite(csvFileName))
                using (var csvWriter = new CsvWriter(stream))
                {
                    var headerRecord = new HeaderRecord(input.Headers);
                    csvWriter.WriteRecord(headerRecord);
                    var underlyingSecurityIdentifierUnique = new List<string>();
                    foreach (var responseEntity in responseEntitiesList)
                    {
                        foreach (var response in responseEntity.AggregationResponses.OrderByDescending(x => x.ContextDate))
                        {
                            var fundName = response.Name;

                            foreach (var statResponse in response.StatResponse)
                            {
                                if (!input.IsIncludeTotalRow && statResponse.Group[0].Equals(Constants.TOTAL, StringComparison.InvariantCultureIgnoreCase))
                                    continue;
                                if (isSecurityLevel && string.IsNullOrEmpty(statResponse.SecurityName))
                                    continue;
                                if (input.TemplateName.Contains(Constants.SecurityMaster))
                                {
                                    if ((securityIDUnique.Contains(statResponse.UniverseItems["HMSecurityIDExternal"].StatValue)) ||
                                        (underlyingSecurityIdentifierUnique.Contains(statResponse.UniverseItems["HMSecurityIDExternal"].StatValue)) ||
                                        (securityIDUnique.Contains(statResponse.UniverseItems["Underlying HM Security ID"].StatValue)) ||
                                        (underlyingSecurityIdentifierUnique.Contains(statResponse.UniverseItems["Underlying HM Security ID"].StatValue)))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        if (statResponse.UniverseItems["HMSecurityIDExternal"].StatValue.Any(x => char.IsDigit(x)))
                                            securityIDUnique.Add(statResponse.UniverseItems["HMSecurityIDExternal"].StatValue);
                                        if (statResponse.UniverseItems["Underlying HM Security ID"].StatValue != "")
                                            underlyingSecurityIdentifierUnique.Add(statResponse.UniverseItems["Underlying HM Security ID"].StatValue);
                                    }
                                }
                                var values = new List<string>();
                                var underlyingValues = new List<string>();
                                switch (input.TemplateName)
                                {
                                    case Constants.FundGL:
                                        values = FormatFundGLData(input.Stats, statResponse);
                                        break;
                                    case Constants.Fund:
                                        values = FormatFundData(input.Stats, statResponse);
                                        break;
                                    case Constants.FundHoldings:
                                        values = FormatFundHoldingsData(input.Stats, statResponse, dictOnboardingCustodianCompanyId);
                                        break;
                                    case Constants.FundServiceProviderInfo:
                                        values = FormatFundServiceProviderInfoData(input.Stats, statResponse);
                                        break;
                                    case Constants.SecurityMaster:
                                        (values,underlyingValues) = FormatSecurityMasterData(input.Stats, statResponse);
                                        break;
                                    case Constants.FundReportingMaster:
                                        values = FormatFundReportingMasterData(input.Stats, statResponse);
                                        break;
                                    case Constants.FinalAnswers:
                                        values = FormatFinalAnswersData(input.Stats, statResponse);
                                        break;
                                }

                                csvWriter.WriteRecord(new DataRecord(headerRecord, values));
                                if (input.TemplateName.Equals(Constants.SecurityMaster))
                                {
                                    if (values[values.Count - 1] != "")
                                    {
                                        csvWriter.WriteRecord(new DataRecord(headerRecord, underlyingValues));
                                    }
                                }
                            }
                        }
                    }
                    Log.Info(input.TemplateName + " GenerateCsvFromAPIData - csvWriter.RecordNumber: " + csvWriter.RecordNumber);
                }
            }
            catch (Exception ex)
            {
                Log.Error(input.TemplateName + " " + Constants.Exception + Constants.GenerateCsvFromAPIData + ex.Message, ex);
                throw;
            }
        }

        private List<string> FormatFundGLData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            try
            {
                var lastDateOfMonth = new DateTime(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month, DateTime.DaysInMonth(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month));
                values.Add("1");
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    values.Add(strValue);

                    if (stat.Equals(Constants.QuartertoDatePnL))
                    {
                        values.Add(lastDateOfMonth.ToString(Constants.LastDateOfMonthFormat));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFundGLData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private List<string> FormatFundData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            try
            {
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    values.Add(strValue);

                    if (stat.Equals(Constants.varCurrencyISO))
                    {
                        values.Add("1");
                    }
                    if (stat.Equals(Constants.varFundLongName))
                    {
                        values.Add("Hedge Fund");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFundData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private List<string> FormatFundHoldingsData(string[] stats, StatResponse statResponse, Dictionary<string, string> dictOnboardingCustodianCompanyId)
        {
            var values = new List<string>();
            try
            {
                var lastDateOfMonth = new DateTime(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month, DateTime.DaysInMonth(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month));
                string HMSecurityIDValue=string.Empty;
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    if (stat.Equals(Constants.intFundUniqueID))
                    {

                        values.Add(dictOnboardingCustodianCompanyId.ContainsKey(strValue) ? dictOnboardingCustodianCompanyId[strValue] : "");
                        values.Add("1");
                        values.Add(strValue);
                    }
                    else if (stat.Equals(Constants.AnalysisDate))
                    {
                        values.Add(lastDateOfMonth.ToString(Constants.LastDateOfMonthFormat));
                    }
                    else if (stat.Equals("HMSecurityIDExternal"))
                    {
                        values.Add(strValue);
                        HMSecurityIDValue = strValue;
                    }
                    else if (stat.Equals("Long-Short"))
                    {
                        values.Add(strValue);
                        values.Add(HMSecurityIDValue);
                    }
                    else
                    {
                        values.Add(strValue);
                    }

                    if (stat.Equals(Constants.AUMContribution))
                    {
                        values.Add(strValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFundHoldingsData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private List<string> FormatFundServiceProviderInfoData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            try
            {
                var lastDateOfMonth = new DateTime(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month, DateTime.DaysInMonth(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month));
                values.Add("1");
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    if (stat.Equals(Constants.AnalysisDate))
                    {
                        values.Add(lastDateOfMonth.ToString(Constants.LastDateOfMonthFormat));
                        values.AddRange(new string[] { "", "", "" });
                    }
                    else
                    {
                        values.Add(strValue);
                    }
                }
                
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFundServiceProviderInfoData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private (List<string>, List<string>) FormatSecurityMasterData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            var underlying_values = new List<string>();
            try
            {
                string securityTypeLevel2 = string.Empty;
                values.Add("1");
                underlying_values.Add("1");
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();
                   
                    if (stat.Equals(Constants.RequestIDType))
                    {
                        values.Add((strValue.Equals("BB_UNIQUE") || strValue.Equals("ISIN") || strValue.Equals("SEDOL") || strValue.Equals("CUSIP") || strValue.Equals("TICKER") || strValue.Equals("BB_GLOBAL") || strValue.Equals("AXIOMADATAID")) ? "1" : "0");
                    }
                    else if (stat.Equals(Constants.Rating))
                    {
                        values.Add((strValue.Equals("A") || strValue.Equals("AA") || strValue.Equals("AAA") || strValue.Equals("B") || strValue.Equals("BB") || strValue.Equals("BBB")) ? "1" : "0");
                    }
                    else if (stat.Equals(Constants.SecurityTypeLevel1))
                    {
                        values.Add((strValue.Equals("Derivatives") || strValue.Equals("Fixed Income")) ? "1" : "0");
                    }
                    else if (stat.Equals(Constants.Industry))
                    {
                        values.Add((strValue.Equals("Banks") || strValue.Equals("Financial")) ? "1" : "0");
                    }
                    else if (stat.Equals(Constants.SecurityTypeLevel2))
                    {
                        securityTypeLevel2 = strValue;
                        values.Add((strValue.Equals("Fixed Income - Structured Product")) ? "1" : "0");
                    }
                    else if (stat.Equals(Constants.Tranche))
                    {
                        if (strValue.Equals("Equity"))
                        {
                            values.Add("Subordinate");
                        }
                        else if (strValue.Equals("Mezzanine"))
                        {
                            values.Add(strValue);
                        }
                        else if (strValue.Equals("Super Senior") || strValue.Equals("Senior Support") || strValue.Equals("Super Senior or Senior Support"))
                        {
                            values.Add("Senior");
                        }
                        else
                        {
                            values.Add("");
                        }
                    }
                    else if (stat.Contains("Underlying"))
                    {
                        if (stat.Equals("Underlying Security Type Level 2"))
                        {
                            securityTypeLevel2 = strValue;
                            underlying_values.Add("");
                        }
                        if (stat.Equals("Underlying HM Security ID"))
                        {
                            values.Add(strValue);
                            underlying_values.Add(strValue);
                            underlying_values.Add("");
                        }
                        else
                        {
                            var underlying_data = FormatSecurityMasterUnderlyingData(stat, strValue);
                            underlying_values.Add(underlying_data);
                        }

                    }
                    else if (stat.Equals("UNDL_ID_TICKER"))
                    {
                        underlying_values.Add(strValue);
                    }
                    else
                    {
                        values.Add(strValue);
                        
                    }

                    if (stat.Equals(Constants.Tranche))
                    {
                        values.Add("0");
                        values.Add((securityTypeLevel2.Equals("Futures") || securityTypeLevel2.Equals("Swaps")) ? "1" : "0");
                      
                    }
                    if (stat.Equals("Underlying Tranche"))
                    {
                        underlying_values.Add("0");
                        underlying_values.Add((securityTypeLevel2.Equals("Futures") || securityTypeLevel2.Equals("Swaps")) ? "1" : "0");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatSecurityMasterData + ex.Message, ex);
                throw;
            }
            return (values,underlying_values);
        }
        private string FormatSecurityMasterUnderlyingData(string statData, string strValue)
        {
            if (statData.Equals("Underlying Rating"))
            {
                return (strValue.Equals("A") || strValue.Equals("AA") || strValue.Equals("AAA") || strValue.Equals("B") || strValue.Equals("BB") || strValue.Equals("BBB")) ? "1" : "0";
            }
            else if (statData.Equals("Underlying Security Type Level 1"))
            {
                return (strValue.Equals("Derivatives") || strValue.Equals("Fixed Income")) ? "1" : "0";
            }
            else if (statData.Equals("Underlying Industry"))
            {
                return (strValue.Equals("Banks") || strValue.Equals("Financial")) ? "1" : "0";
            }
            else if (statData.Equals("Underlying Tranche"))
            {
                if (strValue.Equals("Equity"))
                {
                    return "Subordinate";
                }
                else if (strValue.Equals("Mezzanine"))
                {
                    return strValue;
                }
                else if (strValue.Equals("Super Senior") || strValue.Equals("Senior Support") || strValue.Equals("Super Senior or Senior Support"))
                {
                    return "Senior";
                }
                else
                {
                    return "";
                }
            }
            else 
            {
                return strValue;
            }

        }
        private List<string> FormatFundReportingMasterData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            try
            {
                values.Add("1");
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    values.Add(strValue);
                }
                values.AddRange(new string[] { "", "", "", "", "", "CPOPQR", "1", "Normal mode", "CommodityPool", "0" });
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFundReportingMasterData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private List<string> FormatFinalAnswersData(string[] stats, StatResponse statResponse)
        {
            var values = new List<string>();
            try
            {
                var lastDateOfMonth = new DateTime(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month, DateTime.DaysInMonth(Program._manager.ContextDate.Year, Program._manager.ContextDate.Month));
                values.Add("1");
                values.Add(lastDateOfMonth.ToString(Constants.LastDateOfMonthFormat));
                foreach (var stat in stats)
                {
                    var statValue = statResponse.UniverseItems[stat];
                    var strValue = statValue.StatValueType == OutputValueType.Text
                                        ? GetTextStatValue(statValue)
                                        : statValue.NumericStatValue.ToString();

                    values.Add(strValue);
                }
                values.AddRange(new string[] { "", "", "" });

            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatFinalAnswersData + ex.Message, ex);
                throw;
            }
            return values;
        }

        private List<ServiceProviderMasterFiltered> FormatServiceProviderMasterData()
        {
            List<ServiceProviderMasterFiltered> lstServiceProviderMasterFiltered = new List<ServiceProviderMasterFiltered>();
            try
            {
                List<ServiceProviderMaster> lstServiceProviderMaster = GetServiceProviderMasterData(string.Join(",", Program._manager.FundDetails.Select(x => x.Id).ToList()));
                var DictContactDetails = new Dictionary<string, string>();
                foreach (ServiceProviderMaster objServiceProviderMaster in lstServiceProviderMaster)
                {
                    if (!DictContactDetails.ContainsKey(objServiceProviderMaster.BusinessPhone))
                    {
                        DictContactDetails.Add(objServiceProviderMaster.BusinessPhone, objServiceProviderMaster.Address);
                    }
                }
                foreach (var ContactDetails in DictContactDetails)
                {
                    foreach (ServiceProviderMaster objServiceProviderMaster in lstServiceProviderMaster.Where(x => x.BusinessPhone.Equals(ContactDetails.Key) && x.Address.Equals(ContactDetails.Value)))
                    {
                        if (!string.IsNullOrEmpty(objServiceProviderMaster.DmaOnBoardingAdminChoiceId) && lstServiceProviderMasterFiltered.Where(x => x.BusinessPhone.Equals(ContactDetails.Key) && x.Address.Equals(ContactDetails.Value) && x.Service_Provider_Identifier.Equals(objServiceProviderMaster.DmaOnBoardingAdminChoiceId)).Count().Equals(0))
                        {
                            lstServiceProviderMasterFiltered.Add(new ServiceProviderMasterFiltered()
                            {
                                Address = ContactDetails.Value,
                                BusinessPhone = ContactDetails.Key,
                                Legal_Name = objServiceProviderMaster.AdminChoice,
                                Service_Provider_Identifier = objServiceProviderMaster.DmaOnBoardingAdminChoiceId
                            });
                        }

                        if (!string.IsNullOrEmpty(objServiceProviderMaster.FundManagerID) && lstServiceProviderMasterFiltered.Where(x => x.BusinessPhone.Equals(ContactDetails.Key) && x.Address.Equals(ContactDetails.Value) && x.Service_Provider_Identifier.Equals(objServiceProviderMaster.FundManagerID)).Count().Equals(0))
                        {
                            lstServiceProviderMasterFiltered.Add(new ServiceProviderMasterFiltered()
                            {
                                Address = ContactDetails.Value,
                                BusinessPhone = ContactDetails.Key,
                                Legal_Name = objServiceProviderMaster.FundManagerName,
                                Service_Provider_Identifier = objServiceProviderMaster.FundManagerID
                            });
                        }

                        if (!string.IsNullOrEmpty(objServiceProviderMaster.OnboardingCustodianCompanyId) && lstServiceProviderMasterFiltered.Where(x => x.BusinessPhone.Equals(ContactDetails.Key) && x.Address.Equals(ContactDetails.Value) && x.Service_Provider_Identifier.Equals(objServiceProviderMaster.OnboardingCustodianCompanyId)).Count().Equals(0))
                        {
                            lstServiceProviderMasterFiltered.Add(new ServiceProviderMasterFiltered()
                            {
                                Address = ContactDetails.Value,
                                BusinessPhone = ContactDetails.Key,
                                Legal_Name = objServiceProviderMaster.CustodianCompanyName,
                                Service_Provider_Identifier = objServiceProviderMaster.OnboardingCustodianCompanyId
                            });
                        }

                        if (!string.IsNullOrEmpty(objServiceProviderMaster.DmaCounterPartyOnBoardId) && lstServiceProviderMasterFiltered.Where(x => x.BusinessPhone.Equals(ContactDetails.Key) && x.Address.Equals(ContactDetails.Value) && x.Service_Provider_Identifier.Equals(objServiceProviderMaster.DmaCounterPartyOnBoardId)).Count().Equals(0))
                        {
                            lstServiceProviderMasterFiltered.Add(new ServiceProviderMasterFiltered()
                            {
                                Address = ContactDetails.Value,
                                BusinessPhone = ContactDetails.Key,
                                Legal_Name = objServiceProviderMaster.CounterpartyName,
                                Service_Provider_Identifier = objServiceProviderMaster.DmaCounterPartyOnBoardId
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.FormatServiceProviderMasterData + ex.Message, ex);
                throw;
            }
            return lstServiceProviderMasterFiltered;
        }

        internal void GenerateCsvFromTableData(string csvFileName, ViewInput input)
        {
            try
            {
                using (var stream = File.OpenWrite(csvFileName))
                using (var csvWriter = new CsvWriter(stream))
                {
                    var headerRecord = new HeaderRecord(input.Headers);
                    csvWriter.WriteRecord(headerRecord);

                    if (input.TemplateName.Equals(Constants.CounterpartyMaster))
                    {
                        var lstDmaCounterPartyOnBoardId = GetCounterpartyMasterData(string.Join(",", Program._manager.FundDetails.Select(x => x.Id).ToList()));
                        foreach (var DmaCounterPartyOnBoardId in lstDmaCounterPartyOnBoardId)
                        {
                            var values = new List<string>();
                            values.AddRange(new string[] { DmaCounterPartyOnBoardId, "1", "" });
                            csvWriter.WriteRecord(new DataRecord(headerRecord, values));
                        }
                    }
                    else if (input.TemplateName.Equals(Constants.ServiceProviderMaster))
                    {
                        List<ServiceProviderMasterFiltered> lstServiceProviderMasterFiltered = FormatServiceProviderMasterData();

                        foreach (ServiceProviderMasterFiltered objServiceProviderMasterFiltered in lstServiceProviderMasterFiltered)
                        {
                            var values = new List<string>();
                            values.AddRange(new string[] { objServiceProviderMasterFiltered.Address, objServiceProviderMasterFiltered.BusinessPhone, "1", objServiceProviderMasterFiltered.Legal_Name, "", objServiceProviderMasterFiltered.Service_Provider_Identifier });
                            csvWriter.WriteRecord(new DataRecord(headerRecord, values));
                        }
                    }

                    Log.Info(input.TemplateName + " GenerateCsvFromTableData - csvWriter.RecordNumber: " + csvWriter.RecordNumber);
                }
            }
            catch (Exception ex)
            {
                Log.Error(input.TemplateName + " " + Constants.Exception + Constants.GenerateCsvFromTableData + ex.Message, ex);
                throw;
            }
        }

        public static List<string> GetCounterpartyMasterData(string clientFundIDs)
        {
            List<string> lstDmaCounterPartyOnBoardId = new List<string>();

            try
            {
                Log.Info(Constants.GetCounterpartyMasterData + " - Started. clientFundID: " + clientFundIDs);

                string sqlQuery = "select distinct ca.dmaCounterPartyOnBoardId " +
                                    " from ClientFund cf " +
                                    " join Fund f on cf.FundID = f.FundID " +
                                    " join HMADMIN.onboardingfund onf on f.FundID = onf.FundMapId " +
                                    " left join HMADMIN.onboardingContactFundMap ofm on onf.dmaFundOnBoardId = ofm.dmaFundOnBoardId " +
                                    " left join HMADMIN.vw_CounterpartyAgreements ca on onf.dmaFundOnBoardId = ca.dmaFundOnBoardId " +
                                    " where cf.ClientFundID in(" + clientFundIDs + ") " +
                                    " and ca.dmaCounterPartyOnBoardId is not null";

                using (SqlConnection sqlConnection = new SqlConnection(Configurations.DatabaseConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                    {
                        sqlConnection.Open();
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            if (sqlDataReader.HasRows)
                            {
                                while (sqlDataReader.Read())
                                {
                                    if (sqlDataReader["dmaCounterPartyOnBoardId"] != DBNull.Value)
                                    {
                                        lstDmaCounterPartyOnBoardId.Add(sqlDataReader["dmaCounterPartyOnBoardId"].ToString());
                                    }
                                }
                            }
                            sqlConnection.Close();
                        }
                    }
                }

                Log.Info(Constants.GetCounterpartyMasterData + " - Completed. clientFundID: " + clientFundIDs + " lstMmaCounterPartyOnBoardId.Count:" + lstDmaCounterPartyOnBoardId.Count);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.GetCounterpartyMasterData + ex.Message, ex);
                throw;
            }

            return lstDmaCounterPartyOnBoardId;
        }

        public static List<ServiceProviderMaster> GetServiceProviderMasterData(string clientFundIDs)
        {
            List<ServiceProviderMaster> lstServiceProviderMaster = new List<ServiceProviderMaster>();

            try
            {
                Log.Info(Constants.GetServiceProviderMasterData + " - Started. clientFundID: " + clientFundIDs);

                string sqlQuery = "select distinct ocd.Address, ocd.BusinessPhone,  " +
                                    " oac.dmaOnBoardingAdminChoiceId,oac.AdminChoice, " +
                                    " fm.FundManagerID,fm.FundManagerName, " +
                                    " cc.onboardingCustodianCompanyId,cc.CustodianCompanyName,  " +
                                    " ca.dmaCounterPartyOnBoardId, ca.CounterpartyName from " +
                                    " ClientFund cf " +
                                    " join Fund f on cf.FundID = f.FundID " +
                                    " join HMADMIN.onboardingfund onf on f.FundID = onf.FundMapId " +
                                    " left join HMADMIN.onboardingContactFundMap ofm on onf.dmaFundOnBoardId = ofm.dmaFundOnBoardId " +
                                    " left join HMADMIN.vw_CounterpartyAgreements ca on onf.dmaFundOnBoardId = ca.dmaFundOnBoardId " +
                                    " left join HMADMIN.dmaOnBoardingContactDetail ocd on ofm.dmaOnBoardingContactDetailId = ocd.dmaOnBoardingContactDetailId " +
                                    " left join HMADMIN.dmaOnBoardingAdminChoice oac on onf.FundAdministrator = oac.dmaOnBoardingAdminChoiceId " +
                                    " left join FundManager fm on f.FundManagerID = fm.FundManagerID " +
                                    " left join HMADMIN.custodianCompany cc on onf.FundCustodian = cc.onboardingCustodianCompanyId " +
                                    " join HMADMIN.dmaonboardingtype ont on ocd.dmaOnBoardingTypeId = ont.dmaOnBoardingTypeId " +
                                    " where ont.OnBoardingType in('Counterparty', 'Admin', 'Custodian', 'Auditor', 'Investment Manager') " +
                                    " and cf.ClientFundID in(" + clientFundIDs + ") " +
                                    " and ocd.BusinessPhone is not null " +
                                    " order by ocd.BusinessPhone";

                using (SqlConnection sqlConnection = new SqlConnection(Configurations.DatabaseConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                    {
                        sqlConnection.Open();
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            if (sqlDataReader.HasRows)
                            {
                                while (sqlDataReader.Read())
                                {
                                    lstServiceProviderMaster.Add(new ServiceProviderMaster()
                                    {
                                        Address = sqlDataReader["Address"] == DBNull.Value ? String.Empty : sqlDataReader["Address"].ToString(),
                                        BusinessPhone = sqlDataReader["BusinessPhone"] == DBNull.Value ? String.Empty : sqlDataReader["BusinessPhone"].ToString(),
                                        DmaOnBoardingAdminChoiceId = sqlDataReader["dmaOnBoardingAdminChoiceId"] == DBNull.Value ? String.Empty : sqlDataReader["dmaOnBoardingAdminChoiceId"].ToString(),
                                        AdminChoice = sqlDataReader["AdminChoice"] == DBNull.Value ? String.Empty : sqlDataReader["AdminChoice"].ToString(),
                                        FundManagerID = sqlDataReader["FundManagerID"] == DBNull.Value ? String.Empty : sqlDataReader["FundManagerID"].ToString(),
                                        FundManagerName = sqlDataReader["FundManagerName"] == DBNull.Value ? String.Empty : sqlDataReader["FundManagerName"].ToString(),
                                        OnboardingCustodianCompanyId = sqlDataReader["onboardingCustodianCompanyId"] == DBNull.Value ? String.Empty : sqlDataReader["onboardingCustodianCompanyId"].ToString(),
                                        CustodianCompanyName = sqlDataReader["CustodianCompanyName"] == DBNull.Value ? String.Empty : sqlDataReader["CustodianCompanyName"].ToString(),
                                        DmaCounterPartyOnBoardId = sqlDataReader["dmaCounterPartyOnBoardId"] == DBNull.Value ? String.Empty : sqlDataReader["dmaCounterPartyOnBoardId"].ToString(),
                                        CounterpartyName = sqlDataReader["CounterpartyName"] == DBNull.Value ? String.Empty : sqlDataReader["CounterpartyName"].ToString(),
                                    });
                                }
                            }
                            sqlConnection.Close();
                        }
                    }
                }

                Log.Info(Constants.GetServiceProviderMasterData + " - Completed. clientFundID: " + clientFundIDs + " lstServiceProviderMaster.Count:" + lstServiceProviderMaster.Count);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.GetServiceProviderMasterData + ex.Message, ex);
                throw;
            }

            return lstServiceProviderMaster;
        }

        public static Dictionary<string, string> GetFundHoldingsData(string clientFundIDs)
        {
            Dictionary<string, string> dictOnboardingCustodianCompanyId = new Dictionary<string, string>();

            try
            {
                Log.Info(Constants.GetFundHoldingsData + " - Started. clientFundID: " + clientFundIDs);

                string sqlQuery = "select distinct fe.intFundUniqueID,cc.onboardingCustodianCompanyId " +
                                    " from ClientFund cf " +
                                    " join Fund f on cf.FundID = f.FundID " +
                                    " join hFundExternalIdMap fe on cf.ClientFundID = fe.intFundId " +
                                    " join HMADMIN.onboardingfund onf on f.FundID = onf.FundMapId " +
                                    " left join HMADMIN.onboardingContactFundMap ofm on onf.dmaFundOnBoardId = ofm.dmaFundOnBoardId " +
                                    " left join HMADMIN.custodianCompany cc on onf.FundCustodian = cc.onboardingCustodianCompanyId " +
                                    " where cf.ClientFundID in(" + clientFundIDs + ") " +
                                    " and cc.onboardingCustodianCompanyId is not null";

                using (SqlConnection sqlConnection = new SqlConnection(Configurations.DatabaseConnectionString))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(sqlQuery, sqlConnection))
                    {
                        sqlConnection.Open();
                        using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader())
                        {
                            if (sqlDataReader.HasRows)
                            {
                                while (sqlDataReader.Read())
                                {
                                    if (sqlDataReader["intFundUniqueID"] != DBNull.Value && !dictOnboardingCustodianCompanyId.ContainsKey(sqlDataReader["intFundUniqueID"].ToString()))
                                    {
                                        dictOnboardingCustodianCompanyId.Add(sqlDataReader["intFundUniqueID"].ToString(), sqlDataReader["onboardingCustodianCompanyId"].ToString());
                                    }
                                }
                            }
                            sqlConnection.Close();
                        }
                    }
                }

                Log.Info(Constants.GetFundHoldingsData + " - Completed. clientFundID: " + clientFundIDs + " dictOnboardingCustodianCompanyId.Count:" + dictOnboardingCustodianCompanyId.Count);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.Exception + Constants.GetFundHoldingsData + ex.Message, ex);
                throw;
            }

            return dictOnboardingCustodianCompanyId;
        }

        private static string GetTextStatValue(UniversalStatValue statValue)
        {
            var value = statValue.StatValue ?? string.Empty;
            if (value.ToLower().Equals("null"))
                value = string.Empty;
            return value;
        }
    }
    public class ResponseEntities
    {
        public UniversalStatRequest AggregationRequest { get; internal set; }
        public List<UniversalResponse> AggregationResponses { get; set; }
        public string ReportTableName { get; set; }
        public DateTime ContextDate { get; set; }
        public List<GroupFilterDetails> Groups { get; set; }
        public string FundPortName { get; set; }
    }
    public class ServiceProviderMaster
    {
        public string Address { get; set; }
        public string BusinessPhone { get; set; }
        public string DmaOnBoardingAdminChoiceId { get; set; }
        public string AdminChoice { get; set; }
        public string FundManagerID { get; set; }
        public string FundManagerName { get; set; }
        public string OnboardingCustodianCompanyId { get; set; }
        public string CustodianCompanyName { get; set; }
        public string DmaCounterPartyOnBoardId { get; set; }
        public string CounterpartyName { get; set; }
    }
    public class ServiceProviderMasterFiltered
    {
        public string Address { get; set; }
        public string BusinessPhone { get; set; }
        public string Legal_Name { get; set; }
        public string Service_Provider_Identifier { get; set; }
    }
}
