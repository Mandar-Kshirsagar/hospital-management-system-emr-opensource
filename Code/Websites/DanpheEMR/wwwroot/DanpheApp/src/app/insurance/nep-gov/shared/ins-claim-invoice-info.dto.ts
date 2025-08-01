export class INSClaimableBillingInvoiceInfo_DTO {
    InvoiceId: number = null;
    InvoiceNumber: string = null;
    InvoiceNumFormatted: string = null;
    FiscalYearId: number = 0;
    TotalAmount: number = 0;
    BillingTransactionId: number = 0;
    InvoiceDate: string = null;
    PaymentMode: string = '';
    PackageName: string = '';
    SubTotal: number = 0;
    DiscountAmount: number = 0;
    TaxableAmount: number = 0;
    TaxTotal: number = 0;
    IsCoPayment: boolean = false;
    ReceivedAmount: number = 0;
    DepositUsed: number = 0;
    DepositBalance: number = 0;
    Tender: number = 0;
    Change: number = 0;
    Remarks: string = '';
    OrganizationName: string = '';
    LabTypeName: string = '';
    PrintCount: number = 0;
    UserName: string = '';
    ConsultingDoctor: string = '';
    ItemsRequestingDoctors: string = '';
    TransactionType: string = '';
    IsInsuranceBilling: boolean = false;
    PackageId: number = 0;
    PaymentDetails: string = '';
    OtherCurrencyDetail: string = '';
}
export class INSClaimablePharmacyInvoiceInfo_DTO {
    InvoiceId: number = null;
    InvoiceNumber: string = null;
    CurrentFinYear: string = null;
    InvoiceNumFormatted: string = '';
    TotalAmount: number = 0;
    InvoiceDate: string = null;
    PaymentMode: string = '';
    SubTotal: number = 0;
    DiscountAmount: number = 0;
    IsCoPayment: boolean = false;
    ReceivedAmount: number = 0;
    Tender: number = 0;
    Change: number = 0;
    Remarks: string = '';
    OrganizationName: string = '';
    PrintCount: number = 0;
    UserName: string = '';
    VisitType: string = '';
    ProviderName: string = '';
    ProviderNMCNumber: string = '';
    StoreId: number = 0;
    StoreName: string = '';
    VATAmount: number = 0;
    PaymentDetails: string = '';
    CreditAmount: number = 0;
}