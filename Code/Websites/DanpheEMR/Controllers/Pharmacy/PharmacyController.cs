﻿using DanpheEMR.CommonTypes;
using DanpheEMR.Core;
using DanpheEMR.Core.Configuration;
using DanpheEMR.Core.Parameters;
using DanpheEMR.DalLayer;
using DanpheEMR.Enums;
using DanpheEMR.Security;
using DanpheEMR.ServerModel;
using DanpheEMR.ServerModel.CommonModels;
using DanpheEMR.ServerModel.MasterModels;
using DanpheEMR.ServerModel.NotificationModels;
using DanpheEMR.ServerModel.PharmacyModels;
using DanpheEMR.Services;
using DanpheEMR.Utilities;
using DanpheEMR.ViewModel.Dispensary;
using DanpheEMR.ViewModel.Pharmacy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml;
// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace DanpheEMR.Controllers
{

    public class PharmacyController : CommonController
    {
        public static IHostingEnvironment _environment;
        bool realTimeRemoteSyncEnabled = false;
        private readonly PharmacyDbContext _pharmacyDbContext;
        private readonly BillingDbContext _billingDbContext;
        public PharmacyController(IHostingEnvironment env, IOptions<MyConfiguration> _config) : base(_config)
        {

            realTimeRemoteSyncEnabled = _config.Value.RealTimeRemoteSyncEnabled;
            _environment = env;
            _pharmacyDbContext = new PharmacyDbContext(connString);
            _billingDbContext = new BillingDbContext(connString);
        }
        // GET: api/values
        [HttpGet]
        public string Get(string reqType, int? supplierId, int itemTypeId, int companyId, int categoryId, string status, int purchaseOrderId, int goodsReceiptId, int itemId, string batchNo, int returnToSupplierId, int invoiceId, int writeOffId, int employeeId, int? patientId, int providerId, int visitId, bool IsOutdoorPat, int requisitionId, int dispatchId, DateTime currentDate, DateTime FromDate, DateTime ToDate, int FiscalYearId, int DispensaryId,
            int settlementId, int invoiceid, int? gdprintId, int invoiceretid, string CancelRemarks, int storeId, string invoiceNo, int organizationId, int CreditNotePrintId, int? PriceCategoryId, int? PatientVisitId, string MemberNo)
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            PharmacyDbContext phrmdbcontext = new PharmacyDbContext(connString);
            RbacDbContext rbacDbContext = new RbacDbContext(connString);
            MasterDbContext masterDbContext = new MasterDbContext(connString);
            PatientDbContext patientDbContext = new PatientDbContext(connString);
            BillingDbContext billingDbContext = new BillingDbContext(connString);

            try
            {
                #region GET: list of suppliers
                if (reqType == "supplier")
                {
                    var supplierList = phrmdbcontext.PHRMSupplier.AsEnumerable().OrderBy(a => a.SupplierName)
                        .ToList().Where(x => x.IsActive == true);
                    responseData.Status = "OK";
                    responseData.Results = supplierList;
                }
                #endregion
                #region GET: setting-supplier manage
                else if (reqType == "allSupplier")
                {
                    var supplierList = phrmdbcontext.PHRMSupplier.AsEnumerable()
                        .ToList();
                    responseData.Status = "OK";
                    responseData.Results = supplierList;
                }
                #endregion
                else if (reqType == "getCounter")
                {
                    var getCounter = (from counter in phrmdbcontext.PHRMCounters
                                      select counter
                                      ).ToList<PHRMCounter>().OrderBy(b => b.CounterId);
                    responseData.Status = "OK";
                    responseData.Results = getCounter;
                }
                else if (reqType == "get-credit-organizations") //--shankar 21st may 2020
                {
                    var creditOrganizations = phrmdbcontext.CreditOrganizations.ToList();
                    if (creditOrganizations != null && creditOrganizations.Count > 0)
                    {
                        creditOrganizations = creditOrganizations.OrderBy(c => c.OrganizationName).ToList();
                    }
                    responseData.Status = "OK";
                    responseData.Results = creditOrganizations;
                }
                else if (reqType == "allSaleRecord")
                {
                    var currentdate = currentDate;
                    var TomorrowDate = currentDate.AddDays(1);
                    var invData = phrmdbcontext.PHRMInvoiceTransaction.Where(a => a.BilStatus == "paid" && (a.CreateOn >= currentdate || a.PaidDate >= currentdate)).ToList();
                    var invRetData = phrmdbcontext.PHRMInvoiceReturnModel.ToList();
                    var depositData = phrmdbcontext.DepositModel.ToList();
                    var settlementData = phrmdbcontext.PHRMSettlements.Where(a => a.CreatedOn >= currentdate).ToList();
                    #region Usewise Sales 
                    var getUserSales = invData.Where(a => (a.CreateOn > currentdate && a.CreateOn < TomorrowDate) && (a.IsReturn != true)). //&& (a.IsReturn == null)
                                          Select(st => new
                                          {
                                              CreatedBy = ((st.SettlementId != null) ? (settlementData.Where(s => s.SettlementId == st.SettlementId).Select(ss => ss.CreatedBy).FirstOrDefault()) : (st.CreatedBy)),
                                              TotalAmount = ((st.SettlementId != null) ? (decimal)(settlementData.Where(s => s.SettlementId == st.SettlementId).Select(ss => ss.PaidAmount).FirstOrDefault()) : (decimal)(st.PaidAmount))

                                          }).
                                           GroupBy(a => new { a.CreatedBy }).Select(g => new UserCollectionViewModel
                                           {
                                               UserId = g.Key.CreatedBy.Value,
                                               UserSale = g.Sum(s => s.TotalAmount)
                                           })
                                           .GroupJoin(masterDbContext.Employees.ToList(), a => a.UserId, b => b.EmployeeId, (a, b) => new UserCollectionViewModel
                                           {
                                               UserId = a.UserId,
                                               UserSale = a.UserSale,
                                               UserName = b.Select(s => s.FullName).FirstOrDefault(),

                                           });



                    var depositAmt = depositData.Where(a => a.CreatedOn > currentDate && a.CreatedOn < TomorrowDate)
                        .GroupBy(x => new { x.CreatedBy, x.DepositType })
                        .Select(g => new UserCollectionViewModel
                        {
                            UserId = g.Key.CreatedBy.Value,
                            DepositType = g.Key.DepositType,
                            UserSale = (decimal)g.Sum(s => s.DepositAmount)
                        }).ToList();
                    //to calculate return amount
                    var getUserSaleRet = invRetData.ToList().Where(a => a.CreatedOn > currentdate && a.CreatedOn < TomorrowDate && a.InvoiceId != null && a.PaymentMode != "credit")
                        .GroupBy(a => new { a.CreatedBy })
                        .Select(g => new UserCollectionViewModel
                        {
                            UserId = g.Key.CreatedBy,
                            UserSale = g.Sum(s => s.TotalAmount)
                        })
                        .GroupJoin(masterDbContext.Employees.ToList(), a => a.UserId, b => b.EmployeeId, (a, b) => new UserCollectionViewModel
                        {
                            UserId = a.UserId,
                            UserSale = a.UserSale,
                            UserName = b.Select(s => s.FullName).FirstOrDefault(),
                        });

                    var netSales = getUserSales.GroupJoin(getUserSaleRet, a => a.UserId, b => b.UserId, (a, b) => new UserCollectionViewModel
                    {
                        UserSale = a.UserSale - b.Select(s => s.UserSale).FirstOrDefault(),
                        UserId = a.UserId,
                        UserName = a.UserName,
                    });
                    netSales = netSales.GroupJoin(depositAmt, a => a.UserId, b => b.UserId, (a, b) => new UserCollectionViewModel
                    {
                        UserSale = a.UserSale + b.Where(x => x.DepositType == "deposit").Sum(x => x.UserSale) - b.Where(x => x.DepositType == "depositreturn").Sum(x => x.UserSale),
                        UserId = a.UserId,
                        UserName = a.UserName,
                    });
                    #endregion

                    #region Counter Sales
                    var getCounterSales = invData.Where(a => (a.CreateOn > currentdate) && (a.CreateOn < TomorrowDate && (a.IsReturn != true)) && (a.CounterId > 0)).
                         Select(st => new
                         {
                             CounterId = ((st.SettlementId != null) ? (settlementData.Where(s => s.SettlementId == st.SettlementId).Select(ss => ss.CounterId).FirstOrDefault()) : (st.CounterId)),
                             TotalAmount = ((st.SettlementId != null) ? (decimal)(settlementData.Where(s => s.SettlementId == st.SettlementId).Select(ss => ss.PaidAmount).FirstOrDefault()) : (decimal)(st.PaidAmount))

                         }).
                        GroupBy(a => new { a.CounterId }).Select(g => new CounterCollectionViewModel
                        {
                            CounterId = g.Key.CounterId.Value,
                            CounterSale = g.Sum(s => s.TotalAmount)
                        }).GroupJoin(phrmdbcontext.PHRMCounters.ToList(), a => a.CounterId, b => b.CounterId, (a, b) => new CounterCollectionViewModel
                        {
                            CounterId = a.CounterId,
                            CounterSale = a.CounterSale,
                            CounterName = b.Select(s => s.CounterName).FirstOrDefault(),

                        });
                    var depositByCounter = depositData.Where(a => a.CreatedOn > currentDate && a.CreatedOn < TomorrowDate)
                        .GroupBy(x => new { x.DepositType, x.CounterId })
                        .Select(g => new CounterCollectionViewModel
                        {
                            CounterId = g.Key.CounterId.Value,
                            DepositType = g.Key.DepositType,
                            CounterDeposit = (decimal)g.Sum(s => s.DepositAmount)
                        }).ToList();
                    //to calculate return amount
                    var getCounterSaleRet = invRetData.ToList().Where(a => (a.CreatedOn > currentdate) && (a.CreatedOn < TomorrowDate) && (a.CounterId != null) && a.InvoiceId != null && a.PaymentMode != "credit")
                        .GroupBy(a => new { a.CounterId }).Select(g => new CounterCollectionViewModel
                        {
                            CounterId = g.Key.CounterId,
                            CounterSale = g.Sum(s => s.TotalAmount)
                        }).GroupJoin(phrmdbcontext.PHRMCounters.ToList(), a => a.CounterId, b => b.CounterId, (a, b) => new CounterCollectionViewModel
                        {
                            CounterId = a.CounterId,
                            CounterSale = a.CounterSale,
                            CounterName = b.Select(s => s.CounterName).FirstOrDefault(),
                        }
                        );
                    var netCounterSales = getCounterSales.GroupJoin(getCounterSaleRet, a => a.CounterId, b => b.CounterId, (a, b) => new CounterCollectionViewModel
                    {
                        CounterSale = a.CounterSale - b.Select(s => s.CounterSale).FirstOrDefault(),
                        CounterId = a.CounterId,
                        CounterName = a.CounterName,
                    });
                    netCounterSales = netCounterSales.GroupJoin(depositByCounter, a => a.CounterId, b => b.CounterId, (a, b) => new CounterCollectionViewModel
                    {
                        CounterSale = a.CounterSale + b.Where(x => x.DepositType == "deposit").Sum(x => x.CounterDeposit) - b.Where(x => x.DepositType == "depositreturn").Sum(x => x.CounterDeposit),
                        CounterId = a.CounterId,
                        CounterName = a.CounterName,
                    });
                    #endregion

                    TotalCollectionViewModel allRecords = new TotalCollectionViewModel();

                    var GrossSale = invData.Where(a => a.CreateOn > currentdate && a.CreateOn < TomorrowDate || a.PaidDate > currentdate).
                                                                                Select(st => new
                                                                                {
                                                                                    TotalAmount = ((st.SettlementId != null) ? (decimal)(settlementData.Where(s => s.SettlementId == st.SettlementId
                                                                                ).Select(ss => ss.PaidAmount).FirstOrDefault()) : (decimal)(st.PaidAmount))
                                                                                }).Sum(s => s.TotalAmount);


                    allRecords.TotalReturn = invRetData.Where(a => a.CreatedOn > currentdate && a.CreatedOn < TomorrowDate && a.InvoiceId != null && a.PaymentMode != "credit").Sum(s => s.PaidAmount);
                    allRecords.CreditAmount = phrmdbcontext.PHRMInvoiceTransaction.Where(a => a.CreateOn > currentdate && a.CreateOn < TomorrowDate && a.BilStatus == "unpaid").Sum(s => s.PaidAmount);

                    allRecords.CreditReturn = invRetData.Where(a => a.CreatedOn > currentdate && a.CreatedOn < TomorrowDate && a.PaymentMode == "credit").Sum(s => s.PaidAmount);
                    allRecords.TotalDeposit = depositData.Where(a => a.CreatedOn > currentdate && a.CreatedOn < TomorrowDate && a.DepositType == "deposit").Sum(s => s.DepositAmount);
                    allRecords.DepositReturned = depositData.Where(a => a.CreatedOn > currentdate && a.CreatedOn < TomorrowDate && a.DepositType == "depositreturn").Sum(s => s.DepositAmount);

                    allRecords.TotalSale = GrossSale + allRecords.CreditAmount + (decimal)allRecords.TotalDeposit;
                    allRecords.NetSale = allRecords.TotalSale - (decimal)allRecords.DepositReturned - allRecords.TotalReturn - allRecords.CreditAmount;


                    allRecords.UserCollection = netSales; // netSales;
                    allRecords.CounterCollection = netCounterSales; //  netCounterSales;
                    responseData.Status = "OK";
                    responseData.Results = allRecords;


                }
                else if (reqType == "phrm-pending-bills")
                {
                    decimal? provisionalTotal = phrmdbcontext.PHRMInvoiceTransactionItems
                        .Where(itm => itm.BilItemStatus == "provisional").Sum(itm => itm.TotalAmount);


                    var creditTotObj = (from inv in phrmdbcontext.PHRMInvoiceTransaction.Where(x => x.BilStatus == "unpaid")
                                        join invRet in phrmdbcontext.PHRMInvoiceReturnModel
                                        on
                                        inv.InvoiceId equals invRet.InvoiceId into returnsOfUnpaidInvoice
                                        from invoiceReturn in returnsOfUnpaidInvoice.DefaultIfEmpty()
                                        select new
                                        {
                                            invoiceId = inv.InvoiceId,
                                            paidTotal = inv.PaidAmount,
                                            returnTotal = invoiceReturn != null ? invoiceReturn.PaidAmount : 0.00m

                                        }).ToList();

                    var creditTotAmt = creditTotObj.GroupBy(x => x.invoiceId).Sum(g => g.FirstOrDefault().paidTotal) - creditTotObj.Sum(x => x.returnTotal);

                    //var creditTotObj = (from inv in phrmdbcontext.PHRMInvoiceTransaction
                    //                    join invRet in phrmdbcontext.PHRMInvoiceReturnModel on inv.InvoiceId equals invRet.InvoiceId into invRetJ
                    //                    where inv.BilStatus == "unpaid"
                    //                    select new
                    //                    {
                    //                        TotalAmount = inv.TotalAmount,
                    //                        TotalReturn = invRetJ.Sum(x => x.TotalAmount != null ? x.TotalAmount : 0)
                    //                    }).FirstOrDefault();


                    //decimal? creditTotAmt = phrmdbcontext.PHRMInvoiceTransaction.Where(txn => txn.BilStatus == "unpaid")
                    //    .GroupJoin(phrmdbcontext.PHRMInvoiceReturnModel, inv => inv.InvoiceId, invret => invret.InvoiceId, (inv, invret) => new { inv, invret })
                    //    .SelectMany(a => a.invret.DefaultIfEmpty(), (inv, invretLJ) => new { inv.inv, invretLJ })
                    //    .Select(x => new
                    //    {
                    //        TotalAmount = x.inv.TotalAmount,
                    //        TotalReturn = x.invretLJ != null ? x.invretLJ.TotalAmount : 0
                    //    })
                    //    .Sum(txn => txn.TotalAmount - txn.TotalReturn);

                    var retObject = new
                    {
                        TotalProvisional = provisionalTotal,
                        TotalCredits = creditTotAmt

                    };

                    responseData.Results = retObject;
                    responseData.Status = "OK";
                }
                else if (reqType == "counterSales")
                {
                    var getCounterSales = (from counter in phrmdbcontext.PHRMCounters
                                           select counter
                                      ).ToList<PHRMCounter>().OrderBy(b => b.CounterId);
                    responseData.Status = "OK";
                    responseData.Results = getCounterSales;
                }
                #region getting Supplier details according the supplierId
                else if (reqType == "SupplierDetails")
                {
                    List<PHRMSupplierModel> supplierDetails = new List<PHRMSupplierModel>();
                    supplierDetails = (from supplier in phrmdbcontext.PHRMSupplier
                                       where supplier.SupplierId == supplierId
                                       select supplier).ToList();
                    responseData.Status = "OK";
                    responseData.Results = supplierDetails;
                }
                #endregion
                #region GET: itemtypelist in setting-itemtype manage and in create new order
                else if (reqType == "itemtype")
                {
                    var itemtypeList = (from itmtype in phrmdbcontext.PHRMItemType
                                        join categry in phrmdbcontext.PHRMCategory on itmtype.CategoryId equals categry.CategoryId
                                        select new
                                        {
                                            ItemTypeId = itmtype.ItemTypeId,
                                            CategoryId = itmtype.CategoryId,
                                            ItemTypeName = itmtype.ItemTypeName,
                                            CategoryName = categry.CategoryName,
                                            Description = itmtype.Description,
                                            IsActive = itmtype.IsActive
                                        }).ToList().OrderBy(a => a.ItemTypeId);
                    responseData.Status = "OK";
                    responseData.Results = itemtypeList;
                }
                #endregion
                #region GET: Item type list in setting-item manage
                else if (reqType == "GetItemType")
                {
                    var itmtypelist = (from itemtype in phrmdbcontext.PHRMItemType
                                       select new
                                       {
                                           ItemTypeId = itemtype.ItemTypeId,
                                           ItemTypeName = itemtype.ItemTypeName,
                                           IsActive = itemtype.IsActive
                                       }).ToList().OrderBy(a => a.ItemTypeId);
                    responseData.Status = "OK";
                    responseData.Results = itmtypelist;
                }
                #endregion
                #region GET: Packing type list in setting-PackingType manage
                else if (reqType == "GetPackingType")
                {
                    var packingtypelist = (from packingtype in phrmdbcontext.PHRMPackingType
                                           select new
                                           {
                                               PackingTypeId = packingtype.PackingTypeId,
                                               PackingName = packingtype.PackingName,
                                               PackingQuantity = packingtype.PackingQuantity,
                                               IsActive = packingtype.IsActive
                                           }).ToList().OrderBy(a => a.PackingTypeId);
                    responseData.Status = "OK";
                    responseData.Results = packingtypelist;
                }
                #endregion
                #region GET: setting-item manage : list of items
                else if (reqType == "item")
                {
                    var itemList = (from I in phrmdbcontext.PHRMItemMaster.Include(itm => itm.PHRM_MAP_MstItemsPriceCategories)
                                    join compny in phrmdbcontext.PHRMCompany on I.CompanyId equals compny.CompanyId
                                    join itmtype in phrmdbcontext.PHRMItemType on I.ItemTypeId equals itmtype.ItemTypeId
                                    join catType in phrmdbcontext.PHRMCategory on itmtype.CategoryId equals catType.CategoryId
                                    join unit in phrmdbcontext.PHRMUnitOfMeasurement on I.UOMId equals unit.UOMId
                                    join generic in phrmdbcontext.PHRMGenericModel on I.GenericId equals generic.GenericId
                                    join salesCat in phrmdbcontext.PHRMStoreSalesCategory on I.SalesCategoryId equals salesCat.SalesCategoryId
                                    //from rack  in phrmdbcontext.PHRMRackItem.Where(a => a.ItemId == I.ItemId).DefaultIfEmpty()
                                    let GRI = phrmdbcontext.PHRMGoodsReceiptItems.Where(GRI => I.ItemId == GRI.ItemId).OrderByDescending(GRI => GRI.GoodReceiptItemId).FirstOrDefault()
                                    select new
                                    {
                                        ItemId = I.ItemId,
                                        ItemName = I.ItemName,
                                        IsNarcotic = I.IsNarcotic,
                                        ItemCode = I.ItemCode,
                                        CompanyId = I.CompanyId,
                                        CompanyName = compny.CompanyName,
                                        ItemTypeId = I.ItemTypeId,
                                        ItemTypeName = itmtype.ItemTypeName,
                                        UOMId = I.UOMId,
                                        UOMName = unit.UOMName,
                                        ReOrderQuantity = I.ReOrderQuantity,
                                        MinStockQuantity = I.MinStockQuantity,
                                        BudgetedQuantity = I.BudgetedQuantity,
                                        PurchaseVATPercentage = I.PurchaseVATPercentage,
                                        SalesVATPercentage = I.SalesVATPercentage,
                                        IsVATApplicable = I.IsVATApplicable,
                                        IsActive = I.IsActive,
                                        PackingTypeId = I.PackingTypeId,
                                        IsInternationalBrand = I.IsInternationalBrand,
                                        GenericId = I.GenericId,
                                        ABCCategory = I.ABCCategory,
                                        Dosage = I.Dosage,
                                        GRItemPrice = (GRI != null) ? GRI.GRItemPrice : 0,
                                        GenericName = generic.GenericName,
                                        CategoryName = catType.CategoryName,
                                        StoreRackId = I.StoreRackId,
                                        IsBatchApplicable = salesCat.IsBatchApplicable,
                                        IsExpiryApplicable = salesCat.IsExpiryApplicable,
                                        SalesCategoryId = salesCat.SalesCategoryId,
                                        VED = I.VED,
                                        CCCharge = I.CCCharge,
                                        IsInsuranceApplicable = I.IsInsuranceApplicable,
                                        GovtInsurancePrice = I.GovtInsurancePrice,
                                        PHRM_MAP_MstItemsPriceCategories = I.PHRM_MAP_MstItemsPriceCategories,
                                        RackNoDetails = (from rackItem in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == I.ItemId)
                                                         join rack in phrmdbcontext.PHRMRack on rackItem.RackId equals rack.RackId
                                                         select new
                                                         {
                                                             RackNo = rack.RackNo
                                                         }).ToList()
                                    }).ToList().OrderBy(a => a.ItemId);
                    responseData.Status = "OK";
                    responseData.Results = itemList;
                }
                #endregion
                #region GET: setting-tax manage : list of tax
                else if (reqType == "tax")
                {
                    var taxList = phrmdbcontext.PHRMTAX.ToList();
                    responseData.Status = "OK";
                    responseData.Results = taxList;
                }
                #endregion
                #region Get ItemList by ItemType
                else if (reqType == "GetAllItems")
                {
                    var itemList = phrmdbcontext.PHRMItemMaster.Where(a => a.IsActive).ToList();
                    responseData.Status = "OK";
                    responseData.Results = itemList;
                }
                #endregion
                #region Get: All Item List
                else if (reqType == "GetItemListByItemTypeId")
                {
                    var itmList = (from ItemList in phrmdbcontext.PHRMItemMaster
                                   join itemtype in phrmdbcontext.PHRMItemType on ItemList.ItemTypeId equals itemtype.ItemTypeId
                                   where ItemList.IsActive == true && ItemList.ItemTypeId == itemTypeId ////tis and condition is for selecting itemlist on the basis of ItemType selected by End-User  
                                   select new
                                   {
                                       ItemId = ItemList.ItemId,
                                       ItemName = ItemList.ItemName,
                                       CompanyId = ItemList.CompanyId,
                                       MinStockQuantity = ItemList.MinStockQuantity,
                                       ReOrderQuantity = ItemList.ReOrderQuantity,
                                       //SupplierId = ItemList.SupplierId,
                                       UOMId = ItemList.UOMId,
                                       VATPercentage = ItemList.PurchaseVATPercentage,
                                       BudgetedQuantity = ItemList.BudgetedQuantity,
                                       ItemCode = ItemList.ItemCode,
                                       IsVATApplicable = ItemList.IsVATApplicable
                                   }).ToList().OrderBy(a => a.ItemId);
                    responseData.Status = "OK";
                    responseData.Results = itmList;
                }
                #endregion
                #region GET: setting-company manage : list of companies
                else if (reqType == "company")
                {
                    var companyList = phrmdbcontext.PHRMCompany.ToList();
                    responseData.Status = "OK";
                    responseData.Results = companyList;
                }
                #endregion
                #region GET: setting-category manage : list of categories
                else if (reqType == "category")
                {
                    var categoryList = phrmdbcontext.PHRMCategory.ToList();
                    responseData.Status = "OK";
                    responseData.Results = categoryList;
                }
                #endregion
                #region GET: setting-unitofmeasurement manage : list of unit of measurements
                else if (reqType == "unitofmeasurement")
                {
                    var unitofmeasurementList = phrmdbcontext.PHRMUnitOfMeasurement.ToList();
                    responseData.Status = "OK";
                    responseData.Results = unitofmeasurementList;
                }
                #endregion
                #region GET: Order->OrderList: Getting List of All Purchase Order Generated Against Supplier
                else if (reqType == "getPHRMOrderList")
                {
                    //in status there is comma seperated values so we are splitting status by using  comma(,)
                    // this all we have do because we have to check multiple status at one call
                    //like when user select all we have to we get all PO by matching the status like complete,active,partial and initiated...
                    string[] poSelectedStatus = status.Split(',');
                    var realToDate = ToDate.AddDays(1);

                    var purchaseOrderList = (from po in phrmdbcontext.PHRMPurchaseOrder
                                             join supp in phrmdbcontext.PHRMSupplier on po.SupplierId equals supp.SupplierId
                                             join stats in poSelectedStatus on po.POStatus equals stats
                                             join term in phrmdbcontext.Terms on po.TermsId equals term.TermsId into termJ
                                             from termLJ in termJ.DefaultIfEmpty()
                                             orderby po.PODate descending
                                             select new
                                             {
                                                 PurchaseOrderId = po.PurchaseOrderId,
                                                 PurchaseOrderNo = po.PurchaseOrderNo,
                                                 SupplierId = po.SupplierId,
                                                 PODate = po.PODate,
                                                 POStatus = po.POStatus,
                                                 SubTotal = po.SubTotal,
                                                 TotalAmount = po.TotalAmount,
                                                 VATAmount = po.VATAmount,
                                                 SupplierName = supp.SupplierName,
                                                 ContactNo = supp.ContactNo,
                                                 ContactAddress = supp.ContactAddress,
                                                 Email = supp.Email,
                                                 City = supp.City,
                                                 Pin = supp.PANNumber,
                                                 TermText = termLJ.Text
                                             }).Where(s => s.PODate > FromDate && s.PODate < realToDate).ToList();

                    responseData.Status = "OK";
                    responseData.Results = purchaseOrderList;
                }
                #endregion               
                #region GET: Order->OrderList: Getting POItemsList By POId
                else if (reqType == "getPHRMPOItemsByPOId")
                {
                    var poItemsList = (from poitem in phrmdbcontext.PHRMPurchaseOrderItems
                                       join itms in phrmdbcontext.PHRMItemMaster on poitem.ItemId equals itms.ItemId
                                       join po in phrmdbcontext.PHRMPurchaseOrder on poitem.PurchaseOrderId equals po.PurchaseOrderId
                                       join supplier in phrmdbcontext.PHRMSupplier on po.SupplierId equals supplier.SupplierId
                                       /// join company in phrmdbcontext.PHRMCompany on itms.CompanyId equals company.CompanyId
                                       /// join UOM in phrmdbcontext.PHRMUnitOfMeasurement on itms.UOMId equals UOM.UOMId

                                       where poitem.PurchaseOrderId == purchaseOrderId
                                       select new
                                       {
                                           ItemName = itms.ItemName,
                                           ItemId = itms.ItemId,
                                           VatPercentage = itms.PurchaseVATPercentage,
                                           Quantity = poitem.Quantity,

                                           StandaredPrice = poitem.StandardRate,
                                           SubTotal = poitem.SubTotal,
                                           VATAmount = poitem.VATAmount,
                                           TotalAmount = poitem.TotalAmount,
                                           PurchaseOrderId = poitem.PurchaseOrderId,
                                           POItemStatus = poitem.POItemStatus,
                                           ReceivedQuantity = poitem.ReceivedQuantity,
                                           PendingQuantity = poitem.PendingQuantity,
                                           //PODate = po.PODate,
                                           SupplierId = supplier.SupplierId,
                                           SupplierName = supplier.SupplierName,
                                           Pin = supplier.PANNumber,
                                           Email = supplier.Email,
                                           ContactNo = supplier.ContactNo,
                                           ContactAddress = supplier.ContactAddress,
                                           City = supplier.City,
                                           Remarks = po.Remarks,
                                           IsCancel = poitem.IsCancel
                                       }
                                ).Where(s => s.IsCancel == false || s.IsCancel == null).ToList();
                    responseData.Status = "OK";
                    responseData.Results = poItemsList;
                }
                #endregion
                #region Get nusring drugs request details.
                else if (reqType == "get-provisional-items")
                {
                    AdmissionDbContext adtdbContext = new AdmissionDbContext(connString);

                    var WardName = (from ptinfo in adtdbContext.PatientBedInfos.AsEnumerable()
                                    join wd in adtdbContext.Wards.AsEnumerable() on ptinfo.WardId equals wd.WardId
                                    select new { ptinfo.PatientId, wd.WardName }).ToList().AsEnumerable();

                    var provisionalItems = (from pro in phrmdbcontext.DrugRequistion.AsEnumerable()
                                            join pat in phrmdbcontext.PHRMPatient.AsEnumerable() on pro.PatientId equals pat.PatientId
                                            where pro.Status == status
                                            select new
                                            {

                                                ShortName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                ContactNo = pat.PhoneNumber,
                                                CreatedOn = pro.CreatedOn,
                                                Status = pro.Status,
                                                PatientId = pro.PatientId,
                                                RequisitionId = pro.RequisitionId,
                                                WardName = WardName.Where(w => w.PatientId == pat.PatientId).Select(s => s.WardName).FirstOrDefault()
                                            }).ToList();

                    responseData.Status = "OK";
                    responseData.Results = provisionalItems;

                }
                else if (reqType == "get-all-provisional-items")
                {
                    var provisionalItems = (from pro in phrmdbcontext.DrugRequistion
                                            join pat in phrmdbcontext.PHRMPatient on pro.PatientId equals pat.PatientId
                                            orderby pro.RequisitionId descending
                                            select new
                                            {

                                                ShortName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                ContactNo = pat.PhoneNumber,
                                                CreatedOn = pro.CreatedOn,
                                                Status = pro.Status,
                                                PatientId = pro.PatientId,
                                                RequisitionId = pro.RequisitionId,

                                            }).ToList();
                    responseData.Status = "OK";
                    responseData.Results = provisionalItems;

                }
                #endregion
                #region ward requests by wards
                else if (reqType == "get-ward-requested-items")
                {

                    var realToDate = ToDate.AddDays(1);
                    var wardReqList = (from R in phrmdbcontext.StoreRequisition
                                       join E in phrmdbcontext.Employees on R.CreatedBy equals E.EmployeeId
                                       join S in phrmdbcontext.PHRMStore.Where(s => s.Category == "substore") on R.StoreId equals S.StoreId
                                       select new GetAllRequisitionDTO
                                       {
                                           RequisitionId = R.RequisitionId,
                                           RequisitionNo = R.RequisitionNo,
                                           RequisitionDate = R.RequisitionDate,
                                           RequisitionStatus = R.RequisitionStatus,
                                           RequistingStore = S.Name,
                                           CreatedByName = E.Salutation + ". " + E.FirstName + " " + (string.IsNullOrEmpty(E.MiddleName) ? "" : E.MiddleName + " ") + E.LastName,
                                           CanDispatchItem = true,
                                           CanApproveTransfer = (R.RequisitionStatus == "pending") ? true : false
                                       }).Where(s => s.RequisitionDate >= FromDate && s.RequisitionDate <= realToDate).OrderByDescending(s => s.RequisitionDate).ThenBy(s => s.CanApproveTransfer == false).ThenBy(s => s.RequisitionStatus).ToList();
                    responseData.Status = ENUM_DanpheHttpResponseText.OK;
                    responseData.Results = wardReqList;
                }
                #endregion
                #region drugs request items by selected patient.
                else if (reqType == "get-drugs-request-items")
                {
                    var drugsItem = (from itm in phrmdbcontext.DrugRequistionItem
                                     join itmReq in phrmdbcontext.DrugRequistion on itm.RequisitionId equals itmReq.RequisitionId
                                     join itmName in phrmdbcontext.PHRMItemMaster on itm.ItemId equals itmName.ItemId
                                     where itm.RequisitionId == requisitionId
                                     select new
                                     {
                                         RequisitionItemId = itm.RequisitionItemId,
                                         RequisitionId = itm.RequisitionId,
                                         ItemId = itm.ItemId,
                                         Quantity = itm.Quantity,
                                         ItemName = itmName.ItemName,
                                         PatientId = itmReq.PatientId,
                                     }).ToList();
                    responseData.Status = "OK";
                    responseData.Results = drugsItem;
                }
                #endregion
                #region get requsted drug-list by patientId and VisitId (used in Emergency) 
                else if (reqType == "get-all-drugs-order")
                {
                    var drugsItem = (from itm in phrmdbcontext.DrugRequistionItem
                                     join itmReq in phrmdbcontext.DrugRequistion on itm.RequisitionId equals itmReq.RequisitionId
                                     join itmName in phrmdbcontext.PHRMItemMaster on itm.ItemId equals itmName.ItemId
                                     where itmReq.PatientId == patientId && itmReq.VisitId == visitId
                                     select new
                                     {
                                         RequisitionItemId = itm.RequisitionItemId,
                                         RequisitionId = itm.RequisitionId,
                                         ItemId = itm.ItemId,
                                         Quantity = itm.Quantity,
                                         ItemName = itmName.ItemName,
                                         PatientId = itmReq.PatientId,
                                         PatientVisitId = itmReq.VisitId,
                                         Status = itmReq.Status,
                                         RequestedOn = itmReq.CreatedOn
                                     }).ToList();
                    responseData.Status = "OK";
                    responseData.Results = drugsItem;
                }
                #endregion
                else if (reqType == "get-drugs-dispatch-items")
                {
                    var itm = (from phrmItm in phrmdbcontext.DrugRequistion
                               where phrmItm.RequisitionId == requisitionId
                               select phrmItm).FirstOrDefault();

                    if (itm.ReferenceId is null)
                    {
                        responseData.Results = null;
                    }
                    else
                    {
                        List<int> items = itm.ReferenceId.Split(',').Select(int.Parse).ToList();

                        var drugsItem = (from drugItems in phrmdbcontext.PHRMInvoiceTransactionItems
                                         join itmName in phrmdbcontext.PHRMItemMaster on drugItems.ItemId equals itmName.ItemId
                                         where items.Contains(drugItems.InvoiceItemId)
                                         select new
                                         {
                                             ItemId = drugItems.ItemId,
                                             Quantity = drugItems.Quantity,
                                             ItemName = itmName.ItemName,
                                         }).ToList();
                        responseData.Results = drugsItem;
                    }
                    responseData.Status = "OK";
                }
                #region GET: order-goods receipt grid : gets list of goodsreceipt
                else if (reqType == "goodsreceipt")
                {
                    var goodsReceiptList = (from gr in phrmdbcontext.PHRMGoodsReceipt
                                            join supp in phrmdbcontext.PHRMSupplier on gr.SupplierId equals supp.SupplierId
                                            join fy in phrmdbcontext.PharmacyFiscalYears on gr.FiscalYearId equals fy.FiscalYearId
                                            join rbac in phrmdbcontext.Users on gr.CreatedBy equals rbac.EmployeeId
                                            orderby gr.CreatedOn descending
                                            // where gr.IsCancel==IsCancel
                                            select new
                                            {
                                                GoodReceiptId = gr.GoodReceiptId,
                                                GoodReceiptPrintId = gr.GoodReceiptPrintId,
                                                PurchaseOrderId = gr.PurchaseOrderId,
                                                InvoiceNo = gr.InvoiceNo,
                                                GoodReceiptDate = gr.GoodReceiptDate,
                                                CreatedOn = gr.CreatedOn,             //once GoodReceiptDate is been used replaced createdOn by GoodReceiptDate
                                                SubTotal = gr.SubTotal,
                                                DiscountAmount = gr.DiscountAmount,
                                                VATAmount = gr.VATAmount,
                                                TotalAmount = gr.TotalAmount,
                                                Remarks = gr.Remarks,
                                                SupplierName = supp.SupplierName,
                                                ContactNo = supp.ContactNo,
                                                City = supp.City,
                                                Pin = supp.PANNumber,
                                                ContactAddress = supp.ContactAddress,
                                                Email = supp.Email,
                                                IsCancel = gr.IsCancel,
                                                SupplierId = supp.SupplierId,
                                                UserName = rbac.UserName,
                                                CurrentFiscalYear = fy.FiscalYearName,
                                                IsTransferredToACC = (gr.IsTransferredToACC == null) ? false : true
                                            }
                                            ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = goodsReceiptList;
                }
                #endregion
                #region GET: good receipt group by supplierID
                else if (reqType == "get-goods-receipt-groupby-supplier")
                {
                    //var goodReceiptList = phrmdbcontext.PHRMGoodsReceipt.Where(a => a.IsCancel != null).ToList().GroupJoin(phrmdbcontext.PHRMSupplier.ToList(), a => a.SupplierId, b => b.SupplierId, (a, b) =>
                    //       new 
                    //       {
                    //           SupplierId = a.SupplierId,
                    //           SubTotal = a.SubTotal,
                    //           DiscountAmount = a.DiscountAmount,
                    //           VATAmount = a.VATAmount,
                    //           TotalAmount = a.TotalAmount,
                    //           InvoiceNo = a.InvoiceNo,
                    //           GoodReceiptDate = a.GoodReceiptDate,
                    //           PurchaseOrderId = a.PurchaseOrderId,
                    //           IsCancel = a.IsCancel,
                    //           ContactNo = b.Select(s => s.ContactNo).FirstOrDefault(),
                    //           GoodReceiptPrintId = a.GoodReceiptPrintId,
                    //           Pin = b.Select(s => s.PANNumber).FirstOrDefault(),
                    //           CreditPeriod = a.CreditPeriod,
                    //           SupplierName = b.Select(s => s.SupplierName).FirstOrDefault(),

                    //       }).ToList().GroupBy(a => a.SupplierId).OrderByDescending(a => goodsReceiptId).Select(
                    //    a => new PHRMSupplierGoodReceiptVM
                    //    {
                    //        SupplierId = a.Select(s => s.SupplierId).FirstOrDefault(),
                    //        SubTotal = a.Sum(s => s.SubTotal),
                    //        DiscountAmount = a.Sum(s => s.DiscountAmount),
                    //        VATAmount = a.Sum(s => s.VATAmount),
                    //        TotalAmount = a.Sum(s => s.TotalAmount),
                    //        SupplierName = a.Select(s => s.SupplierName).FirstOrDefault(),
                    //        InvoiceNo = a.Select(s => s.InvoiceNo).FirstOrDefault(),
                    //        GoodReceiptDate = a.Select(s => s.GoodReceiptDate).FirstOrDefault(),
                    //        PurchaseOrderId = a.Select(s => s.PurchaseOrderId).FirstOrDefault(),
                    //        ContactNo = a.Select(s => s.ContactNo).FirstOrDefault(),
                    //        GoodReceiptPrintId = a.Select(s => s.GoodReceiptPrintId).FirstOrDefault(),
                    //        Pin = a.Select(s => s.Pin).FirstOrDefault(),
                    //        CreditPeriod = a.Select(s => s.CreditPeriod).FirstOrDefault(),
                    //        IsCancel = a.Select(s => s.IsCancel).FirstOrDefault(),
                    //    }
                    //    );

                    var supplierDetails = phrmdbcontext.PHRMSupplier.ToList();//.Where(supplier => supplier.SupplierId == providerId).Select(s => s.SupplierName).FirstOrDefault().ToString();


                    List<PHRMSupplierGoodReceiptVM> goodReceiptList = new List<PHRMSupplierGoodReceiptVM>();

                    goodReceiptList = (from s in phrmdbcontext.PHRMSupplier.Where(a => a.IsActive == true)
                                       join gr in phrmdbcontext.PHRMGoodsReceipt
                                       .Where(a => a.IsCancel != true && DbFunctions.TruncateTime(a.GoodReceiptDate) >= DbFunctions.TruncateTime(FromDate) && DbFunctions.TruncateTime(a.GoodReceiptDate) <= DbFunctions.TruncateTime(ToDate)) on s.SupplierId equals gr.SupplierId
                                       into leftJ
                                       from lj in leftJ.DefaultIfEmpty()
                                       group new { s, lj } by new { s.SupplierId, s.SupplierName } into g
                                       select new PHRMSupplierGoodReceiptVM
                                       {
                                           SupplierId = g.Key.SupplierId,
                                           SubTotal = g.Sum(a => a.lj.SubTotal),
                                           DiscountAmount = g.Sum(a => a.lj.DiscountAmount),
                                           VATAmount = g.Sum(a => a.lj.VATAmount),
                                           TotalAmount = g.Sum(a => a.lj.TotalAmount),
                                           SupplierName = g.Key.SupplierName,
                                           IsCancel = g.Select(a => a.lj.IsCancel).FirstOrDefault(),
                                           GoodReceiptDate = g.Select(a => a.lj.GoodReceiptDate).FirstOrDefault(),
                                           CCAmount = g.Sum(a => a.lj.CCAmount)
                                       }).ToList();

                    List<PHRMSupplierGoodReceiptVM> goodReceiptReturn = new List<PHRMSupplierGoodReceiptVM>();
                    goodReceiptReturn = (from s in phrmdbcontext.PHRMSupplier.Where(a => a.IsActive == true)
                                         join grret in phrmdbcontext.PHRMReturnToSupplier.Where(a => DbFunctions.TruncateTime(a.ReturnDate) >= DbFunctions.TruncateTime(FromDate) && DbFunctions.TruncateTime(a.ReturnDate) <= DbFunctions.TruncateTime(ToDate)) on s.SupplierId equals grret.SupplierId into leftJ
                                         from lj in leftJ.DefaultIfEmpty()
                                         group new { s, lj } by new { s.SupplierId, s.SupplierName } into g
                                         select new PHRMSupplierGoodReceiptVM
                                         {
                                             SupplierId = g.Key.SupplierId,
                                             SubTotal = g.Sum(a => -a.lj.SubTotal),
                                             DiscountAmount = g.Sum(a => -a.lj.DiscountAmount),
                                             VATAmount = g.Sum(a => -a.lj.VATAmount),
                                             TotalAmount = g.Sum(a => -a.lj.TotalAmount),
                                             SupplierName = g.Key.SupplierName,
                                             IsCancel = false,
                                             GoodReceiptDate = g.Select(a => a.lj.ReturnDate).FirstOrDefault(),
                                             CCAmount = g.Sum(a => -a.lj.CCAmount)
                                         }).ToList();
                    goodReceiptList.AddRange(goodReceiptReturn);
                    var data = goodReceiptList.GroupBy(a => a.SupplierId).Select(d => new PHRMSupplierGoodReceiptVM
                    {
                        SupplierId = d.Select(a => a.SupplierId).FirstOrDefault(),
                        SubTotal = d.Sum(s => s.SubTotal),
                        DiscountAmount = d.Sum(a => a.DiscountAmount),
                        VATAmount = d.Sum(a => a.VATAmount),
                        TotalAmount = d.Sum(a => a.TotalAmount),
                        SupplierName = d.Select(a => a.SupplierName).FirstOrDefault(),
                        IsCancel = d.Select(a => a.IsCancel).FirstOrDefault(),
                        GoodReceiptDate = d.Select(a => a.GoodReceiptDate).FirstOrDefault(),
                        CCAmount = d.Sum(a => a.CCAmount)
                    }).OrderBy(b => b.SupplierName);
                    responseData.Status = "OK";
                    responseData.Results = data;
                }
                #endregion
                #region GET: good receipt of  unique supplier
                else if (reqType == "get-goods-receipt-by-SupplierID")
                {
                    var supplierDettail = phrmdbcontext.PHRMSupplier.Where(supplier => supplier.SupplierId == providerId).Select(s => new
                    {
                        s.ContactNo,
                        s.SupplierName
                    }).FirstOrDefault();
                    //var goodReciptList = phrmdbcontext.PHRMGoodsReceipt.Where(a => a.IsCancel != null && a.SupplierId == providerId)
                    //                                                   .Select(a =>
                    //                                                   new
                    //                                                   {
                    //                                                       SupplierId = a.SupplierId,
                    //                                                       InvoiceNo = a.InvoiceNo,
                    //                                                       GoodReceiptDate = a.GoodReceiptDate,
                    //                                                       SubTotal = a.SubTotal,
                    //                                                       DiscountAmount = a.DiscountAmount,
                    //                                                       VATAmount = a.VATAmount,
                    //                                                       TotalAmount = a.TotalAmount,
                    //                                                       GoodReceiptId = a.GoodReceiptId,
                    //                                                       CreditPeriod = a.CreditPeriod,
                    //                                                       SupplierName = supplierName,
                    //                                                       GoodReceiptPrintId=a.GoodReceiptPrintId
                    //                                                   }).ToList().OrderByDescending(a=>a.GoodReceiptPrintId);

                    List<PHRMGoodReceiptVM> goodReciptList = new List<PHRMGoodReceiptVM>();

                    goodReciptList = phrmdbcontext.PHRMGoodsReceipt.Where(a => a.IsCancel != true && a.SupplierId == providerId && DbFunctions.TruncateTime(a.GoodReceiptDate) >= DbFunctions.TruncateTime(FromDate) && DbFunctions.TruncateTime(a.GoodReceiptDate) <= DbFunctions.TruncateTime(ToDate))
                                                            .Select(a =>
                                                            new PHRMGoodReceiptVM
                                                            {
                                                                SupplierId = a.SupplierId,
                                                                InvoiceNo = a.InvoiceNo,
                                                                GoodReceiptDate = a.GoodReceiptDate,
                                                                SubTotal = a.SubTotal,
                                                                DiscountAmount = a.DiscountAmount,
                                                                VATAmount = a.VATAmount,
                                                                TotalAmount = a.TotalAmount,
                                                                GoodReceiptId = a.GoodReceiptId,
                                                                CreditPeriod = a.CreditPeriod,
                                                                SupplierName = supplierDettail.SupplierName,
                                                                GoodReceiptPrintId = a.GoodReceiptPrintId,
                                                                GoodReceiptType = "Purchased GR",
                                                                ContactNo = supplierDettail.ContactNo,
                                                            }).ToList();

                    Nullable<int> CreditPeriod = null;
                    List<PHRMGoodReceiptVM> goodReceiptReturn = new List<PHRMGoodReceiptVM>();

                    goodReceiptReturn = phrmdbcontext.PHRMReturnToSupplier.Where(a => a.SupplierId == providerId && DbFunctions.TruncateTime(a.ReturnDate) >= DbFunctions.TruncateTime(FromDate) && DbFunctions.TruncateTime(a.ReturnDate) <= DbFunctions.TruncateTime(ToDate))
                                                                             .Select(a => new PHRMGoodReceiptVM
                                                                             {
                                                                                 SupplierId = a.SupplierId,
                                                                                 InvoiceNo = a.CreditNoteId,
                                                                                 GoodReceiptDate = a.ReturnDate,
                                                                                 SubTotal = -a.SubTotal,
                                                                                 DiscountAmount = -a.DiscountAmount,
                                                                                 VATAmount = -a.VATAmount,
                                                                                 TotalAmount = -a.TotalAmount,
                                                                                 GoodReceiptId = a.GoodReceiptId,
                                                                                 CreditPeriod = CreditPeriod.Value,
                                                                                 SupplierName = supplierDettail.SupplierName,
                                                                                 GoodReceiptPrintId = a.CreditNotePrintId,
                                                                                 GoodReceiptType = "Returned GR",
                                                                                 ContactNo = supplierDettail.ContactNo,
                                                                             }).ToList();

                    goodReciptList.AddRange(goodReceiptReturn);
                    goodReciptList.OrderBy(a => a.GoodReceiptDate);
                    responseData.Status = "OK";
                    responseData.Results = goodReciptList;
                }
                #endregion
                #region GET: all the store from PHRM_MST_STORE table
                else if (reqType == "getMainStore")
                {
                    var test = phrmdbcontext.PHRMStore;
                    var storeList = phrmdbcontext.PHRMStore.FirstOrDefault(a => a.StoreId == 1);
                    responseData.Status = "OK";
                    responseData.Results = storeList;
                }
                #endregion
                #region Get: all the dispensary list from PHRM_MST_DISPENSARY
                else if (reqType == "getDispenaryList")
                {
                    var dispensaryCategory = Enums.ENUM_StoreCategory.Dispensary;
                    var dispensaryList = phrmdbcontext.PHRMStore.Where(s => s.Category == dispensaryCategory).ToList();
                    responseData.Status = "OK";
                    responseData.Results = dispensaryList;
                }
                #endregion
                #region GET: order-goods receipt items-view : gets list of GoodsReceiptItems by GoodReceiptId
                else if (reqType == "GRItemsViewByGRId")
                {
                    var grItemsList = (from gritems in phrmdbcontext.PHRMGoodsReceiptItems
                                       join gr in phrmdbcontext.PHRMGoodsReceipt on gritems.GoodReceiptId equals gr.GoodReceiptId
                                       where gritems.GoodReceiptId == goodsReceiptId
                                       select new
                                       {
                                           GoodReceiptId = gritems.GoodReceiptId,
                                           ItemName = gritems.ItemName,
                                           GoodReceiptNo = gr.GoodReceiptPrintId,
                                           BatchNo = gritems.BatchNo,
                                           // ManufactureDate = gritems.ManufactureDate,
                                           ExpiryDate = gritems.ExpiryDate,
                                           ReceivedQuantity = gritems.ReceivedQuantity,
                                           FreeQuantity = gritems.FreeQuantity,
                                           RejectedQuantity = gritems.RejectedQuantity,
                                           SellingPrice = gritems.SellingPrice,
                                           GRItemPrice = gritems.GRItemPrice,
                                           SubTotal = gritems.SubTotal,
                                           VATAmount = gritems.GrPerItemVATAmt,
                                           DiscountAmount = gritems.GrPerItemDisAmt,
                                           TotalAmount = gritems.TotalAmount,
                                           SalePrice = gritems.SalePrice,
                                           CCAmount = gritems.CCAmount,
                                           GrTotalDisAmt = gritems.GrTotalDisAmt,
                                           StripRate = gritems.StripRate

                                       }
                                ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = grItemsList;
                }
                else if (reqType == "GRItemsViewByGRReturnId")
                {
                    var grItemsList = (from ret in phrmdbcontext.PHRMReturnToSupplier
                                       join retitm in phrmdbcontext.PHRMReturnToSupplierItem on ret.ReturnToSupplierId equals retitm.ReturnToSupplierId
                                       where ret.CreditNotePrintId == CreditNotePrintId && ret.GoodReceiptId == goodsReceiptId
                                       select new
                                       {
                                           GoodReceiptId = ret.GoodReceiptId,
                                           ItemName = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptItemId == retitm.GoodReceiptItemId).FirstOrDefault().ItemName,
                                           CreditNotePrintId = ret.CreditNotePrintId,
                                           BatchNo = retitm.BatchNo,
                                           ExpiryDate = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptItemId == retitm.GoodReceiptItemId).FirstOrDefault().ExpiryDate,
                                           ReceivedQuantity = retitm.Quantity,
                                           FreeQuantity = retitm.FreeQuantity,
                                           RejectedQuantity = 0,
                                           SellingPrice = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptItemId == retitm.GoodReceiptItemId).FirstOrDefault().SellingPrice,
                                           GRItemPrice = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptItemId == retitm.GoodReceiptItemId).FirstOrDefault().GRItemPrice,
                                           ReturnRate = retitm.ReturnRate,
                                           SubTotal = retitm.SubTotal,
                                           VATAmount = retitm.VATAmount,
                                           DiscountAmount = retitm.DiscountedAmount,
                                           TotalAmount = retitm.TotalAmount,
                                           SalePrice = retitm.SalePrice,
                                           CCAmount = retitm.CCAmount,
                                           GrTotalDisAmt = retitm.DiscountedAmount,
                                           StripRate = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptItemId == retitm.GoodReceiptItemId).FirstOrDefault().StripRate

                                       }
                                ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = grItemsList;
                }
                #endregion
                #region GET: Get List Of POItems whose status is Active or Partial
                else if (reqType == "getPHRMPOItemsForGR")
                {

                    var POAndPOItemsForGR = (from po in phrmdbcontext.PHRMPurchaseOrder
                                             join poitm in phrmdbcontext.PHRMPurchaseOrderItems on po.PurchaseOrderId equals poitm.PurchaseOrderId
                                             join itms in phrmdbcontext.PHRMItemMaster on poitm.ItemId equals itms.ItemId
                                             join supplier in phrmdbcontext.PHRMSupplier on po.SupplierId equals supplier.SupplierId
                                             join company in phrmdbcontext.PHRMCompany on itms.CompanyId equals company.CompanyId
                                             join UOM in phrmdbcontext.PHRMUnitOfMeasurement on itms.UOMId equals UOM.UOMId
                                             where poitm.PurchaseOrderId == purchaseOrderId
                                             select new
                                             {
                                                 PHRMPurchaseOrder = po,
                                                 PHRMPurchaseOrderItems = po.PHRMPurchaseOrderItems.Where(a => a.POItemStatus == "partial" || a.POItemStatus == "active").ToList(),
                                                 PHRMSupplier = supplier,
                                                 PHRMItemMaster = itms,
                                                 CompanyName = company.CompanyName,
                                                 UOMName = UOM.UOMName,
                                             }).ToList();
                    foreach (var po in POAndPOItemsForGR)
                    {
                        foreach (var item in po.PHRMPurchaseOrderItems)
                        {
                            var numerator = Convert.ToDouble(item.VATAmount * 100);
                            var denominator = Convert.ToDouble(item.StandardRate) * item.Quantity;
                            item.VATPercentage = (denominator == 0) ? 0 : (decimal)(numerator / denominator);
                        }
                    }
                    responseData.Status = "OK";
                    responseData.Results = POAndPOItemsForGR;
                }
                #endregion
                #region GET: Get Prescription List
                else if (reqType == "getprescriptionlist")
                {
                    List<EmployeeModel> employeeList = (from emp in masterDbContext.Employees select emp).ToList();
                    var presList = (from pres in phrmdbcontext.PHRMPrescriptionItems.AsEnumerable()
                                    where pres.OrderStatus == "active"
                                    join pat in phrmdbcontext.PHRMPatient.AsEnumerable() on pres.PatientId equals pat.PatientId
                                    join emp in employeeList.AsEnumerable() on pres.CreatedBy equals emp.EmployeeId
                                    group new { pres, pat, emp } by new
                                    {
                                        // pres.ProviderId,
                                        pres.PatientId,
                                        pat.PatientCode,
                                        pat.FirstName,
                                        pat.MiddleName,
                                        pat.LastName,
                                        eFirstName = emp.FirstName,
                                        eMiddleName = emp.MiddleName,
                                        eLastName = emp.LastName,
                                        ProviderId = pres.CreatedBy,

                                    }
                                    into t
                                    select new
                                    {
                                        PatientCode = t.Key.PatientCode,
                                        PatientId = t.Key.PatientId,
                                        PatientName = t.Key.FirstName + " " + (string.IsNullOrEmpty(t.Key.MiddleName) ? "" : t.Key.MiddleName + " ") + t.Key.LastName,
                                        ProviderId = t.Key.ProviderId,
                                        ProviderFullName = t.Key.eFirstName + " " + (string.IsNullOrEmpty(t.Key.eMiddleName) ? "" : t.Key.eMiddleName + " ") + t.Key.eLastName,
                                        CreatedOn = t.Max(r => r.pres.CreatedOn)
                                    }
                                    ).OrderByDescending(a => a.CreatedOn).ToList();
                    responseData.Status = "OK";
                    responseData.Results = presList;
                }
                #endregion               
                #region  Get List of Item for Return to Supplier Functionality whose available quantity is greater then zero

                //ramesh/rohit: this method is not used any more
                else if (reqType == "PHRMItemListWithTotalAvailableQuantity")
                {
                    var GRItemList = (from GRI in phrmdbcontext.PHRMGoodsReceiptItems
                                      from GR in phrmdbcontext.PHRMGoodsReceipt.Where(gr => gr.GoodReceiptId == GRI.GoodReceiptId)
                                      from S in phrmdbcontext.StoreStocks.Where(s => s.StockId == GRI.StockId && s.IsActive == true && s.AvailableQuantity > 0)
                                      from I in phrmdbcontext.PHRMItemMaster.Where(i => i.ItemId == S.ItemId)
                                      from U in phrmdbcontext.PHRMUnitOfMeasurement.Where(u => u.UOMId == I.UOMId).DefaultIfEmpty()
                                      from G in phrmdbcontext.PHRMGenericModel.Where(g => g.GenericId == I.GenericId).DefaultIfEmpty()
                                      group new { GRI, GR, S, I, U, G } by new { GRI.GoodReceiptItemId } into GRIGrouped
                                      select new
                                      {
                                          GoodReceiptItemId = GRIGrouped.FirstOrDefault().GRI.GoodReceiptItemId,
                                          GoodReceiptId = GRIGrouped.FirstOrDefault().GR.GoodReceiptId,
                                          SupplierId = GRIGrouped.FirstOrDefault().GR.SupplierId,
                                          FiscalYearId = GRIGrouped.FirstOrDefault().GR.FiscalYearId,
                                          GoodReceiptPrintId = GRIGrouped.FirstOrDefault().GR.GoodReceiptPrintId,
                                          ItemId = GRIGrouped.FirstOrDefault().S.ItemId,
                                          ItemName = GRIGrouped.FirstOrDefault().I.ItemName,
                                          ItemCode = GRIGrouped.FirstOrDefault().I.ItemCode,
                                          GenericName = GRIGrouped.FirstOrDefault().G.GenericName,
                                          UOMName = GRIGrouped.FirstOrDefault().U.UOMName,
                                          BatchNo = GRIGrouped.FirstOrDefault().GRI.BatchNo,
                                          ExpiryDate = GRIGrouped.FirstOrDefault().GRI.ExpiryDate,
                                          SalePrice = GRIGrouped.FirstOrDefault().GRI.SalePrice,
                                          ItemPrice = GRIGrouped.FirstOrDefault().GRI.GRItemPrice,
                                          ReceivedQuantity = GRIGrouped.FirstOrDefault().GRI.ReceivedQuantity,
                                          TotalAvailableQuantity = GRIGrouped.Sum(a => a.S.AvailableQuantity),
                                          BatchWiseAvailableQuantity = GRIGrouped.Sum(a => a.S.AvailableQuantity),
                                          FreeQuantity = GRIGrouped.FirstOrDefault().GRI.FreeQuantity,
                                          DiscountPercentage = GRIGrouped.FirstOrDefault().GRI.DiscountPercentage,
                                          VATPercentage = GRIGrouped.FirstOrDefault().GRI.VATPercentage,
                                          CCCharge = GRIGrouped.FirstOrDefault().GRI.CCCharge,
                                      }).ToList();

                    responseData.Status = "OK";
                    responseData.Results = GRItemList;
                }
                #endregion
                #region Get List Of BatchNumbers By Passing ItemId For Return To Supplier Functionality 
                else if (reqType == "getBatchNoByItemId")
                {
                    var BatchNoListByItemId = (from gritm in phrmdbcontext.PHRMGoodsReceiptItems
                                               join itm in phrmdbcontext.PHRMItemMaster on gritm.ItemId equals itm.ItemId
                                               where gritm.ItemId == itemId
                                               select new
                                               {
                                                   ItemId = gritm.ItemId,
                                                   BatchNo = gritm.BatchNo,
                                                   BatchWiseAvailableQuantity = gritm.AvailableQuantity

                                               }
                                          ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = BatchNoListByItemId;
                }
                #endregion
                #region Get Item Details By BatchNo 
                else if (reqType == "getItemDetailsByBatchNo")
                {
                    var bat = batchNo;
                    var itm1 = itemId;
                    var totalStock = phrmdbcontext.StoreStocks.Where(a => a.ItemId == itemId && a.StockMaster.BatchNo == batchNo).ToList();
                    var btchqty = totalStock.Sum(a => a.AvailableQuantity);
                    var ItemDetailsByBatchNo = (from gritm in phrmdbcontext.PHRMGoodsReceiptItems
                                                join itm in phrmdbcontext.PHRMItemMaster on gritm.ItemId equals itm.ItemId
                                                where gritm.BatchNo == batchNo && gritm.ItemId == itemId
                                                select new
                                                {
                                                    GoodReceiptItemId = gritm.GoodReceiptItemId,
                                                    BatchWiseAvailableQuantity = btchqty,
                                                    ItemPrice = gritm.GRItemPrice,
                                                    VATPercentage = gritm.VATPercentage,
                                                    DiscountPercentage = gritm.DiscountPercentage,
                                                    ExpiryDate = gritm.ExpiryDate,
                                                    SalePrice = gritm.SalePrice,
                                                    ItemId = itemId
                                                }
                                     ).FirstOrDefault();
                    responseData.Status = "OK";
                    responseData.Results = ItemDetailsByBatchNo;
                }
                #endregion
                #region Get Return To Supplier
                else if (reqType == "returnToSupplier")
                {
                    var testdate = ToDate.AddDays(1);
                    var InvoiceIdStr = invoiceid.ToString();
                    //List<SqlParameter> paramList = new List<SqlParameter>() {
                    //     new SqlParameter("@FromDate", FromDate),
                    //     new SqlParameter("@ToDate", ToDate),
                    //     new SqlParameter("@SupplierId", supplierId),
                    //     new SqlParameter("@InvoiceNo", InvoiceIdStr),
                    //     new SqlParameter("@GoodsReceiptPrintId",gdprintId)
                    // };

                    //DataTable returnToSupplier = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GetReturnToSupplier", paramList, phrmdbcontext);

                    var returnToSupplier = (from GR in phrmdbcontext.PHRMGoodsReceipt
                                            join S in phrmdbcontext.PHRMSupplier on GR.SupplierId equals S.SupplierId
                                            where ((GR.SupplierId == supplierId || supplierId == null) && (GR.GoodReceiptPrintId == gdprintId || gdprintId == null)
                                            && (GR.InvoiceNo == invoiceNo || invoiceNo == "null") && (GR.CreatedOn > FromDate && GR.CreatedOn < testdate))
                                            group new { GR, S } by new { GR.GoodReceiptId, GR.GoodReceiptDate, GR.GoodReceiptPrintId, GR.SubTotal, GR.VATAmount, GR.DiscountAmount, GR.TotalAmount, GR.InvoiceNo, S.SupplierName } into GRGrouped
                                            select new
                                            {
                                                GoodReceiptId = GRGrouped.Key.GoodReceiptId,
                                                GoodReceiptDate = GRGrouped.Key.GoodReceiptDate,
                                                GoodReceiptPrintId = GRGrouped.Key.GoodReceiptPrintId,
                                                SupplierName = GRGrouped.Key.SupplierName,
                                                SubTotal = GRGrouped.Key.SubTotal,
                                                DiscountAmount = GRGrouped.Key.DiscountAmount,
                                                VATAmount = GRGrouped.Key.VATAmount,
                                                TotalAmount = GRGrouped.Key.TotalAmount,
                                                InvoiceNo = GRGrouped.Key.InvoiceNo,
                                            }).ToList().OrderByDescending(gr => gr.GoodReceiptPrintId);

                    responseData.Status = "OK";
                    responseData.Results = returnToSupplier;
                }
                #endregion
                #region Get Return All Item To Supplier List
                else if (reqType == "returnItemsToSupplierList")
                {
                    var testdate = ToDate.AddDays(1);
                    var returnItemToSupplierList = (from retSupp in phrmdbcontext.PHRMReturnToSupplier
                                                    join supp in phrmdbcontext.PHRMSupplier on retSupp.SupplierId equals supp.SupplierId
                                                    join retSuppItm in phrmdbcontext.PHRMReturnToSupplierItem on retSupp.ReturnToSupplierId equals retSuppItm.ReturnToSupplierId
                                                    join rbac in phrmdbcontext.Users on retSupp.CreatedBy equals rbac.EmployeeId
                                                    join gr in phrmdbcontext.PHRMGoodsReceipt on retSupp.GoodReceiptId equals gr.GoodReceiptId
                                                    where (retSuppItm.Quantity != 0 && (retSuppItm.CreatedOn > FromDate && retSuppItm.CreatedOn < testdate))
                                                    group new { supp, retSuppItm, retSupp, rbac, gr } by new
                                                    {
                                                        supp.SupplierName,
                                                        retSupp.ReturnToSupplierId,
                                                        retSupp.CreditNotePrintId,

                                                    } into p
                                                    select new
                                                    {
                                                        CreditNotePrintId = p.Key.CreditNotePrintId,
                                                        SupplierName = p.Key.SupplierName,
                                                        ReturnToSupplierId = p.Key.ReturnToSupplierId,
                                                        ReturnDate = p.Select(a => a.retSupp.ReturnDate).FirstOrDefault(),
                                                        CreditNoteNo = p.Select(a => a.retSupp.CreditNoteId).FirstOrDefault(),
                                                        Quantity = p.Sum(a => a.retSuppItm.Quantity),
                                                        FreeQuantity = p.Select(a => a.retSuppItm.FreeQuantity),
                                                        SubTotal = p.Select(a => a.retSupp.SubTotal).FirstOrDefault(),
                                                        DiscountAmount = p.Select(a => a.retSupp.DiscountAmount).FirstOrDefault(),
                                                        VATAmount = p.Select(a => a.retSupp.VATAmount).FirstOrDefault(),
                                                        CCAmount = p.Select(a => a.retSupp.CCAmount).FirstOrDefault(),
                                                        TotalAmount = p.Select(a => a.retSupp.TotalAmount).FirstOrDefault(),
                                                        Email = p.Select(a => a.supp.Email).FirstOrDefault(),
                                                        ContactNo = p.Select(a => a.supp.ContactNo).FirstOrDefault(),
                                                        ContactAddress = p.Select(a => a.supp.ContactAddress).FirstOrDefault(),
                                                        City = p.Select(a => a.supp.City).FirstOrDefault(),
                                                        Pin = p.Select(a => a.supp.PANNumber).FirstOrDefault(),
                                                        Remarks = p.Select(a => a.retSupp.Remarks).FirstOrDefault(),
                                                        ReturnStatus = p.Select(a => a.retSupp.ReturnStatus).FirstOrDefault(),
                                                        UserName = p.Select(a => a.rbac.UserName).FirstOrDefault(),
                                                        CreatedOn = p.Select(a => a.retSupp.CreatedOn).FirstOrDefault(),
                                                        GoodReceiptPrintId = p.Select(a => a.gr.GoodReceiptPrintId).FirstOrDefault()
                                                    }
                            ).ToList().OrderByDescending(a => a.ReturnToSupplierId);
                    responseData.Status = "OK";
                    responseData.Results = returnItemToSupplierList;
                }
                #endregion
                #region Get Write-Off List With SUM of WriteOff Qty 
                else if (reqType == "getWriteOffList")
                {
                    var writeOffList = (from writeOff in phrmdbcontext.PHRMWriteOff
                                        join writeOffItm in phrmdbcontext.PHRMWriteOffItem on writeOff.WriteOffId equals writeOffItm.WriteOffId
                                        join itm in phrmdbcontext.PHRMItemMaster on writeOffItm.ItemId equals itm.ItemId
                                        group new { writeOff, writeOffItm, itm } by new
                                        {
                                            writeOff.WriteOffId

                                        } into p
                                        select new
                                        {
                                            ItemName = p.Select(a => a.itm.ItemName),
                                            WriteOffId = p.Key.WriteOffId,
                                            BatchNo = p.Select(a => a.writeOffItm.BatchNo),
                                            WriteOffDate = p.Select(a => a.writeOff.WriteOffDate).FirstOrDefault(),
                                            ItemPrice = p.Select(a => a.writeOffItm.ItemPrice),
                                            Quantity = p.Sum(a => a.writeOffItm.WriteOffQuantity),
                                            SubTotal = p.Select(a => a.writeOff.SubTotal).FirstOrDefault(),
                                            DiscountAmount = p.Select(a => a.writeOff.DiscountAmount).FirstOrDefault(),
                                            VATAmount = p.Select(a => a.writeOff.VATAmount).FirstOrDefault(),
                                            TotalAmount = p.Select(a => a.writeOff.TotalAmount).FirstOrDefault(),
                                            Remarks = p.Select(a => a.writeOff.WriteOffRemark).FirstOrDefault()

                                        }
                           ).ToList().OrderByDescending(a => a.WriteOffId);

                    responseData.Status = "OK";
                    responseData.Results = writeOffList;
                }
                #endregion
                #region Get All Generic Name List
                else if (reqType == "getGenericList")
                {
                    //var test = phrmdbcontext.PHRMInvoiceTransactionItems.ToList();
                    // DataTable invoiceTransaction = test;
                    //List<PHRMGenericModel> genericList = new List<PHRMGenericModel>();
                    var genericList = (from generics in phrmdbcontext.PHRMGenericModel.Include(g => g.PHRM_MAP_MstItemsPriceCategories)
                                       select generics).ToList();
                    responseData.Status = "OK";
                    responseData.Results = genericList;
                }
                #endregion
                #region Get All Generic Sale List
                else if (reqType == "PHRMDailySalesSummaryReport")
                {
                    var test = phrmdbcontext.PHRMInvoiceTransactionItems.ToList().OrderByDescending(a => a.CreatedOn);

                    responseData.Status = "OK";
                    responseData.Results = test;
                }
                #endregion
                #region Get Stock Details List
                else if (reqType == "stockDetails")
                {
                    //To calculate stock and add batch and items
                    //var totalStock = phrmdbcontext.PHRMStockTransactionModel.Where(a => a.ExpiryDate >= DateTime.Now).ToList().GroupBy(a => new { a.ItemId, a.BatchNo }).Select(g => new PHRMStockTransactionItemsModel
                    //{
                    //    ItemId = g.Key.ItemId,
                    //    BatchNo = g.Key.BatchNo,
                    //    //InOut = g.Key.InOut,
                    //    Quantity = g.Where(w => w.InOut == "in").Sum(q => q.Quantity) + g.Where(w => w.InOut == "in").Sum(f => f.FreeQuantity).Value - g.Where(w => w.InOut == "out").Sum(o => o.Quantity),
                    //    FreeQuantity = g.Where(w => w.InOut == "in").Sum(q => q.Quantity),
                    //    ExpiryDate = g.FirstOrDefault().ExpiryDate,
                    //    SalePrice = g.FirstOrDefault().SalePrice,
                    //    Price = g.FirstOrDefault().Price,


                    //}
                    //).Where(a => a.Quantity > 0).GroupJoin(phrmdbcontext.PHRMItemMaster.Where(a => a.IsActive == true).ToList(), a => a.ItemId, b => b.ItemId, (a, b) =>
                    //new GoodReceiptItemsViewModel
                    //{
                    //    ItemId = a.ItemId.Value,
                    //    BatchNo = a.BatchNo,
                    //    ExpiryDate = a.ExpiryDate.Value.Date,
                    //    ItemName = b.Select(s => s.ItemName).FirstOrDefault(),
                    //    AvailableQuantity = a.Quantity,
                    //    SalePrice = a.SalePrice.Value,
                    //    GRItemPrice = a.Price.Value,
                    //    GenericId = b.Select(s => s.GenericId.Value).FirstOrDefault(),
                    //    IsActive = true

                    //}
                    //).OrderBy(expDate => expDate.ExpiryDate).ToList().Join(phrmdbcontext.PHRMGenericModel.ToList(), a => a.GenericId, b => b.GenericId, (a, b) => new
                    //{ GoodReceiptItemsViewModel = a, PHRMGenericModel = b }).Join(phrmdbcontext.PHRMCategory.ToList(), a => a.PHRMGenericModel.CategoryId, b => b.CategoryId, (a, b) => new { a.GoodReceiptItemsViewModel, a.PHRMGenericModel, PHRMCategory = b })
                    //.Select(s => new GoodReceiptItemsViewModel
                    //{

                    //    ItemId = s.GoodReceiptItemsViewModel.ItemId,
                    //    BatchNo = s.GoodReceiptItemsViewModel.BatchNo,
                    //    ExpiryDate = s.GoodReceiptItemsViewModel.ExpiryDate.Date,
                    //    ItemName = s.GoodReceiptItemsViewModel.ItemName,
                    //    AvailableQuantity = s.GoodReceiptItemsViewModel.AvailableQuantity,
                    //    SalePrice = s.GoodReceiptItemsViewModel.SalePrice,
                    //    GRItemPrice = s.GoodReceiptItemsViewModel.GRItemPrice,
                    //    CategoryName = s.PHRMCategory.CategoryName,
                    //    IsActive = true
                    //});



                    var totalStock = (from itm in phrmdbcontext.StoreStocks
                                      join mstitem in phrmdbcontext.PHRMItemMaster on itm.ItemId equals mstitem.ItemId
                                      select new
                                      {
                                          ItemId = itm.ItemId,
                                          BatchNo = itm.StockMaster.BatchNo,
                                          ExpiryDate = itm.StockMaster.ExpiryDate,
                                          ItemName = mstitem.ItemName,
                                          AvailableQuantity = itm.AvailableQuantity,
                                          SalePrice = itm.StockMaster.SalePrice,
                                          IsActive = true,
                                          DiscountPercentage = 0

                                      }).ToList();

                    responseData.Status = "OK";
                    responseData.Results = totalStock;

                }
                #endregion
                #region Get Narcotics Stock Details List (sales)
                else if (reqType == "natcoticsstockDetails")
                {
                    var totalStock = (from itm in phrmdbcontext.StoreStocks
                                      join mstitem in phrmdbcontext.PHRMItemMaster on itm.ItemId equals mstitem.ItemId
                                      where mstitem.IsNarcotic == true
                                      select new
                                      {
                                          ItemId = itm.ItemId,
                                          BatchNo = itm.StockMaster.BatchNo,
                                          ExpiryDate = itm.StockMaster.ExpiryDate,
                                          ItemName = mstitem.ItemName,
                                          AvailableQuantity = itm.AvailableQuantity,
                                          SalePrice = itm.StockMaster.SalePrice,
                                          IsActive = true,
                                          DiscountPercentage = 0

                                      }).ToList();

                    responseData.Status = "OK";
                    responseData.Results = totalStock;

                }
                #endregion
                #region Get ItemTypeList with All child ItemsList
                //else if (reqType == "itemtypeListWithItems")
                //{
                //    var testdate = DateTime.Now;
                //    // Check if dispensary is of type "insurance"
                //    var currentDispensaryType = phrmdbcontext.PHRMStore.Where(d => d.StoreId == DispensaryId).Select(d => d.SubCategory).FirstOrDefault();
                //    var isCurrentDispensaryInsurance = currentDispensaryType == Enums.ENUM_DispensarySubCategory.Insurance;
                //    // IF yes, show only insurance applicable items, and save SalePrice as Govt Insurance Price
                //    var totalStock = (from S in phrmdbcontext.StoreStocks.Where(s => (s.StoreId == DispensaryId || DispensaryId == 0) && s.AvailableQuantity > 0 &&
                //                      //s.StockMaster.ExpiryDate > testdate &&  //Rohit: As per LPH requirement expire stock should be shown on ItemName dropdown during sales.
                //                      s.IsActive == true)
                //                      join I in phrmdbcontext.PHRMItemMaster.Include(i => i.PHRM_MAP_MstItemsPriceCategories) on S.ItemId equals I.ItemId
                //                      join U in phrmdbcontext.PHRMUnitOfMeasurement on I.UOMId equals U.UOMId
                //                      from G in phrmdbcontext.PHRMGenericModel.Where(g => g.GenericId == I.GenericId).DefaultIfEmpty()
                //                      group new { S, I, G, U } by new { S.ItemId, S.StockMaster.BatchNo, S.StockMaster.CostPrice, S.StockMaster.SalePrice, S.StockMaster.ExpiryDate, S.StockMaster.BarcodeId } into SJ
                //                      select new
                //                      {
                //                          ItemId = SJ.Key.ItemId,
                //                          BatchNo = SJ.Key.BatchNo,
                //                          ExpiryDate = SJ.Key.ExpiryDate,
                //                          ItemName = SJ.FirstOrDefault().I.ItemName,
                //                          AvailableQuantity = SJ.Sum(s => s.S.AvailableQuantity),
                //                          SalePrice = SJ.Key.SalePrice,
                //                          Unit = SJ.FirstOrDefault().U.UOMName,
                //                          InsuranceMRP = SJ.FirstOrDefault().I.GovtInsurancePrice,
                //                          Price = SJ.Key.CostPrice,
                //                          IsActive = SJ.FirstOrDefault().I.IsActive,
                //                          GenericName = SJ.FirstOrDefault().G.GenericName,
                //                          GenericId = SJ.FirstOrDefault().G.GenericId,
                //                          IsNarcotic = SJ.FirstOrDefault().I.IsNarcotic,
                //                          IsInsuranceApplicable = SJ.FirstOrDefault().I.IsInsuranceApplicable,
                //                          IsVATApplicable = SJ.FirstOrDefault().I.IsVATApplicable,
                //                          SalesVATPercentage = SJ.FirstOrDefault().I.SalesVATPercentage,
                //                          BarcodeNumber = SJ.Key.BarcodeId,
                //                          PriceCategoriesDetails = SJ.FirstOrDefault().I.PHRM_MAP_MstItemsPriceCategories.Join(phrmdbcontext.PriceCategories,
                //                          impc => impc.PriceCategoryId,
                //                          pc => pc.PriceCategoryId,
                //                          (impc, pc) => new
                //                          {
                //                              PriceCategoryMapId = impc.PriceCategoryMapId,
                //                              PriceCategoryId = impc.PriceCategoryId,
                //                              PriceCategoryName = pc.PriceCategoryName,
                //                              Price = impc.Price,
                //                              IsPharmacyRateDifferent = pc.IsPharmacyRateDifferent,
                //                              IsCopayment = pc.IsCoPayment,
                //                              IsActive = pc.IsActive,
                //                              PharmacyDefaultCreditOrganization = pc.PharmacyDefaultCreditOrganizationId,
                //                              Discount = impc.Discount,
                //                              Copayment_CashPercent = pc.Copayment_CashPercent,
                //                              Copayment_CreditPercent = pc.Copayment_CreditPercent
                //                          }).Where(p => p.IsActive == true).ToList(),
                //                          RackNo = (from itemrack in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == SJ.Key.ItemId && ri.StoreId == SJ.FirstOrDefault().S.StoreId)
                //                                    join rack in phrmdbcontext.PHRMRack on itemrack.RackId equals rack.RackId
                //                                    select rack.RackNo).FirstOrDefault()
                //                      }).Where(a => a.IsActive == true && (isCurrentDispensaryInsurance == false || (a.IsInsuranceApplicable == true && isCurrentDispensaryInsurance == true))).OrderBy(ex => ex.ExpiryDate).ToList();

                //    responseData.Status = "OK";
                //    responseData.Results = totalStock;
                //}
                #endregion

                #region Get ItemList for Daily Sales Summary Report in Dispensary
                else if (reqType == "getItemList")
                {
                    var itemList = (from itm in phrmdbcontext.PHRMItemMaster.Where(itm => itm.IsActive == true)
                                    select itm).ToList();

                    responseData.Status = "OK";
                    responseData.Results = itemList;
                }
                #endregion

                #region Get GRItems with all details by ItemId
                else if (reqType == "getGRItemsByItemId" && reqType.Length > 0)
                {
                    var itemIdtest = itemId;
                    List<PHRMGoodsReceiptItemsModel> grItemsList =
                       (from grItems in phrmdbcontext.PHRMGoodsReceiptItems
                        where grItems.ItemId == itemId && grItems.AvailableQuantity > 0
                        orderby grItems.ExpiryDate descending
                        select grItems)
                              .ToList();

                    responseData.Status = "OK";
                    responseData.Results = grItemsList;
                }
                #endregion
                #region Get GR for editing
                else if (reqType == "GRforEdit" && reqType.Length > 0)
                {
                    PHRMGoodsReceiptModel GoodReceipt = (from gr in phrmdbcontext.PHRMGoodsReceipt
                                                         where gr.GoodReceiptId == goodsReceiptId
                                                         select gr).FirstOrDefault();
                    var GenericData = phrmdbcontext.PHRMGenericModel.Where(a => a.IsActive == true).ToList();
                    GoodReceipt.GoodReceiptItem = (from gritems in phrmdbcontext.PHRMGoodsReceiptItems
                                                   where gritems.GoodReceiptId == goodsReceiptId && gritems.IsCancel == false
                                                   select gritems).ToList();
                    foreach (var gritm in GoodReceipt.GoodReceiptItem)
                    {
                        var stockTxn = phrmdbcontext.StockTransactions.Where(a => a.StockId == gritm.StockId);
                        //ramesh: check if each grItem is altered ie transfered or dispatched; 
                        var grItemTxnEnum = ENUM_PHRM_StockTransactionType.PurchaseItem;
                        var GenericName = GenericData.Where(a => a.GenericId == gritm.GenericId).Select(a => a.GenericName).FirstOrDefault();
                        gritm.GenericName = GenericName;
                        if (stockTxn.Any(txnType => txnType.TransactionType != grItemTxnEnum) && stockTxn.Count() > 1)
                        {
                            gritm.IsItemAltered = true;
                            GoodReceipt.IsGRModified = true; // Bikash: 29June'20 - added to allow edit of GR details after modification (sale or transfer).
                        }
                    }
                    responseData.Status = "OK";
                    responseData.Results = GoodReceipt;
                }
                #endregion
                #region Get Return To Supplier All Items By ReturnToSupplierId
                else if (reqType == "getReturnToSupplierItemsByReturnToSupplierId")
                {

                    var returnToSupplierItemsList = (from retSuppItm in phrmdbcontext.PHRMReturnToSupplierItem
                                                     join retSuppl in phrmdbcontext.PHRMReturnToSupplier on retSuppItm.ReturnToSupplierId equals retSuppl.ReturnToSupplierId
                                                     join itm in phrmdbcontext.PHRMItemMaster on retSuppItm.ItemId equals itm.ItemId
                                                     where retSuppItm.ReturnToSupplierId == returnToSupplierId
                                                     select new
                                                     {
                                                         ItemName = itm.ItemName,
                                                         ReturnToSupplierId = retSuppItm.ReturnToSupplierId,
                                                         BatchNo = retSuppItm.BatchNo,
                                                         Quantity = retSuppItm.Quantity,
                                                         FreeQuantity = retSuppItm.FreeQuantity,
                                                         FreeAmount = retSuppItm.FreeAmount,
                                                         ExpiryDate = retSuppItm.ExpiryDate,
                                                         SalePrice = retSuppItm.SalePrice,
                                                         ItemPrice = retSuppItm.ItemPrice,
                                                         ReturnRate = retSuppItm.ReturnRate,
                                                         SubTotal = retSuppItm.SubTotal,
                                                         DiscountPercentage = retSuppItm.DiscountPercentage,
                                                         VATPercentage = retSuppItm.VATPercentage,
                                                         TotalAmount = retSuppItm.TotalAmount,
                                                         ReturnStatus = retSuppl.ReturnStatus,
                                                         CreatedBy = retSuppl.CreatedBy,
                                                         CreatedOn = retSuppl.CreatedOn,
                                                         GoodReceiptId = retSuppl.GoodReceiptId,


                                                         //UserName= rbacDbContext.Users.Where(a=>a.EmployeeId == retSuppl.CreatedBy).Select
                                                         //UserName = rbacDbContext.Users.Where(a => a.EmployeeId == retSuppl.CreatedBy).Select(a => a.UserName).FirstOrDefault()
                                                         //UserName = (from rbac in rbacDbContext.Users
                                                         //            where rbac.EmployeeId == retSuppl.CreatedBy
                                                         //            select rbac.UserName).FirstOrDefault()
                                                         //Created
                                                         //TotalAmt= retSuppl.TotalAmount,
                                                         //DiscAmt= retSuppl.DiscountAmount,
                                                         //CreditNoteId= retSuppl.CreditNotePrintId,
                                                         //vatAmt= retSuppl.VATAmount,
                                                         //Remarks = retSuppl.Remarks,
                                                         //CreditNoteDate= retSuppl.CreatedOn,


                                                     }
                                                 ).ToList();
                    var returnSupplierData = (from retSuppl in phrmdbcontext.PHRMReturnToSupplier.Where(rts => rts.ReturnToSupplierId == returnToSupplierId)
                                              join goodsreceipt in phrmdbcontext.PHRMGoodsReceipt on retSuppl.GoodReceiptId equals goodsreceipt.GoodReceiptId
                                              join supdetail in phrmdbcontext.PHRMSupplier on retSuppl.SupplierId equals supdetail.SupplierId into supp
                                              from supplier in supp.DefaultIfEmpty()

                                              select new
                                              {
                                                  CreditNoteNo = retSuppl.CreditNotePrintId,
                                                  SuppliersCRN = retSuppl.CreditNoteId,
                                                  ReturnType = retSuppl.ReturnStatus,
                                                  Time = retSuppl.CreatedOn,
                                                  UserName = (from emp in phrmdbcontext.Employees
                                                              where emp.EmployeeId == retSuppl.CreatedBy
                                                              select emp.FirstName).FirstOrDefault(),
                                                  ReturnDate = retSuppl.ReturnDate,
                                                  ContactNo = supplier.ContactNo,
                                                  PanNo = supplier.PANNumber,
                                                  RefNo = goodsreceipt.GoodReceiptPrintId,
                                                  SupplierName = supplier.SupplierName,
                                                  Remarks = retSuppl.Remarks

                                              }).FirstOrDefault();

                    responseData.Status = "OK";
                    responseData.Results = new { returnToSupplierItemsList, returnSupplierData };
                }
                #endregion
                #region Get WriteOff Items by WriteOffId
                else if (reqType == "getWriteOffItemsByWriteOffId")
                {
                    var WriteOffItemsByWriteOffIdList = (from writeOffItm in phrmdbcontext.PHRMWriteOffItem
                                                         join writeOff in phrmdbcontext.PHRMWriteOff on writeOffItm.WriteOffId equals writeOff.WriteOffId
                                                         join itm in phrmdbcontext.PHRMItemMaster on writeOffItm.ItemId equals itm.ItemId
                                                         where writeOffItm.WriteOffId == writeOffId
                                                         select new
                                                         {
                                                             ItemName = itm.ItemName,
                                                             WriteOffId = writeOffItm.WriteOffId,
                                                             BatchNo = writeOffItm.BatchNo,
                                                             WriteOffQuantity = writeOffItm.WriteOffQuantity,
                                                             ItemPrice = writeOffItm.ItemPrice,
                                                             SubTotal = writeOffItm.SubTotal,
                                                             DiscountPercentage = writeOffItm.DiscountPercentage,
                                                             VATPercentage = writeOffItm.VATPercentage,
                                                             TotalAmount = writeOffItm.TotalAmount
                                                         }
                              ).ToList();
                    var WriteOff = (from writeOff in phrmdbcontext.PHRMWriteOff
                                    join emp in phrmdbcontext.Employees on writeOff.CreatedBy equals emp.EmployeeId
                                    where writeOff.WriteOffId == writeOffId
                                    select new
                                    {
                                        WriteOffId = writeOff.WriteOffId,
                                        CreatedOn = writeOff.CreatedOn,
                                        SubTotal = writeOff.SubTotal,
                                        DiscountAmount = writeOff.DiscountAmount,
                                        VATAmount = writeOff.VATAmount,
                                        TotalAmount = writeOff.TotalAmount,
                                        Remark = writeOff.WriteOffRemark,
                                        UserName = emp.FullName
                                    }
                              ).FirstOrDefault();
                    var WriteOffdata = new { WriteOffitemsdetails = WriteOffItemsByWriteOffIdList, WriteOffdetails = WriteOff };

                    responseData.Status = "OK";
                    responseData.Results = WriteOffdata;
                }
                #endregion
                #region Get all sale invoice list data
                else if (reqType == "getsaleinvoicelist")
                {
                    var testdate = ToDate.AddDays(1);//to include ToDate, 1 day was added--rusha 07/10/2019

                    List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@FromDate", FromDate),
                        new SqlParameter("@ToDate", ToDate),
                        new SqlParameter("@StoreId", DispensaryId)
                    };

                    DataTable dtBilInvoiceDetails = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GetInvoicesBetweenDateRange", paramList, phrmdbcontext);

                    responseData.Results = dtBilInvoiceDetails;

                    responseData.Status = "OK";


                }
                #endregion
                #region Get all sale return list data
                else if (reqType == "getsalereturnlist")
                {
                    List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@FromDate", FromDate),
                        new SqlParameter("@ToDate", ToDate),
                        new SqlParameter("@StoreId", DispensaryId)
                    };

                    DataTable phrmInvoiceReturnDetails = DALFunctions.GetDataTableFromStoredProc("SP_PHRM_GetReturnInvoicesBetweenDateRange", paramList, phrmdbcontext);
                    responseData.Results = phrmInvoiceReturnDetails;
                    responseData.Status = "OK";
                }
                #endregion
                #region Get sale invoice items details by Invoice id
                else if (reqType == "getsaleinvoiceitemsbyid" && invoiceid > 0)
                {
                    //var test = phrmdbcontext.PHRMBillTransactionItems.Join(phrmdbcontext)
                    var saleInvoiceItemsByInvoiceId = (from invitm in phrmdbcontext.PHRMInvoiceTransactionItems
                                                           // join company in phrmdbcontext.PHRMCompany on invitm.CompanyId equals company.CompanyId
                                                       where invitm.InvoiceId == invoiceId
                                                       select new
                                                       {

                                                           InvoiceItemId = invitm.InvoiceItemId,
                                                           InvoiceId = invitm.InvoiceId,
                                                           invitm.CounterId,
                                                           ExpiryDate = invitm.ExpiryDate,
                                                           //ExpiryDate = (from stkTxnItm in phrmdbcontext.PHRMStockTransactionItems
                                                           //              where stkTxnItm.TransactionType == "sale"
                                                           //              && stkTxnItm.ReferenceNo == invitm.InvoiceId && stkTxnItm.BatchNo == invitm.BatchNo
                                                           //              select stkTxnItm.ExpiryDate).FirstOrDefault(),
                                                           ItemId = invitm.ItemId,
                                                           Quantity = invitm.Quantity,
                                                           ItemName = invitm.ItemName,
                                                           BatchNo = invitm.BatchNo,
                                                           Price = invitm.Price,
                                                           SalePrice = invitm.SalePrice,
                                                           FreeQuantity = invitm.FreeQuantity,
                                                           SubTotal = invitm.SubTotal,
                                                           VATPercentage = invitm.VATPercentage,
                                                           DiscountPercentage = invitm.DiscountPercentage,
                                                           TotalAmount = invitm.TotalAmount,
                                                           BilItemStatus = invitm.BilItemStatus,
                                                           CreatedBy = invitm.CreatedBy,
                                                           CreatedOn = invitm.CreatedOn,
                                                           TotalDisAmt = invitm.TotalDisAmt
                                                       }).Where(q => q.Quantity > 0).OrderBy(x => x.ItemName).ToList();
                    responseData.Status = "OK";

                    responseData.Results = saleInvoiceItemsByInvoiceId;

                }
                #endregion
                #region Get sale invoice ret items details by Invoice id
                else if (reqType == "getsaleinvoiceretitemsbyid" && invoiceid > 0)
                {
                    //var test = phrmdbcontext.PHRMBillTransactionItems.Join(phrmdbcontext)
                    var saleInvoiceRetItemsByInvoiceId = (from invret in phrmdbcontext.PHRMInvoiceReturnModel
                                                          join invretitm in phrmdbcontext.PHRMInvoiceReturnItemsModel on invret.InvoiceReturnId equals invretitm.InvoiceReturnId
                                                          join invitm in phrmdbcontext.PHRMInvoiceTransactionItems on invretitm.InvoiceItemId equals invitm.InvoiceItemId
                                                          where invret.InvoiceId == invoiceid
                                                          select new
                                                          {
                                                              InvoiceId = invret.InvoiceId,
                                                              InvoiceReturnId = invret.InvoiceReturnId,
                                                              invret.CounterId,
                                                              InvoiceReturnItemId = invretitm.InvoiceReturnItemId,
                                                              ExpiryDate = invitm.ExpiryDate,
                                                              ItemId = invretitm.ItemId,
                                                              ItemName = invitm.ItemName,
                                                              BatchNo = invretitm.BatchNo,
                                                              ReturnedQty = invretitm.ReturnedQty,
                                                              Price = invretitm.Price,
                                                              SalePrice = invretitm.SalePrice,
                                                              SubTotal = invretitm.SubTotal,
                                                              VATPercentage = invretitm.VATPercentage,
                                                              DiscountPercentage = invretitm.DiscountPercentage,
                                                              DiscountAmount = invretitm.DiscountAmount,
                                                              TotalAmount = invretitm.TotalAmount,
                                                              Remark = invret.Remarks,
                                                              CreditNoteId = invret.CreditNoteId,
                                                              CreatedBy = invretitm.CreatedBy,
                                                              CreatedOn = invretitm.CreatedOn

                                                          }).ToList();
                    responseData.Status = "OK";

                    responseData.Results = saleInvoiceRetItemsByInvoiceId;

                }
                #endregion
                #region Get sale return invoice items details by InvoiceReturnId
                else if (reqType == "getsalereturninvoiceitemsbyid" && invoiceretid > 0)
                {
                    var saleretInvoiceItemsByInvoiceretId = (from invret in phrmdbcontext.PHRMInvoiceReturnModel
                                                             join invretitm in phrmdbcontext.PHRMInvoiceReturnItemsModel on invret.InvoiceReturnId equals invretitm.InvoiceReturnId
                                                             join invitm in phrmdbcontext.PHRMInvoiceTransactionItems on invretitm.InvoiceItemId equals invitm.InvoiceItemId into invitmG
                                                             from invitmLJ in invitmG.DefaultIfEmpty()
                                                             join itm in phrmdbcontext.PHRMItemMaster on invretitm.ItemId equals itm.ItemId
                                                             join gen in phrmdbcontext.PHRMGenericModel on itm.GenericId equals gen.GenericId
                                                             join fy in phrmdbcontext.PharmacyFiscalYears on invret.FiscalYearId equals fy.FiscalYearId
                                                             where invret.InvoiceReturnId == invoiceretid
                                                             select new
                                                             {
                                                                 InvoiceId = invret.InvoiceId,
                                                                 InvoiceReturnId = invret.InvoiceReturnId,
                                                                 invret.CounterId,
                                                                 InvoiceReturnItemId = invretitm.InvoiceReturnItemId,
                                                                 ExpiryDate = invitmLJ.ExpiryDate,
                                                                 ItemId = invretitm.ItemId,
                                                                 GenericName = gen.GenericName,
                                                                 ItemName = invitmLJ == null ? itm.ItemName : invitmLJ.ItemName,
                                                                 BatchNo = invretitm.BatchNo,
                                                                 ReturnedQty = invretitm.ReturnedQty,
                                                                 Price = invretitm.Price,
                                                                 SalePrice = invretitm.SalePrice,
                                                                 SubTotal = invretitm.SubTotal,
                                                                 VATPercentage = invretitm.VATPercentage,
                                                                 DiscountPercentage = invretitm.DiscountPercentage,
                                                                 DiscountAmount = invretitm.DiscountAmount,
                                                                 TotalAmount = invretitm.TotalAmount,
                                                                 Remark = invret.Remarks,
                                                                 CreditNoteId = invret.CreditNoteId,
                                                                 CreatedBy = invretitm.CreatedBy,
                                                                 CreatedOn = invretitm.CreatedOn,
                                                                 PatientId = invret.PatientId,
                                                                 FiscalYearName = fy.FiscalYearName,
                                                                 Remarks = invret.Remarks,
                                                                 RackNo = (from itemrack in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == invretitm.ItemId && ri.StoreId == invretitm.StoreId)
                                                                           join rack in phrmdbcontext.PHRMRack on itemrack.RackId equals rack.RackId
                                                                           select rack.RackNo).FirstOrDefault()
                                                             }).ToList();


                    //if-guard
                    if (saleretInvoiceItemsByInvoiceretId == null || saleretInvoiceItemsByInvoiceretId.Count == 0)
                    {
                        throw new Exception("No returned items were found.");
                    }
                    int selectedPatientId = saleretInvoiceItemsByInvoiceretId.FirstOrDefault().PatientId;
                    var patientData = (from pat in phrmdbcontext.PHRMPatient
                                       join countryd in phrmdbcontext.CountrySubDivision on pat.CountrySubDivisionId equals countryd.CountrySubDivisionId
                                       where pat.PatientId == selectedPatientId
                                       select new
                                       {
                                           pat.PatientId,
                                           pat.FirstName,
                                           pat.MiddleName,
                                           pat.LastName,
                                           ShortName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                           pat.PhoneNumber,
                                           countryd.CountrySubDivisionName,
                                           pat.Age,
                                           pat.PANNumber,
                                           pat.Address,
                                           pat.DateOfBirth,
                                           pat.Gender,
                                           pat.PatientCode
                                       }).FirstOrDefault();
                    int selectedInvoiceId = saleretInvoiceItemsByInvoiceretId.FirstOrDefault().InvoiceId.Value;
                    var invoiceDetails = phrmdbcontext.PHRMInvoiceTransaction.Where(id => id.InvoiceId == selectedInvoiceId).FirstOrDefault();
                    var ReferenceInvoiceNo = "";
                    decimal CashAmount = 0;
                    decimal CreditAmount = 0;
                    var invretdetail = phrmdbcontext.PHRMInvoiceReturnModel.Where(ret => ret.InvoiceReturnId == invoiceretid).FirstOrDefault();
                    if (invretdetail != null)
                    {
                        ReferenceInvoiceNo = invretdetail.ReferenceInvoiceNo;
                        CashAmount = invretdetail.ReturnCashAmount;
                        CreditAmount = invretdetail.ReturnCreditAmount;
                    }

                    var invoiceReturnDetails = new { ReferenceInvoiceNo, CashAmount, CreditAmount };
                    responseData.Status = "OK";

                    responseData.Results = new { invoiceData = invoiceDetails, invoiceRetData = saleretInvoiceItemsByInvoiceretId, patientData = patientData, invoiceReturnDetails = invoiceReturnDetails };

                }
                #endregion
                #region Get Stock Manage by Item Id
                else if (reqType == "stockManage" && itemId > 0)
                {
                    var stkManage = (from gritm in phrmdbcontext.PHRMGoodsReceiptItems
                                     where (gritm.ItemId == itemId && gritm.AvailableQuantity > 0)
                                     select new
                                     {
                                         GoodReceiptItemId = gritm.GoodReceiptItemId,
                                         ItemId = gritm.ItemId,
                                         ItemName = gritm.ItemName,
                                         BatchNo = gritm.BatchNo,
                                         GRItemPrice = gritm.GRItemPrice,
                                         ExpiryDate = gritm.ExpiryDate,
                                         //ManufactureDate = gritm.ManufactureDate,
                                         curtQuantity = gritm.AvailableQuantity,
                                         modQuantity = gritm.AvailableQuantity,
                                         ReceivedQuantity = gritm.ReceivedQuantity,
                                     }
                               ).ToList();

                    var stkZeroManage = (from gritm in phrmdbcontext.PHRMGoodsReceiptItems
                                         where (gritm.ItemId == itemId && gritm.AvailableQuantity == 0)
                                         select new
                                         {
                                             GoodReceiptItemId = gritm.GoodReceiptItemId,
                                             ItemId = gritm.ItemId,
                                             ItemName = gritm.ItemName,
                                             BatchNo = gritm.BatchNo,
                                             GRItemPrice = gritm.GRItemPrice,
                                             ExpiryDate = gritm.ExpiryDate,
                                             // ManufactureDate = gritm.ManufactureDate,
                                             curtQuantity = gritm.AvailableQuantity,
                                             modQuantity = gritm.AvailableQuantity,
                                             ReceivedQuantity = gritm.ReceivedQuantity,
                                         }
                               ).ToList();

                    var finalStkManage = new { stockDetails = stkManage, zeroStockDetails = stkZeroManage };

                    responseData.Status = "OK";
                    responseData.Results = finalStkManage;
                }
                #endregion

                #region Get Invoice Items with details for ReturnFromCustomer functionality                                 
                else if (reqType == "getReturnFromCustDataModelByInvId" && invoiceId > 0)
                {
                    using (new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted }))
                    {
                        var invoiceTxnDetails = (from invoice in phrmdbcontext.PHRMInvoiceTransaction.AsNoTracking().Where(i => i.InvoicePrintId == invoiceid && i.FiscalYearId == FiscalYearId && i.StoreId == storeId)
                                                 join fs in phrmdbcontext.PharmacyFiscalYears on invoice.FiscalYearId equals fs.FiscalYearId
                                                 join user in phrmdbcontext.Employees on invoice.CreatedBy equals user.EmployeeId
                                                 join pat in phrmdbcontext.PHRMPatient on invoice.PatientId equals pat.PatientId
                                                 join countrysub in phrmdbcontext.CountrySubDivision on pat.CountrySubDivisionId equals countrysub.CountrySubDivisionId
                                                 join sett in phrmdbcontext.PHRMSettlements on invoice.SettlementId equals sett.SettlementId into dt
                                                 from subSett in dt.DefaultIfEmpty()
                                                 select new
                                                 {
                                                     PKInvoiceId = invoice.InvoiceId,
                                                     InvoiceId = invoice.InvoicePrintId,
                                                     StoreId = invoice.StoreId,
                                                     InvoiceDate = invoice.CreateOn,
                                                     PatientName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                     PatientType = (pat.IsOutdoorPat == true) ? "Outdoor" : "Indoor",
                                                     NSHINo = pat.Ins_NshiNumber,
                                                     ClaimCode = invoice.ClaimCode,
                                                     InsuranceBalance = (pat.Ins_HasInsurance == true) ? pat.Ins_InsuranceBalance : 0,
                                                     CreditAmount = invoice.CreditAmount.ToString(),
                                                     InvoiceBillStatus = invoice.BilStatus,
                                                     InvoiceTotalMoney = invoice.PaidAmount.ToString(),
                                                     IsReturn = invoice.IsReturn,
                                                     Tender = invoice.Tender,
                                                     SubTotal = invoice.SubTotal,
                                                     Change = invoice.Change,
                                                     Remarks = invoice.Remark,
                                                     PaidAmount = invoice.PaidAmount,
                                                     FiscalYear = fs.FiscalYearName,
                                                     ReceiptPrintNo = invoice.InvoicePrintId,
                                                     DiscountAmount = invoice.DiscountAmount,
                                                     DiscountPercentage = invoice.DiscountPer,
                                                     BillingUser = user.FirstName,
                                                     CashDiscount = subSett.DiscountAmount,
                                                     SettlementId = invoice.SettlementId,
                                                     OrganizationId = invoice.OrganizationId,
                                                     PatientVisitId = invoice.PatientVisitId,
                                                     PaymentMode = invoice.PaymentMode
                                                 }).FirstOrDefault();

                        var patient = (from invoice in phrmdbcontext.PHRMInvoiceTransaction.AsNoTracking().Where(i => i.InvoiceId == invoiceTxnDetails.PKInvoiceId)
                                       join pat in phrmdbcontext.PHRMPatient on invoice.PatientId equals pat.PatientId
                                       join countrysub in phrmdbcontext.CountrySubDivision on pat.CountrySubDivisionId equals countrysub.CountrySubDivisionId
                                       select new
                                       {
                                           pat.PatientId,
                                           pat.FirstName,
                                           pat.MiddleName,
                                           pat.LastName,
                                           ShortName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                           pat.PhoneNumber,
                                           countrysub.CountrySubDivisionName,
                                           pat.Age,
                                           pat.PANNumber,
                                           pat.Address,
                                           pat.DateOfBirth,
                                           pat.Gender,
                                           pat.PatientCode
                                       }).FirstOrDefault();

                        var invoiceItemtxnDetails = (from invItem in phrmdbcontext.PHRMInvoiceTransactionItems
                                                     join invRetItem in phrmdbcontext.PHRMInvoiceReturnItemsModel on invItem.InvoiceItemId equals invRetItem.InvoiceItemId into invretitmJ
                                                     from invretLJ in invretitmJ.DefaultIfEmpty()
                                                     join item in phrmdbcontext.PHRMItemMaster on invItem.ItemId equals item.ItemId
                                                     join generic in phrmdbcontext.PHRMGenericModel on item.GenericId equals generic.GenericId
                                                     where invItem.InvoiceId == invoiceTxnDetails.PKInvoiceId
                                                     group new { invItem, item, generic, invretLJ } by new
                                                     {
                                                         invItem.InvoiceItemId,
                                                         invItem.InvoiceId,
                                                         invItem.BatchNo,
                                                         invItem.ExpiryDate,
                                                         invItem.SalePrice,
                                                         invItem.Quantity,
                                                         invItem.Price,
                                                         invItem.SubTotal,
                                                         invItem.VATPercentage,
                                                         invItem.VATAmount,
                                                         invItem.DiscountPercentage,
                                                         invItem.TotalDisAmt,
                                                         invItem.TotalAmount,
                                                         invItem.CounterId,
                                                         invItem.CreatedBy,
                                                         invItem.CreatedOn,
                                                         item.ItemName,
                                                         item.ItemId,
                                                         generic.GenericName,
                                                         invItem.PriceCategoryId
                                                     } into grouped
                                                     select new
                                                     {
                                                         InvoiceId = grouped.Key.InvoiceId,
                                                         InvoiceItemId = grouped.Key.InvoiceItemId,
                                                         BatchNo = grouped.Key.BatchNo,
                                                         ExpiryDate = grouped.Key.ExpiryDate,
                                                         Quantity = grouped.Key.Quantity,
                                                         SoldQty = grouped.Key.Quantity,
                                                         ReturnedQty = grouped.Sum(x => x.invretLJ.ReturnedQty),
                                                         SalePrice = grouped.Key.SalePrice,
                                                         Price = grouped.Key.Price,
                                                         SubTotal = grouped.Key.SubTotal,
                                                         VATPercentage = grouped.Key.VATPercentage,
                                                         VATAmount = grouped.Key.VATAmount,
                                                         DiscountPercentage = grouped.Key.DiscountPercentage,
                                                         DiscountAmount = grouped.Key.TotalDisAmt,
                                                         TotalAmount = grouped.Key.TotalAmount,
                                                         ItemId = grouped.Key.ItemId,
                                                         ItemName = grouped.Key.ItemName,
                                                         GenericName = grouped.Key.GenericName,
                                                         CounterId = grouped.Key.CounterId,
                                                         CreatedBy = grouped.Key.CreatedBy,
                                                         CreatedOn = grouped.Key.CreatedOn,
                                                         PriceCategoryId = grouped.Key.PriceCategoryId,
                                                         RackNo = (from itemrack in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == grouped.Key.ItemId && ri.StoreId == invoiceTxnDetails.StoreId)
                                                                   join rack in phrmdbcontext.PHRMRack on itemrack.RackId equals rack.RackId
                                                                   select rack.RackNo).FirstOrDefault()
                                                     }).Where(a => a.Quantity > (double)(a.ReturnedQty)).ToList();

                        var PriceCategory = invoiceItemtxnDetails.FirstOrDefault();
                        var PriceCategoryDetails = (PriceCategory != null && PriceCategory.PriceCategoryId != null) ? billingDbContext.PriceCategoryModels.Where(p => p.PriceCategoryId == PriceCategory.PriceCategoryId).FirstOrDefault() : null;
                        responseData.Status = (invoiceItemtxnDetails == null) ? "Failed" : "OK";
                        responseData.Results = new { invoiceHeader = invoiceTxnDetails, patient = patient, invoiceItems = invoiceItemtxnDetails, priceCategoryDetails = PriceCategoryDetails };
                    }
                }
                #endregion
                else if (reqType == "GetRackByItem")
                {
                    var rackId = phrmdbcontext.PHRMItemMaster.Where(x => x.ItemId == itemId).Select(item => item.Rack).FirstOrDefault();

                    var RackName = phrmdbcontext.PHRMRack.Where(rack => rack.RackId == rackId).Select(rack => rack.RackNo).FirstOrDefault();
                    RackName = RackName == null ? "N/A" : RackName;

                    responseData.Status = "OK";
                    responseData.Results = RackName;

                }
                else if (reqType == "employeePreference")
                {
                    var preferenceValue = (from preference in phrmdbcontext.EmployeePreferences
                                           where preference.EmployeeId == employeeId &&
                                           preference.PreferenceName == "Medicationpreferences" &&
                                           preference.IsActive == true
                                           select preference.PreferenceValue).FirstOrDefault();
                    if (preferenceValue != null)
                    {
                        XmlDocument prefXmlDocument = new XmlDocument();
                        prefXmlDocument.LoadXml(preferenceValue);
                        // selecting the node of xml Document with tag LabTestId
                        XmlNodeList nodes = prefXmlDocument.SelectNodes("MedicineId");

                        var itemList = (from itm in phrmdbcontext.PHRMItemMaster
                                        join compny in phrmdbcontext.PHRMCompany on itm.CompanyId equals compny.CompanyId
                                        //join suplier in phrmdbcontext.PHRMSupplier on itm.SupplierId equals suplier.SupplierId
                                        join itmtype in phrmdbcontext.PHRMItemType on itm.ItemTypeId equals itmtype.ItemTypeId
                                        join unit in phrmdbcontext.PHRMUnitOfMeasurement on itm.UOMId equals unit.UOMId
                                        where preferenceValue.Contains(itm.ItemId.ToString())
                                        select new
                                        {
                                            ItemId = itm.ItemId,
                                            ItemName = itm.ItemName,
                                            ItemCode = itm.ItemCode,
                                            CompanyId = itm.CompanyId,
                                            CompanyName = compny.CompanyName,
                                            //SupplierId = itm.SupplierId,
                                            //SupplierName = suplier.SupplierName,
                                            ItemTypeId = itm.ItemTypeId,
                                            ItemTypeName = itmtype.ItemTypeName,
                                            UOMId = itm.UOMId,
                                            UOMName = unit.UOMName,
                                            ReOrderQuantity = itm.ReOrderQuantity,
                                            MinStockQuantity = itm.MinStockQuantity,
                                            BudgetedQuantity = itm.BudgetedQuantity,
                                            VATPercentage = itm.PurchaseVATPercentage,
                                            IsVATApplicable = itm.IsVATApplicable,
                                            IsActive = itm.IsActive
                                        }).ToList().OrderBy(a => a.ItemId);
                        responseData.Status = "OK";
                        responseData.Results = itemList;
                    }
                }
                #region GET: Prescription Items by ProviderId, PatientId 
                else if (reqType == "getPrescriptionItems" && patientId > 0 && providerId > 0)
                {
                    //var presItems = (from pres in phrmdbcontext.PHRMPrescriptionItems
                    //                 where pres.PatientId == patientId && pres.ProviderId == providerId && pres.OrderStatus != "final"
                    //                 select pres).ToList().OrderByDescending(a => a.CreatedOn);
                    //foreach (var presItm in presItems)
                    //{
                    //    presItm.ItemName = phrmdbcontext.PHRMItemMaster.Find(presItm.ItemId).ItemName;
                    //    var AvailableStockList = (from stk in phrmdbcontext.DispensaryStocks
                    //                              where stk.ItemId == presItm.ItemId && stk.AvailableQuantity > 0 && stk.ExpiryDate > DateTime.Now
                    //                              select stk).ToList();
                    //    presItm.IsAvailable = (AvailableStockList.Count > 0) ? true : false;
                    //    //(phrmdbcontext.DispensaryStock.Where(a => a.ItemId == presItm.ItemId).Select(a => a.AvailableQuantity).FirstOrDefault() > 0) ? true : false;
                    //}
                    //responseData.Results = presItems;
                    //responseData.Status = "OK";

                }
                #endregion
                #region GET: Sales Report according to items
                else if (reqType == "getSalesReport")
                {
                    if (IsOutdoorPat)
                    {
                        var invList = (from pat in masterDbContext.Patient
                                       where pat.IsOutdoorPat == IsOutdoorPat
                                       select pat
                                            ).ToList();
                        responseData.Results = invList;
                    }
                    else
                    {
                        var invList = (from pat in masterDbContext.Patient
                                       where (pat.IsOutdoorPat == IsOutdoorPat || pat.IsOutdoorPat == null)
                                       select pat
                                            ).ToList();
                        responseData.Results = invList;
                    }

                    responseData.Status = "OK";


                }
                #endregion
                #region GET: Sample
                else if (reqType == "getInOutPatientDetails")
                {
                    var test = phrmdbcontext.PHRMInvoiceTransactionItems.ToList();
                    if (IsOutdoorPat)
                    {
                        var invList = (from pat in masterDbContext.Patient
                                       where pat.IsOutdoorPat == IsOutdoorPat
                                       select pat
                                            ).ToList();
                        responseData.Results = invList;
                    }
                    else
                    {
                        var invList = (from pat in masterDbContext.Patient
                                       where (pat.IsOutdoorPat == IsOutdoorPat || pat.IsOutdoorPat == null)
                                       select pat
                                            ).ToList();
                        responseData.Results = invList;
                    }

                    responseData.Status = "OK";


                }
                #endregion
                #region GET: Stock Transaction Item List 
                else if (reqType == "getStockTxnItemList")
                {
                    //var result = (from stktxnitm in phrmdbcontext.PHRMStockTransactionModel
                    //              join itm in phrmdbcontext.PHRMItemMaster
                    //              on stktxnitm.ItemId equals itm.ItemId
                    //              select new
                    //              {
                    //                  stktxnitm.StockTxnItemId,
                    //                  stktxnitm.ItemId,
                    //                  itm.ItemName,
                    //                  stktxnitm.BatchNo,
                    //                  stktxnitm.Quantity,
                    //                  stktxnitm.Price,
                    //                  stktxnitm.SalePrice,
                    //                  stktxnitm.SubTotal,
                    //                  stktxnitm.TotalAmount,
                    //                  stktxnitm.InOut,
                    //                  stktxnitm.CreatedOn,
                    //                  stktxnitm.CreatedBy,
                    //                  stktxnitm.VATPercentage,
                    //                  stktxnitm.DiscountPercentage,
                    //                  stktxnitm.ExpiryDate
                    //              }).ToList();

                    var result = (from stktxnitm in phrmdbcontext.StoreStocks
                                  join itm in phrmdbcontext.PHRMItemMaster
                                  on stktxnitm.ItemId equals itm.ItemId
                                  select new
                                  {
                                      StockId = stktxnitm.StockId,
                                      ItemID = stktxnitm.ItemId,
                                      ItemName = itm.ItemName,
                                      BatchNo = stktxnitm.StockMaster.BatchNo,
                                      Quantity = stktxnitm.AvailableQuantity,
                                      Price = stktxnitm.StockMaster.CostPrice,
                                      SalePrice = stktxnitm.StockMaster.SalePrice,
                                      ExpiryDate = stktxnitm.StockMaster.ExpiryDate
                                  }).ToList();
                    responseData.Status = "OK";
                    responseData.Results = result;
                }
                #endregion
                #region GET: Stock Details with 0, null or >0 Quantity
                //this stock details with all unique (by ItemId,ExpiryDate,BatchNo)  records with sum of Quantity
                //items with 0 quantity or more than 0 showing in list
                else if (reqType == "allItemsStockDetails")
                {
                    var totalStock = from stk in phrmdbcontext.StoreStocks
                                     join itm in phrmdbcontext.PHRMItemMaster on stk.ItemId equals itm.ItemId
                                     join gen in phrmdbcontext.PHRMGenericModel on itm.GenericId equals gen.GenericId into gens
                                     from genLj in gens.DefaultIfEmpty()
                                     where stk.StoreId == DispensaryId
                                     group new { stk, itm, genLj } by new { stk.ItemId, stk.StockMaster.BatchNo, stk.StockMaster.ExpiryDate, stk.StockMaster.CostPrice, stk.StockMaster.SalePrice, itm.ItemName } into stkGrouped
                                     select new GetDispensaryStockVm
                                     {
                                         ItemId = stkGrouped.Key.ItemId,
                                         GenericName = (stkGrouped.FirstOrDefault().genLj != null) ? stkGrouped.FirstOrDefault().genLj.GenericName : "N/A",
                                         ItemName = stkGrouped.Key.ItemName,
                                         BatchNo = stkGrouped.Key.BatchNo,
                                         ExpiryDate = stkGrouped.Key.ExpiryDate.Value,
                                         SalePrice = stkGrouped.Key.SalePrice,
                                         CostPrice = stkGrouped.Key.CostPrice,
                                         AvailableQuantity = stkGrouped.Sum(s => s.stk.AvailableQuantity),
                                         IsInsuranceApplicable = stkGrouped.FirstOrDefault().itm.IsInsuranceApplicable,
                                         GovtInsurancePrice = stkGrouped.FirstOrDefault().itm.GovtInsurancePrice,
                                         RackNo = (from itemrack in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == stkGrouped.Key.ItemId && ri.StoreId == stkGrouped.FirstOrDefault().stk.StoreId)
                                                   join rack in phrmdbcontext.PHRMRack on itemrack.RackId equals rack.RackId
                                                   select rack.RackNo).FirstOrDefault()
                                     };

                    responseData.Status = (totalStock == null) ? "Failed" : "OK";
                    responseData.Results = totalStock;
                }
                #endregion
                #region GET: Stock Details with 0, null or >0 Quantity
                //this stock details with all unique (by ItemId,ExpiryDate,BatchNo)  records with sum of Quantity
                //items with 0 quantity or more than 0 showing in list
                else if (reqType == "allItemsStock")
                {
                    var totalStock = (from stk in phrmdbcontext.StoreStocks
                                      join itm in phrmdbcontext.PHRMItemMaster on stk.ItemId equals itm.ItemId
                                      select new
                                      {

                                          ItemId = stk.ItemId,
                                          ItemName = itm.ItemName,
                                          AvailableQuantity = stk.AvailableQuantity,
                                          SalePrice = stk.StockMaster.SalePrice,
                                          IsActive = true

                                      }).ToList();
                    //  var totalStock = phrmdbcontext.PHRMStockTransactionModel.Where(a => a.ExpiryDate >= DateTime.Now).ToList().GroupBy(a => new { a.ItemId }).Select(g =>
                    //      new PHRMStockTransactionItemsModel
                    //      {
                    //          ItemId = g.Key.ItemId,
                    //          SalePrice = g.LastOrDefault().SalePrice,
                    //          Quantity = g.Where(w => w.InOut == "in").Sum(q => q.Quantity) + g.Where(w => w.InOut == "in")
                    //          .Sum(f => f.FreeQuantity).Value - g.Where(w => w.InOut == "out").Sum(o => o.Quantity) - g.Where(w => w.InOut == "out").Sum(o => o.FreeQuantity).Value,
                    //          FreeQuantity = g.Where(w => w.InOut == "in").Sum(q => q.Quantity),
                    //      }
                    //).GroupJoin(phrmdbcontext.PHRMItemMaster.Where(a => a.IsActive == true).ToList(), a => a.ItemId, b => b.ItemId, (a, b) =>
                    //new GoodReceiptItemsViewModel
                    //{
                    //    ItemId = a.ItemId.Value,
                    //    ItemName = b.Select(s => s.ItemName).FirstOrDefault(),
                    //    AvailableQuantity = a.Quantity,
                    //    SalePrice = a.SalePrice.Value,
                    //    //Price = a.Price.Value,
                    //    //GRItemPrice = a.Price.Value,
                    //    GenericId = b.Select(s => s.GenericId.Value).FirstOrDefault(),
                    //    IsActive = true
                    //}
                    //).OrderBy(name => name.ItemName).ToList().Join(phrmdbcontext.PHRMGenericModel.ToList(), a => a.GenericId, b => b.GenericId, (a, b) => new
                    //{ GoodReceiptItemsViewModel = a, PHRMGenericModel = b }).Join(phrmdbcontext.PHRMCategory.ToList(), a => a.PHRMGenericModel.CategoryId, b => b.CategoryId, (a, b) => new { a.GoodReceiptItemsViewModel, a.PHRMGenericModel, PHRMCategory = b })
                    //.Select(s => new GoodReceiptItemsViewModel
                    //{

                    //    ItemId = s.GoodReceiptItemsViewModel.ItemId,
                    //    //BatchNo = s.GoodReceiptItemsViewModel.BatchNo,
                    //    //ExpiryDate = s.GoodReceiptItemsViewModel.ExpiryDate.Date,
                    //    ItemName = s.GoodReceiptItemsViewModel.ItemName,
                    //    AvailableQuantity = s.GoodReceiptItemsViewModel.AvailableQuantity,

                    //    SalePrice = s.GoodReceiptItemsViewModel.SalePrice,
                    //    //Price = s.GoodReceiptItemsViewModel.Price,
                    //    //GRItemPrice = s.GoodReceiptItemsViewModel.GRItemPrice,
                    //    //CategoryName = s.PHRMCategory.CategoryName,
                    //    //VATPercentage = s.GoodReceiptItemsViewModel.VATPercentage,
                    //    IsActive = true
                    //});

                    responseData.Status = (totalStock == null) ? "Failed" : "OK";
                    responseData.Results = totalStock.Where(a => a.AvailableQuantity != 0);
                }
                #endregion
                #region GET: patientSummery
                else if (reqType != null && reqType == "patientSummary" && patientId != null && patientId != 0)
                {
                    //get all deposit related transactions of this patient. and sum them acc to DepositType groups.
                    var patientAllDepositTxns = (from bill in phrmdbcontext.DepositModel
                                                 where bill.PatientId == patientId//here PatientId comes as InputId from client.
                                                 group bill by new { bill.PatientId, bill.DepositType } into p
                                                 select new
                                                 {
                                                     DepositType = p.Key.DepositType,
                                                     DepositAmount = p.Sum(a => a.DepositAmount)
                                                 }).ToList();
                    //separate sum of each deposit types and calculate deposit balance.
                    double? totalDepositAmt, totalDepositDeductAmt, totalDepositReturnAmt, currentDepositBalance;
                    currentDepositBalance = totalDepositAmt = totalDepositDeductAmt = totalDepositReturnAmt = 0;

                    if (patientAllDepositTxns.Where(bil => bil.DepositType == "deposit").FirstOrDefault() != null)
                    {
                        totalDepositAmt = patientAllDepositTxns.Where(bil => bil.DepositType == "deposit").FirstOrDefault().DepositAmount;
                    }
                    if (patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "depositdeduct").FirstOrDefault() != null)
                    {
                        totalDepositDeductAmt = patientAllDepositTxns.Where(bil => bil.DepositType == "depositdeduct").FirstOrDefault().DepositAmount;
                    }
                    if (patientAllDepositTxns.Where(bil => bil.DepositType == "depositreturn").FirstOrDefault() != null)
                    {
                        totalDepositReturnAmt = patientAllDepositTxns.Where(bil => bil.DepositType == "depositreturn").FirstOrDefault().DepositAmount;
                    }
                    //below is the formula to calculate deposit balance.
                    currentDepositBalance = totalDepositAmt - totalDepositDeductAmt - totalDepositReturnAmt;

                    //Part-2: Get Total Provisional Items
                    //for this request type, patientid comes as inputid.
                    var patProvisional = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                              //sud: 4May'18 changed unpaid to provisional
                                          where bill.PatientId == patientId && (bill.BilItemStatus == "provisional" || bill.BilItemStatus == "wardconsumption")//here PatientId comes as InputId from client.
                                          group bill by new { bill.PatientId } into p
                                          select new
                                          {
                                              TotalProvisionalAmt = p.Sum(a => a.TotalAmount)
                                          }).FirstOrDefault();

                    var patProvisionalAmt = patProvisional != null ? (double)patProvisional.TotalProvisionalAmt : 0;



                    //Part-3: Return a single object with Both Balances (Deposit and Credit).
                    //exclude returned invoices from credit total
                    var patCredits = phrmdbcontext.PHRMInvoiceTransaction
                                    .Where(b => b.PatientId == patientId && b.BilStatus != "paid" && b.IsReturn != true)
                                     .Sum(b => b.TotalAmount);

                    double patCreditAmt = patCredits != null ? (double)patCredits : 0;

                    //Rohit: To get Credit Return Amount of Particular Patient
                    var patCredit = (from inv in phrmdbcontext.PHRMInvoiceTransaction
                                     join retinv in phrmdbcontext.PHRMInvoiceReturnModel on inv.InvoiceId equals retinv.InvoiceId
                                     where inv.PatientId == patientId && inv.BilStatus != "paid" && inv.IsReturn != true
                                     select new
                                     {
                                         CreditReturnAmount = retinv.TotalAmount
                                     }).ToList();
                    var patCreditRet = patCredit.Sum(a => a.CreditReturnAmount);
                    double patCreditRetAmt = patCreditRet != null ? (double)patCreditRet : 0;

                    decimal IpCreditLimit = 0;
                    decimal OpCreditLimit = 0;
                    if (PriceCategoryId != null)
                    {
                        var PatientMapPriceCategoryDetail = phrmdbcontext.PatientSchemeMaps.Where(p => p.PatientId == patientId && p.IsActive == true && p.PriceCategoryId == PriceCategoryId && p.LatestPatientVisitId == PatientVisitId).FirstOrDefault();
                        if (PatientMapPriceCategoryDetail != null)
                        {
                            IpCreditLimit = PatientMapPriceCategoryDetail.IpCreditLimit;
                            OpCreditLimit = PatientMapPriceCategoryDetail.OpCreditLimit;
                        }
                    }
                    decimal OpBalance = 0;
                    decimal IpBalance = 0;
                    if (MemberNo != null || MemberNo != "")
                    {
                        var medicareMeber = patientDbContext.MedicareMembers.Where(a => a.MemberNo == MemberNo).FirstOrDefault();
                        if (medicareMeber != null)
                        {
                            if (medicareMeber.IsDependent)
                            {
                                var Balance = patientDbContext.MedicareMembers.Where(a => a.MemberNo == MemberNo && medicareMeber.IsDependent == true).Join(patientDbContext.MedicareMemberBalances,
                                mm => mm.ParentMedicareMemberId,
                                mmb => mmb.MedicareMemberId,
                                (mm, mmb) => new
                                {
                                    OpBalance = mmb.OpBalance,
                                    IpBalance = mmb.IpBalance
                                }).FirstOrDefault();

                                OpBalance = (Balance != null && Balance.OpBalance > 0) ? Balance.OpBalance : 0;
                                IpBalance = (Balance != null && Balance.IpBalance > 0) ? Balance.IpBalance : 0;
                            }
                            else
                            {
                                var Balance = patientDbContext.MedicareMembers.Where(a => a.MemberNo == MemberNo).Join(patientDbContext.MedicareMemberBalances,
                                                            mm => mm.MedicareMemberId,
                                                            mmb => mmb.MedicareMemberId,
                                                            (mm, mmb) => new
                                                            {
                                                                OpBalance = mmb.OpBalance,
                                                                IpBalance = mmb.IpBalance
                                                            }).FirstOrDefault();

                                OpBalance = (Balance != null && Balance.OpBalance > 0) ? Balance.OpBalance : 0;
                                IpBalance = (Balance != null && Balance.IpBalance > 0) ? Balance.IpBalance : 0;
                            }
                        }
                    }


                    //Part-4: Return a single object with Both Balances (Deposit and Credit).
                    var patSummery = new
                    {
                        PatientId = patientId,
                        CreditAmount = patCreditAmt - patCreditRetAmt,
                        ProvisionalAmt = patProvisionalAmt,
                        TotalDue = patCreditAmt + patProvisionalAmt - patCreditRetAmt,
                        DepositBalance = currentDepositBalance,
                        BalanceAmount = currentDepositBalance - (patCreditAmt - patCreditRetAmt + patProvisionalAmt),
                        IpCreditLimit = IpCreditLimit,
                        OpCreditLimit = OpCreditLimit,
                        OpBalance = OpBalance,
                        IpBalance = IpBalance
                    };

                    responseData.Status = "OK";
                    responseData.Results = patSummery;
                }
                #endregion
                // get rack list
                else if (reqType == "getRackList")
                {
                    var rackList = (from rk in phrmdbcontext.PHRMRack
                                    select rk
                                  ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = rackList;
                }
                else if (reqType == "getsalescategorylist")
                {
                    var salescategoryList = (from scl in phrmdbcontext.PHRMStoreSalesCategory
                                             select scl
                                  ).ToList();
                    responseData.Status = "OK";
                    responseData.Results = salescategoryList;
                }
                else if (reqType == "getStoreItemList")
                {
                    List<PHRMStockTransactionModel> storeItemList = (from storeItem in phrmdbcontext.StockTransactions
                                                                     select storeItem).ToList();
                    responseData.Status = "OK";
                    responseData.Results = storeItemList;
                }
                #region //1. List out all the patient with the provisional amount 

                else if (reqType != null && reqType.ToLower() == "listpatientunpaidtotal")
                {

                    var allPatientCreditReceipts = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems.Where(i => i.StoreId == DispensaryId)
                                                    join pat in phrmdbcontext.PHRMPatient on bill.PatientId equals pat.PatientId
                                                    join subdiv in phrmdbcontext.CountrySubDivision on pat.CountrySubDivisionId equals subdiv.CountrySubDivisionId
                                                    join billingUser in phrmdbcontext.Users on bill.CreatedBy equals billingUser.EmployeeId
                                                    where (bill.BilItemStatus == "provisional" || bill.BilItemStatus == "wardconsumption") && bill.Quantity != 0 && ((DbFunctions.TruncateTime(bill.CreatedOn) >= FromDate && DbFunctions.TruncateTime(bill.CreatedOn) <= ToDate))
                                                    //couldn't use Patient.ShortName directly since it's not mapped to DB and hence couldn't be used inside LINQ.
                                                    group bill by new { pat.PatientId, pat.PatientCode, pat.FirstName, pat.LastName, pat.MiddleName, pat.DateOfBirth, pat.Gender, bill.InvoiceId, pat.PhoneNumber, bill.CreatedBy, billingUser.UserName, pat.Address, subdiv.CountrySubDivisionName, pat.PANNumber } into p
                                                    select new
                                                    {
                                                        PatientId = p.Key.PatientId,
                                                        PatientCode = p.Key.PatientCode,
                                                        ShortName = p.Key.FirstName + " " + (string.IsNullOrEmpty(p.Key.MiddleName) ? "" : p.Key.MiddleName + " ") + p.Key.LastName,
                                                        DateOfBirth = p.Key.DateOfBirth,
                                                        CreatedOn = p.Key.CreatedBy,
                                                        Gender = p.Key.Gender,
                                                        Address = p.Key.Address,
                                                        CountrySubDivisionName = p.Key.CountrySubDivisionName,
                                                        PhoneNumber = p.Key.PhoneNumber,
                                                        PANNumber = p.Key.PANNumber,
                                                        LastCreditBillDate = p.Max(a => a.CreatedOn),
                                                        //TotalCredit = Math.Round(p.Sum(a => a.TotalAmount.Value), 0),
                                                        TotalCredit = p.Sum(a => a.TotalAmount),
                                                        ContactNo = p.Key.PhoneNumber,
                                                        UserName = p.Key.UserName
                                                    }).ToList().OrderByDescending(b => b.LastCreditBillDate);

                    responseData.Status = "OK";
                    responseData.Results = allPatientCreditReceipts;
                }

                else if (reqType != null && reqType.ToLower() == "provisionalitemsbypatientid")
                {

                    var patCreditItems = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                          join mstitm in phrmdbcontext.PHRMItemMaster on bill.ItemId equals mstitm.ItemId
                                          join gen in phrmdbcontext.PHRMGenericModel on mstitm.GenericId equals gen.GenericId
                                          where (bill.BilItemStatus == "provisional" || bill.BilItemStatus == "wardconsumption")
                                          && bill.PatientId == patientId && bill.Quantity > 0 && bill.StoreId == DispensaryId
                                          select new ProvisionalSaleDetailVm
                                          {
                                              InvoiceItemId = bill.InvoiceItemId,
                                              StoreId = bill.StoreId,
                                              PatientId = bill.PatientId,
                                              ItemId = bill.ItemId,
                                              ItemName = bill.ItemName,
                                              GenericName = gen.GenericName,
                                              BatchNo = bill.BatchNo,
                                              Quantity = bill.Quantity,
                                              Price = bill.Price,
                                              SalePrice = bill.SalePrice,
                                              SubTotal = bill.SubTotal,
                                              VATPercentage = bill.VATPercentage,
                                              VATAmount = bill.VATAmount,
                                              DiscountPercentage = bill.DiscountPercentage,
                                              TotalAmount = bill.TotalAmount,
                                              BilItemStatus = bill.BilItemStatus,
                                              Remark = bill.Remark,
                                              CreatedBy = bill.CreatedBy,
                                              CreatedOn = bill.CreatedOn,
                                              CounterId = bill.CounterId,
                                              VisitType = bill.VisitType,
                                              TotalDisAmt = bill.TotalDisAmt,
                                              PerItemDisAmt = bill.PerItemDisAmt,
                                              ExpiryDate = bill.ExpiryDate,
                                              PrescriberId = bill.PrescriberId,
                                              RackNo = (from itemrack in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == bill.ItemId && ri.StoreId == DispensaryId)
                                                        join rack in phrmdbcontext.PHRMRack on itemrack.RackId equals rack.RackId
                                                        select rack.RackNo).FirstOrDefault()
                                          }).ToList();


                    foreach (var wardCreditItems in patCreditItems)
                    {
                        var User = phrmdbcontext.Users.Where(a => a.EmployeeId == wardCreditItems.CreatedBy).FirstOrDefault();
                        var stores = phrmdbcontext.PHRMStore.Where(a => a.StoreId == wardCreditItems.StoreId).FirstOrDefault();
                        if (wardCreditItems.BilItemStatus == "wardconsumption")
                        {
                            var Consumption = phrmdbcontext.WardConsumption.Where(a => a.InvoiceItemId == wardCreditItems.InvoiceItemId).FirstOrDefault();
                            wardCreditItems.WardName = phrmdbcontext.WardModel.Find(Consumption.WardId).WardName;
                            wardCreditItems.WardUser = phrmdbcontext.Employees.Find(Consumption.CreatedBy).FullName;
                        }
                        else if (wardCreditItems.BilItemStatus == "provisional")
                        {
                            wardCreditItems.WardUser = User.UserName;
                            wardCreditItems.WardName = stores.Name;
                            wardCreditItems.StockId = (from provItem in phrmdbcontext.PHRMInvoiceTransactionItems.Where(provItem => provItem.InvoiceItemId == wardCreditItems.InvoiceItemId)
                                                       from dispenStock in phrmdbcontext.StoreStocks.Where(d => d.ItemId == provItem.ItemId && d.StockMaster.BatchNo == provItem.BatchNo && d.StockMaster.SalePrice == provItem.SalePrice && d.StockMaster.ExpiryDate == provItem.ExpiryDate)
                                                       select dispenStock.StockId).FirstOrDefault();

                        }


                    }
                    // 21th sep 2020:Ashish : replaced patCreditItems with ascending order list items.
                    var itemList = patCreditItems.OrderByDescending(s => s.InvoiceItemId).ToList();
                    patCreditItems = itemList;

                    responseData.Results = patCreditItems;
                    responseData.Status = "OK";

                }
                #endregion
                #region get patient deposit
                else if (reqType != null && reqType == "patAllDeposits" && patientId != null && patientId != 0)
                {
                    var PatientDeposit = (from data in phrmdbcontext.DepositModel
                                          where data.PatientId == patientId
                                          group data by new { data.PatientId, data.DepositType } into p
                                          select new
                                          {
                                              DepositType = p.Key.DepositType,
                                              DepositAmount = p.Sum(a => a.DepositAmount),
                                          }).ToList();

                    responseData.Status = "OK";
                    responseData.Results = PatientDeposit;
                }
                #endregion
                else if (reqType == "pending-bills-for-settlements")
                {
                    List<SqlParameter> paramList = new List<SqlParameter>()
                        {
                            new SqlParameter("@StoreId", storeId),
                            new SqlParameter("@OrganizationId", organizationId)
                         };
                    DataTable settlInfo = DALFunctions.GetDataTableFromStoredProc("SP_TXNS_PHRM_SettlementSummary", paramList, phrmdbcontext);

                    responseData.Results = settlInfo;
                    responseData.Status = "OK";
                }
                //To get all the settled receipt list
                else if (reqType == "allPHRMSettlements" && storeId != 0)
                {
                    var realToDate = ToDate.AddDays(1);
                    var allSettlements = (from sett in phrmdbcontext.PHRMSettlements
                                          join pat in phrmdbcontext.PHRMPatient on sett.PatientId equals pat.PatientId
                                          where sett.StoreId == storeId
                                          select new
                                          {
                                              HospitalNo = pat.PatientCode,
                                              PatientName = pat.ShortName,
                                              DateOfBirth = pat.DateOfBirth,
                                              Gender = pat.Gender,
                                              ContactNumber = pat.PhoneNumber,
                                              SettlementDate = sett.SettlementDate,
                                              SettlementId = sett.SettlementId,
                                              ReceiptNo = sett.SettlementReceiptNo,
                                          }).OrderByDescending(s => s.ReceiptNo)
                                            .Where(s => s.SettlementDate > FromDate && s.SettlementDate < realToDate).ToList();


                    responseData.Status = "OK";
                    responseData.Results = allSettlements;
                }
                else if (reqType != null && reqType == "unpaidInvoiceByPatientId" && patientId != null && patientId != 0)
                {
                    PHRMPatient currPatient = phrmdbcontext.PHRMPatient.Where(pat => pat.PatientId == patientId).FirstOrDefault();
                    /*if (currPatient != null)
                    {
                        string subDivName = (from pat in phrmdbcontext.PHRMPatient
                                             join countrySubdiv in phrmdbcontext.CountrySubDivision
                                             on pat.CountrySubDivisionId equals countrySubdiv.CountrySubDivisionId
                                             where pat.PatientId == currPatient.PatientId
                                             select countrySubdiv.CountrySubDivisionName
                                          ).FirstOrDefault();

                        currPatient.CountrySubDivisionName = subDivName;
                    }

                    var patCreditInvoice = (from bill in phrmdbcontext.PHRMInvoiceTransaction.Include("InvoiceItems")
                                            where bill.BilStatus == "unpaid" && bill.IsReturn != true && bill.PatientId == patientId
                                            select bill).ToList<PHRMInvoiceTransactionModel>().OrderBy(b => b.InvoiceId);

                    var patCreditDetails = new { Patient = currPatient, CreditItems = patCreditInvoice };


                    responseData.Results = patCreditDetails;
                    responseData.Status = "OK";*/


                    if (currPatient != null)
                    {
                        List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@PatientId", patientId),
                        new SqlParameter("@OrganizationId", organizationId)
                    };
                        DataSet dsPharmacyInfoOfPatientForSettlement = DALFunctions.GetDatasetFromStoredProc("SP_PHRM_GetAllInvoiceOfPatientForSettlement", paramList, phrmdbcontext);

                        DataTable dtPatientInfo = dsPharmacyInfoOfPatientForSettlement.Tables[0];
                        DataTable dtCreditInvoices = dsPharmacyInfoOfPatientForSettlement.Tables[1];
                        DataTable dtDepositInfo = dsPharmacyInfoOfPatientForSettlement.Tables[2];
                        DataTable dtProvisionalInfo = dsPharmacyInfoOfPatientForSettlement.Tables[3];

                        var pharmacyReturnInfo = new
                        {
                            PatientInfo = Settlement_PatientInfoVM.MapDataTableToSingleObject(dtPatientInfo),
                            CreditInvoiceInfo = dtCreditInvoices,
                            DepositInfo = Settlement_DepositInfoVM.MapDataTableToSingleObject(dtDepositInfo),
                            ProvisionalInfo = Settlement_ProvisionalInfoVM.MapDataTableToSingleObject(dtProvisionalInfo)



                        };
                        responseData.Status = "OK";
                        responseData.Results = pharmacyReturnInfo;

                    }


                }

                else if (reqType == "get-settlement-single-invoice-preview")
                {

                    List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@invoiceId", invoiceId),
                    };
                    DataSet dsInvoicePreview = DALFunctions.GetDatasetFromStoredProc("SP_PHRM_Settlement_GetInvoiceAndInvoiceReturnItemsOfInvoiceForPreview", paramList, phrmdbcontext);

                    DataTable dtInvoiceInfo = dsInvoicePreview.Tables[0];
                    DataTable dtInvoiceItemInfo = dsInvoicePreview.Tables[1];
                    DataTable dtCreditNotes = dsInvoicePreview.Tables[2];
                    DataTable dtCreditNoteItems = dsInvoicePreview.Tables[3];

                    var settlmntInvoicePreview = new
                    {
                        InvoiceInfo = Settlement_InvoicePreview_InvoiceInfoVM.MapDataTableToSingleObject(dtInvoiceInfo),
                        InvoiceItems = dtInvoiceItemInfo,
                        CreditNotes = dtCreditNotes,
                        CreditNoteItems = dtCreditNoteItems
                    };
                    responseData.Status = "OK";
                    responseData.Results = settlmntInvoicePreview;

                }

                else if (reqType != null && reqType == "patientPastBillSummary" && patientId != null && patientId != 0)
                {
                    var patientAllDepositTxns = (from bill in phrmdbcontext.DepositModel
                                                 where bill.PatientId == patientId// && bill.i == true//here PatientId comes as InputId from client.
                                                 group bill by new { bill.PatientId, bill.DepositType } into p
                                                 select new
                                                 {
                                                     DepositType = p.Key.DepositType,
                                                     SumAmount = p.Sum(a => a.DepositAmount)
                                                 }).ToList();
                    double? totalDepositAmt, totalDepositDeductAmt, totalDepositReturnAmt, currentDepositBalance;
                    currentDepositBalance = totalDepositAmt = totalDepositDeductAmt = totalDepositReturnAmt = 0;

                    if (patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "deposit").FirstOrDefault() != null)
                    {
                        totalDepositAmt = patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "deposit").FirstOrDefault().SumAmount;
                    }
                    if (patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "depositdeduct").FirstOrDefault() != null)
                    {
                        totalDepositDeductAmt = patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "depositdeduct").FirstOrDefault().SumAmount;
                    }
                    if (patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "depositreturn").FirstOrDefault() != null)
                    {
                        totalDepositReturnAmt = patientAllDepositTxns.Where(bil => bil.DepositType.ToLower() == "depositreturn").FirstOrDefault().SumAmount;
                    }
                    //below is the formula to calculate deposit balance.
                    currentDepositBalance = totalDepositAmt - totalDepositDeductAmt - totalDepositReturnAmt;

                    //Part-2: Get Total Provisional Items
                    //for this request type, patientid comes as inputid.

                    //Rajesh:- Getting InoviceId From TXN_Invoice table
                    //var patId = phrmdbcontext.PHRMInvoiceTransaction.Where(a => a.PatientId == patientId).FirstOrDefault(); 

                    var patProvisional = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                              //sud: 4May'18 changed unpaid to provisional
                                          where bill.PatientId == patientId && (bill.BilItemStatus == "provisional" || bill.BilItemStatus == "wardconsumption") //here PatientId comes as InputId from client.
                                                                                                                                                                // && (bill.IsInsurance == false || bill.IsInsurance == null)
                                          group bill by new { bill.InvoiceId } into p
                                          select new
                                          {
                                              TotalProvisionalAmt = p.Sum(a => a.TotalAmount)
                                          }).FirstOrDefault();

                    var patProvisionalAmt = patProvisional != null ? patProvisional.TotalProvisionalAmt : 0;



                    var patCredits = (from bill in phrmdbcontext.PHRMInvoiceTransaction
                                      where bill.PatientId == patientId
                                      && bill.BilStatus == "unpaid"
                                      group bill by new { bill.PatientId } into p
                                      select new
                                      {
                                          TotalUnPaidAmt = p.Sum(a => a.TotalAmount)
                                      }).FirstOrDefault();

                    var patCreditAmt = patCredits != null ? patCredits.TotalUnPaidAmt : 0;

                    //Part-4: Get Total Paid Amount
                    var patPaid = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                   where bill.PatientId == patientId
                                   && bill.BilItemStatus == "paid"
                                   group bill by new { bill.PatientId } into p
                                   select new
                                   {
                                       TotalPaidAmt = p.Sum(a => a.TotalAmount)
                                   }).FirstOrDefault();

                    var patPaidAmt = patPaid != null ? patPaid.TotalPaidAmt : 0;

                    //Part - 5: get Total Discount Amount
                    //var patDiscount = dbContext.BillingTransactionItems
                    //                .Where(b => b.PatientId == InputId && b.BillStatus == "unpaid" && b.ReturnStatus == false && b.IsInsurance == false)
                    //                 .Sum(b => b.DiscountAmount);

                    //double patDiscountAmt = patDiscount != null ? patDiscount.Value : 0;

                    var patDiscount = (from bill in phrmdbcontext.PHRMInvoiceTransaction
                                       where bill.PatientId == patientId
                                       && bill.BilStatus == "unpaid"
                                       // && (bill.ReturnStatus == false || bill.ReturnStatus == null)
                                       //  && (bill.IsInsurance == false || bill.IsInsurance == null)
                                       select bill.DiscountAmount).FirstOrDefault();

                    var patDiscountAmt = patDiscount != null ? patDiscount : 0;

                    //Part-6: get Total Cancelled Amount
                    var patCancel = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                         //sud: 4May'18 changed unpaid to provisional
                                     where bill.PatientId == patientId
                                     && bill.BilItemStatus == "cancel"
                                     //   && (bill.IsInsurance == false || bill.IsInsurance == null)
                                     group bill by new { bill.PatientId } into p
                                     select new
                                     {
                                         TotalPaidAmt = p.Sum(a => a.TotalAmount)
                                     }).FirstOrDefault();

                    var patCancelAmt = patCancel != null ? patCancel.TotalPaidAmt : 0;

                    //Part-7: get Total Cancelled Amount
                    //var patReturn = dbContext.BillingTransactionItems
                    //                .Where(b => b.PatientId == InputId && b.ReturnStatus == true) //&& (b.BillStatus == "paid" || b.BillStatus == "unpaid") && b.IsInsurance == false)
                    //                 .Sum(b => b.TotalAmount);

                    var patReturn = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems
                                     where bill.PatientId == patientId
                                     //  && bill.ReturnStatus == true
                                     //  && (bill.IsInsurance == false || bill.IsInsurance == null)
                                     group bill by new { bill.PatientId } into p
                                     select new
                                     {
                                         TotalPaidAmt = p.Sum(a => a.TotalAmount)
                                     }).FirstOrDefault();
                    var patReturnAmt = patReturn != null ? patReturn.TotalPaidAmt : 0;

                    //Part-8 get Subtotal amount 
                    var patSubtotal = (from bill in phrmdbcontext.PHRMInvoiceTransaction
                                       where bill.PatientId == patientId
                                       && bill.BilStatus == "unpaid"
                                       // && (bill.ReturnStatus == false || bill.ReturnStatus == null)
                                       //  && (bill.IsInsurance == false || bill.IsInsurance == null)
                                       select bill.SubTotal).FirstOrDefault();

                    var patSubtotalAmt = patSubtotal != null ? patSubtotal : 0;

                    //Part-9: Return a single object with Both Balances (Deposit and Credit).
                    var patBillHistory = new
                    {
                        PatientId = patientId,
                        PaidAmount = patPaidAmt,
                        DiscountAmount = patDiscountAmt,
                        CancelAmount = patCancelAmt,
                        ReturnedAmount = patReturnAmt,
                        CreditAmount = patCreditAmt,
                        ProvisionalAmt = patProvisionalAmt,
                        TotalDue = patCreditAmt + patProvisionalAmt,
                        DepositBalance = currentDepositBalance,
                        //  BalanceAmount = currentDepositBalance - (patCreditAmt + patProvisionalAmt)
                        SubtotalAmount = patSubtotalAmt
                    };


                    responseData.Results = patBillHistory;
                    responseData.Status = "OK";
                }
                else if (reqType != null && reqType == "provisionalItemsByPatientIdForSettle" && patientId != null && patientId != 0)
                {
                    PHRMPatient currPatient = phrmdbcontext.PHRMPatient.Where(pat => pat.PatientId == patientId).FirstOrDefault();
                    if (currPatient != null)
                    {
                        string subDivName = (from pat in phrmdbcontext.PHRMPatient
                                             join countrySubdiv in phrmdbcontext.CountrySubDivision
                                             on pat.CountrySubDivisionId equals countrySubdiv.CountrySubDivisionId
                                             where pat.PatientId == currPatient.PatientId
                                             select countrySubdiv.CountrySubDivisionName
                                          ).FirstOrDefault();

                        currPatient.CountrySubDivisionName = subDivName;
                        //remove relational property of patient//sud: 12May'18
                        currPatient.PHRMInvoiceTransactionItems = null;
                    }

                    //for this request type, patientid comes as inputid.
                    var patCreditItems = (from bill in phrmdbcontext.PHRMInvoiceTransactionItems//.Include("ServiceDepartment")
                                          where bill.BilItemStatus == "provisional" && bill.PatientId == patientId
                                          select bill).ToList<PHRMInvoiceTransactionItemsModel>().OrderBy(b => b.InvoiceId);

                    //clear patient object from Items, not needed since we're returning patient object separately
                    if (patCreditItems != null)
                    {

                        var allEmployees = (from emp in phrmdbcontext.Employees
                                            join dep in phrmdbcontext.Departments
                                            on emp.DepartmentId equals dep.DepartmentId into empDpt
                                            from emp2 in empDpt.DefaultIfEmpty()
                                            select new
                                            {
                                                EmployeeId = emp.EmployeeId,
                                                EmployeeName = emp.FirstName,
                                                DepartmentCode = emp2 != null ? emp2.DepartmentCode : "N/A",
                                                DepartmentName = emp2 != null ? emp2.DepartmentName : "N/A"
                                            }).ToList();

                        PharmacyFiscalYear fiscYear = PharmacyBL.GetFiscalYear(connString);

                    }

                    //create new anonymous type with patient information + Credit Items information : Anish:4May'18
                    var patCreditDetails = new
                    {
                        Patient = currPatient,
                        CreditItems = patCreditItems.OrderBy(itm => itm.CreatedOn).ToList()
                    };


                    responseData.Results = patCreditDetails;
                }
                #region GET: requisition item-wise list
                else if (reqType == "itemwiseRequistionList")
                {
                    var rItems = (from rItms in phrmdbcontext.StoreRequisitionItems
                                  where (rItms.RequisitionItemStatus == "active" || rItms.RequisitionItemStatus == "partial")
                                  group rItms by new
                                  {
                                      rItms.ItemId,
                                      rItms.Item.ItemName
                                  } into p
                                  select new
                                  {
                                      ItemId = p.Key.ItemId,
                                      ItemName = p.Key.ItemName,
                                      Quantity = p.Sum(a => (double)(a.Quantity) - a.ReceivedQuantity)
                                  }).ToList();
                    var stks = (from stk in phrmdbcontext.StoreStocks
                                group stk by new
                                {
                                    stk.ItemId
                                } into q
                                select new
                                {
                                    ItemId = q.Key.ItemId,
                                    AvailableQuantity = q.Sum(a => a.AvailableQuantity)
                                }).ToList();
                    var reqstkItems = (from r in rItems
                                       join stk in stks on r.ItemId equals stk.ItemId into stkTemp
                                       from s in stkTemp.DefaultIfEmpty()
                                       select new
                                       {
                                           ItemId = r.ItemId,
                                           ItemName = r.ItemName,
                                           Quantity = r.Quantity,
                                           AvailableQuantity = (s != null) ? s.AvailableQuantity : 0
                                       }).OrderBy(a => a.ItemName).ToList();
                    responseData.Results = reqstkItems;
                }
                #endregion
                #region //Get RequisitionItems by Requisition Id don't check any status this for View Purpose
                else if (reqType != null && reqType.ToLower() == "requisitionitemsforview")
                {
                    //this for get employee Name

                    var requistionDate = (from req in phrmdbcontext.StoreRequisition
                                          where req.RequisitionId == requisitionId
                                          select req.RequisitionDate).FirstOrDefault();

                    var requestedFromSourceStore = (from req in phrmdbcontext.StoreRequisition
                                                    join S in phrmdbcontext.PHRMStore on req.StoreId equals S.StoreId
                                                    where req.RequisitionId == requisitionId
                                                    select S.Name).FirstOrDefault();

                    var requisitionItems = (from reqItems in phrmdbcontext.StoreRequisitionItems
                                            join itm in phrmdbcontext.PHRMItemMaster on reqItems.ItemId equals itm.ItemId
                                            from Gen in phrmdbcontext.PHRMGenericModel.Where(g => g.GenericId == itm.GenericId).DefaultIfEmpty()
                                            where reqItems.RequisitionId == requisitionId
                                            select new
                                            {
                                                reqItems.ItemId,
                                                reqItems.RequisitionItemId,
                                                reqItems.PendingQuantity,
                                                reqItems.Quantity,
                                                reqItems.Remark,
                                                reqItems.ReceivedQuantity,
                                                reqItems.CreatedBy,
                                                CreatedOn = requistionDate,
                                                reqItems.RequisitionItemStatus,
                                                itm.ItemName,
                                                Gen.GenericName,
                                                reqItems.RequisitionId
                                            }
                                         ).ToList();
                    var employeeList = (from emp in phrmdbcontext.Employees select emp).ToList();

                    var requestDetails = (from reqItem in requisitionItems
                                          join emp in phrmdbcontext.Employees on reqItem.CreatedBy equals emp.EmployeeId
                                          join dispJoined in phrmdbcontext.StoreDispatchItems on reqItem.RequisitionItemId equals dispJoined.RequisitionItemId into dispTemp
                                          from disp in dispTemp.DefaultIfEmpty()
                                          select new
                                          {
                                              reqItem.ItemId,
                                              PendingQuantity = reqItem.PendingQuantity,
                                              reqItem.Quantity,
                                              reqItem.Remark,
                                              ReceivedQuantity = reqItem.ReceivedQuantity,
                                              reqItem.CreatedBy,
                                              CreatedByName = emp.FullName,
                                              reqItem.CreatedOn,
                                              reqItem.RequisitionItemStatus,
                                              reqItem.ItemName,
                                              reqItem.GenericName,
                                              reqItem.RequisitionId,
                                              ReceivedBy = disp == null ? "" : disp.ReceivedBy,
                                              DispatchedByName = disp == null ? "" : employeeList.Find(a => a.EmployeeId == disp.CreatedBy).FullName,
                                              RequestedSourceStore = requestedFromSourceStore
                                          }
                        ).ToList().GroupBy(a => a.ItemId).Select(g => new
                        {
                            ItemId = g.Key,
                            PendingQuantity = g.Select(a => a.PendingQuantity).FirstOrDefault(),
                            Quantity = g.Select(a => a.Quantity).FirstOrDefault(),
                            Remark = g.Select(a => a.Remark).FirstOrDefault(),
                            ReceivedQuantity = g.Select(a => a.ReceivedQuantity).FirstOrDefault(),
                            CreatedBy = g.Select(a => a.CreatedBy).FirstOrDefault(),
                            CreatedByName = g.Select(a => a.CreatedByName).FirstOrDefault(),
                            CreatedOn = g.Select(a => a.CreatedOn).FirstOrDefault(),
                            RequisitionItemStatus = g.Select(a => a.RequisitionItemStatus).FirstOrDefault(),
                            ItemName = g.Select(a => a.ItemName).FirstOrDefault(),
                            GenericName = g.Select(a => a.GenericName).FirstOrDefault(),
                            RequisitionId = g.Select(a => a.RequisitionId).FirstOrDefault(),
                            ReceivedBy = g.Select(a => a.ReceivedBy).FirstOrDefault(),
                            DispatchedByName = g.Select(a => a.DispatchedByName).FirstOrDefault(),
                            RequestedSourceStore = g.Select(a => a.RequestedSourceStore).FirstOrDefault()
                        }).ToList();
                    responseData.Status = "OK";
                    responseData.Results = requestDetails;
                }
                #endregion
                #region//Get All Requisition Items and Related Stock records(by ItemId of RequisitionItems table) 
                //for Dispatch Items to department against Requisition Id
                else if (reqType != null && reqType.ToLower() == "requisitionbyid")
                {
                    PHRMRequisitionStockVM requisitionStockVM = new PHRMRequisitionStockVM();
                    //Getting Requisition and Requisition Items by Requisition Id
                    //Which RequisitionStatus and requisitionItemsStatus is not 'complete','cancel' and 'initiated'
                    List<PHRMStoreRequisitionModel> requisitionDetails = (from requisition in phrmdbcontext.StoreRequisition
                                                                          where (requisition.RequisitionStatus == "partial" ||
                                                                          requisition.RequisitionStatus == "active") && requisition.RequisitionId == requisitionId
                                                                          select requisition)
                                                                .Include(rItems => rItems.RequisitionItems.Select(i => i.Item))
                                                                .ToList();


                    //This for remove complete, initiated and cancel Requisition Items from List
                    //added Decremental counter to avoid index-outofRange exception: since we're removing items from the list inside the loop of its own.
                    for (int i = requisitionDetails[0].RequisitionItems.Count - 1; i >= 0; i--)
                    {
                        if (requisitionDetails[0].RequisitionItems[i].RequisitionItemStatus == "complete"
                            || requisitionDetails[0].RequisitionItems[i].RequisitionItemStatus == "initiated"
                            || requisitionDetails[0].RequisitionItems[i].RequisitionItemStatus == "cancel")
                        { requisitionDetails[0].RequisitionItems.RemoveAt(i); }
                    }

                    //This for Get  Stock record with Matching ItemId of Requisition Item table
                    for (int j = 0; j < requisitionDetails[0].RequisitionItems.Count; j++)
                    {
                        var ItemId = requisitionDetails[0].RequisitionItems[j].ItemId;
                        List<PHRMStockTransactionModel> stockItems = new List<PHRMStockTransactionModel>();
                        stockItems = (from stock in phrmdbcontext.StockTransactions
                                      where (stock.ItemId == ItemId && stock.OutQty > 0)
                                      select stock
                                              ).OrderByDescending(s => s.ExpiryDate).ToList();
                        requisitionStockVM.stockTransactions.AddRange(stockItems);
                    }

                    requisitionStockVM.requisition = requisitionDetails[0];
                    responseData.Status = "OK";
                    responseData.Results = requisitionStockVM;
                }
                #endregion
                #region //Get dIspatch Details
                else if (reqType != null && reqType.ToLower() == "dispatchview")
                {

                    var requestDetails = (from DI in phrmdbcontext.StoreDispatchItems.Where(d => d.RequisitionId == requisitionId)
                                          join R in phrmdbcontext.StoreRequisition on DI.RequisitionId equals R.RequisitionId
                                          join CreatedBy in phrmdbcontext.Employees on R.CreatedBy equals CreatedBy.EmployeeId
                                          join DispatchedBy in phrmdbcontext.Employees on DI.CreatedBy equals DispatchedBy.EmployeeId
                                          join ReceivedBy in phrmdbcontext.Employees on DI.ReceivedById equals ReceivedBy.EmployeeId
                                          group new { DI, R, CreatedBy, DispatchedBy, ReceivedBy } by DI.DispatchId into D
                                          select new
                                          {
                                              DispatchId = D.Key,
                                              RequisitionId = D.FirstOrDefault().R.RequisitionId,
                                              RequisitionNo = D.FirstOrDefault().R.RequisitionNo,
                                              CreatedByName = D.FirstOrDefault().CreatedBy != null ? D.FirstOrDefault().CreatedBy.FullName : null,
                                              CreatedOn = D.FirstOrDefault().DI.DispatchedDate,
                                              DispatchedByName = D.FirstOrDefault().DispatchedBy != null ? D.FirstOrDefault().DispatchedBy.FullName : null,
                                              ReceivedBy = D.FirstOrDefault().ReceivedBy != null ? D.FirstOrDefault().ReceivedBy.FullName : null
                                          }).ToList();

                    responseData.Results = requestDetails;
                    responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
                }
                #endregion
                #region //Get dIspatch Details
                else if (reqType != null && reqType.ToLower() == "dispatchviewbydispatchid")
                {
                    var dispatchDetails = (from disp in phrmdbcontext.StoreDispatchItems
                                           where dispatchId == disp.DispatchId
                                           join item in phrmdbcontext.PHRMItemMaster on disp.ItemId equals item.ItemId
                                           /*join map in phrmdbcontext.PHRMRackItem on item.ItemId equals map.ItemId into items
                                           from itemdata in items.DefaultIfEmpty()
                                           join rack in phrmdbcontext.PHRMRack on itemdata.RackId equals rack.RackId into racks
                                           from rackdata in racks.DefaultIfEmpty()*/
                                           join generic in phrmdbcontext.PHRMGenericModel on item.GenericId equals generic.GenericId
                                           join reqitm in phrmdbcontext.StoreRequisitionItems on disp.RequisitionItemId equals reqitm.RequisitionItemId
                                           join emp in phrmdbcontext.Employees on disp.CreatedBy equals emp.EmployeeId
                                           join uom in phrmdbcontext.PHRMUnitOfMeasurement on item.UOMId equals uom.UOMId
                                           select new
                                           {
                                               disp.DispatchId,
                                               disp.ItemId,
                                               ToRackNo1 = (from rackItem in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == item.ItemId && ri.StoreId == disp.TargetStoreId)
                                                            join rack in phrmdbcontext.PHRMRack on rackItem.RackId equals rack.RackId
                                                            select rack.RackNo)
                                                         .FirstOrDefault(),
                                               FromRack1 = (from rackItem in phrmdbcontext.PHRMRackItem.Where(ri => ri.ItemId == item.ItemId && ri.StoreId == 1)
                                                            join rack in phrmdbcontext.PHRMRack on rackItem.RackId equals rack.RackId
                                                            select rack.RackNo)
                                                         .FirstOrDefault(),
                                               reqitm.RequisitionId,
                                               CreatedByName = emp.Salutation + ". " + emp.FirstName + " " + (string.IsNullOrEmpty(emp.MiddleName) ? "" : emp.MiddleName + " ") + emp.LastName,
                                               disp.CreatedOn,
                                               RequisitionDate = reqitm.CreatedOn,
                                               StandardRate = 0,
                                               item.ItemName,
                                               generic.GenericName,
                                               UOMName = uom.UOMName ?? "N/A",
                                               ItemCode = item.ItemCode ?? "N/A",
                                               disp.BatchNo,
                                               disp.ExpiryDate,
                                               disp.DispatchedQuantity,
                                               ReceivedBy = disp == null ? null : disp.ReceivedBy
                                           }
                        ).ToList();
                    responseData.Results = dispatchDetails;
                    responseData.Status = "OK";
                }
                #endregion
                else if (reqType == "provisional-return-list")
                {
                    var testdate = ToDate.AddDays(1);
                    var provCanceltxn = ENUM_PHRM_StockTransactionType.ProvisionalCancelItem;
                    var result = (
                                  from invItm in phrmdbcontext.PHRMInvoiceTransactionItems
                                  join stkTxn in phrmdbcontext.StockTransactions on invItm.InvoiceItemId equals stkTxn.ReferenceNo
                                  join pat in phrmdbcontext.PHRMPatient on invItm.PatientId equals pat.PatientId
                                  where invItm.InvoiceId == null && stkTxn.CreatedOn > FromDate && stkTxn.CreatedOn < testdate && stkTxn.TransactionType == provCanceltxn
                                  group new { invItm, stkTxn, pat } by new
                                  {
                                      invItm.PatientId,
                                      pat.PatientCode,
                                      pat.FirstName,
                                      pat.MiddleName,
                                      pat.LastName,
                                      pat.PhoneNumber,
                                      pat.Gender,
                                      pat.DateOfBirth,
                                      pat.Address,
                                      pat.PANNumber,
                                  } into t
                                  select new
                                  {
                                      PatientCode = t.Key.PatientCode,
                                      PatientId = t.Key.PatientId,
                                      ShortName = t.Key.FirstName + " " + (string.IsNullOrEmpty(t.Key.MiddleName) ? "" : t.Key.MiddleName + " ") + t.Key.LastName,
                                      Gender = t.Key.Gender,
                                      DateOfBirth = t.Key.DateOfBirth,
                                      ContactNo = t.Key.PhoneNumber,
                                      Address = t.Key.Address,
                                      PhoneNumber = t.Key.PhoneNumber,
                                      PANNumber = t.Key.PANNumber,
                                      LastCreditBillDate = t.Max(r => r.stkTxn.CreatedOn),
                                      //TotalCredit = t.Sum(r => (double)r.stkTxn.CostPrice * r.stkTxn.InQty)
                                      TotalCredit = t.Sum(r => (double)r.stkTxn.SalePrice * r.stkTxn.InQty)   //Rohit-:Need to multiply with the SalePrice.
                                  }).ToList().OrderByDescending(b => b.LastCreditBillDate);
                    responseData.Status = "OK";
                    responseData.Results = result;
                }
                else if (reqType == "provisional-return-duplicate-print")
                {
                    var provCanceltxn = ENUM_PHRM_StockTransactionType.ProvisionalCancelItem;
                    var result = (from invItm in phrmdbcontext.PHRMInvoiceTransactionItems
                                  join stkTxn in phrmdbcontext.StockTransactions on invItm.InvoiceItemId equals stkTxn.ReferenceNo
                                  join patient in phrmdbcontext.PHRMPatient on invItm.PatientId equals patient.PatientId
                                  where patient.PatientId == patientId && stkTxn.TransactionType == provCanceltxn

                                  select new
                                  {
                                      TotalAmount = (double)stkTxn.SalePrice * stkTxn.InQty,
                                      ItemName = invItm.ItemName,
                                      ReturnQty = stkTxn.InQty,
                                      CreatedOn = stkTxn.CreatedOn
                                  }).ToList().OrderByDescending(b => b.CreatedOn);
                    responseData.Status = "OK";
                    responseData.Results = result;
                }
                else if (reqType == "settlements-duplicate-prints")
                {
                    DataTable settlInfo = DALFunctions.GetDataTableFromStoredProc("SP_TXNS_PHRM_SettlementDuplicatePrint", phrmdbcontext);
                    responseData.Results = settlInfo;
                    responseData.Status = "OK";
                }
                else if (reqType == "get-settlements-duplicate-details")
                {
                    /* PHRMSettlementModel phrmsettlement = new PHRMSettlementModel();
                     try
                     {
                         PatientDbContext patDbContext = new PatientDbContext(connString);
                         phrmsettlement = phrmdbcontext.PHRMSettlements.Where(s => s.SettlementId == settlementId).FirstOrDefault();
                         var patient = patDbContext.Patients.Where(s => s.PatientId == phrmsettlement.PatientId).FirstOrDefault();
                         var items = phrmdbcontext.PHRMInvoiceTransaction.Where(inv => inv.SettlementId == phrmsettlement.SettlementId).ToList();
                         phrmsettlement.Patient = patient;
                         phrmsettlement.PHRMInvoiceTransactions = items;
                         responseData.Status = "OK";
                         responseData.Results = phrmsettlement;

                     }
                     catch (Exception ex)
                     {
                         responseData.Status = "Failed";
                         responseData.ErrorMessage = ex.ToString();
                     }
    */


                    List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@SettlementId", settlementId),
                    };

                    DataSet dsPHRMSettlementDetails = DALFunctions.GetDatasetFromStoredProc("SP_Get_PHRM_Settlement_Details_By_SettlementId", paramList, phrmdbcontext);
                    DataTable dtPatientInfo = dsPHRMSettlementDetails.Tables[0];
                    DataTable dtSettlementInfo = dsPHRMSettlementDetails.Tables[1];
                    DataTable dtSalesInfo = dsPHRMSettlementDetails.Tables[2];
                    DataTable dtSalesReturn = dsPHRMSettlementDetails.Tables[3];
                    DataTable dtCashDiscountReturn = dsPHRMSettlementDetails.Tables[4];
                    DataTable dtDepositInfo = dsPHRMSettlementDetails.Tables[5];



                    var settlementPreview = new
                    {
                        PatientInfo = Settlement_PatientInfoVM.MapDataTableToSingleObject(dtPatientInfo),
                        SettlementInfo = Settlement_Info_VM.MapDataTableToSingleObject(dtSettlementInfo),
                        SalesInfo = dtSalesInfo,
                        SalesReturn = dtSalesReturn,
                        CashDiscountReturn = dtCashDiscountReturn,
                        DepositInfo = dtDepositInfo
                    };

                    string billingUser = rbacDbContext.Users.Where(u => u.EmployeeId == settlementPreview.SettlementInfo.CreatedBy).Select(u => u.UserName).FirstOrDefault();
                    settlementPreview.SettlementInfo.BillingUser = billingUser;
                    responseData.Status = "OK";
                    responseData.Results = settlementPreview;
                }
                else if (reqType == "getGRDetailsToReturnByGoodReceiptId" && goodsReceiptId != 0)
                {
                    //var goodReceiptItemWithAvailableQuantity = (from gr in phrmdbcontext.PHRMGoodsReceipt
                    //                                            join gri in phrmdbcontext.PHRMGoodsReceiptItems on gr.GoodReceiptId equals gri.GoodReceiptId
                    //                                            join itmMst in phrmdbcontext.PHRMItemMaster on gri.ItemId equals itmMst.ItemId
                    //                                            join storeStock in phrmdbcontext.StoreStocks on gri.StoreStockId equals storeStock.StoreStockId
                    //                                            where gr.GoodReceiptId == goodsReceiptId && storeStock.AvailableQuantity > 0
                    //                                            select new
                    //                                            {
                    //                                                SupplierId = gr.SupplierId,
                    //                                                GoodReceiptId = gri.GoodReceiptId,
                    //                                                GoodReceiptItemId = gri.GoodReceiptItemId,
                    //                                                ItemName = itmMst.ItemName,
                    //                                                BatchNo = gri.BatchNo,
                    //                                                ReceivedQuantity = gri.ReceivedQuantity,
                    //                                                AvailableQuantity = storeStock.AvailableQuantity,
                    //                                                FreeQuantity = gri.FreeQuantity,
                    //                                                GRItemPrice = gri.GRItemPrice,
                    //                                                VATPercentage = gri.VATPercentage,
                    //                                                ItemId = gri.ItemId,
                    //                                                CCCharge = gri.CCCharge,
                    //                                                SalePrice = gri.SalePrice,
                    //                                                DiscountPercentage = gri.DiscountPercentage,
                    //                                                ExpiryDate = gri.ExpiryDate
                    //                                            }).AsNoTracking()
                    //                                              .ToList();
                    //responseData.Status = "OK";
                    //responseData.Results = goodReceiptItemWithAvailableQuantity;

                    var goodReceiptItemDetails = (from gr in phrmdbcontext.PHRMGoodsReceipt
                                                  join gri in phrmdbcontext.PHRMGoodsReceiptItems on gr.GoodReceiptId equals gri.GoodReceiptId
                                                  join itmMst in phrmdbcontext.PHRMItemMaster on gri.ItemId equals itmMst.ItemId
                                                  join storeStock in phrmdbcontext.StoreStocks on gri.StoreStockId equals storeStock.StoreStockId
                                                  where gr.GoodReceiptId == goodsReceiptId && storeStock.AvailableQuantity > 0
                                                  select new
                                                  {
                                                      SupplierId = gr.SupplierId,
                                                      GoodReceiptId = gri.GoodReceiptId,
                                                      GoodReceiptItemId = gri.GoodReceiptItemId,
                                                      StockId = gri.StockId,
                                                      ItemName = itmMst.ItemName,
                                                      BatchNo = gri.BatchNo,
                                                      ReceivedQuantity = gri.ReceivedQuantity,
                                                      AvailableQuantity = storeStock.AvailableQuantity,
                                                      FreeQuantity = gri.FreeQuantity,
                                                      GRItemPrice = gri.GRItemPrice,
                                                      ItemId = gri.ItemId,
                                                      SalePrice = gri.SalePrice,
                                                      ExpiryDate = gri.ExpiryDate
                                                  }).AsNoTracking().ToList();
                    responseData.Status = "OK";
                    responseData.Results = goodReceiptItemDetails;

                }

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();

            }

            return DanpheJSONConvert.SerializeObject(responseData, true);
        }
        [HttpGet("GetPatientList")]
        public IActionResult GetPatientList(string SearchText, bool IsInsurance)
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                PharmacyDbContext phrmDBContext = new PharmacyDbContext(connString);
                //CoreDbContext coreDbContext = new CoreDbContext(connString);
                SearchText = SearchText == null ? string.Empty : SearchText.ToLower();
                responseData.Results = PharmacyBL.SearchPatient(SearchText, IsInsurance, phrmDBContext);
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }
        [HttpGet]
        [Route("~/api/Pharmacy/getGoodReceiptHistory")]
        public async Task<IActionResult> GetGoodReceiptHistory()
        {
            var pharmacyDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var lastmonth = DateTime.Today.AddMonths(-1);

                var GRH = await (from gr in pharmacyDbContext.PHRMGoodsReceipt
                                 where gr.CreatedOn > lastmonth && gr.IsCancel == false
                                 select new
                                 {
                                     gr.GoodReceiptId,
                                     gr.SupplierId,
                                     gr.CreatedOn,
                                     gr.InvoiceNo,
                                     gr.SubTotal,
                                     items = pharmacyDbContext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptId == gr.GoodReceiptId).ToList()
                                 }).ToListAsync();

                responseData.Status = "Success";
                responseData.Results = GRH;
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = "Failed to obtain GR history.";
                return BadRequest(responseData);
            }
            return Ok(responseData);
        }

        [HttpGet]
        [Route("~/api/Pharmacy/GetInvoiceHeader/{Module}")]
        public IActionResult GetInvoieHeader([FromRoute] string Module)
        {

            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();

            try
            {
                var ILL = phrmDBContext.InvoiceHeader.Where(a => a.Module == Module).ToList();

                var location = (from dbc in phrmDBContext.CFGParameters
                                where dbc.ParameterGroupName.ToLower() == "common"
                                && dbc.ParameterName == "InvoiceHeaderLogoUploadLocation"
                                select dbc.ParameterValue).FirstOrDefault();

                var path = _environment.WebRootPath + location;

                if (ILL == null)
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "No Header Found !";
                    return NotFound(responseData);
                }
                else
                {
                    string fullPath;
                    foreach (var item in ILL)
                    {
                        fullPath = path + item.LogoFileName;

                        FileInfo fl = new FileInfo(fullPath);
                        if (fl.Exists)
                        {
                            item.FileBinaryData = System.IO.File.ReadAllBytes(@fullPath);
                        }

                    }
                }

                responseData.Results = ILL;
                responseData.Status = "OK";


            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
                throw;
            }
            return Ok(responseData);
        }

        [HttpGet("GetGRDetailsByGRId")]
        public IActionResult GetGRDetailsByGRId(int GoodsReceiptId, bool IsGRCancelled)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetGRDetailByGRIdViewModel goodsReceiptVM = phrmDBContext.GetGRDetailByGRId(GoodsReceiptId, IsGRCancelled);
                responseData.Status = "OK";
                responseData.Results = goodsReceiptVM;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
                throw;
            }
            return Ok(responseData);
        }
        [HttpGet]
        [Route("~/api/Pharmacy/GetPODetailsByPOID/{PurchaseOrderId}")]
        public IActionResult GetPODetailsByPOID([FromRoute] int PurchaseOrderId)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetPODetailByPOIdViewModel purchaseOrderVM = phrmDBContext.GetPODetailsByPOIdAsync(PurchaseOrderId);
                responseData.Status = "OK";
                responseData.Results = purchaseOrderVM;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + "exception details:" + ex.ToString();
                throw;
            }
            return Ok(responseData);
        }
        [HttpGet]
        [Route("~/api/Pharmacy/GetInvoiceReceiptByInvoiceId/{InvoiceId}")]
        public IActionResult GetInvoiceReceiptByInvoiceId([FromRoute] int InvoiceId)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            BillingDbContext billingDbContext = new BillingDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetInvoiceReceiptByInvoiceIdViewModel invoiceReceiptVM = phrmDBContext.GetInvoiceReceiptByInvoiceId(InvoiceId, billingDbContext);
                responseData.Status = "OK";
                responseData.Results = invoiceReceiptVM;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
                throw;
            }
            return Ok(responseData);
        }

        [HttpGet]
        [Route("GetMainStoreStock/{ShowAllStock}")]
        public IActionResult GetMainStoreStock(bool ShowAllStock)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                DataTable stockListVM = phrmDBContext.GetMainStoreStock(ShowAllStock);
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
                responseData.Results = stockListVM;
            }
            catch (Exception ex)
            {
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.Failed;
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
                //return BadRequest(responseData);
            }
            return Ok(responseData);
        }

        [HttpGet("GetMainStoreIncomingStock")]
        public IActionResult GetMainStoreIncomingStock(DateTime FromDate, DateTime ToDate)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetMainStoreIncomingStockViewModel incomingStockListVm = phrmDBContext.GetMainStoreIncomingStock(FromDate, ToDate);
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
                responseData.Results = incomingStockListVm;
            }
            catch (Exception ex)
            {
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.Failed;
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }


        [HttpGet("GetMainStoreIncomingStockById/{DispatchId}")]
        public IActionResult GetMainStoreIncomingStockById([FromRoute] int DispatchId)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetMainStoreIncomingStockByIdViewModel dispatchDetailVM = phrmDBContext.GetMainStoreIncomingStockById(DispatchId);
                responseData.Status = "OK";
                responseData.Results = dispatchDetailVM;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }
        [HttpGet("GetItemListForManualReturn")]
        public async Task<IActionResult> GetItemListForManualReturn()
        {
            var db = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var itemList = await db.PHRMItemMaster.Join(db.PHRMGenericModel, item => item.GenericId, generic => generic.GenericId, (item, generic) => new { item.ItemId, item.ItemName, generic.GenericName }).ToListAsync();
                responseData.Status = "OK";
                responseData.Results = itemList;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpGet("GetAvailableBatchesByItemId/{ItemId}")]
        public async Task<IActionResult> GetAvailableBatchesByItemId([FromRoute] int ItemId)
        {
            var db = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var availableBatches = await db.StoreStocks.Include(s => s.StockMaster).Where(stock => stock.ItemId == ItemId).Select(stock => new { stock.StockMaster.BatchNo, stock.StockMaster.ExpiryDate, stock.StockMaster.SalePrice }).ToListAsync();
                if (availableBatches.Count == 0) { throw new Exception("No stocks were found for this item."); }
                responseData.Status = "OK";
                responseData.Results = availableBatches;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpPut("ReceiveIncomingStock/{DispatchId}")]
        public async Task<IActionResult> ReceiveIncomingStock([FromRoute] int DispatchId, [FromBody] string ReceivingRemarks)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                responseData.Results = await phrmDBContext.ReceiveIncomingStockAsync(DispatchId, ReceivingRemarks, currentUser);
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpGet]
        [Route("~/api/Pharmacy/GetRequisitionDetailsForDispatch/{RequisitionId}")]
        public async Task<IActionResult> GetRequisitionDetailsForDispatch([FromRoute] int RequisitionId)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                GetRequisitionDetailsForDispatchViewModel stockListVM = await phrmDBContext.GetRequisitionDetailsForDispatch(RequisitionId);
                responseData.Status = "OK";
                responseData.Results = stockListVM;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpGet("GetPharmacySalePatient/{IsInsurance}")]
        public async Task<IActionResult> GetPharmacySalePatient([FromRoute] bool IsInsurance)
        {
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var phrmDbContext = new PharmacyDbContext(connString);
                var salesphrmpatient = await (from pat in phrmDbContext.PHRMPatient
                                              where pat.IsActive == true
                                              let claimCode = phrmDbContext.PHRMPatientVisit.Where(a => a.PatientId == pat.PatientId && a.Ins_HasInsurance == true).Max(a => a.ClaimCode)
                                              select new
                                              {
                                                  PatientId = pat.PatientId,
                                                  PatientCode = pat.PatientCode,
                                                  ShortName = pat.FirstName + " " + (string.IsNullOrEmpty(pat.MiddleName) ? "" : pat.MiddleName + " ") + pat.LastName,
                                                  IsOutdoorPatient = pat.IsOutdoorPat,
                                                  HasInsurance = pat.Ins_HasInsurance,
                                                  NSHINumber = pat.Ins_NshiNumber,
                                                  LatestClaimCode = IsInsurance ? claimCode : null,
                                                  RemainingBalance = IsInsurance ? pat.Ins_InsuranceBalance : 0
                                              }).Where(p => IsInsurance == false || (IsInsurance == true && p.HasInsurance == true && p.LatestClaimCode != null))
                                              .OrderByDescending(p => p.PatientId)
                                              .ToListAsync();
                responseData.Status = "OK";
                responseData.Results = salesphrmpatient;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }
        [HttpGet("GetDateFilteredGoodsReceiptList")]
        public async Task<IActionResult> GetDateFilteredGoodsReceiptList(DateTime FromDate, DateTime ToDate)
        {
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var realToDate = ToDate.AddDays(1);
                var phrmDbContext = new PharmacyDbContext(connString);
                var goodsReceiptList = await (from gr in phrmDbContext.PHRMGoodsReceipt
                                              join supp in phrmDbContext.PHRMSupplier on gr.SupplierId equals supp.SupplierId
                                              join fy in phrmDbContext.PharmacyFiscalYears on gr.FiscalYearId equals fy.FiscalYearId
                                              join rbac in phrmDbContext.Users on gr.CreatedBy equals rbac.EmployeeId
                                              orderby gr.CreatedOn descending
                                              select new
                                              {
                                                  GoodReceiptId = gr.GoodReceiptId,
                                                  GoodReceiptPrintId = gr.GoodReceiptPrintId,
                                                  PurchaseOrderId = gr.PurchaseOrderId,
                                                  InvoiceNo = gr.InvoiceNo,
                                                  GoodReceiptDate = gr.GoodReceiptDate,
                                                  SupplierBillDate = gr.SupplierBillDate,
                                                  CreatedOn = gr.CreatedOn,             //once GoodReceiptDate is been used replaced createdOn by GoodReceiptDate
                                                  SubTotal = gr.SubTotal,
                                                  DiscountAmount = gr.DiscountAmount,
                                                  VATAmount = gr.VATAmount,
                                                  TotalAmount = gr.TotalAmount,
                                                  Remarks = gr.Remarks,
                                                  SupplierName = supp.SupplierName,
                                                  ContactNo = supp.ContactNo,
                                                  City = supp.City,
                                                  Pin = supp.PANNumber,
                                                  ContactAddress = supp.ContactAddress,
                                                  Email = supp.Email,
                                                  IsCancel = gr.IsCancel,
                                                  SupplierId = supp.SupplierId,
                                                  UserName = rbac.UserName,
                                                  CurrentFiscalYear = fy.FiscalYearName,
                                                  IsTransferredToACC = (gr.IsTransferredToACC == null) ? false : true
                                              }).Where(s => s.GoodReceiptDate >= FromDate && s.GoodReceiptDate < realToDate).ToListAsync();
                responseData.Status = "OK";
                responseData.Results = goodsReceiptList;
            }
            catch (Exception ex)
            {

                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);

        }
        [HttpPost]
        [Route("~/api/Pharmacy/postInvoiceHeader")]
        public IActionResult PostInvoiceHeader()
        {
            var pharmacyDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            try
            {
                // Read Files From Clent Side 
                var f = this.ReadFiles();
                var file = f[0];

                // Read File details from form model
                var FD = Request.Form["fileDetails"];
                InvoiceHeaderModel fileDetails = DanpheJSONConvert.DeserializeObject<InvoiceHeaderModel>(FD);

                using (var dbContextTransaction = pharmacyDbContext.Database.BeginTransaction())
                {
                    try
                    {

                        fileDetails.CreatedBy = currentUser.EmployeeId;
                        fileDetails.CreatedOn = DateTime.Now;

                        pharmacyDbContext.InvoiceHeader.Add(fileDetails);
                        pharmacyDbContext.SaveChanges();

                        var location = (from dbc in pharmacyDbContext.CFGParameters
                                        where dbc.ParameterGroupName.ToLower() == "common"
                                        && dbc.ParameterName == "InvoiceHeaderLogoUploadLocation"
                                        select dbc.ParameterValue).FirstOrDefault();

                        var path = _environment.WebRootPath + location;

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        var fullPath = path + fileDetails.LogoFileName;


                        // Converting Files to Byte there for we require MemoryStream object
                        using (var ms = new MemoryStream())
                        {
                            // Copy Each file to MemoryStream
                            file.CopyTo(ms);

                            // Convert File to Byte[]
                            byte[] imageBytes = ms.ToArray();

                            FileInfo fi = new FileInfo(fullPath);
                            fi.Directory.Create(); // If the directory already exists, this method does nothing.
                            System.IO.File.WriteAllBytes(fi.FullName, imageBytes);
                            ms.Dispose();
                        }

                        // After File Added Commit the Transaction
                        dbContextTransaction.Commit();

                    }
                    catch (Exception ex)
                    {
                        dbContextTransaction.Rollback();
                        throw ex;
                    }
                }

                responseData.Results = null;
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = "Failed to Posting Invoice Header.";
                return BadRequest(responseData);
            }
            return Ok(responseData);
        }


        // POST api/values
        /*   #region:POST: Direct Dispatch
           [HttpPost]
           [Route("~/api/Pharmacy/PostDirectDispatch")]
           public async Task<IActionResult> PostDirectDispatch([FromBody] List<PHRMDispatchItemsModel> dispatchedItems)
           {
               var responseData = new DanpheHTTPResponse<object>();
               var phrmDbContext = new PharmacyDbContext(connString);
               var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
               try
               {
                   responseData.Results = await PharmacyBL.DirectDispatch(dispatchedItems, phrmDbContext, currentUser);
                   responseData.Status = "OK";
               }
               catch (Exception ex)
               {
                   responseData.Status = "Failed";
                   responseData.ErrorMessage = ex.ToString();

               }
               return Ok(responseData);
           }
           #endregion: POST: Direct Dispatch*/
        [HttpPost]
        public string Post()
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            string reqType = this.ReadQueryStringData("reqType");
            int requisitionId = Convert.ToInt32(this.ReadQueryStringData("requisitionId"));
            int StoreId = Convert.ToInt32(this.ReadQueryStringData("storeId"));

            NotiFicationDbContext notificationDbContext = new NotiFicationDbContext(connString);
            PharmacyDbContext phrmdbcontext = new PharmacyDbContext(connString);
            RbacDbContext rbacDbContext = new RbacDbContext(connString);
            RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            string str = this.ReadPostData();
            try
            {
                phrmdbcontext = this.AddAuditField(phrmdbcontext);
                var test = phrmdbcontext.PHRMBillTransactionItems;
                #region POST : setting-supplier manage
                if (reqType == "supplier")
                {
                    using (var dbTxn = phrmdbcontext.Database.BeginTransaction())
                    {
                        try
                        {
                            PHRMSupplierModel supplierData = DanpheJSONConvert.DeserializeObject<PHRMSupplierModel>(str);
                            if (phrmdbcontext.PHRMSupplier.Any(x => x.SupplierName == supplierData.SupplierName && x.PANNumber == supplierData.PANNumber))
                            //if (supplierData.SupplierName == supplierFromDB.Where(x => x.SupplierName == supplierData.SupplierName).Select(x => x.SupplierName).FirstOrDefault())
                            {
                                throw new InvalidOperationException($"Failed. Supplier: {supplierData.SupplierName} with PAN No: {supplierData.PANNumber} is already registered. ");
                            }
                            else
                            {

                                supplierData.CreatedOn = System.DateTime.Now;
                                phrmdbcontext.PHRMSupplier.Add(supplierData);
                                phrmdbcontext.SaveChanges();

                                if (supplierData.IsLedgerRequired == true)
                                {
                                    //Add Supplier Ledger;
                                    var newSupplierLedger = new PHRMSupplierLedgerModel()
                                    {
                                        SupplierId = supplierData.SupplierId,
                                        CreditAmount = 0,
                                        DebitAmount = 0,
                                        BalanceAmount = 0,
                                        IsActive = true,
                                        CreatedBy = currentUser.EmployeeId,
                                        CreatedOn = DateTime.Now
                                    };
                                    phrmdbcontext.SupplierLedger.Add(newSupplierLedger);
                                    phrmdbcontext.SaveChanges();
                                }

                                responseData.Results = supplierData;
                                responseData.Status = "OK";
                                dbTxn.Commit();
                            }

                        }
                        catch (Exception ex)
                        {

                            dbTxn.Rollback();
                            responseData.ErrorMessage = "Supplier details failed to Save. Exception Detail: " + ex.Message.ToString();
                            responseData.Status = "Failed";
                        }

                    }


                }
                #endregion
                #region POST : setting-company manage
                else if (reqType == "company")
                {
                    PHRMCompanyModel companyData = DanpheJSONConvert.DeserializeObject<PHRMCompanyModel>(str);
                    companyData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMCompany.Add(companyData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = companyData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST : setting-dispensary manage
                else if (reqType == "dispensary")
                {
                    PHRMStoreModel dispensaryData = DanpheJSONConvert.DeserializeObject<PHRMStoreModel>(str);
                    dispensaryData.CreatedOn = System.DateTime.Now;
                    dispensaryData.CreatedBy = currentUser.EmployeeId;
                    phrmdbcontext.PHRMStore.Add(dispensaryData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = dispensaryData;
                    responseData.Status = "OK";
                }
                #endregion

                #region POST : sales-category details
                else if (reqType == "postsalescategorydetail")
                {
                    PHRMStoreSalesCategoryModel salescategoryData = DanpheJSONConvert.DeserializeObject<PHRMStoreSalesCategoryModel>(str);
                    salescategoryData.CreatedOn = System.DateTime.Now;
                    salescategoryData.CreatedBy = currentUser.EmployeeId;
                    phrmdbcontext.PHRMStoreSalesCategory.Add(salescategoryData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = salescategoryData;
                    responseData.Status = "OK";
                }
                #endregion

                #region POST : setting-category manage
                else if (reqType == "category")
                {
                    PHRMCategoryModel categoryData = DanpheJSONConvert.DeserializeObject<PHRMCategoryModel>(str);
                    categoryData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMCategory.Add(categoryData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = categoryData;
                    responseData.Status = "OK";
                }

                #endregion
                #region POST : Send SMS
                else if (reqType == "sendsms")
                {

                    using (var client = new WebClient())
                    {
                        var values = new NameValueCollection();
                        values["from"] = "Demo";
                        values["token"] = "1eZClpxXFuZXd7PJ0xmv";
                        values["to"] = "9843284915";
                        values["text"] = "Hello Brother!!! This is sparrow";
                        var response = client.UploadValues("http://api.sparrowsms.com/v2/sms/", "Post", values);
                        var responseString = Encoding.Default.GetString(response);
                        return responseString;
                    }
                }

                #endregion
                #region POST : setting-item manage
                else if (reqType == "item")
                {
                    PHRMItemMasterModel itemData = DanpheJSONConvert.DeserializeObject<PHRMItemMasterModel>(str);
                    itemData.CreatedOn = System.DateTime.Now;
                    itemData.PHRM_MAP_MstItemsPriceCategories.ForEach(itm =>
                    {
                        itm.CreatedOn = System.DateTime.Now;
                        itm.CreatedBy = currentUser.EmployeeId;
                        itm.GenericId = itemData.GenericId;
                    });
                    phrmdbcontext.PHRMItemMaster.Add(itemData);
                    phrmdbcontext.SaveChanges();

                    NotificationViewModel notification = new NotificationViewModel();
                    notification.Notification_ModuleName = "Pharmacy_Module";
                    notification.Notification_Title = "New Medicine";
                    notification.Notification_Details = currentUser.UserName + " has added new item " + itemData.ItemName;
                    notification.RecipientId = rbacDbContext.Roles.Where(a => a.RoleName == "Pharmacy").Select(a => a.RoleId).FirstOrDefault();
                    notification.RecipientType = "rbac-role";
                    notification.ParentTableName = "PHRM_MST_Item";
                    notification.NotificationParentId = 0;
                    notification.IsArchived = false;
                    notification.IsRead = false;
                    notification.ReadBy = 0;
                    notification.CreatedOn = DateTime.Now;
                    notification.Sub_ModuleName = "Store Stock";
                    notificationDbContext.Notifications.Add(notification);
                    notificationDbContext.SaveChanges();



                    responseData.Results = itemData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST : setting-tax manage
                else if (reqType == "tax")
                {
                    PHRMTAXModel taxData = DanpheJSONConvert.DeserializeObject<PHRMTAXModel>(str);
                    taxData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMTAX.Add(taxData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = taxData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST : Generic Name manage
                else if (reqType == "genericName")
                {
                    PHRMGenericModel genericData = DanpheJSONConvert.DeserializeObject<PHRMGenericModel>(str);
                    genericData.CreatedOn = System.DateTime.Now;
                    int genId = phrmdbcontext.PHRMGenericModel.OrderByDescending(a => a.GenericId).First().GenericId;
                    genericData.GenericId = genId + 1;
                    genericData.CreatedBy = currentUser.EmployeeId;
                    phrmdbcontext.PHRMGenericModel.Add(genericData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = genericData;
                    responseData.Status = "OK";
                }
                #endregion

                #region Post:Drugs Request
                else if (reqType == "drug-requistion")
                {

                    PHRMDrugsRequistionModel drugReq = DanpheJSONConvert.DeserializeObject<PHRMDrugsRequistionModel>(str);
                    drugReq.CreatedOn = System.DateTime.Now;
                    drugReq.CreatedBy = currentUser.EmployeeId;
                    drugReq.Status = "pending";
                    phrmdbcontext.DrugRequistion.Add(drugReq);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = drugReq;
                    responseData.Status = "OK";
                }

                #endregion
                #region Post : drugs request Item from the nursing dept.
                else if (reqType == "post-provisional-item")
                {
                    List<PHRMDrugsRequistionItemsModel> provisionlaItem = DanpheJSONConvert.DeserializeObject<List<PHRMDrugsRequistionItemsModel>>(str);

                    if (provisionlaItem != null) //check client data is null or not
                    {
                        List<PHRMDrugsRequistionItemsModel> finalInvoiceData = PharmacyBL.ProvisionalItem(provisionlaItem, phrmdbcontext, currentUser);

                        if (finalInvoiceData != null)
                        {

                            for (int i = 0; i < finalInvoiceData.Count; i++)
                            {
                                PHRMDrugsRequistionModel drugReqData = new PHRMDrugsRequistionModel();

                                drugReqData.RequisitionId = finalInvoiceData[i].RequisitionId;
                                drugReqData.PatientId = finalInvoiceData[i].PatientId;
                                var requisitionItemId = finalInvoiceData[i].RequisitionItemId;
                                var newDrugReq = phrmdbcontext.DrugRequistion.Where(itm => itm.RequisitionId == drugReqData.RequisitionId).FirstOrDefault();

                                newDrugReq.Status = "Complete";
                                phrmdbcontext.DrugRequistion.Attach(newDrugReq);
                                phrmdbcontext.Entry(newDrugReq).State = EntityState.Modified;
                                phrmdbcontext.Entry(newDrugReq).Property(x => x.Status).IsModified = true;
                                phrmdbcontext.SaveChanges();


                            }
                            responseData.Status = "OK";
                            responseData.Results = finalInvoiceData;

                        }
                        else
                        {
                            responseData.ErrorMessage = "Nursing Drugs Details is null or failed to Save";
                            responseData.Status = "Failed";
                        }
                    }
                    else
                    {
                        responseData.ErrorMessage = "Nursing Drugs Details is null.";
                        responseData.Status = "Failed";
                    }

                }
                #endregion

                #region Post:Drugs Request Item
                else if (reqType == "drug-requistion-item")
                {
                    PHRMDrugsRequistionModel drugReq = DanpheJSONConvert.DeserializeObject<PHRMDrugsRequistionModel>(str);
                    drugReq.CreatedOn = System.DateTime.Now;
                    drugReq.CreatedBy = currentUser.EmployeeId;
                    phrmdbcontext.DrugRequistion.Add(drugReq);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = drugReq;
                    responseData.Status = "OK";


                }
                #endregion

                #region Post : ward request Item from the wardSupply dept.
                else if (reqType == "post-ward-requesition-item")
                {
                    WARDDispatchModel wardDispatchItems = DanpheJSONConvert.DeserializeObject<WARDDispatchModel>(str);


                    if (wardDispatchItems != null)//check client data is null or not
                    {
                        WARDDispatchModel finalInvoiceData = PharmacyBL.WardRequisitionItemsDispatch(wardDispatchItems, phrmdbcontext, currentUser, requisitionId);
                        if (finalInvoiceData != null)
                        {

                            responseData.Status = "OK";
                            responseData.Results = finalInvoiceData;
                        }
                        else
                        {
                            responseData.ErrorMessage = "Requistiion details are null or failed to Save";
                            responseData.Status = "Failed";
                        }
                    }
                    else
                    {
                        responseData.ErrorMessage = "Ward Requisition details is null.";
                        responseData.Status = "Failed";
                    }

                }
                #endregion
                //#region POST : NarcoticRecord Name manage
                //else if (reqType == "NarcoticName")
                //{
                //    //to insert records for narcotics drugs and their respective patient and doctor
                //    PHRMNarcoticRecord narcoticData = DaitemtypeListWithItemsnpheJSONConvert.DeserializeObject<PHRMNarcoticRecord>(str);
                //    narcoticData.CreatedOn = System.DateTime.Now;
                //    narcoticData.CreatedBy = currentUser.EmployeeId;
                //    phrmdbcontext.PHRMNarcoticRecord.Add(narcoticData);
                //    phrmdbcontext.SaveChanges();
                //    responseData.Results = narcoticData;
                //    responseData.Status = "OK";
                //}
                //#endregion
                //PatientitemtypeListWithItems
                //We register Outdoor patient information using this method in pharmacy
                #region POST: Patient Registration
                else if (reqType == "outdoorPatRegistration")
                {
                    PHRMPatient patientData = DanpheJSONConvert.DeserializeObject<PHRMPatient>(str);
                    patientData.CreatedOn = System.DateTime.Now;
                    patientData.CountrySubDivisionId = 76; //this is hardcoded because there is no provision to enter in countrysubdivision id 
                    patientData.CountryId = 1;//this is hardcoded because there is no provision to enter in country id
                    phrmdbcontext.PHRMPatient.Add(patientData);
                    phrmdbcontext.SaveChanges();
                    patientData.PatientCode = this.GetPatientCode(patientData.PatientId);
                    patientData.PatientNo = this.GetPatientNo(patientData.PatientId);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = patientData;
                    responseData.Status = "OK";
                }
                #endregion
                #region creating New Order (saving the PO and POitems)
                //else if (reqType != null && reqType == "PurchaseOrder")
                //{
                //    PHRMPurchaseOrderModel poFromClient = DanpheJSONConvert.DeserializeObject<PHRMPurchaseOrderModel>(str);
                //    if (poFromClient != null && poFromClient.PHRMPurchaseOrderItems != null && poFromClient.PHRMPurchaseOrderItems.Count > 0)
                //    {
                //        ////setting Flag for checking whole transaction 
                //        Boolean flag = false;
                //        flag = PharmacyBL.PostPOWithPOItems(poFromClient, phrmdbcontext);
                //        if (flag)
                //        {
                //            responseData.Status = "OK";
                //            responseData.Results = 1;
                //        }
                //        else
                //        {
                //            responseData.ErrorMessage = "PO and PO Items is null or failed to Save";
                //            responseData.Status = "Failed";
                //        }

                //    }

                //}
                #endregion

                #region ////POST New GOOD Receipt Request To Db with Entry in GR, GRItems, Stock, StockIn, StockItem 
                else if (reqType != null && reqType == "postGoodReceipt")
                {

                    PHRMGoodsReceiptViewModel grViewModelData = DanpheJSONConvert.DeserializeObject<PHRMGoodsReceiptViewModel>(str);

                    if (grViewModelData != null)
                    {
                        grViewModelData.goodReceipt.FiscalYearId = PharmacyBL.GetFiscalYearGoodsReceipt(phrmdbcontext, grViewModelData.goodReceipt.GoodReceiptDate).FiscalYearId;
                        grViewModelData.goodReceipt.GoodReceiptPrintId = PharmacyBL.GetGoodReceiptPrintNo(phrmdbcontext, grViewModelData.goodReceipt.FiscalYearId);

                        bool flag = false; //PharmacyBL.GoodReceiptTransaction(grViewModelData, phrmdbcontext, currentUser);
                        if (flag)
                        {
                            responseData.Status = "OK";
                            responseData.Results = grViewModelData.goodReceipt.GoodReceiptId;
                        }
                        else
                        {
                            responseData.ErrorMessage = "Goods Related Items is null or failed to Save";
                            responseData.Status = "Failed";
                        }
                    }

                }
                #endregion
                #region POST : setting-unitofmeasurement manage
                else if (reqType == "unitofmeasurement")
                {
                    PHRMUnitOfMeasurementModel uomData = DanpheJSONConvert.DeserializeObject<PHRMUnitOfMeasurementModel>(str);
                    uomData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMUnitOfMeasurement.Add(uomData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = uomData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST : setting-itemtype manage
                else if (reqType == "itemtype")
                {
                    PHRMItemTypeModel itemtypeData = DanpheJSONConvert.DeserializeObject<PHRMItemTypeModel>(str);
                    itemtypeData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMItemType.Add(itemtypeData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = itemtypeData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST : setting-packingtype manage
                else if (reqType == "packingtype")
                {
                    PHRMPackingTypeModel packingtypeData = DanpheJSONConvert.DeserializeObject<PHRMPackingTypeModel>(str);
                    packingtypeData.CreatedOn = System.DateTime.Now;
                    phrmdbcontext.PHRMPackingType.Add(packingtypeData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = packingtypeData;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST: Prescription
                //reqType == "postprescription" might be removed, not needed after integration with clinical-orders.
                else if (reqType == "postprescription")
                {
                    PHRMPrescriptionModel prescriptionData = DanpheJSONConvert.DeserializeObject<PHRMPrescriptionModel>(str);
                    phrmdbcontext.PHRMPrescription.Add(prescriptionData);
                    phrmdbcontext.SaveChanges();
                    responseData.Results = prescriptionData;
                    responseData.Status = "OK";
                }
                else if (reqType == "postprescriptionitem")
                {
                    List<PHRMPrescriptionItemModel> prescItems = DanpheJSONConvert.DeserializeObject<List<PHRMPrescriptionItemModel>>(str);
                    if (prescItems != null && prescItems.Count > 0)
                    {
                        foreach (var prItm in prescItems)
                        {
                            prItm.CreatedOn = System.DateTime.Now;
                            prItm.Quantity = prItm.Frequency.Value * prItm.HowManyDays.Value;
                            phrmdbcontext.PHRMPrescriptionItems.Add(prItm);

                        }

                    }

                    phrmdbcontext.SaveChanges();
                    responseData.Results = prescItems;
                    responseData.Status = "OK";
                }
                #endregion
                #region POST: Credit Invoice with Invoice details- single transaction
                //Save Invoice Item Items
                //This Post for InvoiceItems table,StockTransaction table, 
                //Update on GRItems, update stock
                //else if (reqType == "postProvisional")
                //{
                //    PHRMInvoiceTransactionModel invoiceDataFromClient = DanpheJSONConvert.DeserializeObject<PHRMInvoiceTransactionModel>(str);
                //    if (invoiceDataFromClient != null && invoiceDataFromClient?.InvoiceItems != null)//check client data is null or not
                //    {
                //        // this helps invoice id to get started from one then increment by one
                //        PHRMInvoiceTransactionModel finalInvoiceData = PharmacyBL.ProvisionalTransaction(invoiceDataFromClient, phrmdbcontext, currentUser, requisitionId);
                //        if (finalInvoiceData != null)
                //        {
                //            // 9th sep 2020:Ashish : replaced finalInvoiceData.InvoiceItems with ascending order list items.
                //            var itemList = finalInvoiceData.InvoiceItems.ToList();
                //            finalInvoiceData.InvoiceItems = itemList;
                //            responseData.Status = "OK";
                //            responseData.Results = finalInvoiceData;
                //        }
                //        else
                //        {
                //            responseData.ErrorMessage = "Invoice details is null or failed to Save";
                //            responseData.Status = "Failed";
                //        }
                //    }
                //    else
                //    {
                //        responseData.ErrorMessage = "Invoice details is null.";
                //        responseData.Status = "Failed";
                //    }
                //}

                else if (reqType == "addInvoiceForCrItems")
                {
                    MasterDbContext masterDbContext = new MasterDbContext(connString);
                    List<PHRMEmployeeCashTransaction> empCashTxns = new List<PHRMEmployeeCashTransaction>();
                    PHRMEmployeeCashTransaction empCashTxn = new PHRMEmployeeCashTransaction();

                    PHRMInvoiceTransactionModel invoiceObjFromClient = DanpheJSONConvert.DeserializeObject<PHRMInvoiceTransactionModel>(str);

                    if (invoiceObjFromClient == null || invoiceObjFromClient.InvoiceItems == null || invoiceObjFromClient.InvoiceItems.Count == 0)
                        throw new ArgumentException("No Items to update.");

                    using (var dbTxn = phrmdbcontext.Database.BeginTransaction())
                    {
                        try
                        {
                            var currFiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId;
                            var currentDate = DateTime.Now;
                            List<PHRMInvoiceTransactionItemsModel> invoiceItemsFromClient = invoiceObjFromClient.InvoiceItems;
                            invoiceObjFromClient.InvoiceItems = null;

                            //Make a Service later--Abhishek 5 Sep18
                            invoiceObjFromClient.IsOutdoorPat = false;
                            invoiceObjFromClient.PatientId = phrmdbcontext.PHRMInvoiceTransactionItems.Find(invoiceItemsFromClient[0].InvoiceItemId).PatientId;
                            invoiceObjFromClient.CreateOn = currentDate;
                            invoiceObjFromClient.CreatedBy = currentUser.EmployeeId;
                            //add fiscal year scope here.. MaxInvoiceNumber from CurrentFiscal Year..
                            invoiceObjFromClient.InvoicePrintId = PharmacyBL.GetInvoiceNumber(phrmdbcontext);
                            invoiceObjFromClient.FiscalYearId = currFiscalYearId;
                            invoiceObjFromClient.BilStatus = invoiceObjFromClient.PaymentMode == "credit" ? "unpaid" : "paid";
                            if (invoiceObjFromClient.PaymentMode == "credit")
                            {
                                invoiceObjFromClient.Creditdate = currentDate;
                                invoiceObjFromClient.ReceivedAmount = 0;
                            }
                            else
                            {
                                invoiceObjFromClient.Creditdate = null;
                                invoiceObjFromClient.ReceivedAmount = (decimal)invoiceObjFromClient.TotalAmount;
                            }
                            phrmdbcontext.PHRMInvoiceTransaction.Add(invoiceObjFromClient);
                            phrmdbcontext.SaveChanges();
                            PHRMInvoiceTransactionItemsModel itemFromServer = new PHRMInvoiceTransactionItemsModel();
                            foreach (PHRMInvoiceTransactionItemsModel itmFromClient in invoiceItemsFromClient)
                            {
                                itemFromServer = phrmdbcontext.PHRMInvoiceTransactionItems.Where(itm => itm.InvoiceItemId == itmFromClient.InvoiceItemId).FirstOrDefault();
                                if (itemFromServer != null)
                                {
                                    itemFromServer.InvoiceId = invoiceObjFromClient.InvoiceId;
                                    // sanjit/ramesh: 15July21 : quantity update logic is moved in Update Bill Mode, removed from Finalize Bill Mode
                                    // itemFromServer.Quantity = itmFromClient.Quantity;
                                    // itemFromServer.SubTotal = itmFromClient.SubTotal;
                                    // itemFromServer.TotalAmount = itmFromClient.TotalAmount;

                                    if (invoiceObjFromClient.PaymentMode == "cash")
                                    {
                                        itemFromServer.BilItemStatus = "paid";
                                    }
                                    else
                                    {
                                        itemFromServer.BilItemStatus = "unpaid";
                                    }
                                    //itemFromServer.BilItemStatus = "paid";
                                    //To update the Discount Percentage and totalDisAmount if changes comes from frontend
                                    itemFromServer.Quantity = itmFromClient.Quantity;
                                    itemFromServer.SubTotal = itmFromClient.SubTotal;
                                    itemFromServer.DiscountPercentage = itmFromClient.DiscountPercentage;
                                    itemFromServer.TotalDisAmt = itmFromClient.TotalDisAmt;
                                    itemFromServer.VATAmount = itmFromClient.VATAmount;
                                    itemFromServer.TotalAmount = itmFromClient.TotalAmount;
                                    phrmdbcontext.SaveChanges();


                                    //to update client side
                                    if (invoiceObjFromClient.PaymentMode == "cash")
                                    {
                                        itmFromClient.BilItemStatus = "paid";
                                    }
                                    else
                                    {
                                        itmFromClient.BilItemStatus = "unpaid";
                                    }
                                    itmFromClient.InvoiceId = itemFromServer.InvoiceId;

                                }
                                // perform the stock manipulation operation here
                                // perform validation check to avoid concurrent user issue or stale data issue
                                var provisionalSaleTxn = ENUM_PHRM_StockTransactionType.ProvisionalSaleItem;
                                var provisionalCancelTxn = ENUM_PHRM_StockTransactionType.ProvisionalCancelItem;
                                // find the total sold stock, substract with total returned stock
                                var allStockTxnsForThisInvoiceItem = phrmdbcontext.StockTransactions
                                                                                .Where(s => (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalSaleTxn)
                                                                                || (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalCancelTxn)).ToList();

                                // if-guard
                                // the provToSale Qty must be equal to the (outQty-inQty) of the stock transactions, otherwise, there might be an issue
                                var provToSaleQtyForThisInvoice = allStockTxnsForThisInvoiceItem.Sum(s => s.OutQty - s.InQty);
                                if (itmFromClient.Quantity != provToSaleQtyForThisInvoice)
                                    throw new InvalidOperationException($"Failed. Item: {itmFromClient.ItemName} with Batch: {itmFromClient.BatchNo} has quantity mismatch. ");
                                // Find the stock that was sold
                                var storeStockIdList = allStockTxnsForThisInvoiceItem.Select(s => s.StoreStockId).Distinct().ToList();
                                var soldByStoreId = allStockTxnsForThisInvoiceItem[0].StoreId;
                                var storeStockList = phrmdbcontext.StoreStocks.Include(s => s.StockMaster).Where(s => storeStockIdList.Contains((int)s.StoreStockId) && s.StoreId == soldByStoreId).ToList();

                                foreach (var storeStock in storeStockList)
                                {
                                    // add a provisional-to-sale stock transaction with quantity = (sold qty - previously returned qty)
                                    var provToSaleQty = allStockTxnsForThisInvoiceItem.Where(s => s.StoreStockId == storeStock.StoreStockId).Sum(s => s.OutQty - s.InQty);
                                    if (provToSaleQty == 0) // by pass the same stock with same StockId with same batch and Expiry and SalePrice ie (that item is prov sale and return already);
                                    {
                                        continue;
                                    }

                                    var provToSaleTxn = new PHRMStockTransactionModel(
                                                               storeStock: storeStock,
                                                               transactionType: ENUM_PHRM_StockTransactionType.ProvisionalToSale,
                                                               transactionDate: currentDate,
                                                               referenceNo: itemFromServer.InvoiceItemId,
                                                               createdBy: currentUser.EmployeeId,
                                                               createdOn: currentDate,
                                                               fiscalYearId: currFiscalYearId
                                                               );
                                    provToSaleTxn.SetInOutQuantity(inQty: provToSaleQty, outQty: 0);

                                    // add a sale-item stock transaction with quantity = (sold qty - previously returned qty)
                                    var SaleTxn = new PHRMStockTransactionModel(
                                                               storeStock: storeStock,
                                                               transactionType: ENUM_PHRM_StockTransactionType.SaleItem,
                                                               transactionDate: currentDate,
                                                               referenceNo: itemFromServer.InvoiceItemId,
                                                               createdBy: currentUser.EmployeeId,
                                                               createdOn: currentDate,
                                                               fiscalYearId: currFiscalYearId
                                                               );
                                    SaleTxn.SetInOutQuantity(inQty: 0, outQty: provToSaleQty);
                                    // add to db
                                    phrmdbcontext.StockTransactions.Add(provToSaleTxn);
                                    phrmdbcontext.StockTransactions.Add(SaleTxn);
                                }
                                phrmdbcontext.SaveChanges();
                            }
                            //To Add Employee Cash Transaction
                            if (invoiceObjFromClient.PaymentMode == "cash")
                            {
                                invoiceObjFromClient.PHRMEmployeeCashTransactions.ForEach(cashTxn =>
                                {
                                    cashTxn.ReferenceNo = invoiceObjFromClient.InvoiceId;
                                    cashTxn.PatientId = invoiceObjFromClient.PatientId;
                                    cashTxn.EmployeeId = currentUser.EmployeeId;
                                    cashTxn.CounterID = invoiceObjFromClient.CounterId;
                                    cashTxn.TransactionDate = currentDate;
                                    cashTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.CashSales;
                                    cashTxn.IsActive = true;
                                    empCashTxns.Add(cashTxn);
                                });
                            }

                            //To save deposit deduct details in both deposit table and EmployeeCashTransaction Table
                            if (invoiceObjFromClient.DepositDeductAmount != null && invoiceObjFromClient.DepositDeductAmount > 0)
                            {
                                //To save Deposit deduct detail on Deposit table.
                                PHRMDepositModel dep = new PHRMDepositModel();

                                dep.DepositType = "depositdeduct";
                                dep.Remark = "deposit used for transactionid:" + invoiceObjFromClient.InvoiceId;
                                dep.DepositAmount = invoiceObjFromClient.DepositAmount;
                                dep.DepositBalance = invoiceObjFromClient.DepositBalance;
                                dep.TransactionId = invoiceObjFromClient.InvoiceId;
                                dep.FiscalYear = invoiceObjFromClient.FiscalYear;
                                dep.CounterId = invoiceObjFromClient.CounterId;
                                dep.CreatedBy = invoiceObjFromClient.CreatedBy;
                                dep.CreatedOn = DateTime.Now;
                                dep.PatientId = invoiceObjFromClient.PatientId;
                                dep.StoreId = invoiceObjFromClient.StoreId;
                                phrmdbcontext.DepositModel.Add(dep);
                                phrmdbcontext.SaveChanges();

                                //To save Deposit deduct details on EmployeeCashTransaction Table
                                PaymentModes MstPaymentModes = masterDbContext.PaymentModes.Where(a => a.PaymentSubCategoryName.ToLower() == "deposit").FirstOrDefault();
                                empCashTxn.ReferenceNo = dep.DepositId;
                                empCashTxn.InAmount = 0;
                                empCashTxn.OutAmount = invoiceObjFromClient.DepositDeductAmount;
                                empCashTxn.PaymentModeSubCategoryId = MstPaymentModes.PaymentSubCategoryId;
                                empCashTxn.PatientId = invoiceObjFromClient.PatientId;
                                empCashTxn.EmployeeId = currentUser.EmployeeId;
                                empCashTxn.CounterID = invoiceObjFromClient.CounterId;
                                empCashTxn.TransactionDate = currentDate;
                                empCashTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.DepositDeduct;
                                empCashTxn.ModuleName = ENUM_ModuleNames.Dispensary;
                                empCashTxn.Remarks = "deduct from deposit";
                                empCashTxn.IsActive = true;
                                empCashTxns.Add(empCashTxn);
                            }
                            //To save Transaction details on EmployeeCashTransaction Table
                            phrmdbcontext.phrmEmployeeCashTransaction.AddRange(empCashTxns);
                            phrmdbcontext.SaveChanges();

                            invoiceObjFromClient.InvoiceItems = invoiceItemsFromClient;

                            if (realTimeRemoteSyncEnabled)
                            {
                                if (invoiceObjFromClient.IsRealtime == null)
                                {
                                    PHRMInvoiceTransactionModel invoiceSale = phrmdbcontext.PHRMInvoiceTransaction.Where(p => p.InvoiceId == invoiceObjFromClient.InvoiceId).FirstOrDefault();
                                    invoiceObjFromClient = invoiceSale;
                                }
                                if (invoiceObjFromClient.IsReturn == null)
                                {
                                    //Sud:24Dec'18--making parallel thread call (asynchronous) to post to IRD. so that it won't stop the normal execution of logic.
                                    Task.Run(() => PharmacyBL.SyncPHRMBillInvoiceToRemoteServer(invoiceObjFromClient, "phrm-invoice", phrmdbcontext));
                                }
                            }


                            responseData.Status = "OK";
                            responseData.Results = invoiceObjFromClient;
                            dbTxn.Commit();
                        }

                        catch (Exception ex)
                        {
                            dbTxn.Rollback();
                            responseData.ErrorMessage = "Invoice details is null or failed to Save. Exception Detail: " + ex.Message.ToString();
                            responseData.Status = "Failed";

                        }
                    }

                }
                #endregion
                else if (reqType == "updateInvoiceForCrItems")
                {
                    List<PHRMInvoiceTransactionItemsModel> invoiceObjFromClient = DanpheJSONConvert.DeserializeObject<List<PHRMInvoiceTransactionItemsModel>>(str);

                    // if invoiceObjFromClient is empty, then stop the process
                    if (invoiceObjFromClient == null || invoiceObjFromClient.Count() == 0)
                        throw new ArgumentException("No Items to update.");

                    var currFiscalYear = PharmacyBL.GetFiscalYear(phrmdbcontext);
                    var currentDate = DateTime.Now;

                    using (var dbTransaction = phrmdbcontext.Database.BeginTransaction())
                    {
                        try
                        {
                            List<PHRMInvoiceTransactionItemsModel> invoiceItemsFromClient = invoiceObjFromClient;
                            List<PHRMInvoiceTransactionItemsModel> itemsFromServer = new List<PHRMInvoiceTransactionItemsModel>();
                            // Retrieve all the invoice items in one request.
                            var invoiceItemIds = invoiceItemsFromClient.Select(i => i.InvoiceItemId).ToList();
                            itemsFromServer = phrmdbcontext.PHRMInvoiceTransactionItems.Where(i => invoiceItemIds.Contains(i.InvoiceItemId)).ToList();

                            foreach (PHRMInvoiceTransactionItemsModel itm in invoiceItemsFromClient)
                            {
                                var itemFromServer = itemsFromServer.FirstOrDefault(a => a.InvoiceItemId == itm.InvoiceItemId);
                                var returningQtyForThisItem = itemFromServer.Quantity - itm.Quantity;
                                // if stock is returned, then only go to this process.
                                if (returningQtyForThisItem > 0)
                                {
                                    if (itemFromServer != null)
                                    {
                                        itemFromServer.Quantity = itm.Quantity;
                                        itemFromServer.SubTotal = itemFromServer.SubTotal - itm.SubTotal;
                                        itemFromServer.TotalDisAmt = itemFromServer.TotalDisAmt - itm.TotalDisAmt;
                                        itemFromServer.TotalAmount = itemFromServer.TotalAmount - itm.TotalAmount;
                                        itemFromServer.VATAmount = itemFromServer.VATAmount - itm.VATAmount;
                                        itemFromServer.BilItemStatus = "provisional";
                                        phrmdbcontext.SaveChanges();
                                    }
                                    // perform the stock manipulation operation here
                                    // perform validation check to avoid concurrent user issue or stale data issue
                                    var provisionalSaleTxn = ENUM_PHRM_StockTransactionType.ProvisionalSaleItem;
                                    var provisionalCancelTxn = ENUM_PHRM_StockTransactionType.ProvisionalCancelItem;
                                    // find the total sold stock, substract with total returned stock
                                    var allStockTxnsForThisInvoiceItem = phrmdbcontext.StockTransactions
                                                                                    .Where(s => (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalSaleTxn)
                                                                                    || (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalCancelTxn)).ToList();
                                    // if no stock was returned previously, do not go further
                                    if (allStockTxnsForThisInvoiceItem.Count(i => i.TransactionType == provisionalCancelTxn) > 0)
                                    {
                                        double totalSoldQtyForThisItem = allStockTxnsForThisInvoiceItem.Where(a => a.TransactionType == provisionalSaleTxn).Sum(b => b.OutQty);
                                        double? totalReturnedQtyForThisItem = allStockTxnsForThisInvoiceItem.Where(a => a.TransactionType == provisionalCancelTxn).Sum(b => b.InQty);

                                        double totalReturnableQtyForThisItem = totalSoldQtyForThisItem - (totalReturnedQtyForThisItem ?? 0);

                                        //if total returnable quantity for the item is less than returned quantity from client side, throw exception
                                        if (totalReturnableQtyForThisItem < returningQtyForThisItem) throw new Exception($"{totalReturnableQtyForThisItem} qty is already returned for {itm.ItemName} with Batch : {itm.BatchNo} ");
                                    }
                                    //Find the stock that was sold
                                    var stockIdList = allStockTxnsForThisInvoiceItem.Select(s => s.StockId).Distinct().ToList();
                                    var soldByStoreId = allStockTxnsForThisInvoiceItem[0].StoreId;
                                    var stockList = phrmdbcontext.StoreStocks.Include(s => s.StockMaster).Where(s => stockIdList.Contains(s.StockId) && s.StoreId == soldByStoreId).ToList();

                                    //use fifo to return the items into dispensary stock
                                    //at first, total remaining returned quantity will be the total quantity returned from the client-side, later deducted with every iteration
                                    var remainingReturnedQuantity = returningQtyForThisItem;

                                    foreach (var stock in stockList)
                                    {
                                        double soldQuantityForThisStock = allStockTxnsForThisInvoiceItem.Where(s => s.StockId == stock.StockId).Sum(s => s.OutQty);
                                        double? previouslyReturnedQuantityForThisStock = allStockTxnsForThisInvoiceItem.Where(s => s.StockId == stock.StockId).Sum(s => s.InQty);
                                        double totalReturnableQtyForThisStock = soldQuantityForThisStock - (previouslyReturnedQuantityForThisStock ?? 0);
                                        //since we are modifying stock, we need to store the record in transaction
                                        PHRMStockTransactionModel newStockTxn = null;
                                        if (totalReturnableQtyForThisStock == 0)
                                        {
                                            continue;
                                        }
                                        if (totalReturnableQtyForThisStock < remainingReturnedQuantity)
                                        {
                                            //Check if the sold store and returning store are same
                                            if (stock.StoreId == itm.StoreId)
                                            {
                                                stock.UpdateAvailableQuantity(newQty: stock.AvailableQuantity + totalReturnableQtyForThisStock);

                                                // create new txn for this store
                                                newStockTxn = new PHRMStockTransactionModel(
                                                                    storeStock: stock,
                                                                    transactionType: ENUM_PHRM_StockTransactionType.ProvisionalCancelItem,
                                                                    transactionDate: currentDate,
                                                                    referenceNo: itemFromServer.InvoiceItemId,
                                                                    createdBy: currentUser.EmployeeId,
                                                                    createdOn: currentDate,
                                                                    fiscalYearId: currFiscalYear.FiscalYearId
                                                                    );
                                            }
                                            //If store is not same, then find the stock for the returning store
                                            else
                                            {
                                                var returningStoreStock = phrmdbcontext.StoreStocks.Include(a => a.StockMaster).FirstOrDefault(s => s.StockId == stock.StockId && s.StoreId == itm.StoreId);
                                                if (returningStoreStock != null)
                                                {
                                                    //If stock found, update the available quantity
                                                    returningStoreStock.UpdateAvailableQuantity(returningStoreStock.AvailableQuantity + totalReturnableQtyForThisStock);
                                                }
                                                else
                                                {
                                                    // If stock not found, create a new stock for this store
                                                    returningStoreStock = new PHRMStoreStockModel(
                                                        stockMaster: stock.StockMaster,
                                                        storeId: itm.StoreId,
                                                        quantity: totalReturnableQtyForThisStock,
                                                        costPrice: returningStoreStock.CostPrice,
                                                        salePrice: returningStoreStock.SalePrice
                                                        );

                                                    phrmdbcontext.StoreStocks.Add(returningStoreStock);
                                                    phrmdbcontext.SaveChanges();
                                                }
                                                // create new txn for this store
                                                newStockTxn = new PHRMStockTransactionModel(
                                                                    storeStock: returningStoreStock,
                                                                    transactionType: ENUM_PHRM_StockTransactionType.ProvisionalCancelItem,
                                                                    transactionDate: currentDate,
                                                                    referenceNo: itemFromServer.InvoiceItemId,
                                                                    createdBy: currentUser.EmployeeId,
                                                                    createdOn: currentDate,
                                                                    fiscalYearId: currFiscalYear.FiscalYearId
                                                                    );
                                            }
                                            newStockTxn.SetInOutQuantity(inQty: totalReturnableQtyForThisStock, outQty: 0);
                                            remainingReturnedQuantity -= totalReturnableQtyForThisStock;
                                        }
                                        else
                                        {
                                            //Check if the sold store and returning store are same
                                            if (stock.StoreId == itm.StoreId)
                                            {
                                                stock.UpdateAvailableQuantity(newQty: stock.AvailableQuantity + remainingReturnedQuantity);
                                                // create new txn for this store
                                                newStockTxn = new PHRMStockTransactionModel(
                                                                    storeStock: stock,
                                                                    transactionType: ENUM_PHRM_StockTransactionType.ProvisionalCancelItem,
                                                                    transactionDate: currentDate,
                                                                    referenceNo: itemFromServer.InvoiceItemId,
                                                                    createdBy: currentUser.EmployeeId,
                                                                    createdOn: currentDate,
                                                                    fiscalYearId: currFiscalYear.FiscalYearId
                                                                    );
                                            }
                                            //If store is not same, then find the stock for the returning store
                                            else
                                            {
                                                var returningStoreStock = phrmdbcontext.StoreStocks.Include(a => a.StockMaster).FirstOrDefault(s => s.StoreStockId == stock.StoreStockId && s.StoreId == itm.StoreId);
                                                if (returningStoreStock != null)
                                                {
                                                    //If stock found, update the available quantity
                                                    returningStoreStock.UpdateAvailableQuantity(newQty: returningStoreStock.AvailableQuantity + (remainingReturnedQuantity));
                                                }
                                                else
                                                {
                                                    // If stock not found, create a new stock for this store
                                                    returningStoreStock = new PHRMStoreStockModel(
                                                        stockMaster: stock.StockMaster,
                                                        storeId: itm.StoreId,
                                                        quantity: remainingReturnedQuantity,
                                                        costPrice: stock.CostPrice,
                                                        salePrice: stock.SalePrice
                                                        );
                                                    phrmdbcontext.StoreStocks.Add(returningStoreStock);
                                                    phrmdbcontext.SaveChanges();
                                                }
                                                // create new txn for this store
                                                newStockTxn = new PHRMStockTransactionModel(
                                                                    storeStock: returningStoreStock,
                                                                    transactionType: ENUM_PHRM_StockTransactionType.ProvisionalCancelItem,
                                                                    transactionDate: currentDate,
                                                                    referenceNo: itemFromServer.InvoiceItemId,
                                                                    createdBy: currentUser.EmployeeId,
                                                                    createdOn: currentDate,
                                                                    fiscalYearId: currFiscalYear.FiscalYearId
                                                                    );
                                            }
                                            newStockTxn.SetInOutQuantity(inQty: remainingReturnedQuantity, outQty: 0);
                                            remainingReturnedQuantity = 0;
                                        }
                                        //add txn to dispensary stock txn and then check if fifo is completed.
                                        phrmdbcontext.StockTransactions.Add(newStockTxn);
                                        phrmdbcontext.SaveChanges();

                                        if (remainingReturnedQuantity == 0)
                                        {
                                            break;
                                        }
                                    }
                                    phrmdbcontext.SaveChanges();
                                }
                            }

                            dbTransaction.Commit();
                            responseData.Status = "OK";
                            responseData.Results = invoiceObjFromClient;
                        }
                        catch (Exception ex)
                        {
                            dbTransaction.Rollback();
                            responseData.ErrorMessage = "Invoice details is null or failed to Save. Exception Details:" + ex.Message.ToString();
                            responseData.Status = "Failed";
                        }
                    }

                }
                else if (reqType == "post-credit-organizations")
                {
                    PHRMCreditOrganizationsModel org = DanpheJSONConvert.DeserializeObject<PHRMCreditOrganizationsModel>(str);
                    org.CreatedOn = DateTime.Now;
                    org.CreatedBy = currentUser.EmployeeId;
                    phrmdbcontext.CreditOrganizations.Add(org);

                    phrmdbcontext.SaveChanges();
                    responseData.Results = org;
                    responseData.Status = "OK";
                }
                else if (reqType == "cancelCreditItems")
                {
                    List<PHRMInvoiceTransactionItemsModel> invoiceObjFromClient = DanpheJSONConvert.DeserializeObject<List<PHRMInvoiceTransactionItemsModel>>(str);
                    // if invoiceObjFromClient is empty, then stop the process
                    if (invoiceObjFromClient == null || invoiceObjFromClient.Count() == 0)
                        throw new ArgumentException("No Items to update.");

                    using (var dbTxn = phrmdbcontext.Database.BeginTransaction())
                    {
                        try
                        {
                            var currentDate = DateTime.Now;
                            var currFiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId;
                            PHRMInvoiceTransactionItemsModel itemFromServer = new PHRMInvoiceTransactionItemsModel();
                            foreach (PHRMInvoiceTransactionItemsModel itmFromClient in invoiceObjFromClient)
                            {

                                itemFromServer = phrmdbcontext.PHRMInvoiceTransactionItems
                                                                          .Where(itm => itm.InvoiceItemId == itmFromClient.InvoiceItemId).FirstOrDefault();
                                if (itemFromServer != null)
                                {
                                    phrmdbcontext.PHRMInvoiceTransactionItems.Attach(itemFromServer);

                                    itemFromServer.Quantity = itmFromClient.Quantity;
                                    itemFromServer.SubTotal = itmFromClient.SubTotal;
                                    itemFromServer.TotalAmount = itmFromClient.TotalAmount;
                                    itemFromServer.BilItemStatus = "provisionalcancel";

                                    phrmdbcontext.Entry(itemFromServer).State = EntityState.Modified;
                                    phrmdbcontext.Entry(itemFromServer).Property(x => x.BilItemStatus).IsModified = true;
                                    phrmdbcontext.Entry(itemFromServer).Property(x => x.Quantity).IsModified = true;
                                    //to update client side
                                    itmFromClient.BilItemStatus = "provisionalcancel";
                                    itmFromClient.InvoiceId = itemFromServer.InvoiceId;
                                    phrmdbcontext.SaveChanges();
                                }

                                // perform the stock manipulation operation here
                                // perform validation check to avoid concurrent user issue or stale data issue
                                var provisionalSaleTxn = ENUM_PHRM_StockTransactionType.ProvisionalSaleItem;
                                var provisionalCancelTxn = ENUM_PHRM_StockTransactionType.ProvisionalCancelItem;
                                // find the total sold stock, substract with total returned stock
                                var allStockTxnsForThisInvoiceItem = phrmdbcontext.StockTransactions
                                                                                .Where(s => (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalSaleTxn)
                                                                                || (s.ReferenceNo == itemFromServer.InvoiceItemId && s.TransactionType == provisionalCancelTxn)).ToList();

                                // if-guard
                                // the provToSale Qty must be equal to the (outQty-inQty) of the stock transactions, otherwise, there might be an issue
                                var cancellableQtyForThisItem = allStockTxnsForThisInvoiceItem.Sum(s => s.OutQty - s.InQty);
                                if (itmFromClient.Quantity != cancellableQtyForThisItem)
                                    throw new InvalidOperationException($"Failed. Item: {itmFromClient.ItemName} with Batch: {itmFromClient.BatchNo} has quantity mismatch. ");
                                // Find the stock that was sold
                                var stockIdList = allStockTxnsForThisInvoiceItem.Select(s => s.StockId).Distinct().ToList();
                                var soldByStoreId = allStockTxnsForThisInvoiceItem[0].StoreId;
                                var stockList = phrmdbcontext.StoreStocks.Include(s => s.StockMaster).Where(s => stockIdList.Contains(s.StockId) && s.StoreId == soldByStoreId).ToList();

                                foreach (var stock in stockList)
                                {
                                    // add a cancel stock transaction with quantity = (sold qty - previously returned qty)
                                    var cancellableQty = allStockTxnsForThisInvoiceItem.Where(s => s.StockId == stock.StockId).Sum(s => s.OutQty - s.InQty);

                                    var cancelStockTxn = new PHRMStockTransactionModel(
                                                               storeStock: stock,
                                                               transactionType: ENUM_PHRM_StockTransactionType.ProvisionalCancelItem,
                                                               transactionDate: currentDate,
                                                               referenceNo: itemFromServer.InvoiceItemId,
                                                               createdBy: currentUser.EmployeeId,
                                                               createdOn: currentDate,
                                                               fiscalYearId: currFiscalYearId
                                                               );
                                    cancelStockTxn.SetInOutQuantity(inQty: cancellableQty, outQty: 0);
                                    stock.UpdateAvailableQuantity(newQty: stock.AvailableQuantity + cancellableQty);
                                    // add to db
                                    phrmdbcontext.StockTransactions.Add(cancelStockTxn);
                                }
                                phrmdbcontext.SaveChanges();
                            }
                            responseData.Status = "OK";
                            responseData.Results = invoiceObjFromClient;
                            dbTxn.Commit();
                        }

                        catch (Exception ex)
                        {
                            dbTxn.Rollback();
                            responseData.ErrorMessage = "Invoice details is null or failed to Save. Exception Details: " + ex.Message.ToString();
                            responseData.Status = "Failed";
                        }
                    }

                }


                #region post Return Items to Supplier or Vendor
                else if (reqType != null && reqType == "postReturnToSupplierItems")
                {

                    PHRMReturnToSupplierModel retSupplModel = DanpheJSONConvert.DeserializeObject<PHRMReturnToSupplierModel>(str);
                    if (retSupplModel != null && retSupplModel.ReturnToSupplierItems != null)
                    {
                        var maxretSupp = (from RTS in phrmdbcontext.PHRMReturnToSupplier select RTS.CreditNotePrintId).DefaultIfEmpty(0).Max();
                        retSupplModel.CreditNotePrintId = maxretSupp + 1;

                        int outputRTSId = PharmacyBL.ReturnItemsToSupplierTransaction(retSupplModel, currentUser, phrmdbcontext);

                        responseData.Results = outputRTSId;
                        responseData.Status = "OK";

                    }
                    else
                    {
                        responseData.Status = "Failed";
                        responseData.ErrorMessage = "Return to Supplier Items is null or failed to Save";
                    }

                }
                #endregion
                #region Save WriteOff and WriteOffItems
                else if (reqType != null && reqType == "postWriteOffItems")
                {
                    PHRMWriteOffModel writeOffModel = DanpheJSONConvert.DeserializeObject<PHRMWriteOffModel>(str);
                    try
                    {
                        if (writeOffModel == null) throw new Exception("Write Off Model cannot be null");
                        if (writeOffModel.phrmWriteOffItem == null) throw new Exception("No items to write-off");

                        responseData.Results = PharmacyBL.WriteOffItemTransaction(writeOffModel, phrmdbcontext, currentUser);
                        responseData.Status = "OK";
                    }
                    catch (Exception ex)
                    {
                        responseData.ErrorMessage = "Write Off Items is null or failed to Save. Exception Detail: " + ex.Message.ToString();
                        responseData.Status = "Failed";
                    }
                }
                #endregion
                #region POST : update stockManage transaction, Post to StockManage table and post to stockTxnItem table                 
                else if (reqType == "manage-stock-detail")
                {
                    PHRMStockManageModel stockManageData = DanpheJSONConvert.DeserializeObject<PHRMStockManageModel>(str);
                    if (stockManageData != null)
                    {
                        Boolean flag = false;
                        flag = PharmacyBL.StockManageTransaction(stockManageData, phrmdbcontext, currentUser);
                        if (flag)
                        {
                            responseData.Status = "OK";
                            responseData.Results = 1;
                        }
                        else
                        {
                            responseData.ErrorMessage = "Write Off Items is null or failed to Save";
                            responseData.Status = "Failed";
                        }
                    }
                }
                #endregion
                #region POST : update storeManage transaction, Post to StoreStock table           
                else if (reqType == "manage-store-detail")
                {
                    ManageStockItem manageStock = DanpheJSONConvert.DeserializeObject<ManageStockItem>(str);
                    if (manageStock != null)
                    {
                        Boolean flag = false;
                        flag = PharmacyBL.StoreManageTransaction(manageStock, phrmdbcontext, currentUser);
                        if (flag)
                        {
                            responseData.Status = "OK";
                            responseData.Results = 1;
                        }
                        else
                        {
                            responseData.ErrorMessage = "Write Off Items is null or failed to Save";
                            responseData.Status = "Failed";
                        }
                    }
                }
                #endregion
                #region POST: transfer to Dispensary, update Store Stock
                else if (reqType == "transfer-to-dispensary")
                {
                    PHRMStockTransactionModel storeStockData = DanpheJSONConvert.DeserializeObject<PHRMStockTransactionModel>(str);
                    if (storeStockData != null)
                    {
                        using (var dbContextTransaction = phrmdbcontext.Database.BeginTransaction())
                        {
                            try
                            {
                                Boolean flag = false;
                                flag = PharmacyBL.TransferStoreStockToDispensary(storeStockData, phrmdbcontext, currentUser);
                                if (flag)
                                {
                                    dbContextTransaction.Commit();//Commit Transaction

                                    responseData.Status = "OK";
                                    responseData.Results = 1;
                                }
                                else
                                {
                                    dbContextTransaction.Rollback();//Rollback transaction

                                    responseData.ErrorMessage = "Transfer failed";
                                    responseData.Status = "Failed";
                                }
                            }
                            catch (Exception ex)
                            {
                                dbContextTransaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }
                #endregion

                #region POST: transfer to Store, update Dispensary Stock
                else if (reqType == "transfer-to-store")
                {
                    //PHRMDispensaryStockTransactionModel dispensaryStockData = DanpheJSONConvert.DeserializeObject<PHRMDispensaryStockTransactionModel>(str);
                    //if (dispensaryStockData != null)
                    //{
                    //    Boolean flag = false;
                    //    flag = PharmacyBL.TransferDispensaryStockToStore(dispensaryStockData, StoreId, phrmdbcontext, currentUser);
                    //    if (flag)
                    //    {
                    //        responseData.Status = "OK";
                    //        responseData.Results = 1;
                    //    }
                    //    else
                    //    {
                    //        responseData.ErrorMessage = "Transfer failed";
                    //        responseData.Status = "Failed";
                    //    }
                    //}
                }
                #endregion
                #region cancel goods receipt and post to stock transaction items table
                else if (reqType == "cancel-goods-receipt")
                {

                    string goodReceiptIdStr = this.ReadQueryStringData("goodsReceiptId");
                    string cancelRemarks = this.ReadQueryStringData("CancelRemarks");
                    int goodReceiptId;
                    if (int.TryParse(goodReceiptIdStr, out goodReceiptId))
                    {
                        try
                        {
                            PharmacyBL.CancelGoodsReceipt(phrmdbcontext, goodReceiptId, currentUser, cancelRemarks);
                            responseData.Status = "OK";
                            responseData.Results = goodReceiptId;
                        }
                        catch (Exception ex)
                        {
                            responseData.Status = "Failed";
                            responseData.ErrorMessage = "Goods Receipt Cancelation Failed!! Exception Detail : " + ex.Message.ToString();
                        }
                    }
                    else
                    {
                        responseData.ErrorMessage = "Goods Receipt Cancelation Failed!! GoodsReceiptId is Invalid.";
                        responseData.Status = "Failed";
                    }
                }
                #endregion
                #region post deposit data
                else if (reqType == "depositData")
                {
                    MasterDbContext masterDbContext = new MasterDbContext(connString);
                    PHRMDepositModel deposit = DanpheJSONConvert.DeserializeObject<PHRMDepositModel>(str);

                    List<PHRMEmployeeCashTransaction> empTxns = new List<PHRMEmployeeCashTransaction>();
                    PHRMEmployeeCashTransaction empTxn = new PHRMEmployeeCashTransaction();
                    deposit.DepositId = 0;
                    deposit.CreatedOn = DateTime.Now;
                    deposit.CreatedBy = currentUser.EmployeeId;
                    PharmacyFiscalYear fiscYear = PharmacyBL.GetFiscalYear(connString);
                    deposit.FiscalYearId = fiscYear.FiscalYearId;
                    if (deposit.DepositType != "depositdeduct")
                        deposit.ReceiptNo = PharmacyBL.GetDepositReceiptNo(connString);
                    deposit.FiscalYear = fiscYear.FiscalYearName;
                    EmployeeModel currentEmp = phrmdbcontext.Employees.Where(emp => emp.EmployeeId == currentUser.EmployeeId).FirstOrDefault();
                    deposit.PhrmUser = currentEmp.FirstName + " " + currentEmp.LastName;
                    phrmdbcontext.DepositModel.Add(deposit);
                    phrmdbcontext.SaveChanges();

                    var depositTxn = deposit.PHRMEmployeeCashTransactions;

                    //Save deposit details in Pharmacy Employee Cash Transaction table
                    PaymentModes MstPaymentModes = masterDbContext.PaymentModes.Where(a => a.PaymentSubCategoryName.ToLower() == "cash").FirstOrDefault();      //Get PaymentMode Sub Category Id
                    if (deposit.DepositType != "depositreturn")
                    {
                        deposit.PHRMEmployeeCashTransactions.ForEach(empTxnDetails =>
                        {
                            empTxnDetails.TransactionType = ENUM_PHRM_EmpCashTxnTypes.DepositAdd;
                            empTxnDetails.PatientId = (int)deposit.PatientId;
                            empTxnDetails.ReferenceNo = deposit.DepositId;
                            empTxnDetails.Remarks = "deposit added";
                            empTxnDetails.TransactionDate = DateTime.Now;
                            empTxnDetails.CounterID = (int)deposit.CounterId;
                            empTxnDetails.EmployeeId = currentUser.EmployeeId;
                            empTxnDetails.IsActive = true;
                            empTxns.Add(empTxnDetails);
                        });
                        phrmdbcontext.phrmEmployeeCashTransaction.AddRange(empTxns);
                    }
                    else
                    {   //To save Deposit Deduct details in Pharmacy Employee Cash Transaction table
                        empTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.ReturnDeposit;
                        empTxn.PaymentModeSubCategoryId = MstPaymentModes.PaymentSubCategoryId;
                        empTxn.OutAmount = (int)deposit.DepositAmount;
                        empTxn.PatientId = (int)deposit.PatientId;
                        empTxn.ReferenceNo = deposit.DepositId;
                        empTxn.Remarks = "deposit return";
                        empTxn.TransactionDate = DateTime.Now;
                        empTxn.CounterID = (int)deposit.CounterId;
                        empTxn.EmployeeId = currentUser.EmployeeId;
                        empTxn.IsActive = true;
                        empTxn.ModuleName = ENUM_ModuleNames.Dispensary;
                        phrmdbcontext.phrmEmployeeCashTransaction.Add(empTxn);
                    }
                    phrmdbcontext.SaveChanges();

                    responseData.Status = "OK";
                    responseData.Results = deposit;
                }
                #endregion

                else if (reqType == "postSettlementInvoice")
                {
                    PHRMSettlementModel phrmsettlement = DanpheJSONConvert.DeserializeObject<PHRMSettlementModel>(str);
                    // RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                    MasterDbContext masterDbContext = new MasterDbContext(connString);

                    using (var dbTransaction = phrmdbcontext.Database.BeginTransaction())
                    {
                        try
                        {
                            var txns = phrmsettlement.PHRMInvoiceTransactions;
                            List<PHRMInvoiceTransactionModel> newTxnList = new List<PHRMInvoiceTransactionModel>();
                            foreach (PHRMInvoiceTransactionModel txn in txns)
                            {
                                /*PHRMInvoiceTransactionModel newTxn = PHRMInvoiceTransactionModel.GetCloneWithItems(txn);
                                newTxnList.Add(newTxn);*/

                                PHRMInvoiceTransactionModel newTxn = phrmdbcontext.PHRMInvoiceTransaction
                                   .Where(b => b.InvoiceId == txn.InvoiceId).FirstOrDefault();
                                newTxnList.Add(newTxn);

                            }
                            phrmsettlement.PHRMInvoiceTransactions = null;
                            phrmsettlement.SettlementReceiptNo = GetSettlementReceiptNo(phrmdbcontext);
                            phrmsettlement.CreatedOn = System.DateTime.Now;
                            phrmsettlement.SettlementDate = System.DateTime.Now;
                            phrmsettlement.FiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId;
                            phrmsettlement.CreatedBy = currentUser.EmployeeId;

                            phrmdbcontext.PHRMSettlements.Add(phrmsettlement);
                            phrmdbcontext.SaveChanges();

                            // Update necessary fields of PharmacyTrnsaction acc to above Settlement Object
                            if (newTxnList != null && newTxnList.Count > 0 || phrmsettlement.RefundableAmount > 0)
                            {
                                foreach (var txn in newTxnList)
                                {
                                    phrmdbcontext.PHRMInvoiceTransaction.Attach(txn);
                                    txn.SettlementId = phrmsettlement.SettlementId;
                                    txn.BilStatus = ENUM_BillingStatus.paid;
                                    txn.PaidDate = phrmsettlement.SettlementDate;
                                    txn.Remark = phrmsettlement.Remarks;

                                    phrmdbcontext.Entry(txn).Property(b => b.BilStatus).IsModified = true;
                                    phrmdbcontext.Entry(txn).Property(b => b.PaymentMode).IsModified = true;
                                    phrmdbcontext.Entry(txn).Property(b => b.SettlementId).IsModified = true;
                                    phrmdbcontext.Entry(txn).Property(b => b.PaidDate).IsModified = true;
                                    phrmdbcontext.Entry(txn).Property(b => b.Remark).IsModified = true;


                                    phrmdbcontext.SaveChanges();
                                }
                                List<PHRMEmployeeCashTransaction> empCashTxns = new List<PHRMEmployeeCashTransaction>();
                                PHRMEmployeeCashTransaction empCashTxn = new PHRMEmployeeCashTransaction();

                                phrmsettlement.PHRMEmployeeCashTransactions.ForEach(cashTxn =>
                                {
                                    cashTxn.ReferenceNo = phrmsettlement.SettlementId;
                                    cashTxn.PatientId = phrmsettlement.PatientId;
                                    cashTxn.EmployeeId = currentUser.EmployeeId;
                                    cashTxn.CounterID = (int)phrmsettlement.CounterId;
                                    cashTxn.TransactionDate = DateTime.Now;
                                    cashTxn.IsActive = true;
                                    empCashTxns.Add(cashTxn);
                                });
                                //Add new row in Deposit Table if Deposit is deducted while settlement.
                                if (phrmsettlement.DepositDeducted != null && phrmsettlement.DepositDeducted > 0)
                                {

                                    PHRMDepositModel depositModel = new PHRMDepositModel()
                                    {
                                        DepositAmount = phrmsettlement.DepositDeducted,
                                        //DepositType = "depositdeduct",
                                        DepositType = ENUM_DepositTransactionType.DepositDeduct,
                                        FiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId,
                                        Remark = "Deposit used in Settlement Receipt No. SR" + phrmsettlement.SettlementReceiptNo + " on " + phrmsettlement.SettlementDate,
                                        CreatedBy = currentUser.EmployeeId,
                                        CreatedOn = DateTime.Now,
                                        CounterId = phrmsettlement.CounterId,
                                        SettlementId = phrmsettlement.SettlementId,
                                        PatientId = phrmsettlement.PatientId,
                                        DepositBalance = 0,
                                        ReceiptNo = PharmacyBL.GetDepositReceiptNo(connString),
                                        StoreId = phrmsettlement.StoreId,
                                        //PaymentMode = "cash",
                                        PaymentMode = ENUM_BillPaymentMode.cash,
                                    };

                                    phrmdbcontext.DepositModel.Add(depositModel);
                                    phrmdbcontext.SaveChanges();

                                    //Save deposit deduct details on Employee Cash Transaction table.
                                    PaymentModes MstPaymentModes = masterDbContext.PaymentModes.Where(a => a.PaymentSubCategoryName.ToLower() == "deposit").FirstOrDefault();
                                    empCashTxn.ReferenceNo = depositModel.DepositId;
                                    empCashTxn.InAmount = 0;
                                    empCashTxn.OutAmount = (decimal)phrmsettlement.DepositDeducted;
                                    empCashTxn.PaymentModeSubCategoryId = MstPaymentModes.PaymentSubCategoryId;
                                    empCashTxn.PatientId = phrmsettlement.PatientId;
                                    empCashTxn.EmployeeId = currentUser.EmployeeId;
                                    empCashTxn.CounterID = (int)phrmsettlement.CounterId;
                                    empCashTxn.TransactionDate = DateTime.Now;
                                    empCashTxn.ModuleName = ENUM_ModuleNames.Dispensary;
                                    empCashTxn.Remarks = "deduct from deposit";
                                    empCashTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.DepositDeduct;
                                    empCashTxn.IsActive = true;
                                    empCashTxns.Add(empCashTxn);

                                    //Save deposit return into Pharmacy Employee Cash Transaction table
                                    if (phrmsettlement.RefundableAmount != null && phrmsettlement.RefundableAmount > 0)
                                    {
                                        empCashTxn.ReferenceNo = depositModel.DepositId;
                                        empCashTxn.InAmount = 0;
                                        empCashTxn.OutAmount = (decimal)phrmsettlement.RefundableAmount;
                                        empCashTxn.PaymentModeSubCategoryId = MstPaymentModes.PaymentSubCategoryId;
                                        empCashTxn.PatientId = phrmsettlement.PatientId;
                                        empCashTxn.EmployeeId = currentUser.EmployeeId;
                                        empCashTxn.CounterID = (int)phrmsettlement.CounterId;
                                        empCashTxn.TransactionDate = DateTime.Now;
                                        empCashTxn.ModuleName = ENUM_ModuleNames.Dispensary;
                                        empCashTxn.Remarks = "return deposit";
                                        empCashTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.DepositReturn;
                                        empCashTxn.IsActive = true;
                                        empCashTxns.Add(empCashTxn);
                                    }
                                }
                                //adding CashDiscountGiven during settlement as OutAmount into EmpCashTransaction table.. 
                                if (phrmsettlement.DiscountAmount.HasValue && phrmsettlement.DiscountAmount.Value > 0)
                                {

                                    PaymentModes MstPaymentModes = masterDbContext.PaymentModes.Where(a => a.PaymentSubCategoryName.ToLower() == "cash").FirstOrDefault();

                                    empCashTxn.TransactionType = ENUM_PHRM_EmpCashTxnTypes.CashDiscountGiven;
                                    empCashTxn.ReferenceNo = phrmsettlement.SettlementId;
                                    empCashTxn.InAmount = 0;
                                    empCashTxn.OutAmount = (decimal)phrmsettlement.DiscountAmount;
                                    empCashTxn.EmployeeId = currentUser.EmployeeId;
                                    empCashTxn.TransactionDate = DateTime.Now;
                                    empCashTxn.CounterID = (int)phrmsettlement.CounterId;
                                    empCashTxn.PatientId = phrmsettlement.PatientId;
                                    empCashTxn.PaymentModeSubCategoryId = MstPaymentModes.PaymentSubCategoryId;
                                    empCashTxn.ModuleName = ENUM_ModuleNames.Dispensary;
                                    empCashTxn.IsActive = true;
                                    empCashTxns.Add(empCashTxn);

                                }
                                //Save Employee Cash Transaction
                                phrmdbcontext.phrmEmployeeCashTransaction.AddRange(empCashTxns);
                                phrmdbcontext.SaveChanges();

                                //Add new row in Deposit Table if Deposit is Returned while settlement.
                                if (phrmsettlement.RefundableAmount != null && phrmsettlement.RefundableAmount > 0)
                                {

                                    PHRMDepositModel depositModel = new PHRMDepositModel()
                                    {
                                        DepositAmount = phrmsettlement.RefundableAmount,
                                        DepositType = "depositreturn",
                                        //DepositType = ENUM_BillDepositType.ReturnDeposit,
                                        FiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId,
                                        Remark = "Deposit used in Settlement Receipt No. SR" + phrmsettlement.SettlementReceiptNo + " on " + phrmsettlement.SettlementDate,
                                        CreatedBy = currentUser.EmployeeId,
                                        CreatedOn = DateTime.Now,
                                        CounterId = phrmsettlement.CounterId,
                                        SettlementId = phrmsettlement.SettlementId,
                                        PatientId = phrmsettlement.PatientId,
                                        DepositBalance = 0,
                                        ReceiptNo = PharmacyBL.GetDepositReceiptNo(connString),
                                        StoreId = phrmsettlement.StoreId,
                                        //PaymentMode = "cash",
                                        PaymentMode = ENUM_BillPaymentMode.cash,
                                    };

                                    phrmdbcontext.DepositModel.Add(depositModel);
                                    phrmdbcontext.SaveChanges();
                                }

                            }

                            //to update InvoiceReturn table by updating SettlementId for the rows that are being settled.
                            if (phrmsettlement.PHRMReturnIdsCSV.Count > 0)
                            {
                                PHRMInvoiceReturnModel phrmInvocieReturn = new PHRMInvoiceReturnModel();
                                foreach (int stl in phrmsettlement.PHRMReturnIdsCSV)
                                {
                                    phrmInvocieReturn = phrmdbcontext.PHRMInvoiceReturnModel.Where(b => b.InvoiceReturnId == stl && b.SettlementId != 0).FirstOrDefault();
                                    if (phrmInvocieReturn != null)
                                    {

                                        phrmInvocieReturn.SettlementId = phrmsettlement.SettlementId;
                                        phrmdbcontext.Entry(phrmInvocieReturn).Property(a => a.SettlementId).IsModified = true;
                                    }

                                }
                                phrmdbcontext.SaveChanges();
                            }


                            dbTransaction.Commit();

                            responseData.Status = "OK";
                            responseData.Results = phrmsettlement;

                        }
                        catch (Exception ex)
                        {
                            dbTransaction.Rollback();
                            responseData.Status = "Failed";
                            responseData.ErrorMessage = ex.ToString();
                        }
                    }
                }

                else if (reqType != null && reqType == "StoreRequisition")
                {
                    string Str = this.ReadPostData();
                    PHRMStoreRequisitionModel RequisitionFromClient = DanpheJSONConvert.DeserializeObject<PHRMStoreRequisitionModel>(Str);

                    List<PHRMStoreRequisitionItemsModel> requisitionItems = new List<PHRMStoreRequisitionItemsModel>();
                    PHRMStoreRequisitionModel requisition = new PHRMStoreRequisitionModel();

                    //giving List Of RequisitionItems to requItemsFromClient because we have save the requisition and RequisitionItems One by one ..
                    //first the requisition is saved  after that we have to take the requisitionid and give the requisitionid  to the RequisitionItems ..and then we can save the RequisitionItems
                    requisitionItems = RequisitionFromClient.RequisitionItems;

                    //removing the RequisitionItems from RequisitionFromClient because RequisitionItems will be saved later 
                    RequisitionFromClient.RequisitionItems = null;

                    //asigining the value to POFromClient with POitems= null
                    requisition = RequisitionFromClient;
                    requisition.CreatedOn = DateTime.Now;
                    if (requisition.RequisitionDate == null)
                    {
                        requisition.RequisitionDate = DateTime.Now;
                    }
                    phrmdbcontext.StoreRequisition.Add(requisition);

                    //this is for requisition only
                    phrmdbcontext.SaveChanges();

                    //getting the lastest RequistionId 
                    int lastRequId = requisition.RequisitionId;

                    //assiging the RequisitionId and CreatedOn i requisitionitem list
                    requisitionItems.ForEach(item =>
                    {
                        item.RequisitionId = lastRequId;
                        item.CreatedOn = DateTime.Now;
                        item.AuthorizedOn = DateTime.Now;
                        item.PendingQuantity = (double)item.Quantity;
                        phrmdbcontext.StoreRequisitionItems.Add(item);

                    });
                    //this Save for requisitionItems
                    phrmdbcontext.SaveChanges();
                    responseData.Results = RequisitionFromClient.RequisitionId;
                    responseData.Status = "OK";

                }
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();

            }

            return DanpheJSONConvert.SerializeObject(responseData, true);
        }
        [HttpPost()]
        [Route("~/api/Pharmacy/PostStoreDispatch")]
        public IActionResult PostStoreDispatch([FromBody] IList<PostStoreDispatchViewModel> dispatchItems)
        {
            var responseData = new DanpheHTTPResponse<object>();
            if (dispatchItems != null && dispatchItems.Count > 0)
            {
                var phrmdbcontext = new PharmacyDbContext(connString);
                RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                var dispatchId = PostStoreDispatchFunc.PostStoreDispatch(phrmdbcontext, dispatchItems, currentUser);

                responseData.Status = "OK";
                responseData.Results = dispatchId;
            }
            else
            {
                responseData.ErrorMessage = "Dispatch Items is null";
                responseData.Status = "Failed";
            }
            return Ok(responseData);
        }
        //[HttpPost("PostInvoice")]
        //public IActionResult PostInvoice([FromBody] PHRMInvoiceTransactionModel invoiceDataFromClient)
        //{
        //    var responseData = new DanpheHTTPResponse<object>();
        //    var phrmDbContext = new PharmacyDbContext(connString);
        //    PatientDbContext patientDbContext = new PatientDbContext(connString);
        //    CoreDbContext coreDbContext = new CoreDbContext(connString);
        //    MasterDbContext masterDbContext = new MasterDbContext(connString);
        //    var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
        //    try
        //    {
        //        if (invoiceDataFromClient == null || invoiceDataFromClient?.InvoiceItems == null) throw new Exception("Invoice Data is empty.");

        //        if (invoiceDataFromClient.SelectedPatient.PatientId == 0)
        //        {
        //            invoiceDataFromClient.SelectedPatient.CreatedBy = currentUser.EmployeeId;
        //            invoiceDataFromClient.PatientId = PharmacyBL.RegisterPatient(invoiceDataFromClient.SelectedPatient, phrmDbContext, patientDbContext, coreDbContext);
        //        }



        //        PHRMInvoiceTransactionModel finalInvoiceData = PharmacyBL.InvoiceTransaction(invoiceDataFromClient, phrmDbContext, masterDbContext, currentUser);

        //        // 27th Aug 2020:Vikas : replaced finalInvoiceData.InvoiceItems with ascending order list items.
        //        //TO-DO : remove this order by itemname ---ramesh
        //        var itemList = finalInvoiceData.InvoiceItems.OrderBy(s => s.ItemName).ToList();
        //        finalInvoiceData.InvoiceItems = itemList;

        //        responseData.Status = "OK";
        //        responseData.Results = finalInvoiceData;

        //        if (realTimeRemoteSyncEnabled)
        //        {
        //            if (invoiceDataFromClient.IsRealtime == null)
        //            {
        //                PHRMInvoiceTransactionModel invoiceSale = phrmDbContext.PHRMInvoiceTransaction.Where(p => p.InvoiceId == finalInvoiceData.InvoiceId).FirstOrDefault();
        //                finalInvoiceData = invoiceSale;
        //            }
        //            if (finalInvoiceData.IsReturn == null)
        //            {
        //                //Sud:24Dec'18--making parallel thread call (asynchronous) to post to IRD. so that it won't stop the normal execution of logic.
        //                ///PharmacyBL.SyncPHRMBillInvoiceToRemoteServer(finalInvoiceData, "phrm-invoice", phrmdbcontext);
        //                Task.Run(() => PharmacyBL.SyncPHRMBillInvoiceToRemoteServer(finalInvoiceData, "phrm-invoice", phrmDbContext));
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        responseData.Status = "Failed";
        //        responseData.ErrorMessage = ex.Message.ToString();
        //    }
        //    return Ok(responseData);
        //}
        [HttpPost("PostReturnFromCustomer")]
        public IActionResult PostReturnFromCustomer([FromBody] PHRMInvoiceReturnModel retCustModel)
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                PharmacyDbContext phrmdbcontext = new PharmacyDbContext(connString);
                MasterDbContext masterDbContext = new MasterDbContext(connString);
                BillingDbContext billingDbContext = new BillingDbContext(connString);
                RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                if (retCustModel == null) throw new ArgumentNullException();
                if (retCustModel.InvoiceReturnItems == null) throw new ArgumentNullException();
                if (retCustModel.InvoiceReturnItems.Count == 0) throw new ArgumentNullException();

                if (retCustModel.InvoiceReturnItems != null && retCustModel.InvoiceReturnItems.Count > 0)
                {
                    //Since InvoiceId is not coming correctly from above InvoiceReturnModel, we're taking from first row of InvoiceReturnItemModel.
                    //suggestion: Pass proper InvoiceId (PK) of current return model.
                    int? invoiceId = retCustModel.InvoiceReturnItems[0].InvoiceId;

                    //ramesh: 4th feb'21: added to avoid returning from multiple tab.
                    bool isReturnInvalid = CheckIfReturnItemsInvalid(phrmdbcontext, invoiceId, retCustModel);
                    if (isReturnInvalid)
                    {
                        responseData.ErrorMessage = "Invoice Items are already returned.";
                        responseData.Status = "Failed";
                    }
                    else
                    {
                        #region To show PolicyNo in Return Invoice
                        int invid = (from txn in phrmdbcontext.PHRMInvoiceTransaction
                                     where txn.InvoicePrintId == retCustModel.InvoiceId
                                     select txn.InvoiceId).DefaultIfEmpty(0).Max();

                        int? priceCategoryId = (from txnItem in phrmdbcontext.PHRMInvoiceTransactionItems
                                                where txnItem.InvoiceId == invid
                                                select txnItem.PriceCategoryId).DefaultIfEmpty(0).Max();
                        if (priceCategoryId != null)
                        {
                            var PatientCreditAmountDetails = billingDbContext.PatientSchemeMaps.Where(pmc => pmc.PatientId == retCustModel.PatientId && pmc.PriceCategoryId == priceCategoryId).OrderByDescending(pmc => pmc.LatestPatientVisitId).FirstOrDefault();

                            if (PatientCreditAmountDetails != null)
                            {
                                retCustModel.PolicyNo = PatientCreditAmountDetails.PolicyNo;
                            }
                        }
                        #endregion

                        //RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                        //flag check all transaction successfully completed or not
                        Boolean flag = false;
                        flag = PharmacyBL.ReturnFromCustomerTransaction(retCustModel, phrmdbcontext, masterDbContext, currentUser, billingDbContext);
                        if (flag)
                        {
                            responseData.Status = "OK";
                            responseData.Results = retCustModel;
                        }
                        else
                        {
                            responseData.ErrorMessage = "Return invoice Items is null or failed to Save";
                            responseData.Status = "Failed";
                        }
                        var invoiceretId = retCustModel.InvoiceReturnItems.Select(a => a.InvoiceId).FirstOrDefault();
                        //sync to remote server once return invoice is created
                        if (realTimeRemoteSyncEnabled)
                        {
                            if (flag == true)
                            {
                                PHRMInvoiceTransactionModel invoiceReturn = phrmdbcontext.PHRMInvoiceTransaction.Where(p => p.InvoiceId == invoiceretId).FirstOrDefault();
                                //Sud:24Dec'18--making parallel thread call (asynchronous) to post to IRD. so that it won't stop the normal execution of logic.
                                ///PharmacyBL.SyncPHRMBillInvoiceToRemoteServer(invoiceReturn, "phrm-invoice-return", phrmdbcontext);
                                Task.Run(() => PharmacyBL.SyncPHRMBillInvoiceToRemoteServer(invoiceReturn, "phrm-invoice-return", phrmdbcontext));

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = "Failed to perform return from customer. Details : " + ex.Message;
            }
            return Ok(responseData);
        }
        //[HttpPost("PostManualReturn")]
        //public async Task<IActionResult> PostManualReturn([FromBody] PHRMInvoiceReturnModel salesReturn)
        //{
        //    DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
        //    try
        //    {
        //        PharmacyDbContext phrmdbcontext = new PharmacyDbContext(connString);
        //        RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
        //        if (salesReturn == null) throw new ArgumentNullException();
        //        if (salesReturn.InvoiceReturnItems == null) throw new ArgumentNullException();
        //        if (salesReturn.InvoiceReturnItems.Count == 0) throw new ArgumentNullException();

        //         PharmacyBL.ManualReturnTransaction(salesReturn, phrmdbcontext, currentUser);

        //        responseData.Status = "OK";
        //        responseData.Results = salesReturn;
        //    }
        //    catch (Exception ex)
        //    {
        //        responseData.Status = "Failed";
        //        responseData.ErrorMessage = "Failed to perform return from customer. Details : " + ex.Message;
        //    }
        //    return Ok(responseData);
        //}
        // PUT invoice header
        [HttpPut]
        [Route("~/api/Pharmacy/putInvoiceHeader")]
        public IActionResult PutInvoiceHeader()
        {
            var pharmacyDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            try
            {
                // Read Files From Clent Side 
                var f = this.ReadFiles();

                // Read File details from form model
                var FD = Request.Form["fileDetails"];
                InvoiceHeaderModel selectedInvoiceHeader = DanpheJSONConvert.DeserializeObject<InvoiceHeaderModel>(FD);

                selectedInvoiceHeader.ModifiedBy = currentUser.EmployeeId;
                selectedInvoiceHeader.ModifiedOn = DateTime.Now;

                pharmacyDbContext.InvoiceHeader.Attach(selectedInvoiceHeader);
                pharmacyDbContext.Entry(selectedInvoiceHeader).State = EntityState.Modified;
                pharmacyDbContext.Entry(selectedInvoiceHeader).Property(x => x.CreatedOn).IsModified = false;
                pharmacyDbContext.Entry(selectedInvoiceHeader).Property(x => x.CreatedBy).IsModified = false;
                pharmacyDbContext.SaveChanges();

                if (f.Count > 0)
                {
                    var file = f[0];

                    var location = (from dbc in pharmacyDbContext.CFGParameters
                                    where dbc.ParameterGroupName.ToLower() == "common"
                                    && dbc.ParameterName == "InvoiceHeaderLogoUploadLocation"
                                    select dbc.ParameterValue).FirstOrDefault();

                    var path = _environment.WebRootPath + location;
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    var fullPath = path + selectedInvoiceHeader.LogoFileName;


                    // Converting Files to Byte there for we require MemoryStream object
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms); // Copy Each file to MemoryStream

                        byte[] imageBytes = ms.ToArray(); // Convert File to Byte[]

                        FileInfo fi = new FileInfo(fullPath);
                        fi.Directory.Create(); // If the directory already exists, this method does nothing.
                        System.IO.File.WriteAllBytes(fi.FullName, imageBytes);
                        ms.Dispose();
                    }
                }

                responseData.Results = selectedInvoiceHeader;
                responseData.Status = "OK";

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = "Failed to Updating Invoice Header.";
                return BadRequest(responseData);
            }
            return Ok(responseData);
        }

        // PUT api/values/5
        [HttpPut]
        public string Put(int settlementId)
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            PharmacyDbContext phrmdbcontext = new PharmacyDbContext(connString);
            RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            string reqType = this.ReadQueryStringData("reqType");
            int invoiceNo = ToInt(this.ReadQueryStringData("invoiceNo"));
            int PrintCount = ToInt(this.ReadQueryStringData("PrintCount"));
            int itemId = ToInt(this.ReadQueryStringData("itemId"));

            string str = this.ReadPostData();
            try
            {
                phrmdbcontext = this.AddAuditField(phrmdbcontext);
                if (!String.IsNullOrEmpty(str))
                {
                    #region PUT : setting-supplier manage
                    if (reqType == "supplier")
                    {
                        PHRMSupplierModel supplierData = DanpheJSONConvert.DeserializeObject<PHRMSupplierModel>(str);

                        //find the supplier and check whether the Ledger is already added : If not add new Supplier Ledger;
                        var supplier = phrmdbcontext.PHRMSupplier.Find(supplierData.SupplierId);
                        if (supplier.IsLedgerRequired == false && supplierData.IsLedgerRequired == true)
                        {
                            //check if the ledger for that supplier is already created or not.
                            var ledgerEntity = phrmdbcontext.SupplierLedger.Where(s => s.SupplierId == supplier.SupplierId).FirstOrDefault();

                            //if ledger is already created for that supplier, just activates it : else, add a new Ledger ;
                            if (ledgerEntity != null && ledgerEntity.SupplierId == supplier.SupplierId)
                            {
                                ledgerEntity.IsActive = true;
                            }
                            else
                            {
                                //Add Supplier Ledger;
                                var newSupplierLedger = new PHRMSupplierLedgerModel()
                                {
                                    SupplierId = supplierData.SupplierId,
                                    CreditAmount = 0,
                                    DebitAmount = 0,
                                    BalanceAmount = 0,
                                    IsActive = true,
                                    CreatedBy = currentUser.EmployeeId,
                                    CreatedOn = DateTime.Now
                                };
                                phrmdbcontext.SupplierLedger.Add(newSupplierLedger);

                            }
                        }
                        //if ledger already created and furthur does not wants it then deactivate the ledger.
                        else if (supplier.IsLedgerRequired == true && supplierData.IsLedgerRequired == false)
                        {
                            var supplierLedger = phrmdbcontext.SupplierLedger.Where(s => s.SupplierId == supplierData.SupplierId).FirstOrDefault();
                            supplierLedger.IsActive = false;
                            phrmdbcontext.SaveChanges();
                        }
                        //update the Master Supplier table Data;
                        supplier.SupplierId = supplierData.SupplierId;
                        supplier.SupplierName = supplierData.SupplierName;
                        supplier.ContactNo = supplierData.ContactNo;
                        supplier.ContactAddress = supplierData.ContactAddress;
                        supplier.AdditionalContactInformation = supplierData.AdditionalContactInformation;
                        supplier.Description = supplierData.Description;
                        supplier.City = supplierData.City;
                        supplier.PANNumber = supplierData.PANNumber;
                        supplier.DDA = supplierData.DDA;
                        supplier.Email = supplierData.Email;
                        supplier.IsActive = supplierData.IsActive;
                        supplier.CreditPeriod = supplierData.CreditPeriod;
                        supplier.IsLedgerRequired = supplierData.IsLedgerRequired;
                        supplier.CreatedBy = supplierData.CreatedBy;
                        supplier.CreatedOn = supplierData.CreatedOn;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = supplierData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-company manage
                    else if (reqType == "company")
                    {
                        PHRMCompanyModel companyData = DanpheJSONConvert.DeserializeObject<PHRMCompanyModel>(str);
                        phrmdbcontext.PHRMCompany.Attach(companyData);
                        phrmdbcontext.Entry(companyData).State = EntityState.Modified;
                        phrmdbcontext.Entry(companyData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(companyData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = companyData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-dispensary manage
                    else if (reqType == "dispensary")
                    {
                        PHRMStoreModel dispensaryData = DanpheJSONConvert.DeserializeObject<PHRMStoreModel>(str);
                        phrmdbcontext.PHRMStore.Attach(dispensaryData);
                        phrmdbcontext.Entry(dispensaryData).State = EntityState.Modified;
                        phrmdbcontext.Entry(dispensaryData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(dispensaryData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = dispensaryData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-category manage
                    else if (reqType == "category")
                    {
                        PHRMCategoryModel categoryData = DanpheJSONConvert.DeserializeObject<PHRMCategoryModel>(str);
                        phrmdbcontext.PHRMCategory.Attach(categoryData);
                        phrmdbcontext.Entry(categoryData).State = EntityState.Modified;
                        phrmdbcontext.Entry(categoryData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(categoryData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = categoryData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-unitofmeasurement manage
                    else if (reqType == "unitofmeasurement")
                    {
                        PHRMUnitOfMeasurementModel uomData = DanpheJSONConvert.DeserializeObject<PHRMUnitOfMeasurementModel>(str);
                        phrmdbcontext.PHRMUnitOfMeasurement.Attach(uomData);
                        phrmdbcontext.Entry(uomData).State = EntityState.Modified;
                        phrmdbcontext.Entry(uomData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(uomData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = uomData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-itemtype manage
                    else if (reqType == "itemtype")
                    {
                        PHRMItemTypeModel itemtypeData = DanpheJSONConvert.DeserializeObject<PHRMItemTypeModel>(str);
                        phrmdbcontext.PHRMItemType.Attach(itemtypeData);
                        phrmdbcontext.Entry(itemtypeData).State = EntityState.Modified;
                        phrmdbcontext.Entry(itemtypeData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(itemtypeData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = itemtypeData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-packingtype manage
                    else if (reqType == "packingtype")
                    {
                        PHRMPackingTypeModel packingtypeData = DanpheJSONConvert.DeserializeObject<PHRMPackingTypeModel>(str);
                        phrmdbcontext.PHRMPackingType.Attach(packingtypeData);
                        phrmdbcontext.Entry(packingtypeData).State = EntityState.Modified;
                        phrmdbcontext.Entry(packingtypeData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(packingtypeData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = packingtypeData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-item manage
                    else if (reqType == "item")
                    {
                        PHRMItemMasterModel itemData = DanpheJSONConvert.DeserializeObject<PHRMItemMasterModel>(str);
                        phrmdbcontext.PHRMItemMaster.Attach(itemData);
                        phrmdbcontext.Entry(itemData).State = EntityState.Modified;
                        phrmdbcontext.Entry(itemData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(itemData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        var priceCategoryDetails = phrmdbcontext.PHRM_MAP_MstItemsPriceCategories.Where(p => p.ItemId == itemData.ItemId).ToList();
                        itemData.PHRM_MAP_MstItemsPriceCategories = priceCategoryDetails;
                        responseData.Results = itemData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT : setting-tax manage
                    else if (reqType == "tax")
                    {
                        PHRMTAXModel taxData = DanpheJSONConvert.DeserializeObject<PHRMTAXModel>(str);
                        phrmdbcontext.PHRMTAX.Attach(taxData);
                        phrmdbcontext.Entry(taxData).State = EntityState.Modified;
                        phrmdbcontext.Entry(taxData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(taxData).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = taxData;
                        responseData.Status = "OK";
                    }
                    #endregion
                    #region PUT: Generic Name
                    else if (reqType == "genericName")
                    {
                        PHRMGenericModel genericData = DanpheJSONConvert.DeserializeObject<PHRMGenericModel>(str);
                        phrmdbcontext.PHRMGenericModel.Attach(genericData);
                        phrmdbcontext.Entry(genericData).State = EntityState.Modified;
                        phrmdbcontext.Entry(genericData).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(genericData).Property(x => x.CreatedBy).IsModified = false;
                        genericData.ModifiedOn = System.DateTime.Now;
                        genericData.ModifiedBy = currentUser.EmployeeId;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = genericData;
                        responseData.Status = "OK";
                    }
                    #endregion

                    #region PUT : sale-Invoice Items provisional Bill Payment
                    else if (reqType == "InvItemsCreditPay")
                    {
                        List<PHRMInvoiceTransactionItemsModel> invItmsData = DanpheJSONConvert.DeserializeObject<List<PHRMInvoiceTransactionItemsModel>>(str);
                        decimal? paidAmount = 0;
                        int invoiceId = invItmsData[0].InvoiceId.Value;//Invoice id for update provisional amount and status
                        invItmsData.ForEach(
                            invItm =>
                            {
                                phrmdbcontext.PHRMInvoiceTransactionItems.Attach(invItm);
                                phrmdbcontext.Entry(invItm).State = EntityState.Modified;
                                phrmdbcontext.Entry(invItm).Property(x => x.BilItemStatus).IsModified = true;
                                phrmdbcontext.Entry(invItm).Property(x => x.BatchNo).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.CompanyId).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.CreatedBy).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.CreatedOn).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.DiscountPercentage).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.FreeQuantity).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.GrItemPrice).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.InvoiceId).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.ItemId).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.ItemName).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.SalePrice).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.Price).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.Quantity).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.Remark).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.SubTotal).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.TotalAmount).IsModified = false;
                                phrmdbcontext.Entry(invItm).Property(x => x.VATPercentage).IsModified = false;
                                paidAmount = paidAmount + invItm.TotalAmount; //addition of paidAmount for Invoice provisional Amount Updation
                            }
                            );
                        phrmdbcontext.SaveChanges();//save changes of items status updation

                        //get paid invoice items count , if all items bill status is paid then  need to update invoice bill status also
                        var unpaidBillStatusCount = (from invItms in phrmdbcontext.PHRMInvoiceTransactionItems
                                                     where invItms.BilItemStatus != "paid" && invItms.InvoiceId == invoiceId
                                                     select invItms).ToList().Count;

                        //get Invoice details by invoice id for updation status and provisional amount
                        PHRMInvoiceTransactionModel invoiceDeta = (from inv in phrmdbcontext.PHRMInvoiceTransaction
                                                                   where inv.InvoiceId == invoiceId
                                                                   select inv).FirstOrDefault();
                        invoiceDeta.BilStatus = (unpaidBillStatusCount > 0) ? "unpaid" : "paid";
                        //invoiceDeta.CreditAmount = invoiceDeta.CreditAmount - paidAmount;
                        // invoiceDeta.PaidAmount = invoiceDeta.PaidAmount + paidAmount ;
                        //modify invoice bill status and provisional amount details
                        phrmdbcontext.PHRMInvoiceTransaction.Attach(invoiceDeta);
                        //Use property level EntityState Modified -- sudarshan: 5Sept'18
                        phrmdbcontext.Entry(invoiceDeta).State = EntityState.Modified;
                        phrmdbcontext.SaveChanges();

                        responseData.Results = 1;
                        responseData.Status = "OK";
                    }
                    #endregion

                    #region: Update Print Count After Print
                    else if (reqType == "UpdatePrintCountafterPrint")
                    {
                        PHRMInvoiceTransactionModel dbPhrmBillPrintReq = phrmdbcontext.PHRMInvoiceTransaction
                                       .Where(a => a.InvoiceId == invoiceNo).FirstOrDefault<PHRMInvoiceTransactionModel>();
                        if (dbPhrmBillPrintReq != null)
                        {
                            dbPhrmBillPrintReq.PrintCount = PrintCount;
                            phrmdbcontext.Entry(dbPhrmBillPrintReq).State = EntityState.Modified;
                        }

                        phrmdbcontext.SaveChanges();
                        responseData.Status = "OK";
                        responseData.Results = "Print count updated successfully.";
                    }
                    #endregion

                    else if (reqType == "add-Item-to-rack")
                    {
                        string dispRackIdstr = this.ReadQueryStringData("dispensaryRackId");
                        string storeRackIdstr = this.ReadQueryStringData("storeRackId");
                        int? dispensaryRackId = dispRackIdstr == "null" ? (int?)null : ToInt(dispRackIdstr);
                        int? storeRackId = storeRackIdstr == "null" ? (int?)null : ToInt(storeRackIdstr);

                        PHRMItemMasterModel dbphrmItem = phrmdbcontext.PHRMItemMaster
                                       .Where(a => a.ItemId == itemId).FirstOrDefault<PHRMItemMasterModel>();
                        if (dbphrmItem != null)
                        {
                            dbphrmItem.Rack = dispensaryRackId;
                            dbphrmItem.StoreRackId = storeRackId;
                            phrmdbcontext.Entry(dbphrmItem).State = EntityState.Modified;
                        }

                        phrmdbcontext.SaveChanges();
                        responseData.Status = "OK";
                        responseData.Results = "Print count updated successfully.";
                    }
                    #region Update Deposit PrintCount
                    else if (reqType == "updateDepositPrint")
                    {
                        PHRMDepositModel depositModel = DanpheJSONConvert.DeserializeObject<PHRMDepositModel>(str);
                        PHRMDepositModel data = (from depositData in phrmdbcontext.DepositModel
                                                 where depositModel.DepositId == depositData.DepositId
                                                 select depositData).FirstOrDefault();
                        if (data.DepositId > 0)
                        {
                            data.PrintCount = data.PrintCount == 0 ? 1 : data.PrintCount == null ? 1 : data.PrintCount + 1;
                            phrmdbcontext.DepositModel.Attach(data);
                            phrmdbcontext.Entry(data).State = EntityState.Modified;
                            phrmdbcontext.Entry(data).Property(x => x.PrintCount).IsModified = true;
                            phrmdbcontext.SaveChanges();
                            responseData.Results = data;
                            responseData.Status = "OK";
                        }
                        else
                        {
                            responseData.Results = "";
                            responseData.Status = "failed";
                        }
                    }
                    #endregion
                    //Rajesh: 24Aug2019
                    else if (reqType == "updateSettlementPrintCount")
                    {
                        int settlmntId = settlementId;
                        var currSettlment = phrmdbcontext.PHRMSettlements.Where(s => s.SettlementId == settlmntId).FirstOrDefault();
                        if (currSettlment != null)
                        {
                            int? printCount = currSettlment.PrintCount.HasValue ? currSettlment.PrintCount : 0;
                            printCount += 1;
                            phrmdbcontext.PHRMSettlements.Attach(currSettlment);
                            currSettlment.PrintCount = printCount;
                            currSettlment.PrintedOn = System.DateTime.Now;
                            currSettlment.PrintedBy = currentUser.EmployeeId;
                            phrmdbcontext.Entry(currSettlment).Property(b => b.PrintCount).IsModified = true;
                            phrmdbcontext.SaveChanges();

                            responseData.Results = new { SettlementId = settlementId, PrintCount = printCount };
                        }
                    }
                    //PUT credit organizations
                    else if (reqType == "put-credit-organizations")
                    {
                        PHRMCreditOrganizationsModel creditOrganization = DanpheJSONConvert.DeserializeObject<PHRMCreditOrganizationsModel>(str);
                        phrmdbcontext.CreditOrganizations.Attach(creditOrganization);
                        phrmdbcontext.Entry(creditOrganization).State = EntityState.Modified;
                        phrmdbcontext.Entry(creditOrganization).Property(x => x.CreatedOn).IsModified = false;
                        phrmdbcontext.Entry(creditOrganization).Property(x => x.CreatedBy).IsModified = false;
                        phrmdbcontext.SaveChanges();
                        responseData.Results = creditOrganization;
                        responseData.Status = "OK";
                    }
                    else if (reqType == "updateGoodReceipt")
                    {

                        PHRMGoodsReceiptModel goodsReceipt = DanpheJSONConvert.DeserializeObject<PHRMGoodsReceiptModel>(str);
                        goodsReceipt.FiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId;
                        if (goodsReceipt != null && goodsReceipt.GoodReceiptItem != null && goodsReceipt.GoodReceiptItem.Count > 0)
                        {

                            using (var dbTransaction = phrmdbcontext.Database.BeginTransaction())
                            {
                                try
                                {
                                    //to assign GRID if new item has been added.
                                    var GRId = goodsReceipt.GoodReceiptId;
                                    var storeId = goodsReceipt.StoreId;
                                    var currentDate = DateTime.Now;
                                    var fiscalYearId = PharmacyBL.GetFiscalYear(phrmdbcontext).FiscalYearId;
                                    goodsReceipt.ModifiedBy = currentUser.EmployeeId;
                                    goodsReceipt.ModifiedOn = currentDate;

                                    //if any old grItems has been deleted, we need to compare GRItemIdlist
                                    List<int> grItemsIdList = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptId == GRId && a.IsCancel != true).Select(a => a.GoodReceiptItemId).ToList();
                                    //also find the storestocklist for that deleted old GRItems
                                    List<int> grItemStoreStockIdList = phrmdbcontext.PHRMGoodsReceiptItems.Where(a => a.GoodReceiptId == GRId).Select(a => a.StoreStockId).ToList();

                                    goodsReceipt.GoodReceiptItem.ForEach(itm =>
                                    {

                                        if (itm.GoodReceiptItemId > 0) //old elememnt will have the goodsReceiptItemId
                                        {
                                            if (itm.ReceivedQuantity != 0)
                                            {
                                                itm.GrPerItemVATAmt = itm.VATAmount;
                                                itm.GrPerItemDisAmt = itm.DiscountAmount;
                                            }
                                            phrmdbcontext.PHRMGoodsReceiptItems.Attach(itm);
                                            phrmdbcontext.Entry(itm).State = EntityState.Modified;
                                            phrmdbcontext.Entry(itm).Property(x => x.GoodReceiptId).IsModified = false;
                                            phrmdbcontext.Entry(itm).Property(x => x.CreatedOn).IsModified = false;
                                            phrmdbcontext.Entry(itm).Property(x => x.CreatedBy).IsModified = false;

                                            var mainStock = phrmdbcontext.StockMasters.Where(s => s.StockId == itm.StockId && s.ItemId == itm.ItemId).FirstOrDefault();
                                            mainStock.UpdateBatch(itm.BatchNo, currentUser.EmployeeId);
                                            mainStock.UpdateExpiry(itm.ExpiryDate, currentUser.EmployeeId);

                                            var storeStock = phrmdbcontext.StoreStocks.Where(a => a.StockId == itm.StockId && a.StoreStockId == itm.StoreStockId).FirstOrDefault();
                                            if (itm.IsItemAltered == false)
                                            {
                                                storeStock.UpdateAvailableQuantity(newQty: itm.ReceivedQuantity + itm.FreeQuantity);
                                                storeStock.UpdateNewCostPrice((decimal)itm.CostPrice);
                                                storeStock.UpdateMRP(itm.SalePrice);
                                                mainStock.UpdateNewCostPrice((decimal)itm.CostPrice);
                                                mainStock.UpdateMRP((decimal)itm.SalePrice, currentUser.EmployeeId);

                                                //find the stock txn list for that particular grItem to update Batch and Expiry
                                                var stockTxn = phrmdbcontext.StockTransactions.Where(a => a.StockId == itm.StockId && a.StoreStockId == itm.StoreStockId).FirstOrDefault();
                                                stockTxn.SetInOutQuantity(itm.ReceivedQuantity + itm.FreeQuantity, 0);

                                                //TODO: Update Batch, ExpiryDate, CostPrice and Update SalePrice for that item
                                                stockTxn.UpdateBatch(itm.BatchNo, currentUser.EmployeeId);
                                                stockTxn.UpdateExpiry(itm.ExpiryDate, currentUser.EmployeeId);
                                                stockTxn.UpdateCostPrice((decimal)itm.CostPrice);
                                                stockTxn.UpdateMRP(itm.SalePrice);
                                            }
                                            phrmdbcontext.SaveChanges();

                                        }
                                        else //this is the case "if new item is added during Edit";
                                        {
                                            // Initially,Add to Stock Master 
                                            var newStockMaster = new PHRMStockMaster(
                                                                        itemId: itm.ItemId,
                                                                        batchNo: itm.BatchNo,
                                                                        expiryDate: itm.ExpiryDate,
                                                                        costPrice: itm.GRItemPrice,
                                                                        salePrice: itm.SalePrice,
                                                                        mrp: itm.MRP,
                                                                        createdBy: currentUser.EmployeeId,
                                                                        createdOn: currentDate);

                                            // add the new barcode id
                                            var barcodeService = new PharmacyStockBarcodeService(phrmdbcontext);
                                            newStockMaster.UpdateBarcodeId(barcodeService.AddStockBarcode(
                                               stock: newStockMaster,
                                               createdBy: currentUser.EmployeeId
                                                ));

                                            phrmdbcontext.StockMasters.Add(newStockMaster);
                                            phrmdbcontext.SaveChanges();

                                            // Add  store stock first
                                            var newStoreStock = new PHRMStoreStockModel(newStockMaster, storeId, (itm.ReceivedQuantity + itm.FreeQuantity), (decimal)itm.CostPrice, itm.SalePrice);
                                            phrmdbcontext.StoreStocks.Add(newStoreStock);
                                            phrmdbcontext.SaveChanges();

                                            // Add GoodsReceiptItem
                                            itm.GoodReceiptId = GRId;
                                            itm.StockId = newStoreStock.StockId;
                                            itm.StoreStockId = newStoreStock.StoreStockId.Value;
                                            itm.CreatedBy = currentUser.EmployeeId;
                                            itm.CreatedOn = currentDate;
                                            itm.AvailableQuantity = itm.ReceivedQuantity + itm.FreeQuantity;
                                            //below fields are used for accounting do not remove
                                            if (itm.AvailableQuantity != 0)
                                            {
                                                itm.GrPerItemVATAmt = itm.VATAmount;
                                                itm.GrPerItemDisAmt = itm.DiscountAmount;
                                            }
                                            phrmdbcontext.PHRMGoodsReceiptItems.Add(itm);
                                            phrmdbcontext.SaveChanges();

                                            // Add stock txns
                                            var newMainStockTxns = new PHRMStockTransactionModel(newStoreStock, ENUM_PHRM_StockTransactionType.PurchaseItem, currentDate, itm.GoodReceiptItemId, currentUser.EmployeeId, currentDate, fiscalYearId);
                                            newMainStockTxns.SetInOutQuantity(newStoreStock.AvailableQuantity, 0);
                                            phrmdbcontext.StockTransactions.Add(newMainStockTxns);
                                            phrmdbcontext.SaveChanges();
                                        }
                                        grItemsIdList = grItemsIdList.Where(a => a != itm.GoodReceiptItemId).ToList();
                                        grItemStoreStockIdList = grItemStoreStockIdList.Where(a => a != itm.StoreStockId).ToList();

                                    });
                                    if (grItemsIdList.Any() && grItemStoreStockIdList.Any())
                                    {
                                        foreach (int grItemId in grItemsIdList)
                                        {
                                            var grItem = phrmdbcontext.PHRMGoodsReceiptItems.Find(grItemId);
                                            grItem.IsCancel = true;

                                            var strstk = phrmdbcontext.StoreStocks.Include(s => s.StockMaster).Where(a => a.StoreStockId == grItem.StoreStockId).FirstOrDefault();

                                            //Add transaction data in stock transaction table
                                            var stockTxn = new PHRMStockTransactionModel(
                                                 storeStock: strstk,
                                                 transactionType: ENUM_PHRM_StockTransactionType.CancelledGR,
                                                 transactionDate: currentDate,
                                                 referenceNo: grItem.GoodReceiptItemId,
                                                 createdBy: currentUser.EmployeeId,
                                                 createdOn: currentDate,
                                                 fiscalYearId: goodsReceipt.FiscalYearId);
                                            stockTxn.SetInOutQuantity(inQty: 0, outQty: grItem.AvailableQuantity);
                                            phrmdbcontext.StockTransactions.Add(stockTxn);
                                        }
                                        foreach (int storeStockId in grItemStoreStockIdList)
                                        {
                                            var storeStock = phrmdbcontext.StoreStocks.Find(storeStockId);
                                            storeStock.UpdateAvailableQuantity(newQty: 0);

                                            //TODO:find the stockTxn and Update the IsActive flag to false;

                                        }
                                        phrmdbcontext.SaveChanges();
                                    }

                                    phrmdbcontext.PHRMGoodsReceipt.Attach(goodsReceipt);
                                    phrmdbcontext.Entry(goodsReceipt).State = EntityState.Modified;
                                    phrmdbcontext.Entry(goodsReceipt).Property(x => x.CreatedOn).IsModified = false;
                                    phrmdbcontext.Entry(goodsReceipt).Property(x => x.CreatedBy).IsModified = false;
                                    phrmdbcontext.SaveChanges();
                                    dbTransaction.Commit();
                                    responseData.Results = goodsReceipt.GoodReceiptId;
                                }
                                catch (Exception Ex)
                                {
                                    dbTransaction.Rollback();
                                    throw Ex;
                                }
                            }

                            responseData.Results = goodsReceipt;
                            responseData.Status = "OK";
                        }
                    }

                    #region PUT : CC Charge value
                    else if (reqType == "cccharge")
                    {
                        CfgParameterModel parameter = DanpheJSONConvert.DeserializeObject<CfgParameterModel>(str);
                        MasterDbContext masterDBContext = new MasterDbContext(connString);


                        //  phrmdbcontext.SaveChanges();
                        var parmToUpdate = (from paramData in masterDBContext.CFGParameters
                                            where
                                            //paramData.ParameterId == parameter.ParameterId
                                            //no need of below comparision since parameter id is Primary Key and we can compare only to it.
                                            //&&
                                            paramData.ParameterName == "PharmacyCCCharge"
                                            && paramData.ParameterGroupName == "Pharmacy"
                                            select paramData
                                            ).FirstOrDefault();

                        parmToUpdate.ParameterValue = parameter.ParameterValue;
                        masterDBContext.CFGParameters.Attach(parmToUpdate);

                        masterDBContext.Entry(parmToUpdate).Property(p => p.ParameterValue).IsModified = true;

                        masterDBContext.SaveChanges();
                        responseData.Status = "OK";
                        responseData.Results = parmToUpdate;
                    }
                    #endregion


                }
                else
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "Client Object is empty";
                }

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return DanpheJSONConvert.SerializeObject(responseData, true);
        }
        [HttpPut("UpdateStockMRP")]
        public async Task<IActionResult> UpdateStockMRP([FromBody] PHRMUpdatedStockVM mrpUpdatedStock)
        {
            var responseData = new DanpheHTTPResponse<object>();
            var db = new PharmacyDbContext(connString);
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            try
            {
                responseData.Results = await PharmacyBL.UpdateMRPForAllStock(mrpUpdatedStock, db, currentUser);
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = $"Could not perform the request. {ex.Message}";
            }
            return Ok(responseData);

        }
        [HttpPut("UpdateStockExpiryDateandBatchNo")]
        public async Task<IActionResult> UpdateStockExpiryDateandBatchNo([FromBody] PHRMUpdatedStockVM expbatchUpdatedStock)
        {
            var responseData = new DanpheHTTPResponse<object>();
            var db = new PharmacyDbContext(connString);
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            try
            {
                responseData.Results = await PharmacyBL.UpdateStockExpiryDateandBatchNoForAllStock(expbatchUpdatedStock, db, currentUser);
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = $"Could not perform the request. {ex.Message}";
                return BadRequest(responseData);
            }
            return Ok(responseData);

        }

        private int GetPatientNo(int patientId)
        {
            try
            {
                if (patientId != 0)
                {
                    int newPatNo = 0;

                    PatientDbContext patientDbContext = new PatientDbContext(connString);
                    var maxPatNo = patientDbContext.Patients.DefaultIfEmpty().Max(p => p == null ? 0 : p.PatientNo);
                    newPatNo = maxPatNo + 1;

                    return newPatNo;
                }
                else
                    throw new Exception("Invalid PatientId");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private string GetPatientCode(int patientId)
        {
            try
            {
                if (patientId != 0)
                {
                    NewPatientUniqueNumbersVM retValue = new NewPatientUniqueNumbersVM();
                    int newPatNo = 0;
                    string newPatCode = "";

                    PatientDbContext patientDbContext = new PatientDbContext(connString);
                    var maxPatNo = patientDbContext.Patients.DefaultIfEmpty().Max(p => p == null ? 0 : p.PatientNo);
                    newPatNo = maxPatNo + 1;


                    string patCodeFormat = "YYMM-PatNum";//this is default value.
                    string hospitalCode = "";//default empty


                    CoreDbContext coreDbContext = new CoreDbContext(connString);

                    List<ParameterModel> allParams = coreDbContext.Parameters.ToList();


                    ParameterModel patCodeFormatParam = allParams
                       .Where(a => a.ParameterGroupName == "Patient" && a.ParameterName == "PatientCodeFormat")
                       .FirstOrDefault<ParameterModel>();
                    if (patCodeFormatParam != null)
                    {
                        patCodeFormat = patCodeFormatParam.ParameterValue;
                    }


                    ParameterModel hospCodeParam = allParams
                        .Where(a => a.ParameterName == "HospitalCode")
                        .FirstOrDefault<ParameterModel>();
                    if (hospCodeParam != null)
                    {
                        hospitalCode = hospCodeParam.ParameterValue;
                    }



                    if (patCodeFormat == "YYMM-PatNum")
                    {
                        newPatCode = DateTime.Now.ToString("yy") + DateTime.Now.ToString("MM") + String.Format("{0:D6}", newPatNo);
                    }
                    else if (patCodeFormat == "HospCode-PatNum")
                    {
                        newPatCode = hospitalCode + newPatNo;
                    }
                    else if (patCodeFormat == "PatNum")
                    {
                        newPatCode = newPatNo.ToString();
                    }

                    return newPatCode;
                }
                else
                    throw new Exception("Invalid PatientId");


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private int GetSettlementReceiptNo(PharmacyDbContext dbContext)
        {
            int? currSettlmntNo = dbContext.PHRMSettlements.Max(a => a.SettlementReceiptNo);
            if (!currSettlmntNo.HasValue)
            {
                currSettlmntNo = 0;
            }

            return currSettlmntNo.Value + 1;
        }

        [HttpGet]
        [Route("~/api/Pharmacy/getItemRateHistory")]
        public async Task<IActionResult> GetItemRateHistory()
        {
            var phrmDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var itemRateHistory = await (from GRI in phrmDbContext.PHRMGoodsReceiptItems
                                             join GR in phrmDbContext.PHRMGoodsReceipt on GRI.GoodReceiptId equals GR.GoodReceiptId
                                             join S in phrmDbContext.PHRMSupplier on GR.SupplierId equals S.SupplierId
                                             select new
                                             {
                                                 GRI.ItemId,
                                                 GRI.GRItemPrice,
                                                 S.SupplierName,
                                                 GR.GoodReceiptDate
                                             }).OrderByDescending(GRI => GRI.GoodReceiptDate).ToListAsync();
                if (itemRateHistory == null || itemRateHistory.Count() == 0)
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "No price history found.";
                }
                responseData.Results = itemRateHistory;
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = $"Failed to obtain price history. Message: {ex.Message}";
            }
            return Ok(responseData);
        }

        [HttpGet]
        [Route("~/api/Pharmacy/getItemFreeQuantityReceivedHistory")]
        public async Task<IActionResult> GetItemFreeQuantityReceivedHistory()
        {
            var phrmDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var itemFreeQuantityHistory = await (from GRI in phrmDbContext.PHRMGoodsReceiptItems
                                                     join GR in phrmDbContext.PHRMGoodsReceipt on GRI.GoodReceiptId equals GR.GoodReceiptId
                                                     join S in phrmDbContext.PHRMSupplier on GR.SupplierId equals S.SupplierId
                                                     select new
                                                     {
                                                         SupplierName = S.SupplierName,
                                                         GoodReceiptDate = GR.GoodReceiptDate,
                                                         FreeQuantity = GRI.FreeQuantity,
                                                         ReceivedQuantity = GRI.ReceivedQuantity,
                                                         ItemId = GRI.ItemId
                                                     }).OrderByDescending(GRI => GRI.GoodReceiptDate).ToListAsync();
                if (itemFreeQuantityHistory == null || itemFreeQuantityHistory.Count() == 0)
                {
                    responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.Failed;
                    responseData.ErrorMessage = "No free quantity history found.";
                }
                responseData.Results = itemFreeQuantityHistory;
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
            }
            catch (Exception ex)
            {
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.Failed;
                responseData.ErrorMessage = $"Failed to obtain Free Quantity History. Message: {ex.Message}";
            }
            return Ok(responseData);
        }
        [HttpGet]
        [Route("~/api/Pharmacy/getMRPHistory")]
        public async Task<IActionResult> GetMRPHistory()
        {
            var phrmDbContext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var itemMRPHistory = await (from stkmst in phrmDbContext.StockMasters
                                            join gri in phrmDbContext.PHRMGoodsReceiptItems on stkmst.StockId equals gri.StockId
                                            join gr in phrmDbContext.PHRMGoodsReceipt on gri.GoodReceiptId equals gr.GoodReceiptId
                                            join supplier in phrmDbContext.PHRMSupplier on gr.SupplierId equals supplier.SupplierId
                                            select new
                                            {
                                                stkmst.ItemId,
                                                stkmst.SalePrice,
                                                gr.GoodReceiptDate,
                                                supplier.SupplierName
                                            }).OrderByDescending(a => a.GoodReceiptDate).ToListAsync();
                if (itemMRPHistory == null || itemMRPHistory.Count() == 0)
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "No SalePrice history found.";
                }
                responseData.Results = itemMRPHistory;
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = $"Failed to obtain SalePrice history. Message: {ex.Message}";
            }
            return Ok(responseData);
        }

        //ramesh:4th Feb'21 : Function to Check if more item are being returned than sold items. 
        private bool CheckIfReturnItemsInvalid(PharmacyDbContext phrmdbcontext, int? invoiceId, PHRMInvoiceReturnModel retInvoiceModel)
        {
            bool isInvalidReturn = false;


            var availableItemDetails = (
                from invoice in phrmdbcontext.PHRMInvoiceTransaction.AsNoTracking().Where(i => i.InvoiceId == invoiceId)
                join invItem in phrmdbcontext.PHRMInvoiceTransactionItems.AsNoTracking() on invoice.InvoiceId equals invItem.InvoiceId
                join invRetItem in phrmdbcontext.PHRMInvoiceReturnItemsModel.AsNoTracking() on invItem.InvoiceItemId equals invRetItem.InvoiceItemId into invretitmJ
                from invretLJ in invretitmJ.DefaultIfEmpty()
                group new { invItem, invretLJ } by new
                { invItem.InvoiceId, invItem.InvoiceItemId, invItem.ItemId, invItem.Quantity }
                into grouped
                select new
                {
                    InvoiceId = grouped.Key.InvoiceId,
                    InvoiceItemId = grouped.Key.InvoiceItemId,
                    ItemId = grouped.Key.ItemId,
                    SoldQty = grouped.Key.Quantity,
                    ReturnedQty = grouped.Sum(x => x.invretLJ != null ? x.invretLJ.ReturnedQty : 0),
                    AvailableQty = (grouped.Key.Quantity) - (double)(grouped.Sum(x => x.invretLJ != null ? x.invretLJ.ReturnedQty : 0))
                }).Where(a => a.AvailableQty > 0).ToList();

            //if nothing available then don't allow to return.
            if (availableItemDetails.Count == 0)
            {
                isInvalidReturn = true;
                return isInvalidReturn;
            }

            //Check if any of the Items coming from Client Side is having more ReturnedQty than available Quantity.
            isInvalidReturn = retInvoiceModel.InvoiceReturnItems.Any(ret => availableItemDetails.FirstOrDefault(b => b.InvoiceItemId == ret.InvoiceItemId)?.AvailableQty < (double)ret.ReturnedQty);


            return isInvalidReturn;
        }

        [HttpGet("ExportStocksForReconciliationToExcel")]
        public IActionResult ExportStocksForReconciliationToExcel()
        {
            try
            {
                var phrmDbContext = new PharmacyDbContext(connString);
                string tempFIleName = "PharmacyStockReconciliation_" + DateTime.Now.ToString("yyyy-MM-dd h:mm tt");
                string tempFIleName1 = tempFIleName.Replace(" ", "-");
                string fileName = tempFIleName1.Replace(":", "-");

                //To get Stock Data
                DataTable stockListVM = phrmDbContext.GetMainStoreStock(true);

                //To Made available Quantity at last
                int index = stockListVM.Columns.Count;
                stockListVM.Columns["AvailableQuantity"].SetOrdinal(index - 1);

                //Add New Column 
                stockListVM.Columns.Add("NewAvailableQuantity");

                //To copy all the stock AvailableQuantity into Column NewAvailableQuantity
                foreach (DataRow row in stockListVM.Rows)
                {
                    row["NewAvailableQuantity"] = row["AvailableQuantity"];
                }

                byte[] fileByteArray;

                #region Save Excel File with the protection
                using (ExcelEngine engine = new ExcelEngine())
                {
                    IApplication application = engine.Excel;
                    application.DefaultVersion = ExcelVersion.Xlsx;

                    IWorkbook workbook = application.Workbooks.Create(1);
                    IWorksheet sheet = workbook.Worksheets[0];

                    sheet.ImportDataTable(stockListVM, true, 1, 1, true);

                    IListObject listObj = sheet.ListObjects.Create("PharmacyStockList", sheet.UsedRange);
                    listObj.BuiltInTableStyle = TableBuiltInStyles.TableStyleLight14;
                    sheet.UsedRange.AutofitColumns();

                    int colcount = sheet.Columns.Count();
                    int rowcount = sheet.Rows.Count();

                    sheet.Protect("", ExcelSheetProtection.All);
                    int av = 64 + colcount;
                    for (int i = colcount - 1; i < colcount; i++)
                    {
                        for (int j = 1; j < rowcount - 1; j++)
                        {
                            string value = Convert.ToChar(av) + j.ToString();
                            sheet[value].CellStyle.Locked = false;
                        }
                        av++;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        fileByteArray = memoryStream.ToArray();
                    }

                }
                #endregion

                return File(fileByteArray, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName + ".xlsx");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("UpdateReconciledStockFromExcelFile")]
        public IActionResult UpdateReconciledStockFromExcelFile()
        {
            string Str = this.ReadPostData();
            List<PharmacyStockModel> stock = DanpheJSONConvert.DeserializeObject<List<PharmacyStockModel>>(Str);
            var responseData = new DanpheHTTPResponse<object>();
            var phrmDbContext = new PharmacyDbContext(connString);
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            try
            {
                PharmacyBL.UpdateReconciledStockFromExcel(stock, currentUser, phrmDbContext);
                responseData.Status = "OK";
                responseData.Results = null;
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.ToString();

            }
            return Ok(responseData);
        }

        [HttpPut("~/api/Pharmacy/updateNMCNo/{EmployeeId}")]
        public async Task<IActionResult> UpdateNMCNo([FromRoute] int EmployeeId, [FromBody] string MedCertificateNo)
        {
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var employeeDetails = phrmDBContext.Employees.Where(e => e.EmployeeId == EmployeeId).FirstOrDefault();
                employeeDetails.MedCertificationNo = MedCertificateNo;
                employeeDetails.ModifiedBy = currentUser.EmployeeId;
                employeeDetails.ModifiedOn = DateTime.Now;
                phrmDBContext.Entry(employeeDetails).Property(x => x.MedCertificationNo).IsModified = true;
                phrmDBContext.Entry(employeeDetails).Property(x => x.ModifiedBy).IsModified = true;
                phrmDBContext.Entry(employeeDetails).Property(x => x.ModifiedOn).IsModified = true;
                phrmDBContext.SaveChanges();
                responseData.Results = employeeDetails;
                responseData.Status = "OK";
            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpPost("AddPriceCategory")]
        public IActionResult AddPriceCategory([FromBody] PHRM_MAP_MstItemsPriceCategory category)
        {
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var isExists = phrmDBContext.PHRM_MAP_MstItemsPriceCategories.Any(p => p.ItemId == category.ItemId && p.PriceCategoryId == category.PriceCategoryId);
                if (!isExists)
                {
                    category.CreatedOn = DateTime.Now;
                    category.CreatedBy = currentUser.EmployeeId;
                    phrmDBContext.PHRM_MAP_MstItemsPriceCategories.Add(category);
                    phrmDBContext.SaveChanges();
                    responseData.Status = "OK";
                    responseData.Results = category;
                }
                else
                {
                    responseData.Status = "Failed";
                    responseData.ErrorMessage = "Price Category for this Item already exists";
                }

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        [HttpPut("UpdatePriceCategory")]
        public IActionResult UpdatePriceCategory([FromBody] PHRM_MAP_MstItemsPriceCategory category)
        {
            var currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                var priceCategory = phrmDBContext.PHRM_MAP_MstItemsPriceCategories.Where(p => p.PriceCategoryMapId == category.PriceCategoryMapId).SingleOrDefault();
                if (priceCategory != null)
                {
                    priceCategory.IsActive = category.IsActive;
                    priceCategory.DiscountApplicable = category.DiscountApplicable;
                    priceCategory.Price = category.Price;
                    priceCategory.Discount = category.Discount;
                    priceCategory.ItemLegalCode = category.ItemLegalCode;
                    priceCategory.ItemLegalName = category.ItemLegalName;
                    priceCategory.ModifiedOn = DateTime.Now;
                    priceCategory.ModifiedBy = currentUser.EmployeeId;
                    phrmDBContext.SaveChanges();
                    responseData.Status = "OK";
                    responseData.Results = priceCategory;
                }

            }
            catch (Exception ex)
            {
                responseData.Status = "Failed";
                responseData.ErrorMessage = ex.Message + " exception details:" + ex.ToString();
            }
            return Ok(responseData);
        }

        #region Get All Pharmacy Related Stores
        [HttpGet("GetAllPharmacyStore")]
        public IActionResult GetAllPharmacyStore()
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            var stores = phrmDBContext.PHRMStore.Where(s => (s.Category == ENUM_StoreCategory.Store && s.SubCategory.ToLower() == ENUM_StoreCategory.Pharmacy.ToLower()) || s.Category == ENUM_StoreCategory.Dispensary && s.IsActive == true).Select(s => new
            {
                StoreId = s.StoreId,
                StoreName = s.Name
            }).ToList();
            responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
            responseData.Results = stores;
            return Ok(responseData);
        }
        #endregion Get All Pharmacy Related Stores


        [HttpPost()]
        [Route("~/api/Pharmacy/PostSubStoreDispatch")]
        public IActionResult PostSubStoreDispatch([FromBody] IList<PostStoreDispatchViewModel> dispatchItems)
        {
            var responseData = new DanpheHTTPResponse<object>();
            if (dispatchItems != null && dispatchItems.Count > 0)
            {
                var phrmdbcontext = new PharmacyDbContext(connString);
                RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
                var dispatchId = phrmdbcontext.PostSubStoreDispatch(dispatchItems, currentUser);

                responseData.Status = ENUM_DanpheHttpResponseText.OK;
                responseData.Results = dispatchId;
            }
            else
            {
                responseData.ErrorMessage = "Dispatch Items is null";
                responseData.Status = ENUM_DanpheHttpResponseText.Failed;
            }
            return Ok(responseData);
        }

        #region Get RackNo From ItemId and StoreId From ItemRack Table
        [HttpGet("GetRackNoByItemIdAndStoreId/{ItemId}/{StoreId}")]
        public IActionResult GetRackNoByItemId(int ItemId, int? StoreId)
        {
            var phrmDBContext = new PharmacyDbContext(connString);
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            int? NewStoreId = StoreId != null ? StoreId : phrmDBContext.PHRMStore.Where(s => s.SubCategory == ENUM_StoreCategory.Pharmacy).Select(s => s.StoreId).FirstOrDefault(); //For Pharmacy MainStoreId
            var RackNo = (from rackItem in phrmDBContext.PHRMRackItem.Where(ri => ri.ItemId == ItemId && ri.StoreId == NewStoreId)
                          join rack in phrmDBContext.PHRMRack on rackItem.RackId equals rack.RackId
                          select rack.RackNo).FirstOrDefault();
            responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
            responseData.Results = RackNo;
            return Ok(responseData);
        }
        #endregion  Get RackNo From ItemId and StoreId From ItemRack Table

        [HttpGet]
        [Route("~/api/Pharmacy/GetFiscalYearList")]
        public async Task<IActionResult> GetFiscalYearList()
        {
            var phrmdbcontext = new PharmacyDbContext(connString);
            var responseData = new DanpheHTTPResponse<object>();
            try
            {
                var fiscalYearList = await phrmdbcontext.PharmacyFiscalYears.Where(fy => fy.IsActive == true).ToListAsync();
                responseData.Status = ENUM_DanpheHttpResponseText.OK;
                responseData.Results = fiscalYearList;
            }
            catch (Exception ex)
            {
                responseData.ErrorMessage = ex.ToString();
                responseData.Status = ENUM_DanpheHttpResponseText.Failed;
            }
            return Ok(responseData);
        }

        [HttpGet]
        [Route("GetMultipleInvoiceItemsToReturn")]
        public async Task<IActionResult> GetMultipleInvoiceItemsToReturn(string HospitalNo, string PaymentMode, DateTime FromDate, DateTime ToDate, int StoreId, int? PriceCategoryId)
        {
            DanpheHTTPResponse<object> responseData = new DanpheHTTPResponse<object>();
            try
            {
                List<SqlParameter> paramList = new List<SqlParameter>() {
                        new SqlParameter("@HospitalNo", HospitalNo),
                        new SqlParameter("@PaymentMode", PaymentMode),
                        new SqlParameter("@FromDate", FromDate),
                        new SqlParameter("@ToDate", ToDate),
                        new SqlParameter("@StoreId", StoreId),
                        new SqlParameter("@PriceCategoryId", PriceCategoryId),
                    };
                DataSet invoiceItemDetailsToReturn = await Task.Run(() => DALFunctions.GetDatasetFromStoredProc("SP_PHRM_InvoiceItemDetailsToReturnByHospitalNo", paramList, _pharmacyDbContext));
                DataTable patientInfo = invoiceItemDetailsToReturn.Tables[0];
                DataTable priceCategoryDetail = invoiceItemDetailsToReturn.Tables[1];
                DataTable invoiceItemDetails = invoiceItemDetailsToReturn.Tables[2];

                var InvoiceItemToBeReturnDetails = new
                {
                    PatientInfo = PatientViewModel.MapDataTableToSingleObject(patientInfo),
                    PriceCategoryInfo = MapDataTableToSinglePriceCategoryModel(priceCategoryDetail),
                    InvoiceItems = invoiceItemDetails
                };
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.OK;
                responseData.Results = InvoiceItemToBeReturnDetails;
            }
            catch (Exception ex)
            {
                responseData.Status = ENUM_Danphe_HTTP_ResponseStatus.Failed;
                responseData.ErrorMessage = ex.ToString();
            }

            return Ok(responseData);
        }
        [HttpPost]
        [Route("ReturnMultipleInvoiceItemsFromCustomer")]
        public IActionResult ReturnMultipleInvoiceItemsFromCustomer([FromBody] PHRMInvoiceReturnModel invoiceDetailToBeRetrun)
        {
            RbacUser currentUser = HttpContext.Session.Get<RbacUser>("currentuser");
            Func<object> func = () => PharmacyBL.PostReturnMultipleInvoiceFromCustomer(_pharmacyDbContext, invoiceDetailToBeRetrun, currentUser, _billingDbContext);
            return InvokeHttpPostFunction<object>(func);

        }

        [HttpGet]
        [Route("ReceiptDetailToPrintMultipleInvoiceReturn")]
        public IActionResult ReceiptDetailToPrintMultipleInvoiceItemReturn(int InvoiceReturnId)
        {
            Func<object> func = () => GetMultipleInvoiceReturnDetailToPrint(InvoiceReturnId);
            return InvokeHttpGetFunction<object>(func);
        }

        private object GetMultipleInvoiceReturnDetailToPrint(int InvoiceReturnId)
        {
            var invoiceItemReturnDetailToPrint = (from invitmret in _pharmacyDbContext.PHRMInvoiceReturnItemsModel.Where(invitmret => invitmret.InvoiceReturnId == InvoiceReturnId)
                                                  join itm in _pharmacyDbContext.PHRMItemMaster on invitmret.ItemId equals itm.ItemId
                                                  join gn in _pharmacyDbContext.PHRMGenericModel on itm.GenericId equals gn.GenericId
                                                  join stktxn in _pharmacyDbContext.StockTransactions.Where(s => s.TransactionType == ENUM_PHRM_StockTransactionType.SaleReturnedItem) on invitmret.InvoiceReturnItemId equals stktxn.ReferenceNo
                                                  select new
                                                  {
                                                      InvoiceReturnId = invitmret.InvoiceReturnId,
                                                      ItemName = itm.ItemName,
                                                      GenericName = gn.GenericName,
                                                      BatchNo = invitmret.BatchNo,
                                                      ExpiryDate = stktxn.ExpiryDate,
                                                      ReturnedQty = invitmret.ReturnedQty,
                                                      SalePrice = invitmret.SalePrice,
                                                      Price = invitmret.Price,
                                                      SubTotal = invitmret.SubTotal,
                                                      DiscountPercentagae = invitmret.DiscountPercentage,
                                                      TotalDisAmt = invitmret.DiscountAmount,
                                                      VATPercantage = invitmret.VATPercentage,
                                                      TotalAmount = invitmret.TotalAmount,
                                                      RackNo = (from rackItem in _pharmacyDbContext.PHRMRackItem.Where(ri => ri.ItemId == itm.ItemId && ri.StoreId == invitmret.StoreId)
                                                                join rack in _pharmacyDbContext.PHRMRack on rackItem.RackId equals rack.RackId
                                                                select rack.RackNo).FirstOrDefault()
                                                  }).ToList();

            var invoiceReturnDetailToPrint = (from inv in _pharmacyDbContext.PHRMInvoiceReturnModel.Where(inv => inv.InvoiceReturnId == InvoiceReturnId)
                                              select new
                                              {
                                                  CRNNo = inv.CreditNoteId,
                                                  SubTotal = inv.SubTotal,
                                                  DiscountAmount = inv.DiscountAmount,
                                                  TaxableAmount = inv.SubTotal - inv.DiscountAmount,
                                                  NonTaxableAmount = inv.SubTotal,
                                                  VATAmount = inv.VATAmount,
                                                  TotalAmount = inv.TotalAmount,
                                                  PaidAmount = inv.PaidAmount,
                                                  CashAmount = inv.ReturnCashAmount,
                                                  CreditAmount = inv.ReturnCreditAmount,
                                                  IsReturned = true,
                                                  billingUser = _pharmacyDbContext.Users.Where(e => e.EmployeeId == inv.CreatedBy).FirstOrDefault().UserName,
                                                  CurrentFinYear = _pharmacyDbContext.PharmacyFiscalYears.Where(fy => fy.FiscalYearId == inv.FiscalYearId).FirstOrDefault().FiscalYearName,
                                                  Remarks = inv.Remarks,
                                                  ReceiptDate = inv.CreatedOn,
                                                  ReferenceInvoiceNo = inv.ReferenceInvoiceNo,
                                                  Patient = _pharmacyDbContext.PHRMPatient.Join(_pharmacyDbContext.CountrySubDivision,
                                                  pat => pat.CountrySubDivisionId,
                                                  sub => sub.CountrySubDivisionId,
                                                  (pat, sub) => new
                                                  {
                                                      PatientId = pat.PatientId,
                                                      ShortName = pat.ShortName,
                                                      Address = pat.Address,
                                                      DateOfBirth = pat.DateOfBirth,
                                                      Gender = pat.Gender,
                                                      PhoneNo = pat.PhoneNumber,
                                                      PANNumber = pat.PANNumber,
                                                      PatientCode = pat.PatientCode,
                                                      CountrySubDivisionName = sub.CountrySubDivisionName
                                                  }).Where(p => p.PatientId == inv.PatientId).FirstOrDefault()
                                              }).FirstOrDefault();
            var multipleInvoiceItemReturnData = new
            {
                ReturnItemDetail = invoiceItemReturnDetailToPrint,
                ReturnDetail = invoiceReturnDetailToPrint
            };

            return multipleInvoiceItemReturnData;
        }

        private static object MapDataTableToSinglePriceCategoryModel(DataTable data)
        {
            PriceCategoryModel priceCategory = new PriceCategoryModel();
            if (data != null)
            {
                string strData = JsonConvert.SerializeObject(data);
                List<PriceCategoryModel> priceCategoryData = JsonConvert.DeserializeObject<List<PriceCategoryModel>>(strData);
                if (priceCategoryData != null && priceCategoryData.Count > 0)
                {
                    priceCategory = priceCategoryData.First();
                }
            }
            return priceCategory;
        }
    }

}
