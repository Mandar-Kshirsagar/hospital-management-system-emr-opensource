﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using DanpheEMR.ServerModel;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using DanpheEMR.ServerModel.ReportingModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DanpheEMR.DalLayer
{
    public class PharmacyReportingDbContext : DbContext
    {
        private string connStr = null;
        public PharmacyReportingDbContext(string Conn) : base(Conn)
        {
            connStr = Conn;
            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = false;
        }

        #region Purchase Order Report
        public DataTable PHRMPurchaseOrderReport(DateTime FromDate, DateTime ToDate, string Status)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                 new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                 new SqlParameter("@Status", Status)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";
                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_PurchaseOrderSummaryReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region stock manage detail report
        public DataTable PHRMStockManageDetailReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";
                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_StockManageDetailReport", paramList, this);
            return stockItems;
        }
        #endregion


        #region Deposit Balance Report
        public DataTable PHRMDepositBalanceReport()
        {
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_DepositBalanceReport", this);
            return stockItems;
        }
        #endregion

        #region Dispensary Store Stock report
        public DataTable PHRMDispensaryStoreStockReport(string Status)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
               new SqlParameter("@Status", Status),
            };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";
                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_DispensaryStoreStockReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region Narcorics Dispensary Store Stock report
        public DataTable PHRMNarcoticsDispensaryStoreStockReport()
        {
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_NarcoticsDispensaryStoreStockReport", this);
            return stockItems;
        }
        #endregion

        #region PHRM UserwiseCollection DataTable
        //public DataTable PHRMUserwiseCollectionReport(DateTime FromDate, DateTime ToDate)
        //{
        //    List<SqlParameter> paramList = new List<SqlParameter>() {
        //        new SqlParameter("@FromDate", FromDate),
        //         new SqlParameter("@ToDate", ToDate)
        //    };

        //    foreach (SqlParameter parameter in paramList)
        //    {
        //        if (parameter.Value == null)
        //        {
        //            parameter.Value = "";

        //        }
        //    }
        //    DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_UserwiseCollectionReport", paramList, this);
        //    return stockItems;
        //}
        public DynamicReport PHRMUserwiseCollectionReport(DateTime FromDate, DateTime ToDate, string CounterId, string CreatedBy, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
            new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                    new SqlParameter("@CounterId", CounterId),
                            new SqlParameter("@CreatedBy", CreatedBy == null ? string.Empty : CreatedBy),
                                 new SqlParameter("@StoreId", StoreId)

              };

            DataSet dataSet = DALFunctions.GetDatasetFromStoredProc("SP_PHRM_UserwiseCollectionReport", paramList, this);
            DynamicReport dReport = new DynamicReport();
            if (dataSet.Tables.Count > 0)
            {
                var data = new
                {
                    SalesData = dataSet.Tables[0],
                    SettlementData = dataSet.Tables[1]
                };
                dReport.Schema = null;
                dReport.JsonData = JsonConvert.SerializeObject(data);
            }
            return dReport;
        }
        #endregion

        #region PHRM CashCollectionSummary Report DataTable
        public DataTable PHRMCashCollectionSummaryReport(DateTime FromDate, DateTime ToDate, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                  new SqlParameter("@StoreId", StoreId)
            };

            //foreach (SqlParameter parameter in paramList)
            //{
            //    if (parameter.Value == null)
            //    {
            //        parameter.Value = "";

            //    }
            //}
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_CashCollectionSummaryReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region PHRM Sale Return DataTable
        public DataTable PHRMSaleReturnReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_SaleReturnReport", paramList, this);
            return stockItems;
        }
        #endregion


        #region PHRM Counterwise Collection DataTable
        public DataTable PHRMCunterwiseCollectionReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_CounterCollectionReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region PHRM_ItemWise Sales Report
        public DataTable PHRMItemWiseSalesReport(DateTime fromDate, DateTime toDate, int? itemId, int? storeId, int? counterId, int? userId, int? genericId, string salesType)
        {
            if (salesType == "null")
            {
                salesType = null;
            }

            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", fromDate),
                 new SqlParameter("@ToDate", toDate),
                 new SqlParameter("@ItemId", itemId),
                 new SqlParameter("@StoreId", storeId),
                 new SqlParameter("@CounterId", counterId),
                 new SqlParameter("@CreatedBy", userId),
                 new SqlParameter("@GenericId", genericId),
                 new SqlParameter("@SalesType",salesType)
            };

            DataTable itemWiseSalesData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_ItemWiseSalesReport", paramList, this);
            return itemWiseSalesData;
        }
        #endregion

        #region PHRM_ItemWise Sales Summary Report
        public DataTable PHRMItemWiseSalesSummaryReport(DateTime fromDate, DateTime toDate, int? itemId, int? storeId, int? counterId, int? userId, int? genericId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", fromDate),
                 new SqlParameter("@ToDate", toDate),
                 new SqlParameter("@ItemId", itemId),
                 new SqlParameter("@StoreId", storeId),
                 new SqlParameter("@CounterId", counterId),
                 new SqlParameter("@CreatedBy", userId),
                 new SqlParameter("@GenericId", genericId)
            };

            DataTable itemWiseSalesData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_ItemWiseSalesSummaryReport", paramList, this);
            return itemWiseSalesData;
        }
        #endregion


        #region PHRM Breakage Items DataTable
        public DataTable PHRMBreakageItemReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_BreakageItemReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region PHRM Return To Supplier DataTable
        public DataTable PHRMReturnToSupplierReport(DateTime FromDate, DateTime ToDate, int? SuppId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                 new SqlParameter("@SuppId", SuppId)
            };
            DataTable returntosupplier = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_ReturnToSupplierReport", paramList, this);
            return returntosupplier;
        }
        #endregion

        #region PHRM Return To Store DataTable
        public DataTable PHRMTransferToStoreReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable transfertostore = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_TransferToStoreReport", paramList, this);
            return transfertostore;
        }
        #endregion


        #region PHRM Return To Dispensary DataTable
        public DataTable PHRMTransferToDispensaryReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable transfertodispensary = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_TransferToDispensaryReport", paramList, this);
            return transfertodispensary;
        }
        #endregion

        #region PHRM Goods Receipt Product 
        public DataTable PHRMGoodReceiptProductReport(DateTime FromDate, DateTime ToDate, int ItemId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                 new SqlParameter("@ItemId", ItemId)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GoodsReceiptProductReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region
        public List<PHRMItemWiseStockReportModel> PHRMItemWiseStockReport()
        {
            var data = Database.SqlQuery<PHRMItemWiseStockReportModel>("exec SP_PHRMReport_ItemWiseStockReport").ToList();

            return data.ToList<PHRMItemWiseStockReportModel>();
        }
        #endregion

        #region Doctor Revenue Report        
        public DataTable PHRMSupplierInformationReport()
        {
            DataTable doctorRevenue = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_SupplierInfoReport", this);
            return doctorRevenue;
        }
        #endregion


        #region PHRM Credit InOut Patient Report        
        public DataTable PHRMCreditInOutPatReport(DateTime FromDate, DateTime ToDate, string VisitType)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                new SqlParameter("@VisitType", VisitType)
              
            };

            
            DataTable creditInOutPat = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_LedgerCredit_IndoorOutdoorPatient", paramList, this);
            return creditInOutPat;
        }
        #endregion


        #region PHRM Supplier Stock Summary Report        
        public DataTable PHRMSupplierStockSummaryReport(string SupplierName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@SupplierName", SupplierName) };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable supplierStkSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_SupplierStockSummaryReport", paramList, this);
            return supplierStkSummary;
        }
        #endregion

        #region PHRM Stock Items Report        
        public DataTable PHRMStockItemsReport(string ItemName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ItemName", ItemName) };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_StockItemsReport", paramList, this);
            return stockItems;
        }
        #endregion

        #region PHRM Stock Movement Report        
        public DataTable PHRMStockMovementReport(string ItemName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ItemName", ItemName) };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockMovement = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_StockMovementReport", paramList, this);
            return stockMovement;
        }
        #endregion

        #region PHRM Batch Stock Report        
        public DataTable PHRMBatchStockReport(string ItemName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ItemName", ItemName) };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable batchStock = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_BatchStockReport", paramList, this);
            return batchStock;
        }
        #endregion

        #region PHRM Expiry Report        
        public DataTable PHRMExpiryReport(int? ItemId, int? GenericId, int? StoreId, DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@ItemId", ItemId),
                new SqlParameter("@GenericId", GenericId),
                 new SqlParameter("@StoreId", StoreId),
                  new SqlParameter("@FromDate", FromDate),
                   new SqlParameter("@ToDate", ToDate)
            };

            DataTable expiryStock = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_ExpiryReport", paramList, this);

            return expiryStock;
        }
        #endregion
        #region PHRM Item Wise Purchase Report SP call
        public DataTable PHRMItemWisePurchaseReport(DateTime FromDate, DateTime ToDate, int? itemId, string invoiceNo, int? grNo, int? supplierId, int? genericId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                  new SqlParameter("@ItemId", (Object)itemId ?? DBNull.Value),
                   new SqlParameter("@InvoiceNo", (Object)invoiceNo ?? DBNull.Value),
                    new SqlParameter("@GoodsReceiptNo", (Object)grNo ?? DBNull.Value),
                      new SqlParameter("@SupplierId", (Object)supplierId ?? DBNull.Value),
                       new SqlParameter("@GenericId", (Object)genericId ?? DBNull.Value)
            };
            DataTable purchaseDetails = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_ItemWisePurchaseReport", paramList, this);
            return purchaseDetails;
        }
        #endregion

        #region PHRM Minimum Report    
        public DataTable PHRMMinStockReport(string ItemName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ItemName", ItemName) };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable expiryStock = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_MinStockReport", paramList, this);

            return expiryStock;
        }
        #endregion

        #region PHRM ABC/VED Stock Report    
        public DataTable PHRMABCVEDStockReport(string Status)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@Status", Status)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_ABC/VEDStockReport", paramList, this);

            return stockItems;
        }
        #endregion

        #region PHRM Supplier Stock Report        
        public DataTable PHRMSupplierStockReport(DateTime FromDate, DateTime ToDate, int SupplierId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@SupplierId", SupplierId),
                new SqlParameter("@fromDate",FromDate),
                new SqlParameter("@toDate",ToDate)
            };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable supplierStock = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_SupplierStockReport", paramList, this);
            return supplierStock;
        }
        #endregion

        #region PHRM Ending Stock Summary Report        
        public DataTable PHRMEndingStockSummaryReport(string ItemName)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() { new SqlParameter("@ItemName", ItemName) };


            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable endingStockSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_EndingStockSummaryReport", paramList, this);
            return endingStockSummary;
        }
        #endregion

        #region PHRM Billing Report        

        public DataTable PHRMBillingReport(DateTime FromDate, DateTime ToDate, int? InvoiceNumber, int? PatientId, int? StoreId, string VisitType, string TransactionType, int? SchemeId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                new SqlParameter("@ToDate", ToDate),
                new SqlParameter("@InvoiceNumber", InvoiceNumber),
                new SqlParameter("@PatientId",PatientId),
                new SqlParameter("@StoreId", StoreId),
                new SqlParameter("@VisitType",VisitType =="null" ? null : VisitType),
                new SqlParameter("@TransactionType",TransactionType =="null"? null: TransactionType),
                new SqlParameter("@SchemeId",SchemeId),
            };
            DataTable billingreport = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_BillingReport", paramList, this);
            return billingreport;
        }
        #endregion

        #region PHRM Daily Stock Summary Report        
        public DataTable PHRMDailyStockSummaryReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate", FromDate),
                new SqlParameter("@ToDate", ToDate)
             };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable dailyStkSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_DailyStockSummaryReport", paramList, this);
            return dailyStkSummary;
        }
        #endregion

        #region PHRM  Stock Summary Report        
        public DataTable PHRMStockSummaryReport(DateTime FromDate, DateTime ToDate, int FiscalYearId, int? StoreId, int? ItemId, int? GenericId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate", ToDate),
                new SqlParameter("@FiscalYearId", FiscalYearId),
                new SqlParameter("@StoreId", (Object)StoreId ?? DBNull.Value),
                new SqlParameter("@GenericId", (Object)GenericId ?? DBNull.Value),
                new SqlParameter("@ItemId", (Object)ItemId ?? DBNull.Value)
             };
            //foreach (SqlParameter parameter in paramList)
            //{
            //    if (parameter.Value == null)
            //    {
            //        parameter.Value = null;

            //    }
            //}
            DataTable StkSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_RPT_GetStockSummary", paramList, this);
            return StkSummary;
        }
        #endregion

        #region PHRM  Item Txn Stock Summary Report        
        public DataTable PHRMItemTxnSummaryReport(DateTime FromDate, DateTime ToDate, int ItemId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate", ToDate),
                new SqlParameter("@Itemid", ItemId)
             };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable StkSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_ItemTxnSummaryReport", paramList, this);
            return StkSummary;
        }
        #endregion
        #region Daily stock value report
        public DynamicReport PHRM_Daily_StockValue()
        {
            List<SqlParameter> paramsList = new List<SqlParameter>();
            ReportingDbContext reportingDbContext = new ReportingDbContext(this.connStr);
            DataSet dsDailyRev = DALFunctions.GetDatasetFromStoredProc("SP_Report_PHRM_Daily_StockValue", paramsList, reportingDbContext);

            DynamicReport dReport = new DynamicReport();

            dReport.Schema = null;//we have only one table returning from the database.. 
            //wee need datetime in yyyy-MM-dd format.
            dReport.JsonData = JsonConvert.SerializeObject(dsDailyRev.Tables[0],
                                         new IsoDateTimeConverter() { DateTimeFormat = "yyyy-MM-dd" });
            return dReport;
        }
        #endregion

        #region pharmacy store stock
        public DataTable PHRMStoreStock(String Status)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@Status", Status)
            };

            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable stockItems = DALFunctions.GetDataTableFromStoredProc("SP_PHRMStoreStock", paramList, this);

            return stockItems;
        }
        #endregion

        #region PHRM Drug Category Wise Report        
        public DataTable PHRMDrugCategoryWiseReport(DateTime FromDate, DateTime ToDate, string category)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                new SqlParameter("@ToDate", ToDate),
                new SqlParameter("@Category", category)
            };
            foreach (SqlParameter parameter in paramList)
            {
                if (parameter.Value == null)
                {
                    parameter.Value = "";

                }
            }
            DataTable dailyStkSummary = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_DrugCategoryWiseReport", paramList, this);
            return dailyStkSummary;
        }
        #endregion

        #region Date Wise Purchase Repoprt
        public DataTable PHRMDateWisePurchaseReport(DateTime FromDate, DateTime ToDate, int? supplierId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@SupplierId",supplierId)
            };

            DataTable dateWisePurchaseDetails = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_DateWisePurchaseReport", paramList, this);
            return dateWisePurchaseDetails;
        }
        #endregion

        #region Get Return From Customer Report Data
        public DataTable ReturnFromCustomerReport(DateTime fromDate, DateTime toDate, int? userId, int? dispensaryId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",fromDate),
                new SqlParameter("@ToDate",toDate),
                new SqlParameter("@UserId",userId),
                new SqlParameter("@DispensaryId",dispensaryId),
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("SP_PHRMReport_ReturnFromCustomerReport", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Sales Statement Report
        public DataTable SalesStatementReport(DateTime FromDate, DateTime ToDate)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_SalesStatementReport", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Insurance Patient Bima Report
        public DataTable InsurancePatientBimaReport(DateTime FromDate, DateTime ToDate, int? CounterId, int? UserId, Int64? ClaimCode, string NSHINumber)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@CounterId",CounterId),
                new SqlParameter("@UserId",UserId),
                new SqlParameter("@ClaimCode",ClaimCode),
                new SqlParameter("@NSHINumber",NSHINumber)
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_INS_PATIENT_BIMA", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region PatientSalesDetailReport
        public DataTable PatientSalesDetailReport(DateTime FromDate, DateTime ToDate, int? PatientId, int? CounterId, int? UserId, int? StoreId)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@CounterId",CounterId),
                new SqlParameter("@UserId",UserId),
                new SqlParameter("@StoreId",StoreId),
                new SqlParameter("@PatientId",PatientId)
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_PatientSalesDetail", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Sales Summary Report
        public DataTable SalesSummaryReport(DateTime FromDate, DateTime ToDate)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate)
            };
            DataTable settlementSummaryResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_SalesSummary", paramList, this);
            return settlementSummaryResult;
        }
        #endregion

        #region Settlement Summary Report
        public DataTable SettlementSummaryReport(DateTime FromDate, DateTime ToDate, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@StoreId",StoreId)
            };
            DataTable settlementSummaryResult = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GetSettlementSummaryReport", paramList, this);
            return settlementSummaryResult;
        }
        #endregion
        #region narcotics daily saled  report
        public DataTable NarcoticsDailySalesReport(DateTime FromDate, DateTime ToDate, int? ItemId, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate),
                 new SqlParameter("@ItemId", ItemId),
                 new SqlParameter("@StoreId", StoreId),
            };
            DataTable narcoticsDailySalesData = DALFunctions.GetDataTableFromStoredProc("PHRM_NarcoticsDailySalesReport", paramList, this);
            return narcoticsDailySalesData;
        }
        #endregion


        #region Purchase Summary Report
        public DataTable PurchaseSummaryReport(DateTime FromDate, DateTime ToDate, int? StoreId)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@StoreId",StoreId),
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_PurchaseSummary", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Stock Summary II Report
        public DataTable StockSummarySecondReport(DateTime TillDate)
        {
            //var TillDatePlusOne = TillDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@TillDate",TillDate)
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_StockSummary2", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Stock Transfers Report
        public DataTable PHRMStockTransfersReport(DateTime FromDate, DateTime ToDate, int? itemId, int? sourceStoreId, int? targetStoreId, bool notReceivedStocks)
        {
            //var ToDatePlusOne = ToDate.AddDays(1);//sud:12ul'22--No need to add 1 day. In SP, Convert DATETIME -> DATE and Compare the Date.
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@ItemId",itemId),
                new SqlParameter("@SourceStoreId",sourceStoreId),
                new SqlParameter("@TargetStoreId",targetStoreId),
                new SqlParameter("@NotReceivedStocks",notReceivedStocks)
            };
            DataTable stockTransfersResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_StockTransfers", paramList, this);
            return stockTransfersResult;
        }
        #endregion

        #region Supplier Wise Stock report
        public DataTable PHRMSupplierWiseStockReport(DateTime FromDate, DateTime ToDate, int? itemId, int? storeId, int? supplierId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@ItemId",itemId),
                new SqlParameter("@StoreId",storeId),
                new SqlParameter("@SupplierId",supplierId)
            };
            DataTable supplierWiseStockReportResult = DALFunctions.GetDataTableFromStoredProc("SP_Report_Pharmacy_SupplierWiseStock", paramList, this);
            return supplierWiseStockReportResult;
        }
        #endregion

        #region Return On Investment Report
        public DataTable ReturnOnInvestmentReport(DateTime FromDate, DateTime ToDate)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {
                new SqlParameter("@FromDate", FromDate),
                 new SqlParameter("@ToDate", ToDate)
            };
            DataTable narcoticsDailySalesData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_Report_ReturnOnInvestment", paramList, this);
            return narcoticsDailySalesData;
        }
        #endregion
        #region Multiple PaymentModeWiseReport
        public DynamicReport PHRM_PaymentModeWiseReport(DateTime FromDate, DateTime ToDate, string PaymentMode, string Type, int? User, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>() {  new SqlParameter("@FromDate", FromDate),
                            new SqlParameter("@ToDate", ToDate),
                            new SqlParameter("@PaymentMode", PaymentMode),
                            new SqlParameter("@Type", Type),
                            new SqlParameter("@User", User),
                            new SqlParameter("@StoreId", StoreId)
                              };
            DataSet dataSet = DALFunctions.GetDatasetFromStoredProc("SP_PHRM_MultiplePaymentModeWiseReport", paramList, this);
            DynamicReport dReport = new DynamicReport();
            if (dataSet.Tables.Count > 0)
            {
                var data = new
                {
                    InvoiceWisePaymentModeReport = dataSet.Tables[0],
                    ReportSummary = dataSet.Tables[1]
                };
                dReport.Schema = null;
                dReport.JsonData = JsonConvert.SerializeObject(data);
            }
            return dReport;
        }
        #endregion

        #region Stock Transfers Report
        public DataTable RankMembershipwiseSalesReport(DateTime FromDate, DateTime ToDate, string Rank, string Membership)
        {

            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",FromDate),
                new SqlParameter("@ToDate",ToDate),
                new SqlParameter("@Ranks", Rank),
                new SqlParameter("@Memberships", Membership),

            };

            DataTable RankMembershipwiseSalesResult = DALFunctions.GetDataTableFromStoredProc("SP_Phrm_RankMembershipwiseSalesReport", paramList, this);
            return RankMembershipwiseSalesResult;
        }
        #endregion

        #region Generic Wise Purchase Summary Report
        public DataTable GenericWisePurchaseSummaryReport(DateTime fromDate, DateTime toDate, int? genericId, int? supplierId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",fromDate),
                new SqlParameter("@ToDate",toDate),
                new SqlParameter("@GenericId",genericId),
                new SqlParameter("@SupplierId",supplierId),
            };
            DataTable genericWisePurchaseSummaryReportData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GenericWisePurchaseSummaryReport", paramList, this);
            return genericWisePurchaseSummaryReportData;
        }

        #endregion

        #region Stock Transfers Summary Report
        public DataTable PHRMStockTransferSummaryReport(DateTime fromDate, DateTime toDate, int? genericId, int? sourceStoreId, int? targetStoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@FromDate",fromDate),
                new SqlParameter("@ToDate",toDate),
                new SqlParameter("@GenericId",genericId),
                new SqlParameter("@SourceStoreId",sourceStoreId),
                new SqlParameter("@TargetStoreId",targetStoreId)
            };
            DataTable stockTransfersResult = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_StockTransferSummaryReport", paramList, this);
            return stockTransfersResult;
        }
        #endregion


        #region VisitWiseBillSummaryReport
        public DataTable VisitWiseBillSummaryReport(int? PatientId, string VisitType, bool ShowDischargeStatementBillReport)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@VisitType",VisitType =="null" ? null : VisitType),
                new SqlParameter("@PatientId",PatientId),
                new SqlParameter("@ShowDischargeStatementBillReport",ShowDischargeStatementBillReport)

            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_VisitWiseBillReport", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion
        #region VisitWiseBillSummaryDetailReport
        public DataTable VisitWiseBillSummaryDetailReport(string PaymentMode, int PatientVisitId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@PaymentMode",PaymentMode =="null" ? null : PaymentMode),
                new SqlParameter("@PatientVisitId",PatientVisitId)
            };
            DataTable returnFromCustomerResult = DALFunctions.GetDataTableFromStoredProc("PHRM_RPT_VisitWiseBillDetailsReport", paramList, this);
            return returnFromCustomerResult;
        }
        #endregion

        #region Provisional Sales Report
        public DataTable ProvisionalSalesReport(int? PatientId, string VisitType, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@PatientId", PatientId),
                new SqlParameter("@VisitType",VisitType),
                new SqlParameter("@StoreId",StoreId)
            };
            DataTable provisionalSaleReportData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_ProvisionalSalesReport", paramList, this);
            return provisionalSaleReportData;
        }
        #endregion

        #region Provisional Sales Detail Report
        public DataTable ProvisionalSalesDetailReport(int? PatientId, string VisitType,int SchemeId, int? StoreId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>()
            {
                new SqlParameter("@PatientId", PatientId),
                new SqlParameter("@VisitType",VisitType),
                new SqlParameter("@SchemeId",SchemeId),
                new SqlParameter("@StoreId",StoreId),
            };
            DataTable provisionalSalesDetailReportData = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_ProvisionalSalesDetailReport", paramList, this);
            return provisionalSalesDetailReportData;
        }
        #endregion
    }
}
