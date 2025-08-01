import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { response } from '../../core/response.model';
import { DanpheHTTPResponse } from '../../shared/common-models';
import { AccBillingLedgerMapping_DTO } from '../settings/shared/dto/acc-billing-ledger-mapping.dto';
import { VoucherVerify_DTO } from '../transactions/shared/DTOs/voucher-verify.DTO';
import { AddAccountTenantPost_DTO } from './DTOs/account-tenant-section-map.dto';
import { SuspenseAccountTransaction_DTO } from './DTOs/suspense-account-transaction.dto';
@Injectable()
export class AccountingDLService {
    public options = {
        headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' })
    };
    public optionJson = { headers: new HttpHeaders({ 'Content-Type': 'application/json' }) };
    constructor(public http: HttpClient) {
    }
    //get information of current accounts.
    public GetAccountInfoById(accountId: number) {
        try {
            return this.http.get<any>("/api/Accounting" + "?accountId=" + accountId);
        } catch (ex) {
            throw ex;
        }
    }

    public GetTransactionType() {
        try {
            return this.http.get<any>("/api/Accounting?reqType=VoucherType");
        } catch (ex) {
            throw ex;
        }
    }
    public GetLedgerList() {
        try {
            return this.http.get<any>("/api/Accounting/Ledgers");
        } catch (ex) {
            throw ex;
        }
    }
    public GetLedgerFromVoucherId(voucherId: number) {
        try {
            return this.http.get<any>("/api/Accounting?reqType=ledgersFrmVoucherId&voucherId=" + voucherId);
        } catch (ex) {
            throw ex;
        }
    }
    public GetAccountClosureData() {
        return this.http.get<any>("/api/Accounting?reqType=account-closure");
    }
    public GetActiveFiscalYear() {
        return this.http.get<any>("/api/Accounting?reqType=ActiveFiscalYears");
    }
    public GetVoucher() {
        try {
            return this.http.get<any>("/api/Accounting/Vouchers");
        } catch (ex) {
            throw ex;
        }
    }
    public GetVoucherHead() {
        try {
            return this.http.get<any>("/api/Accounting/VoucherHeads");
        } catch (ex) {
            throw ex;
        }
    }
    public GetLedgerItem(ledgerId: number) {
        try {
            return this.http.get<any>("/api/Accounting?reqType=ledger-items&ledgerId=" + ledgerId);
        } catch (ex) {
            throw ex;
        }
    }
    public GetItemList() {
        try {
            return this.http.get<response>('/api/Accounting?reqType=ItemList');
        } catch (ex) {
            throw ex;
        }
    }

    public GetLedgerGroup() {
        return this.http.get<any>("/api/AccountingSettings/LedgerGroups");
    }

    public GetFiscalYearList() {
        try {
            return this.http.get<response>('/api/Accounting/FiscalYears');
        } catch (ex) {
            throw ex;
        }
    }
    public GetTransaction(transactionId: number) {
        try {
            return this.http.get<response>('/api/Accounting/Transactions?transactionId=' + transactionId);
        } catch (ex) {
            throw ex;
        }
    }
    public GetTransactionbyVoucher(vouchernumber: string, secId, fsYearId, HospitalId) {
        try {
            return this.http.get<response>('/api/Accounting/TransactionByVoucherNumber?voucherNumber=' + vouchernumber + "&sectionId=" + secId + "&FiscalYearId=" + fsYearId + "&HospitalId=" + HospitalId);
        } catch (ex) {
            throw ex;
        }
    }
    ///get Voucher detail for manual edit 
    public GetVoucherforedit(vouchernumber: string, secId, FsYId) {
        try {
            return this.http.get<response>('/api/Accounting/Voucher?voucherNumber=' + vouchernumber + "&sectionId=" + secId + "&FiscalYearId=" + FsYId);
        } catch (ex) {
            throw ex;
        }
    }
    public CheckTransaction(transactionId: number, voucherId: number) {
        try {
            return this.http.get<response>('/api/Accounting/RefrenceTransactionId?voucherNumber=' + transactionId + '&voucherId=' + voucherId);
        } catch (ex) {
            throw ex;
        }
    }
    public GetCostCenterList() {
        try {
            return this.http.get<response>('/api/Accounting/CostCenterItems');
        } catch (ex) {
            throw ex;
        }
    }
    public GetInventoryItemsForTransferToACC(selectedDate, fiscalyearId) {
        try {
            return this.http.get<any>("/api/Accounting/InventoryToAccounting?SelectedDate=" + selectedDate + "&FiscalYearId=" + fiscalyearId);
        } catch (exception) {
            throw exception
        };
    }
    //get all bil txn items from billing for transfer to accounting
    public GetBilTxnItemsForTransferToACC(selectedDate, fiscalyearId) {
        try {
            // return this.http.get<any>("/api/Accounting?reqType=billing-to-accounting&FromDate=" + frmDt + "&ToDate=" + toDt);
            return this.http.get<any>("/api/Accounting/BillingToAccounting?SelectedDate=" + selectedDate + "&FiscalYearId=" + fiscalyearId);
        } catch (exception) {
            throw exception
        };
    }

    //get all pharmacy txn item for transfer to accounting
    public GetPharmItemsForTransferToACC(selectedDate, fiscalyearId) {
        try {
            return this.http.get<any>("/api/Accounting/PharmacyToAccounting?SelectedDate=" + selectedDate + "&FiscalYearId=" + fiscalyearId);
        } catch (exception) {
            throw exception
        };
    }
    //get all incentive txn item for transfer to accounting
    public GetIncentivesForTransferToACC(selectedDate, fiscalyearId) {
        try {
            return this.http.get<any>("/api/Accounting/IncentiveToAccounting?SelectedDate=" + selectedDate + "&FiscalYearId=" + fiscalyearId);
        } catch (exception) {
            throw exception
        };
    }
    //get ledger mapping details for  map with phrm supplier or inventory vendor
    GetLedgerMappingDetails() {
        try {
            return this.http.get<any>("/api/Accounting/LedgerMapping", this.options);
        } catch (ex) {
            throw ex
        }
    }
    LoadTxnDates(fromdate, todate, sectionId) {
        try {
            return this.http.get<any>("/api/Accounting/TransactionDates?FromDate=" + fromdate + "&ToDate=" + todate + "&sectionId=" + sectionId, this.options);
        } catch (ex) {
            throw ex
        }
    }
    public GetAllActiveAccTenants() {
        try {
            return this.http.get<response>('/api/Accounting/Hospitals');
        } catch (ex) {
            throw ex;
        }
    }
    //START: GET Reporting DATA

    //GET:this function get all transfer rule with details
    public GetACCTransferRule() {
        try {
            return this.http.get<any>("/api/Accounting?reqType=accTransferRule");
        } catch (exception) {
            throw exception;
        }
    }
    //this method for get provisional Voucher number for curernt new created voucher
    public GettempVoucherNumber(voucherId: number, sectionId, transactiondate) {
        try {
            return this.http.get<any>("/api/Accounting/ProvisionalVoucherNumber?voucherId=" + voucherId + "&sectionId=" + sectionId + "&transactiondate=" + transactiondate);
        }
        catch (exception) {
            throw exception;
        }
    }
    //Get Provisional Ledger using ledger type and reference id
    GetProvisionalLedger(referenceId, ledgerType) {
        try {
            return this.http.get<any>("/api/Accounting/ProvisionalLedgerDetail?ReferenceId=" + referenceId + "&LedgerType=" + ledgerType);
        }
        catch (ex) {
            throw ex;
        }
    }
    //END: GET Reporting DATA

    //get inventory vendors
    GetInvVendorList() {
        try {
            return this.http.get<any>("/api/Accounting/InventoryVendorLedgers", this.options);
        } catch (ex) {
            throw ex
        }
    }
    //get pharmacy supplier
    GetPharmacySupplier() {
        try {
            return this.http.get<any>("/api/Accounting/PharmacySupplierLedgers", this.options);
        } catch (ex) {
            throw ex
        }
    }
    //get good receipt list 
    GetGRList(vendorId: number, sectionId: number, number: any, date: string, goodReceiptNo: number) {
        try {
            return this.http.get<response>(`/api/Accounting/GoodReceipts?vendorId=${vendorId}&sectionId=${sectionId}&invoiceNumber=${number}&transactiondate=${date}&goodReceiptNo=${goodReceiptNo}`);
        } catch (ex) {
            throw ex;
        }
    }

    //START: POST
    public PostTransaction(TransactionObjString: string) {
        let data = TransactionObjString;
        return this.http.post<any>("/api/Accounting/Transaction", data);
    }

    //post TxnList to accounting Transaction table
    PostTxnListToACC(txnListObjString: string) {
        try {
            let data = txnListObjString;
            return this.http.post<any>("/api/Accounting/Transactions", data);
        } catch (ex) {
            throw (ex);
        }
    }

    PostLedgers(ledgList: string) {
        try {
            let data = ledgList;
            return this.http.post<any>("/api/Accounting/Ledgers", data);
        } catch (ex) {
            throw (ex);
        }
    }
    //create single ledger 
    AddLedger(ledgList: string) {
        try {
            let data = ledgList;
            return this.http.post<any>("/api/Accounting/Ledger", data);
        } catch (ex) {
            throw (ex);
        }
    }

    //END: POST

    public PostAccountClosure(data: string) {
        return this.http.post<any>("/api/Accounting/AccountClose", data);
    }

    public PostAccountingInvoiceData(data: string) {
        return this.http.post<any>("/api/Accounting?reqType=post-accounting-invoice-data", data);
    }

    public UndoTransaction(data: string) {
        return this.http.post<any>("/api/Accounting/ReverseTransaction", data);
    }

    //START: PUT

    public PutTransaction(TransactionObjString: string) {
        let data = TransactionObjString;
        return this.http.put<any>("/api/Accounting/Transaction", data);
    }

    public ActivateAccountingTenant(hospitalId: number) {
        return this.http.put<any>("/api/Security/ActivateAccountingHospital?hospitalId=" + hospitalId, this.options);
    }
    //END: PUT

    //post payment to accounting Payment table
    public PostPayment(makePayment) {
        return this.http.post<any>("/api/Accounting/Payment", makePayment, this.optionJson);
    }

    public VerifyVoucher(voucherData: VoucherVerify_DTO) {
        return this.http.put<response>(`/api/Accounting/VerifyVoucher`, voucherData, this.optionJson);
    }
    public SaveBillingLedgerMapping(data: AccBillingLedgerMapping_DTO) {
        return this.http.post<response>("/api/AccLedgerMapping/MapBillingIncomeLedger", data, this.optionJson);
    }
    public UpdateBillLedgerMapping(data: AccBillingLedgerMapping_DTO) {
        return this.http.put<response>(`/api/AccLedgerMapping/MapBillingLedger`, data, this.optionJson);
    }

    public GetSuspenaseAccountReconciliationDetail(BankLedgerId: number, SuspenseAccountLedgerId: number) {
        return this.http.get<response>(`/api/Accounting/SuspenseAccountReconciliationDetail?BankLedgerId=${BankLedgerId}&SuspenseAccountLedgerId=${SuspenseAccountLedgerId}`, this.options);
    }

    public PostSuspenseAccReconciliationTransaction(data: SuspenseAccountTransaction_DTO) {
        return this.http.post<response>(`/api/Accounting/SuspenseAcc/Reconcile`, data, this.optionJson);
    }

    public GetTenantSectionMap() {
        return this.http.get<response>(`/api/Accounting/AccountTenantSectionMap`, this.options);
    }

    public AddNetAccountTenant(object: AddAccountTenantPost_DTO) {
        return this.http.post<response>(`/api/Accounting/AccountTenant`, object, this.optionJson);
    }

    public DeactivateAccountTenant() {
        return this.http.put<any>("/api/Security/DeactivateActiveAccountTenant", this.options);
    }
    public GetSupplierList() {
        return this.http.get<DanpheHTTPResponse>("/api/Accounting/GetSupplierList", this.optionJson)
    }
}
